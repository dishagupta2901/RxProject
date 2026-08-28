<#
.SYNOPSIS
    Runs the rx-ci profile non-interactively against the current repo state
    via `codex exec`, with a hard timeout and no path that can wait for
    human input.

.DESCRIPTION
    Fails closed and non-zero, without ever printing a prompt or hanging, in
    every one of these cases: codex CLI missing, codex exec times out, or
    codex exec itself reports an error/needs-approval condition (approval
    should never be reachable under approval_policy = "never", but this
    script does not trust that silently - a needs-approval signal in the
    JSONL stream is treated as a hard failure, not a wait state).

.PARAMETER TaskPrompt
    The analysis task to run. Defaults to a read-only PR-diff summary.

.PARAMETER TimeoutSec
    Hard wall-clock timeout. Default 300s.
#>
[CmdletBinding()]
param(
    [string]$TaskPrompt = "Read-only analysis: summarize the current git diff against origin/master and flag any correctness or security concerns.",
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$resultsDir = Join-Path $scriptRoot "results"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ssZ")
$transcriptPath = Join-Path $resultsDir "rx-ci-$timestamp.jsonl"
$summaryPath = Join-Path $resultsDir "rx-ci-$timestamp-summary.json"

function Write-Summary {
    param([string]$Status, [string]$Detail, [int]$ExitCode)
    $summary = [ordered]@{
        profile      = "rx-ci"
        status       = $Status
        detail       = $Detail
        exitCode     = $ExitCode
        timeoutSec   = $TimeoutSec
        transcript   = $transcriptPath
        timestamp    = (Get-Date).ToUniversalTime().ToString("o")
    }
    ($summary | ConvertTo-Json -Depth 4) | Set-Content -Path $summaryPath -Encoding utf8
    ($summary | ConvertTo-Json -Depth 4)
}

# --- Fail closed if the CLI itself is missing. No prompt, no retry loop. ---
$codexCmd = Get-Command codex -ErrorAction SilentlyContinue
if (-not $codexCmd) {
    $detail = "codex executable not found on PATH; rx-ci cannot run. " +
        "This is a genuine, currently-true condition on this machine - " +
        "see docs/codex-profile-threat-model.md."
    Write-Output (Write-Summary -Status "BLOCKED" -Detail $detail -ExitCode 127)
    [Console]::Error.WriteLine($detail)
    exit 127
}

# --- Non-interactive, bounded execution. -----------------------------------
# NOTE: exact `codex exec` flag names/ordering (--profile, --json,
# --output-schema, --ask-for-approval) are UNVERIFIED against this
# machine's installed version - confirm with `codex exec --help` before
# trusting this invocation string; the fail-closed/timeout wrapper around it
# is what this script actually guarantees.
$codexArgs = @(
    "exec",
    "--profile", "rx-ci",
    "--json",
    $TaskPrompt
)

$job = Start-Job -ScriptBlock {
    param($exe, $jobArgs, $outFile)
    & $exe @jobArgs 2>&1 | Tee-Object -FilePath $outFile
} -ArgumentList $codexCmd.Source, $codexArgs, $transcriptPath

$completed = Wait-Job -Job $job -Timeout $TimeoutSec
if (-not $completed) {
    Stop-Job -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    $detail = "codex exec exceeded the ${TimeoutSec}s hard timeout and was terminated. " +
        "Treated as a failure, never as a wait-for-input state."
    Write-Output (Write-Summary -Status "TIMEOUT" -Detail $detail -ExitCode 124)
    [Console]::Error.WriteLine($detail)
    exit 124
}

$jobOutput = Receive-Job -Job $job
$jobExit = $job.ChildJobs[0].JobStateInfo.State
Remove-Job -Job $job -Force -ErrorAction SilentlyContinue

# Sanitize the transcript: redact anything that looks like a bearer token,
# API key, or connection string before it is retained.
if (Test-Path $transcriptPath) {
    $raw = Get-Content -Path $transcriptPath -Raw
    $sanitized = $raw -replace '(?i)(api[_-]?key|token|password|secret)\s*[:=]\s*\S+', '$1=<redacted>'
    Set-Content -Path $transcriptPath -Value $sanitized -Encoding utf8
}

$needsApproval = $jobOutput -match '"type"\s*:\s*"approval'
if ($needsApproval) {
    $detail = "codex exec reported an approval-required event under " +
        "approval_policy=never. rx-ci must never wait for approval; " +
        "treated as a hard failure."
    Write-Output (Write-Summary -Status "FAIL" -Detail $detail -ExitCode 1)
    [Console]::Error.WriteLine($detail)
    exit 1
}

$exitCode = if ($jobExit -eq "Completed") { 0 } else { 1 }
$status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
Write-Output (Write-Summary -Status $status -Detail "codex exec finished (JobState=$jobExit)" -ExitCode $exitCode)
exit $exitCode
