param(
    [Parameter(Mandatory)][string]$PluginRoot,
    [string]$Dotnet = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$plugin = (Resolve-Path -LiteralPath $PluginRoot).Path
$manifest = Get-Content -LiteralPath (Join-Path $plugin 'plugin.json') -Raw | ConvertFrom-Json
if ($manifest.'$schema' -ne 'https://agent-plugins.org/schemas/1.0.0/plugin.schema.json' -or $manifest.name -ne 'tada') { throw 'Unsupported plugin identity.' }
if ($manifest.extensions.'com.openai'.PSObject.Properties.Name -contains 'onboardingSkill') {
    throw 'Installing TADA must not launch project setup through an onboarding skill.'
}
$entry = Join-Path $plugin 'skills/tada/SKILL.md'
if (!(Test-Path -LiteralPath $entry) -or !(Test-Path -LiteralPath (Join-Path $plugin 'skills/tada/agents/openai.yaml'))) { throw 'Plugin skill or invocation metadata is missing.' }
$repository = Split-Path $PSScriptRoot -Parent
$workspace = Join-Path $repository ('.artifacts/plugin-controls/' + [guid]::NewGuid().ToString('N'))
$application = Join-Path $workspace 'My API'
New-Item -ItemType Directory -Path $application -Force | Out-Null
$instructions = Join-Path $application 'AGENTS.md'
[IO.File]::WriteAllText($instructions, 'Existing project authority.')
$before = (Get-FileHash -LiteralPath $instructions).Hash
$previousHome = $env:DOTNET_CLI_HOME
$previousCache = $env:NUGET_PACKAGES
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$previousCertificate = $env:DOTNET_GENERATE_ASPNET_CERTIFICATE
$env:DOTNET_CLI_HOME = Join-Path $workspace 'cli home'
$env:NUGET_PACKAGES = Join-Path $workspace 'package cache'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
try {
    $initializer = Join-Path $plugin 'skills/tada/scripts/Initialize-Tada.ps1'
    & $initializer -Repository $application -Dotnet $Dotnet | Out-Host
    & $initializer -Repository $application -Dotnet $Dotnet | Out-Host
    if ((Get-FileHash -LiteralPath $instructions).Hash -ne $before) { throw 'Plugin setup altered project instructions.' }
    if (!(Test-Path -LiteralPath (Join-Path $application '.tada/context.md'))) { throw 'Plugin setup did not initialise project context.' }
    $report = [ordered]@{
        version = $manifest.version
        pluginRoot = $plugin
        application = $application
        setupRepeatedSuccessfully = $true
        originalInstructionsByteExact = $true
        installationOnboardingSkillAbsent = $true
        nativeHostToggleTrial = 'pending'
        ordinaryRequestAgentTrial = 'pending'
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $workspace 'validation.json') -Encoding utf8
    Write-Host "Plugin setup evidence saved at $workspace/validation.json"
}
finally {
    $env:DOTNET_CLI_HOME = $previousHome
    $env:NUGET_PACKAGES = $previousCache
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = $previousCertificate
}
