param([Parameter(Mandatory)][string]$Package)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repository 'plugins/tada'
$manifest = Get-Content -LiteralPath (Join-Path $source 'plugin.json') -Raw | ConvertFrom-Json
$claudeManifest = Get-Content -LiteralPath (Join-Path $source '.claude-plugin/plugin.json') -Raw | ConvertFrom-Json
if ($claudeManifest.name -ne $manifest.name -or $claudeManifest.version -ne $manifest.version) {
    throw 'Codex and Claude Code plugin identities must match.'
}
$cursorManifest = Get-Content -LiteralPath (Join-Path $source '.cursor-plugin/plugin.json') -Raw | ConvertFrom-Json
if ($cursorManifest.name -ne $manifest.name -or $cursorManifest.version -ne $manifest.version) {
    throw 'Codex and Cursor plugin identities must match.'
}
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$packageZip = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $reader = [IO.StreamReader]::new($packageZip.GetEntry('Tada.Cli.nuspec').Open())
    try { [xml]$metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($metadata.package.metadata.id -ne 'Tada.Cli' -or $metadata.package.metadata.version -ne $manifest.version) {
        throw 'Plugin and verifier package identities must match.'
    }
}
finally { $packageZip.Dispose() }

$output = Join-Path $repository ('.artifacts/plugin-bundles/' + [guid]::NewGuid().ToString('N'))
$marketplaceRoot = Join-Path $output 'marketplace'
$plugin = Join-Path $marketplaceRoot 'tada'
New-Item -ItemType Directory -Path $marketplaceRoot -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $plugin -Recurse
New-Item -ItemType Directory -Path (Join-Path $plugin 'feed') | Out-Null
Copy-Item -LiteralPath $packagePath -Destination (Join-Path $plugin "feed/Tada.Cli.$($manifest.version).nupkg")
Copy-Item -LiteralPath (Join-Path $repository 'LICENSE') -Destination $plugin
Copy-Item -LiteralPath (Join-Path $repository 'THIRD_PARTY_NOTICES.md') -Destination $plugin
$catalogDirectory = Join-Path $marketplaceRoot '.agents/plugins'
New-Item -ItemType Directory -Path $catalogDirectory -Force | Out-Null
$catalog = [ordered]@{
    name = 'tada-development'
    plugins = @([ordered]@{
        name = 'tada'
        source = @{ source = 'local'; path = './tada' }
        policy = @{ installation = 'AVAILABLE'; authentication = 'ON_INSTALL' }
        category = 'Productivity'
    })
}
$catalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $catalogDirectory 'marketplace.json') -Encoding utf8
$standaloneCatalogDirectory = Join-Path $plugin '.agents/plugins'
New-Item -ItemType Directory -Path $standaloneCatalogDirectory -Force | Out-Null
$catalog.plugins[0].source.path = './'
$catalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $standaloneCatalogDirectory 'marketplace.json') -Encoding utf8
$claudeCatalogDirectory = Join-Path $marketplaceRoot '.claude-plugin'
New-Item -ItemType Directory -Path $claudeCatalogDirectory -Force | Out-Null
$claudeCatalog = [ordered]@{
    name = 'tada-development'
    description = 'TADA development previews for test-assured software development.'
    owner = @{ name = 'TADA contributors' }
    plugins = @(@{ name = $manifest.name; source = './tada' })
}
$claudeCatalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $claudeCatalogDirectory 'marketplace.json') -Encoding utf8
# The standalone ZIP also registers as a Claude Code marketplace that resolves its own root.
$claudeCatalog.plugins[0].source = './'
$claudeCatalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $plugin '.claude-plugin/marketplace.json') -Encoding utf8
$cursorCatalogDirectory = Join-Path $marketplaceRoot '.cursor-plugin'
New-Item -ItemType Directory -Path $cursorCatalogDirectory -Force | Out-Null
$cursorCatalog = [ordered]@{
    name = 'tada-development'
    owner = @{ name = 'TADA contributors' }
    metadata = @{ description = 'TADA development previews for test-assured software development.' }
    plugins = @([ordered]@{ name = $manifest.name; source = 'tada'; description = $manifest.description })
}
$cursorCatalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $cursorCatalogDirectory 'marketplace.json') -Encoding utf8
$archive = Join-Path $output "TADA-plugin-$($manifest.version).zip"
[IO.Compression.ZipFile]::CreateFromDirectory($plugin, $archive)
$result = [pscustomobject]@{
    version = $manifest.version
    sourceCommit = [string]$metadata.package.metadata.repository.commit
    archive = $archive
    archiveSha256 = (Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant()
    packageSha256 = (Get-FileHash -LiteralPath $packagePath).Hash.ToLowerInvariant()
    marketplaceRoot = $marketplaceRoot
    pluginRoot = $plugin
    hosts = @('Codex', 'Claude Code', 'Cursor')
    status = 'Local plugin prepared for host installation and enable/disable testing'
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'plugin.json') -Encoding utf8
$result
