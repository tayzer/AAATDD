param(
    [Parameter(Mandatory)][string]$PluginRoot,
    [string]$Dotnet = 'dotnet',
    [string]$Archive,
    [ValidatePattern('^10\.0\.[1-9][0-9]{2,}$')][string]$SdkVersion
)

$ErrorActionPreference = 'Stop'
$plugin = (Resolve-Path -LiteralPath $PluginRoot).Path
# The Claude Code marketplace sits beside the packaged plugin, not inside the standalone ZIP.
$marketplaceRoot = Split-Path $plugin -Parent
$repository = Split-Path $PSScriptRoot -Parent
$workspace = Join-Path $repository ('.artifacts/plugin-controls/' + [guid]::NewGuid().ToString('N'))
if ($Archive) {
    $plugin = Join-Path $workspace 'extracted plugin'
    [IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path -LiteralPath $Archive).Path, $plugin)
}
$catalog = Get-Content -LiteralPath (Join-Path $plugin '.agents/plugins/marketplace.json') -Raw | ConvertFrom-Json
if ($catalog.name -ne 'tada-development' -or @($catalog.plugins).Count -ne 1 -or
    $catalog.plugins[0].name -ne 'tada' -or $catalog.plugins[0].source.source -ne 'local' -or
    $catalog.plugins[0].source.path -ne './') { throw 'Standalone plugin marketplace must resolve its own plugin root.' }
$manifest = Get-Content -LiteralPath (Join-Path $plugin 'plugin.json') -Raw | ConvertFrom-Json
if ($manifest.'$schema' -ne 'https://agent-plugins.org/schemas/1.0.0/plugin.schema.json' -or $manifest.name -ne 'tada') { throw 'Unsupported plugin identity.' }
if ($manifest.extensions.'com.openai'.PSObject.Properties.Name -contains 'onboardingSkill') {
    throw 'Installing TADA must not launch project setup through an onboarding skill.'
}
$claudeManifest = Get-Content -LiteralPath (Join-Path $plugin '.claude-plugin/plugin.json') -Raw | ConvertFrom-Json
if ($claudeManifest.name -ne $manifest.name -or $claudeManifest.version -ne $manifest.version -or $claudeManifest.license -ne $manifest.license) {
    throw 'Claude Code and Codex plugin metadata differ.'
}
$claudeCatalog = Get-Content -LiteralPath (Join-Path $marketplaceRoot '.claude-plugin/marketplace.json') -Raw | ConvertFrom-Json
if ($claudeCatalog.name -ne 'tada-development' -or !$claudeCatalog.owner.name -or @($claudeCatalog.plugins).Count -ne 1 -or
    $claudeCatalog.plugins[0].name -ne $manifest.name -or $claudeCatalog.plugins[0].source -ne './tada') {
    throw 'Claude Code marketplace does not resolve the shared TADA plugin.'
}
$entry = Join-Path $plugin 'skills/tada/SKILL.md'
if (!(Test-Path -LiteralPath $entry) -or !(Test-Path -LiteralPath (Join-Path $plugin 'skills/tada/agents/openai.yaml'))) { throw 'Plugin skill or invocation metadata is missing.' }
$application = Join-Path $workspace 'My API'
New-Item -ItemType Directory -Path $application -Force | Out-Null
$instructions = Join-Path $application 'AGENTS.md'
[IO.File]::WriteAllText($instructions, 'Existing project authority.')
$before = (Get-FileHash -LiteralPath $instructions).Hash
$sdkPolicy = Join-Path $application 'global.json'
if ($SdkVersion) {
    [IO.File]::WriteAllText($sdkPolicy, (@{sdk=@{version=$SdkVersion;rollForward='disable';allowPrerelease=$false};test=@{runner='Microsoft.Testing.Platform'}} | ConvertTo-Json -Depth 4))
    $sdkPolicyBefore = (Get-FileHash -LiteralPath $sdkPolicy).Hash
}
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
    if ($SdkVersion -and (Get-FileHash -LiteralPath $sdkPolicy).Hash -ne $sdkPolicyBefore) { throw 'Plugin setup changed the existing SDK policy.' }
    Push-Location -LiteralPath $application
    try {
        $resolvedSdk = & $Dotnet --version
        if ($LASTEXITCODE -or ($SdkVersion -and $resolvedSdk -ne $SdkVersion)) { throw 'Plugin setup did not use the application SDK policy.' }
    } finally { Pop-Location }
    $report = [ordered]@{
        version = $manifest.version
        pluginRoot = $plugin
        standaloneArchive = $Archive
        standaloneMarketplaceResolvesPluginRoot = $true
        application = $application
        setupRepeatedSuccessfully = $true
        originalInstructionsByteExact = $true
        installationOnboardingSkillAbsent = $true
        claudeCodeManifestAndMarketplace = $true
        sharedSkillAndVerifierFeed = $true
        resolvedSdk = $resolvedSdk
        existingSdkPolicyByteExact = [bool]$SdkVersion
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
