param([Parameter(Mandatory)][string]$Package)

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repository 'plugins/tada'
$manifest = Get-Content -LiteralPath (Join-Path $source 'plugin.json') -Raw | ConvertFrom-Json
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
    status = 'Local plugin prepared for host installation and enable/disable testing'
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'plugin.json') -Encoding utf8
$result
