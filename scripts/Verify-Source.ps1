param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $RepositoryRoot 'Mcl.Core'
$problems = [System.Collections.Generic.List[string]]::new()
$sources = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
foreach ($file in $sources) {
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -match '(?m)^\s*//\s*(?:\((?:get|set|add|remove)\)\s*)?(?:Token:\s*0x|RVA:\s*0x|File Offset:\s*0x)') {
        $problems.Add("Decompiler metadata: $($file.FullName)")
    }
    foreach ($line in $text -split "`n") {
        if ($line -match '^\s*//' -or $line -notmatch '\bConsole\.Write(?:Line)?\(') { continue }
        if ($file.Name -notin @('GameProcessHooks.cs', 'ConsoleOutput.cs') -or
            ($file.Name -eq 'GameProcessHooks.cs' -and $line -notmatch 'if\s*\(WpfConfig\.ShowGameLogsInConsole\)') ) {
            $problems.Add("Unfiltered console diagnostic: $($file.FullName): $($line.Trim())")
        }
    }
}

$xamlNamespace = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'
$windowCount = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Dotnetdetour') -Recurse -File -Filter '*.xaml') {
    [xml]$document = [IO.File]::ReadAllText($file.FullName)
    if ($document.DocumentElement.LocalName -ne 'Window') { continue }
    $windowCount++
    $namespaces = [System.Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespaces.AddNamespace('p', $xamlNamespace)
    $theme = $document.SelectSingleNode('//p:ResourceDictionary[@Source="/Mcl.Core;component/Dotnetdetour/UI/Themes/Fluent.xaml"]', $namespaces)
    if ($null -eq $theme) { $problems.Add("Missing scoped Fluent theme: $($file.FullName)") }
    if ($document.SelectNodes('/p:Window/p:Window.Resources/p:Style', $namespaces).Count -gt 0) {
        $problems.Add("Duplicated window-level styles: $($file.FullName)")
    }
}
if ($windowCount -ne 21) { $problems.Add("Expected 21 XAML windows; found $windowCount. Update the audit when adding a view.") }
if ($problems.Count -gt 0) { throw ($problems -join [Environment]::NewLine) }
Write-Host "PASS: $($sources.Count) source files checked; $windowCount windows use the shared Fluent theme."
