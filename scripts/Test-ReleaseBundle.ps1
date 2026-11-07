param([Parameter(Mandatory)][string]$Archive, [string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$archivePath = (Resolve-Path -LiteralPath $Archive).Path
$workspace = Join-Path $repository ('.artifacts/bundle-install/bundle install ' + [guid]::NewGuid().ToString('N'))
$extracted = Join-Path $workspace 'extracted bundle'
New-Item -ItemType Directory -Path $extracted -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $extracted)
$manifest = Get-Content -LiteralPath (Join-Path $extracted 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.version -notmatch '^0\.1\.0-alpha\.[0-9]+$' -or $manifest.package -ne "feed/Tada.Cli.$($manifest.version).nupkg") { throw 'Unsupported bundle manifest.' }
$expected = @('LICENSE','README.md','THIRD_PARTY_NOTICES.md',$manifest.package) | Sort-Object
if ((@($manifest.files.path | Sort-Object) -join "`n") -ne ($expected -join "`n")) { throw 'Unexpected or duplicate bundle assets.' }
$actual = @(Get-ChildItem -LiteralPath $extracted -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath($extracted, $_.FullName).Replace('\','/') } | Sort-Object)
$all = @($expected + @('manifest.json','SHA256SUMS') | Sort-Object)
if (($actual -join "`n") -ne ($all -join "`n")) { throw 'Bundle file set changed.' }
foreach ($file in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $extracted $file.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $file.sha256) { throw "Bundle asset checksum mismatch: $($file.path)" }
}
$expectedSums = @($manifest.files | ForEach-Object { "$($_.sha256)  $($_.path)" }) + @((Get-FileHash -LiteralPath (Join-Path $extracted 'manifest.json')).Hash.ToLowerInvariant() + '  manifest.json')
if (([IO.File]::ReadAllLines((Join-Path $extracted 'SHA256SUMS')) -join "`n") -ne ($expectedSums -join "`n")) { throw 'Bundle checksum list changed.' }
$previousCache = $env:NUGET_PACKAGES
$previousCliHome = $env:DOTNET_CLI_HOME
$previousGlobalToolPath = $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$previousCertificate = $env:DOTNET_GENERATE_ASPNET_CERTIFICATE
$userPathBefore = [Environment]::GetEnvironmentVariable('Path', 'User')
$env:NUGET_PACKAGES = Join-Path $workspace 'fresh package cache'
$env:DOTNET_CLI_HOME = Join-Path $workspace 'fresh cli home'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$application = Join-Path $workspace 'My API'
New-Item -ItemType Directory -Path $application -Force | Out-Null
$instructions = Join-Path $application 'AGENTS.md'
[IO.File]::WriteAllText($instructions, 'Existing application instructions must remain unchanged.')
$checks = [Collections.Generic.List[object]]::new()
Push-Location $application
try {
    & $Dotnet new tool-manifest | Out-Host
    if ($LASTEXITCODE) { throw 'Bundle tool-manifest creation failed.' }
    $feed = [Security.SecurityElement]::Escape((Join-Path $extracted 'feed'))
    [IO.File]::WriteAllText((Join-Path $application 'NuGet.Bundle.Config'), '<configuration><packageSources><clear/><add key="bundle" value="' + $feed + '"/></packageSources></configuration>')
    & $Dotnet tool install Tada.Cli --version $manifest.version --configfile NuGet.Bundle.Config | Out-Host
    if ($LASTEXITCODE) { throw 'Bundle package installation failed.' }
    foreach ($arguments in @(@('--version'), @('init'), @('init'))) {
        $output = & $Dotnet tool run tada @arguments --json
        if ($LASTEXITCODE) { throw "Bundle command failed: $arguments : $output" }
        $checks.Add(($output -join "`n" | ConvertFrom-Json))
    }
    $enabledInstructions = [IO.File]::ReadAllText($instructions)
    if ($checks[0].version -ne $manifest.version -or @($checks[2].created).Count -ne 0 -or @($checks[2].updated).Count -ne 0 -or $enabledInstructions -ne 'Existing application instructions must remain unchanged.') { throw 'Installed bundle version or non-destructive project preparation failed.' }
    if (!(Test-Path -LiteralPath '.tada/WORKFLOW.md') -or !(Test-Path -LiteralPath '.tada/context.md')) { throw 'Bundle omitted agent workflow/context guidance.' }
    $context = [IO.File]::ReadAllText((Join-Path $application '.tada/context.md'))
    if (!$context.Contains('## Local SDK host') -or ([IO.Path]::IsPathRooted($Dotnet) -and !$context.Contains($Dotnet, [StringComparison]::OrdinalIgnoreCase))) { throw 'Initialisation did not capture the invoking SDK host for ordinary-request agent use.' }
    if ([Environment]::GetEnvironmentVariable('Path', 'User') -ne $userPathBefore) { throw 'Bundle installation control changed the user PATH.' }
    $report = [pscustomobject]@{archive=$archivePath;archiveSha256=(Get-FileHash -LiteralPath $archivePath).Hash.ToLowerInvariant();packageSha256=(Get-FileHash -LiteralPath (Join-Path $extracted $manifest.package)).Hash.ToLowerInvariant();workspace=$workspace;version=$manifest.version;checks=@($checks);existingInstructionsPreserved=$true;activationOwner='host plugin controls';repeatedInitCreatesNothing=$true;isolatedCliAndPackageCaches=$true;pathsContainSpaces=$true;limitation='Package transport and preparation on this host. Native plugin activation and requester experience require their own trial.'}
    $report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $workspace 'validation.json') -Encoding utf8
    Write-Host "Bundle installation evidence: $workspace/validation.json"
} finally { $env:NUGET_PACKAGES=$previousCache; $env:DOTNET_CLI_HOME=$previousCliHome; $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=$previousGlobalToolPath; $env:DOTNET_CLI_TELEMETRY_OPTOUT=$previousTelemetry; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE=$previousCertificate; Pop-Location }
