param([Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?$')][string]$Version)

# Stamps one version into the CLI build and every plugin manifest, changing nothing else.
# Release automation uses it in its own checkout to give each published build a unique version.
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$encoding = [Text.UTF8Encoding]::new($false)

function Set-Once([string]$Path, [string]$Pattern, [string]$Replacement) {
    $text = [IO.File]::ReadAllText($Path)
    $found = [regex]::Matches($text, $Pattern).Count
    if ($found -ne 1) { throw "Expected exactly one version in $Path, found $found." }
    [IO.File]::WriteAllText($Path, [regex]::Replace($text, $Pattern, $Replacement), $encoding)
}

Set-Once (Join-Path $repository 'Directory.Build.props') '<Version>[^<]+</Version>' "<Version>$Version</Version>"
foreach ($manifest in @('plugin.json', '.codex-plugin/plugin.json', '.claude-plugin/plugin.json', '.cursor-plugin/plugin.json')) {
    Set-Once (Join-Path $repository "plugins/tada/$manifest") '"version":\s*"[^"]+"' "`"version`": `"$Version`""
}
$Version
