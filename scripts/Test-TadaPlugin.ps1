param(
    [Parameter(Mandatory)][string]$PluginRoot,
    [string]$Dotnet = 'dotnet',
    [string]$Archive,
    [ValidatePattern('^10\.0\.[1-9][0-9]{2,}$')][string]$SdkVersion
)

$ErrorActionPreference = 'Stop'
$plugin = (Resolve-Path -LiteralPath $PluginRoot).Path
# The Claude Code and Cursor marketplaces sit beside the packaged plugin, not inside the standalone ZIP.
$marketplaceRoot = Split-Path $plugin -Parent
$repository = Split-Path $PSScriptRoot -Parent
$workspace = Join-Path $repository ('.artifacts/plugin-controls/' + [guid]::NewGuid().ToString('N'))
if ($Archive) {
    $plugin = Join-Path $workspace 'extracted plugin'
    [IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path -LiteralPath $Archive).Path, $plugin)
}
$catalog = Get-Content -LiteralPath (Join-Path $plugin '.agents/plugins/marketplace.json') -Raw | ConvertFrom-Json
if ($catalog.name -ne 'tada' -or @($catalog.plugins).Count -ne 1 -or
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
if ($claudeCatalog.name -ne 'tada' -or !$claudeCatalog.owner.name -or @($claudeCatalog.plugins).Count -ne 1 -or
    $claudeCatalog.plugins[0].name -ne $manifest.name -or $claudeCatalog.plugins[0].source -ne './tada') {
    throw 'Claude Code marketplace does not resolve the shared TADA plugin.'
}
$standaloneClaudeCatalog = Get-Content -LiteralPath (Join-Path $plugin '.claude-plugin/marketplace.json') -Raw | ConvertFrom-Json
if ($standaloneClaudeCatalog.name -ne 'tada' -or @($standaloneClaudeCatalog.plugins).Count -ne 1 -or
    $standaloneClaudeCatalog.plugins[0].name -ne $manifest.name -or $standaloneClaudeCatalog.plugins[0].source -ne './') {
    throw 'Standalone Claude Code marketplace must resolve its own plugin root.'
}
$cursorManifest = Get-Content -LiteralPath (Join-Path $plugin '.cursor-plugin/plugin.json') -Raw | ConvertFrom-Json
if ($cursorManifest.name -ne $manifest.name -or $cursorManifest.version -ne $manifest.version -or $cursorManifest.license -ne $manifest.license -or
    $cursorManifest.name -cnotmatch '^[a-z0-9][a-z0-9.-]*$') {
    throw 'Cursor and Codex plugin metadata differ.'
}
# Cursor resolves each marketplace entry as <source>/.cursor-plugin/plugin.json beside the catalog.
$cursorCatalog = Get-Content -LiteralPath (Join-Path $marketplaceRoot '.cursor-plugin/marketplace.json') -Raw | ConvertFrom-Json
if ($cursorCatalog.name -ne 'tada' -or !$cursorCatalog.owner.name -or @($cursorCatalog.plugins).Count -ne 1 -or
    $cursorCatalog.plugins[0].name -ne $manifest.name -or $cursorCatalog.plugins[0].source -ne 'tada' -or
    !(Test-Path -LiteralPath (Join-Path $marketplaceRoot "$($cursorCatalog.plugins[0].source)/.cursor-plugin/plugin.json"))) {
    throw 'Cursor marketplace does not resolve the shared TADA plugin.'
}
# The Codex-format manifest is what the OpenAI plugin directory reviews.
$codexManifest = Get-Content -LiteralPath (Join-Path $plugin '.codex-plugin/plugin.json') -Raw | ConvertFrom-Json
if ($codexManifest.name -ne $manifest.name -or $codexManifest.version -ne $manifest.version -or $codexManifest.license -ne $manifest.license -or
    $codexManifest.PSObject.Properties.Name -contains '$schema' -or $codexManifest.skills -ne './skills/' -or !$codexManifest.author.name) {
    throw 'Codex-format manifest differs from the shared plugin identity or omits required fields.'
}
foreach ($field in @('displayName', 'shortDescription', 'longDescription', 'developerName', 'category', 'composerIcon', 'logo')) {
    if (!$codexManifest.interface.$field) { throw "Codex-format manifest omits interface.$field." }
}
if ($codexManifest.interface.displayName.Length -gt 30 -or $codexManifest.interface.shortDescription.Length -gt 30) {
    throw 'Codex display name and short description must be 30 characters or fewer.'
}
# OpenAI reads the root manifest's inline interface when present, without merging it with
# .codex-plugin/plugin.json, so the two interfaces must be identical.
$inlineInterface = $manifest.extensions.'com.openai'.interface
if ($inlineInterface -and (($inlineInterface | ConvertTo-Json -Depth 5 -Compress) -ne ($codexManifest.interface | ConvertTo-Json -Depth 5 -Compress))) {
    throw 'The inline OpenAI interface in plugin.json differs from .codex-plugin/plugin.json.'
}
foreach ($icon in @($codexManifest.interface.logo, $codexManifest.interface.composerIcon, $claudeManifest.icon, $cursorManifest.logo)) {
    if (!$icon -or !(Test-Path -LiteralPath (Join-Path $plugin $icon) -PathType Leaf)) { throw "Plugin icon $icon is missing." }
}
if (!$Archive) {
    foreach ($published in @('README.md', 'LICENSE', '.gitattributes')) {
        if (!(Test-Path -LiteralPath (Join-Path $marketplaceRoot $published) -PathType Leaf)) { throw "Marketplace repository content omits $published." }
    }
    if ((Get-Content -LiteralPath (Join-Path $marketplaceRoot 'README.md') -Raw) -match 'VERSION_PLACEHOLDER') { throw 'Marketplace README version was not filled in.' }
}
$skillHeader = (Get-Content -LiteralPath (Join-Path $plugin 'skills/tada/SKILL.md') -TotalCount 4) -join "`n"
if ($skillHeader -notmatch '(?m)^name: tada$' -or $skillHeader -notmatch '(?m)^description: \S') {
    throw 'Shared skill frontmatter must give the kebab-case name and description that Codex, Claude Code and Cursor read.'
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
    if (!(Test-Path -LiteralPath (Join-Path $plugin 'skills/tada/references/discovery-and-knowledge.md'))) { throw 'Plugin skill omitted its discovery and knowledge reference.' }
    foreach ($knowledge in @('.tada/knowledge.json', '.tada/knowledge.md', '.tada/knowledge')) {
        if (Test-Path -LiteralPath (Join-Path $application $knowledge)) { throw "Plugin setup created $knowledge. Reference mode is the default and needs no knowledge files." }
    }
    Push-Location -LiteralPath $application
    try {
        $knowledgeReport = (& $Dotnet tool run tada knowledge check --json) -join [Environment]::NewLine | ConvertFrom-Json
        if ($LASTEXITCODE -or $knowledgeReport.knowledgeMode -ne 'reference' -or $knowledgeReport.modeSource -ne 'default') {
            throw 'The installed verifier did not report the default reference knowledge mode.'
        }
    }
    finally { Pop-Location }
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
        cursorManifestAndMarketplace = $true
        codexDirectoryManifest = $true
        marketplace = $catalog.name
        sharedSkillAndVerifierFeed = $true
        resolvedSdk = $resolvedSdk
        existingSdkPolicyByteExact = [bool]$SdkVersion
        discoveryAndKnowledgeReferencePackaged = $true
        defaultKnowledgeMode = $knowledgeReport.knowledgeMode
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
