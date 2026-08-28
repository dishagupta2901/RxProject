<#
.SYNOPSIS
  Read-only verifier for RxFlow's AGENTS.md / .codex/skills governance hierarchy.

.DESCRIPTION
  This repo does not use a nested-directory AGENTS.md tree (see subagents.md,
  "Skills update (2026-08-28)"): the root Agents.md carries universal/durable
  rules, and .codex/skills/*/SKILL.md carries component-scoped guidance, keyed
  by an explicit "Use for a change that touches <paths>" line rather than by
  filesystem location. This script's "effective chain" for a directory is
  therefore: Agents.md + subagents.md + every SKILL.md whose declared paths
  cover that directory.

  It does NOT invoke a real Codex CLI (none is installed in this environment).
  Anything that would require live Codex instruction discovery is printed as
  UNVERIFIED rather than asserted true. This script never modifies any file.

.NOTES
  Run from anywhere; paths are resolved relative to the repo root (two levels
  above this script: scripts/agents/verify-hierarchy.ps1 -> repo root).
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $repoRoot

$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Test-Check {
    param([string]$Name, [bool]$Passed, [string]$Detail = '')
    $status = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1}" -f $status, $Name)
    if ($Detail) { Write-Host ("       {0}" -f $Detail) }
    if (-not $Passed) { $failures.Add($Name) }
}

Write-Host "=== RxFlow AGENTS.md hierarchy verifier ==="
Write-Host "Repo root: $repoRoot"
Write-Host ""

# ---------------------------------------------------------------------------
# 1. Required files exist only at justified scopes
# ---------------------------------------------------------------------------
Write-Host "--- 1. File existence ---"
$rootAgents = Join-Path $repoRoot 'Agents.md'
Test-Check 'Root Agents.md exists' (Test-Path $rootAgents)
Test-Check 'subagents.md exists' (Test-Path (Join-Path $repoRoot 'subagents.md'))

$skillDir = Join-Path $repoRoot '.codex\skills'
$skillFiles = Get-ChildItem -Path $skillDir -Recurse -Filter 'SKILL.md' -ErrorAction SilentlyContinue
Test-Check "At least one SKILL.md under .codex/skills" ($skillFiles.Count -gt 0) "$($skillFiles.Count) found"

# No fictional service directories invented (order_api/routing/pricing/analytics as top-level dirs)
$inventedNames = @('order_api', 'routing', 'pricing', 'analytics')
$invented = @()
foreach ($n in $inventedNames) {
    $hits = Get-ChildItem -Path $repoRoot -Recurse -Directory -Filter $n -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\node_modules\\|\\bin\\|\\obj\\|\\.git\\' }
    $invented += $hits
}
Test-Check 'No fictional order_api/routing/pricing/analytics directories were created' ($invented.Count -eq 0) `
    ($(if ($invented.Count -gt 0) { ($invented.FullName -join '; ') } else { 'none present, as expected' }))

# No Java anywhere (repo is .NET-only; a nested Java AGENTS.md would be unjustified)
$javaFiles = Get-ChildItem -Path $repoRoot -Recurse -Include '*.java', 'pom.xml', 'build.gradle*' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\node_modules\\|\\.git\\' }
Test-Check 'No Java sources/build files present (confirms no Java-specific AGENTS.md is warranted)' ($javaFiles.Count -eq 0)

Write-Host ""

# ---------------------------------------------------------------------------
# 2/3. Effective chain per representative directory (closest-scope model)
# ---------------------------------------------------------------------------
Write-Host "--- 2/3. Effective instruction chain by representative directory ---"

# path (relative to repo root) -> skill file name(s) whose "Use for" scope covers it, or $null for a documented gap
$componentMap = [ordered]@{
    '.'                      = @()
    'src\RxFlow.Api'         = @('rxflow-api-application-engineer')
    'src\RxFlow.Application' = @('rxflow-api-application-engineer')
    'src\RxFlow.Domain'      = @('rxflow-domain-modeller')
    'src\RxFlow.Infrastructure' = @('rxflow-infrastructure-engineer')
    'database\migrations'    = @('rxflow-infrastructure-engineer')
    'src\RxFlow.Workers'     = @('rxflow-worker-reliability-engineer')
    'src\RxFlow.Reporting'   = @()   # documented gap, see subagents.md
    'frontend'               = @('rxflow-frontend-contract-engineer')
    'deploy'                 = @('rxflow-infrastructure-engineer')
    'tests'                  = @('rxflow-verification-engineer')
}

$chainResults = @{}
foreach ($rel in $componentMap.Keys) {
    $abs = Join-Path $repoRoot $rel
    $exists = Test-Path $abs
    $chain = @('Agents.md', 'subagents.md') + ($componentMap[$rel] | ForEach-Object { ".codex/skills/$_/SKILL.md" })
    $chainResults[$rel] = $chain
    $label = if ($rel -eq '.') { '<repo root>' } else { $rel }
    $note = if ($componentMap[$rel].Count -eq 0 -and $rel -ne '.') { ' (no component skill -- root rules only, documented gap)' } else { '' }
    Write-Host ("  {0}{1}" -f $label, $note)
    Write-Host ("    exists: {0}" -f $exists)
    Write-Host ("    effective chain (discovery order): {0}" -f ($chain -join ' -> '))
    if (-not $exists -and $rel -ne '.') { $warnings.Add("Directory '$rel' referenced in the map does not exist") }
}
Write-Host ""
Write-Host "  NOTE: this is the documented-algorithm chain (root + closest-scope skill)," -ForegroundColor Yellow
Write-Host "  not a live Codex instruction-summary dump. No Codex CLI is installed in this" -ForegroundColor Yellow
Write-Host "  environment, so live discovery is UNVERIFIED, per the task's own fallback rule." -ForegroundColor Yellow
Write-Host ""

# ---------------------------------------------------------------------------
# 4. Durable-rule coverage: each of the 8 rule-topics appears somewhere in
#    the effective chain for the directory it must cover.
# ---------------------------------------------------------------------------
Write-Host "--- 4. Durable rule coverage (grep-based, root Agents.md) ---"
$agentsText = Get-Content $rootAgents -Raw
$ruleChecks = [ordered]@{
    '1 idempotency/duplicate-submission'      = 'duplicate submission|Order-submission idempotency'
    '2 database migration upgrade/rollback'   = 'rollback-or-forward-fix|Database migrations'
    '3 public API compatibility review'       = 'compatibility review|Public API compatibility'
    '4 external call timeout/retry'           = 'timeout and retry policy|New external calls'
    '5 sensitive data never logged'           = 'never be logged|Sensitive data must never'
    '6 infra security validation'             = 'security validation|Infrastructure changes require'
    '7 defect fix regression test'            = 'fails before the fix and passes after|Defect fixes require'
    '8 final report evidence'                 = 'exact command, working directory, exit code|Final reports show evidence'
}
foreach ($k in $ruleChecks.Keys) {
    $pattern = $ruleChecks[$k]
    Test-Check "Rule present in Agents.md: $k" ($agentsText -match $pattern)
}
Write-Host ""

# ---------------------------------------------------------------------------
# 5. No nested skill file weakens/re-narrows the universal rules
#    (heuristic: none of the edited skill files contain a contradictory
#    "may log" / "no idempotency required" style override)
# ---------------------------------------------------------------------------
Write-Host "--- 5. No nested weakening (heuristic) ---"
$forbiddenPatterns = @('may be logged', 'no idempotency required', 'skip regression test', 'no compatibility review needed')
$weakened = $false
foreach ($f in $skillFiles) {
    $t = Get-Content $f.FullName -Raw
    foreach ($p in $forbiddenPatterns) {
        if ($t -match [regex]::Escape($p)) {
            $weakened = $true
            $warnings.Add("Possible rule-weakening phrase '$p' in $($f.FullName)")
        }
    }
}
Test-Check 'No skill file contains a known rule-weakening phrase' (-not $weakened)
Write-Host ""

# ---------------------------------------------------------------------------
# 6. Links referenced from Agents.md resolve
# ---------------------------------------------------------------------------
Write-Host "--- 6. Link resolution (Agents.md references) ---"
$refCandidates = [regex]::Matches($agentsText, '`([A-Za-z0-9_.\\/-]+\.(md|ps1|sh))`') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
foreach ($ref in $refCandidates) {
    $refPath = Join-Path $repoRoot ($ref -replace '/', '\')
    Test-Check "Reference resolves: $ref" (Test-Path $refPath)
}
Write-Host ""

# ---------------------------------------------------------------------------
# 7. No obvious secret/patient-identifier patterns in instruction files
# ---------------------------------------------------------------------------
Write-Host "--- 7. Secret/PII pattern scan (instruction files only) ---"
$instructionFiles = @($rootAgents, (Join-Path $repoRoot 'subagents.md')) + ($skillFiles.FullName)
$secretPatterns = @(
    'BEGIN (RSA|EC|OPENSSH) PRIVATE KEY',
    'AKIA[0-9A-Z]{16}',
    'xox[baprs]-[0-9A-Za-z-]+',
    'patient[_-]?id\s*[:=]\s*\d',
    'ssn\s*[:=]\s*\d{3}-?\d{2}-?\d{4}'
)
$secretHit = $false
foreach ($f in $instructionFiles) {
    $t = Get-Content $f -Raw
    foreach ($p in $secretPatterns) {
        if ($t -match $p) { $secretHit = $true; $warnings.Add("Pattern '$p' matched in $f") }
    }
}
Test-Check 'No secret/patient-identifier pattern found in instruction files' (-not $secretHit)
Write-Host ""

# ---------------------------------------------------------------------------
# 8. Instruction size budget (informational — no configured Codex byte limit
#    could be located in this environment, so this is a size report, not a
#    pass/fail against a verified vendor limit)
# ---------------------------------------------------------------------------
Write-Host "--- 8. Instruction size (informational) ---"
foreach ($rel in $chainResults.Keys) {
    $chain = $chainResults[$rel]
    $bytes = 0
    foreach ($c in $chain) {
        $p = Join-Path $repoRoot $c
        if (Test-Path $p) { $bytes += (Get-Item $p).Length }
    }
    $label = if ($rel -eq '.') { '<repo root>' } else { $rel }
    Write-Host ("  {0}: {1} bytes across {2} file(s)" -f $label, $bytes, $chain.Count)
}
Write-Host "  No installed-Codex byte-limit configuration was found in this environment;" -ForegroundColor Yellow
Write-Host "  treat any specific numeric limit as UNVERIFIED until checked against a real install." -ForegroundColor Yellow
Write-Host ""

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host "=== Summary ==="
Write-Host ("Failures: {0}" -f $failures.Count)
foreach ($f in $failures) { Write-Host ("  - {0}" -f $f) -ForegroundColor Red }
Write-Host ("Warnings: {0}" -f $warnings.Count)
foreach ($w in $warnings) { Write-Host ("  - {0}" -f $w) -ForegroundColor Yellow }

if ($failures.Count -gt 0) {
    exit 1
} else {
    exit 0
}
