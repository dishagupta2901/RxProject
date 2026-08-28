<#
.SYNOPSIS
    Creates a disposable, isolated Git worktree bound to a specific commit
    SHA for the rx-remediator profile to operate in.

.DESCRIPTION
    The new worktree is created outside the primary checkout (default:
    under $env:TEMP), never inside it, and never falls back to the primary
    worktree on failure. A marker file is written inside the new worktree so
    cleanup-remediation-worktree.ps1 can prove it created the specific path
    it is about to remove, rather than trusting a path string alone.

.PARAMETER Sha
    Commit SHA (or ref) to bind the worktree to. Defaults to the current
    HEAD of the primary repo.

.PARAMETER TargetPath
    Where to create the isolated worktree. Defaults to a fresh directory
    under $env:TEMP, well outside the primary checkout.
#>
[CmdletBinding()]
param(
    [string]$Sha,
    [string]$TargetPath
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) {
    [Console]::Error.WriteLine("Not inside a git repository; refusing to create a remediation worktree.")
    exit 1
}
$repoRoot = $repoRoot.Trim()

if (-not $Sha) {
    $Sha = (git -C $repoRoot rev-parse HEAD).Trim()
}

# Validate the SHA actually resolves before creating anything.
$resolved = git -C $repoRoot rev-parse --verify "$Sha^{commit}" 2>$null
if (-not $resolved) {
    [Console]::Error.WriteLine("SHA/ref '$Sha' does not resolve to a commit in $repoRoot. Refusing to proceed.")
    exit 1
}
$resolved = $resolved.Trim()

if (-not $TargetPath) {
    $shortSha = $resolved.Substring(0, 12)
    $stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
    $TargetPath = Join-Path $env:TEMP "rxflow-remediation-worktrees\$shortSha-$stamp"
}
$TargetPath = [System.IO.Path]::GetFullPath($TargetPath)

# Refuse to target anything inside the primary worktree, or the primary
# worktree root itself.
$repoRootFull = [System.IO.Path]::GetFullPath($repoRoot)
if ($TargetPath -eq $repoRootFull -or $TargetPath.StartsWith($repoRootFull + [System.IO.Path]::DirectorySeparatorChar)) {
    [Console]::Error.WriteLine("Target path '$TargetPath' is inside the primary worktree ($repoRootFull). Refusing - a remediation worktree must be isolated.")
    exit 1
}
if (Test-Path $TargetPath) {
    [Console]::Error.WriteLine("Target path '$TargetPath' already exists. Refusing to reuse a possibly non-empty directory; pass a fresh -TargetPath.")
    exit 1
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $TargetPath) | Out-Null

Write-Output "Creating isolated worktree at '$TargetPath' bound to commit $resolved ..."
git -C $repoRoot worktree add $TargetPath $resolved
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("git worktree add failed (exit $LASTEXITCODE). No fallback to the primary worktree will occur.")
    exit $LASTEXITCODE
}

$marker = [ordered]@{
    createdBy      = "scripts/codex-profiles/create-remediation-worktree.ps1"
    primaryRepo    = $repoRootFull
    boundSha       = $resolved
    createdAtUtc   = (Get-Date).ToUniversalTime().ToString("o")
    artifactsDir   = (Join-Path $TargetPath ".rxflow-artifacts")
}
($marker | ConvertTo-Json -Depth 4) | Set-Content -Path (Join-Path $TargetPath ".rxflow-remediation-marker.json") -Encoding utf8
New-Item -ItemType Directory -Force -Path $marker.artifactsDir | Out-Null

Write-Output "Isolated worktree ready: $TargetPath"
Write-Output "Bound to commit: $resolved"
Write-Output "Artifact directory: $($marker.artifactsDir)"
Write-Output ""
Write-Output "Next step: cd into '$TargetPath' before invoking 'codex --profile rx-remediator ...' - isolation relies on Codex's default workspace-write sandbox scoping to this working directory."
