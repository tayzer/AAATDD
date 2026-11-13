param([Parameter(Mandatory)][string]$Package)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$packagePath = (Resolve-Path -LiteralPath $Package).Path
if ([IO.Path]::GetExtension($packagePath) -ne '.nupkg') { throw 'Expected a NuGet tool package.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $nuspec = @($archive.Entries | Where-Object FullName -match '^[^/]+\.nuspec$')
    if ($nuspec.Count -ne 1) { throw 'Package must contain exactly one nuspec.' }
    $reader = [IO.StreamReader]::new($nuspec[0].Open())
    try { [xml]$metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $id = [string]$metadata.package.metadata.id
    $version = [string]$metadata.package.metadata.version
    if ($id -ne 'Tada.Cli' -or $version -notmatch '^0\.1\.0-alpha\.[0-9]+(?:\.[0-9]+)?$') { throw 'Expected a versioned TADA development alpha package.' }
    if ($metadata.package.metadata.license.InnerText -ne 'MIT' -or $metadata.package.metadata.license.type -ne 'expression') { throw 'Package must declare its MIT licence.' }
    $expectedDlls = @('Cucumber.Messages.dll','Gherkin.dll','Tada.BuildLogger.dll','Tada.Cli.dll','Tada.Core.dll','Tada.RuntimeProbe.dll','tada_native_observer.dll') | Sort-Object
    $dlls = @($archive.Entries | Where-Object FullName -match '\.dll$' | ForEach-Object { $_.Name } | Sort-Object)
    if (($dlls -join "`n") -ne ($expectedDlls -join "`n")) { throw 'Redistributed DLL set changed; review dependencies and notices before bundling.' }
    $notice = $archive.GetEntry('THIRD_PARTY_NOTICES.md')
    if (!$notice) { throw 'Packaged third-party notices are missing.' }
    $reader = [IO.StreamReader]::new($notice.Open())
    try { $notices = $reader.ReadToEnd() } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
$output = Join-Path $repository ('.artifacts/release-bundles/' + $version + '-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $output 'payload'
New-Item -ItemType Directory -Path (Join-Path $payload 'feed') -Force | Out-Null
$packageName = "Tada.Cli.$version.nupkg"
Copy-Item -LiteralPath $packagePath -Destination (Join-Path $payload "feed/$packageName")
Copy-Item -LiteralPath (Join-Path $repository 'LICENSE') -Destination (Join-Path $payload 'LICENSE')
[IO.File]::WriteAllText((Join-Path $payload 'THIRD_PARTY_NOTICES.md'), $notices, [Text.UTF8Encoding]::new($false))
$guide = @'
# Try TADA

This alpha provides local advisory L1 acceptance verification.
Prerequisites are Windows x64, a stable .NET 10.0 SDK and Git for Windows. No C compiler or model API is required.
Workspace paths may contain spaces; commas and semicolons are unsupported in this alpha.

Extract the ZIP. From its extracted directory, create a separate application repository:

```powershell
$bundle = (Get-Location).Path
mkdir MyApi
cd MyApi
git init
git config core.longpaths true
dotnet new tool-manifest
$feed = [Security.SecurityElement]::Escape((Join-Path $bundle 'feed'))
'<configuration><packageSources><clear/><add key="bundle" value="' + $feed + '"/></packageSources></configuration>' | Set-Content NuGet.Bundle.Config
dotnet tool install Tada.Cli --version VERSION_PLACEHOLDER --configfile NuGet.Bundle.Config
dotnet tool run tada --version
dotnet tool run tada init
```

The local-only feed selects this package rather than an identically named public package.
Keep the extracted feed available for later `dotnet tool restore --configfile NuGet.Bundle.Config`.
`init` prepares `.tada/WORKFLOW.md`, `.tada/context.md` and harness scaffolding. It migrates recognised generated agent rules from earlier alphas so the host plugin controls can govern automatic workflow selection. The SDK host used for preparation is recorded in new context.

Enable the matching TADA plugin through the host's plugin controls. The plugin distribution includes this verifier package and its setup helper. Open MyApi in a fresh session and ask for your project normally.

The matching plugin supports Codex, Claude Code and Cursor through their native manifests and marketplaces. New application projects select the latest installed stable 10.0 feature band from a 10.0.100 floor. Existing global.json files are preserved. Changing a captured SDK policy requires renewed baseline review and human approval. Each run records the resolved SDK and observed toolchain identities.

> Build me a simple API for tracking my reading list.

Choose your own project and wording. The enabled plugin should make the agent discover behaviour, ask for business agreement and follow the workflow itself. Record reminders or missing adoption as usability findings. Check disable and re-enable controls in fresh sessions as part of the experience trial.

The human approves the captured specification and oracle. Broken setup is INDETERMINATE, not business RED.
An observed response violating an agreed outcome establishes FAIL. Oracle edits require a new review and approval.
Retained evidence can be verified without re-execution. Chores and refactors use proportionate checks without invented scenarios.

After the first feature, request a defect fix, a behaviour-preserving refactor and a small chore in normal language. Later start another fresh agent session with an ordinary continuation request. It must recover Git context without a handoff-reading prompt.
Do not weaken assertions or delete failed runs to obtain PASS. Missing checkpoints, damaged evidence or unsupported inputs require investigation.
Complete automatic I/O/child-process observation and protected independent assurance are not claimed.

The manifest and SHA256SUMS identify this bundle's files. Checksums establish correspondence, not publisher authentication.
Repository and full development quickstart: https://github.com/tayzer/AAATDD
Plugin installation and activation: https://github.com/tayzer/AAATDD/blob/main/docs/plugin.md
'@
[IO.File]::WriteAllText((Join-Path $payload 'README.md'), $guide.Replace('VERSION_PLACEHOLDER', $version), [Text.UTF8Encoding]::new($false))
$assets = @('LICENSE','README.md','THIRD_PARTY_NOTICES.md',"feed/$packageName")
$files = @($assets | Sort-Object | ForEach-Object { [pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $payload $_) -Algorithm SHA256).Hash.ToLowerInvariant()} })
$manifest = [ordered]@{schemaVersion=1;status='Alpha; local advisory L1';name='TADA';version=$version;package="feed/$packageName";supportedPlatform='Windows x64';dotnetSdk='stable 10.0 SDK, minimum 10.0.100';assurance='L1-advisory';files=$files}
[IO.File]::WriteAllText((Join-Path $payload 'manifest.json'), ($manifest | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
$checksums = @($files | ForEach-Object { "$($_.sha256)  $($_.path)" }) + @((Get-FileHash -LiteralPath (Join-Path $payload 'manifest.json')).Hash.ToLowerInvariant() + '  manifest.json')
[IO.File]::WriteAllLines((Join-Path $payload 'SHA256SUMS'), $checksums, [Text.UTF8Encoding]::new($false))
$zip = Join-Path $output "TADA-$version-win-x64.zip"
[IO.Compression.ZipFile]::CreateFromDirectory($payload, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
$result = [pscustomobject]@{version=$version;archive=$zip;sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant();packageSha256=(Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant();status=$manifest.status}
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $output 'bundle.json') -Encoding utf8
$result
