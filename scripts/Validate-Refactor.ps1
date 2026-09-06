param(
    [string]$LauncherDirectory = 'D:\Games\MCLauncher',
    [string]$LauncherAssemblyPath = 'H:\Code\C#\C#杂项\WPFTool\WPFLauncher.dump-cleaned.exe',
    [switch]$SkipMainBuild
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
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
Invoke-CheckedBuild (Join-Path $repository 'tests\Mcl.Core.SmokeTests\Mcl.Core.SmokeTests.csproj') 'tests' @()
$testProgram = Join-Path $artifacts 'tests\bin\Mcl.Core.exe'
& $testProgram $repository (Join-Path $artifacts 'renders') 2>&1 |
    Tee-Object -FilePath (Join-Path $artifacts 'smoke-tests.log') | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Smoke tests failed with exit code $LASTEXITCODE" }
Write-Host "Validation complete. Logs and rendered previews: $artifacts"
