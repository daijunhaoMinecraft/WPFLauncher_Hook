param(
    [string]$LauncherDirectory,
    [string]$LauncherAssemblyPath,
    [switch]$SkipMainBuild
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($LauncherDirectory)) {
    $LauncherDirectory = Join-Path $repository 'MCLauncherDebug'
}
$LauncherDirectory = [IO.Path]::GetFullPath($LauncherDirectory)
if ([string]::IsNullOrWhiteSpace($LauncherAssemblyPath)) {
    $LauncherAssemblyPath = Join-Path $LauncherDirectory 'WPFLauncher.exe'
}
$LauncherAssemblyPath = [IO.Path]::GetFullPath($LauncherAssemblyPath)
$artifacts = Join-Path $repository '.artifacts\validation'
[IO.Directory]::CreateDirectory($artifacts) | Out-Null

& (Join-Path $PSScriptRoot 'Verify-Source.ps1') -RepositoryRoot $repository

function Invoke-CheckedBuild([string]$Project, [string]$Name, [string[]]$ExtraArguments) {
    $output = (Join-Path $artifacts "$Name\bin") + [IO.Path]::DirectorySeparatorChar
    $intermediate = (Join-Path $artifacts "$Name\obj") + [IO.Path]::DirectorySeparatorChar
    & dotnet build $Project --nologo -v:minimal '-p:Platform=x86' "-p:LauncherDirectory=$LauncherDirectory" `
        "-p:OutputPath=$output" "-p:BaseIntermediateOutputPath=$intermediate" @ExtraArguments 2>&1 |
        Tee-Object -FilePath (Join-Path $artifacts "$Name-build.log") | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$Name build failed with exit code $LASTEXITCODE" }
}

if (-not $SkipMainBuild) {
    Invoke-CheckedBuild (Join-Path $repository 'Mcl.Core\Mcl.Core.csproj') 'main' @("-p:LauncherAssemblyPath=$LauncherAssemblyPath")
}
Write-Host "Validation complete. Source audit and main build logs: $artifacts"
