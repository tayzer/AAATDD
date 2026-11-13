param(
    [string]$Repository = (Get-Location).Path,
    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'

# Each validated main build publishes 0.1.0-alpha.<n>[.<build>]. A project may move forward
# within that line, never backward and never to a different release line.
function Test-OlderAlpha([string]$Installed, [string]$Target) {
    $pattern = '^0\.1\.0-alpha\.(\d+)(?:\.(\d+))?$'
    $from = [regex]::Match($Installed, $pattern)
    $to = [regex]::Match($Target, $pattern)
    if (!$from.Success -or !$to.Success) { return $false }
    $fromKey = @([long]$from.Groups[1].Value, $(if ($from.Groups[2].Success) { [long]$from.Groups[2].Value } else { -1 }))
    $toKey = @([long]$to.Groups[1].Value, $(if ($to.Groups[2].Success) { [long]$to.Groups[2].Value } else { -1 }))
    return $fromKey[0] -lt $toKey[0] -or ($fromKey[0] -eq $toKey[0] -and $fromKey[1] -lt $toKey[1])
}
$skillDirectory = Split-Path $PSScriptRoot -Parent
$skillsDirectory = Split-Path $skillDirectory -Parent
$pluginRoot = Split-Path $skillsDirectory -Parent
$manifest = Get-Content -LiteralPath (Join-Path $pluginRoot 'plugin.json') -Raw | ConvertFrom-Json
$feed = Join-Path $pluginRoot 'feed'
$package = Join-Path $feed "Tada.Cli.$($manifest.version).nupkg"
if (!(Test-Path -LiteralPath $package)) { throw 'The packaged TADA feed is missing. Use the distribution built by New-TadaPlugin.ps1.' }

$projectRoot = (Resolve-Path -LiteralPath $Repository).Path
$previousPathSetup = $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
Push-Location -LiteralPath $projectRoot
try {
    $sdk = & $Dotnet --version
    if ($LASTEXITCODE -or $sdk -notmatch '^10\.0\.[1-9][0-9]{2,}$') {
        throw 'Select a stable .NET 10.0 SDK with -Dotnet that satisfies the application repository SDK policy.'
    }
    if (!(Test-Path -LiteralPath 'dotnet-tools.json') -and !(Test-Path -LiteralPath '.config/dotnet-tools.json')) {
        & $Dotnet new tool-manifest | Out-Host
        if ($LASTEXITCODE) { throw 'Local tool manifest creation failed.' }
    }
    $tools = (& $Dotnet tool list --local --format json | ConvertFrom-Json).data
    if ($LASTEXITCODE) { throw 'Cannot inspect the local tool manifest.' }
    $installed = @($tools | Where-Object packageId -eq 'tada.cli')
    $configuration = Join-Path $projectRoot ('.tada-feed-' + [guid]::NewGuid().ToString('N') + '.config')
    try {
        $escapedFeed = [Security.SecurityElement]::Escape($feed)
        [IO.File]::WriteAllText($configuration, '<configuration><packageSources><clear/><add key="tada" value="' + $escapedFeed + '"/></packageSources></configuration>')
        if ($installed.Count -eq 0) {
            & $Dotnet tool install Tada.Cli --version $manifest.version --configfile $configuration | Out-Host
        }
        else {
            if ($installed[0].version -ne $manifest.version -and !(Test-OlderAlpha $installed[0].version $manifest.version)) {
                throw "This project selects Tada.Cli $($installed[0].version). Select a compatible TADA plugin for that version."
            }
            & $Dotnet tool update Tada.Cli --version $manifest.version --configfile $configuration | Out-Host
        }
        if ($LASTEXITCODE) { throw 'TADA local tool installation or restore failed.' }
    }
    finally { if (Test-Path -LiteralPath $configuration) { Remove-Item -LiteralPath $configuration } }
    & $Dotnet tool run tada init
    if ($LASTEXITCODE) { throw 'TADA project preparation failed.' }
}
finally {
    Pop-Location
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = $previousPathSetup
}
