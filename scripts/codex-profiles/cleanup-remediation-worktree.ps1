<#
.SYNOPSIS
    Safely removes a remediation worktree created by
    create-remediation-worktree.ps1, and only that worktree.

.DESCRIPTION
    Before removing anything, this script requires the target path to
    contain the ".rxflow-remediation-marker.json" file written by
    create-remediation-worktree.ps1, requires that marker's recorded
    primaryRepo to match the caller's actual primary repo, and requires the
    target path to not equal (or be an ancestor of) the primary worktree.
    Any of those checks failing aborts with a non-zero exit and touches
    nothing. After removal, it prints `git worktree list` so the caller can
    see the primary and any other user worktrees are unaffected.

.PARAMETER Path
    The isolated worktree path to remove.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) {
    [Console]::Error.WriteLine("Not inside a git repository; refusing to clean up.")
    exit 1
}
$repoRootFull = [System.IO.Path]::GetFullPath($repoRoot.Trim())

if (-not (Test-Path $Path)) {
    [Console]::Error.WriteLine("Path '$Path' does not exist. Nothing to clean up.")
    exit 1
}
$targetFull = [System.IO.Path]::GetFullPath($Path)

if ($targetFull -eq $repoRootFull -or $repoRootFull.StartsWith($targetFull + [System.IO.Path]::DirectorySeparatorChar)) {
    [Console]::Error.WriteLine("Path '$targetFull' is, or contains, the primary worktree ($repoRootFull). Refusing to remove it.")
    exit 1
}

$markerPath = Join-Path $targetFull ".rxflow-remediation-marker.json"
if (-not (Test-Path $markerPath)) {
    [Console]::Error.WriteLine("No .rxflow-remediation-marker.json found at '$targetFull'. This does not look like a worktree created by create-remediation-worktree.ps1 - refusing to remove an unvalidated path.")
    exit 1
}

$marker = Get-Content -Path $markerPath -Raw | ConvertFrom-Json
if ($marker.primaryRepo -ne $repoRootFull) {
    [Console]::Error.WriteLine("Marker's recorded primaryRepo ('$($marker.primaryRepo)') does not match this repo ($repoRootFull). Refusing to remove a worktree that belongs to a different checkout.")
    exit 1
}

Write-Output "Before cleanup:"
git -C $repoRootFull worktree list

Write-Output "Removing validated remediation worktree: $targetFull (bound to $($marker.boundSha), created $($marker.createdAtUtc))"
git -C $repoRootFull worktree remove --force $targetFull
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("git worktree remove failed (exit $LASTEXITCODE).")
    exit $LASTEXITCODE
}
git -C $repoRootFull worktree prune

Write-Output "After cleanup:"
$after = git -C $repoRootFull worktree list
$after

$repoRootNormalized = $repoRootFull.Replace('\', '/')
if (-not (($after -join "`n").Replace('\', '/') -match [regex]::Escape($repoRootNormalized))) {
    [Console]::Error.WriteLine("Primary worktree no longer listed after cleanup - this should never happen. Investigate immediately.")
    exit 1
}
if (Test-Path $targetFull) {
    [Console]::Error.WriteLine("Target path '$targetFull' still exists on disk after 'git worktree remove'. Investigate before trusting cleanup.")
    exit 1
}

Write-Output "Cleanup verified: primary worktree preserved, removed worktree no longer present."
