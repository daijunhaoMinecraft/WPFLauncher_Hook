[CmdletBinding()]
param(
    [switch]$Disable
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifact = 'Mcl.Core/bin/x86/Debug/net48/Mcl.Core.dll'

Push-Location $repository
try {
    & git ls-files --error-unmatch -- $artifact *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Tracked compatibility artifact not found: $artifact"
    }

    $flag = if ($Disable) { '--no-skip-worktree' } else { '--skip-worktree' }
    & git update-index $flag -- $artifact
    if ($LASTEXITCODE -ne 0) {
        throw "git update-index failed for $artifact"
    }

    $state = (& git ls-files -v -- $artifact | Out-String).Trim()
    if (-not $Disable -and -not $state.StartsWith('S', [StringComparison]::Ordinal)) {
        throw "skip-worktree was not applied to $artifact"
    }

    if ($Disable) {
        Write-Host "Local changes to $artifact are visible to Git again."
    }
    else {
        Write-Host "Local changes to $artifact are hidden from Git and editor commits."
    }
}
finally {
    Pop-Location
}
