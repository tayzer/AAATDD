param([switch]$Fixture, [string]$ToolchainDirectory)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$artifactRoot = Join-Path $repository '.artifacts'
$toolchain = if ($ToolchainDirectory) { [IO.Path]::GetFullPath($ToolchainDirectory) } else { Join-Path $artifactRoot 'native-toolchain' }
$version = '0.15.2'
$archiveName = "zig-x86_64-windows-$version.zip"
$expectedHash = '3a0ed1e8799a2f8ce2a6e6290a9ff22e6906f8227865911fb7ddedc3cc14cb0c'

function Assert-ArtifactPath([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if (!$full.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Native build path escapes the workspace artifact directory: $full"
    }
    for ($current = $full; $current -and $current -ne $repository; $current = [IO.Path]::GetDirectoryName($current)) {
        if ((Test-Path -LiteralPath $current) -and ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Native build paths cannot traverse reparse points: $current"
        }
    }
}

function Get-Compiler {
    Assert-ArtifactPath $toolchain
    New-Item -ItemType Directory -Path $toolchain -Force | Out-Null
    $lockPath = Join-Path $toolchain 'restore.lock'
    $deadline = [Diagnostics.Stopwatch]::StartNew()
    $restoreLock = $null
    while (!$restoreLock) {
        try { $restoreLock = [IO.FileStream]::new($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
        catch [IO.IOException] {
            if (($_.Exception.HResult -band 65535) -notin @(32,33) -or $deadline.Elapsed.TotalMinutes -gt 10) { throw }
            Start-Sleep -Milliseconds 100
        }
    }
    try {
        $archive = Join-Path $toolchain $archiveName
        if (!(Test-Path -LiteralPath $archive)) {
            $download = Join-Path $toolchain ([guid]::NewGuid().ToString('N') + '.download')
            Invoke-WebRequest -Uri "https://ziglang.org/download/$version/$archiveName" -OutFile $download -TimeoutSec 120
            if ((Get-FileHash -Algorithm SHA256 -LiteralPath $download).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Native compiler archive checksum mismatch.' }
            [IO.File]::Move($download, $archive)
        }
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Cached native compiler archive checksum mismatch.' }
        $compilerDirectory = Join-Path $toolchain "zig-x86_64-windows-$version"
        $compiler = Join-Path $compilerDirectory 'zig.exe'
        if (!(Test-Path -LiteralPath $compiler)) {
            $stage = Join-Path $toolchain ('extract-' + [guid]::NewGuid().ToString('N'))
            Assert-ArtifactPath $stage
            [IO.Compression.ZipFile]::ExtractToDirectory($archive, $stage)
            $unpacked = Join-Path $stage "zig-x86_64-windows-$version"
            Assert-ArtifactPath $unpacked
            Assert-ArtifactPath $compilerDirectory
            if (Test-Path -LiteralPath $compilerDirectory) {
                $backup = Join-Path $toolchain ('incomplete-' + [guid]::NewGuid().ToString('N'))
                Assert-ArtifactPath $backup
                [IO.Directory]::Move($compilerDirectory, $backup)
            }
            [IO.Directory]::Move($unpacked, $compilerDirectory)
            [IO.Directory]::Delete($stage, $false)
        }
        $reportedVersion = (& $compiler version) -join "`n"
        if ($LASTEXITCODE -ne 0 -or $reportedVersion.Trim() -ne $version) { throw 'Native compiler version differs from the reviewed build pin.' }
        return $compiler
    }
    finally { $restoreLock.Dispose() }
}

$compiler = Get-Compiler
$env:ZIG_GLOBAL_CACHE_DIR = Join-Path $toolchain 'cache'
$env:ZIG_LOCAL_CACHE_DIR = Join-Path $toolchain 'local'
$outputDirectory = Join-Path $artifactRoot $(if ($Fixture) { 'native/tests' } else { 'native' })
Assert-ArtifactPath $outputDirectory
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$stage = Join-Path $outputDirectory ('build-' + [guid]::NewGuid().ToString('N'))
Assert-ArtifactPath $stage
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$source = Join-Path $repository $(if ($Fixture) { 'tests/Tada.Core.Tests/Fixtures/native.c' } else { 'src/Tada.RuntimeProbe/native/observer.c' })
$name = if ($Fixture) { 'tada_fixture.dll' } else { 'tada_native_observer.dll' }
$temporary = Join-Path $stage $name
$arguments = @('cc','-target','x86_64-windows-gnu','-std=c11','-O2','-g0','-s','-Wall','-Wextra','-Werror','-fno-stack-protector','-fno-builtin','-shared','-nostdlib','-Wl,--entry,DllMain','-Wl,/Brepro',$source,'-lkernel32','-o',$temporary)
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Native compilation failed for $name (exit $LASTEXITCODE)." }
$bytes = [IO.File]::ReadAllBytes($temporary)
$header = [BitConverter]::ToInt32($bytes,0x3c)
if ([BitConverter]::ToUInt16($bytes,$header+4) -ne 0x8664) { throw 'Native observer output is not Windows x64.' }
$destination = Join-Path $outputDirectory $name
Assert-ArtifactPath $temporary
Assert-ArtifactPath $destination
[IO.File]::Move($temporary, $destination, $true)
$metadata = [ordered]@{
    compilerVersion=$version; compilerArchiveSha256=$expectedHash;
    compilerSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $compiler).Hash.ToLowerInvariant();
    source=[IO.Path]::GetRelativePath($repository,$source).Replace('\','/');
    sourceSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash.ToLowerInvariant();
    outputSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $destination).Hash.ToLowerInvariant();
    target='x86_64-windows-gnu';flags=@('-std=c11','-O2','-g0','-s','-Wall','-Wextra','-Werror','-fno-stack-protector','-fno-builtin','-shared','-nostdlib','-Wl,--entry,DllMain','-Wl,/Brepro','-lkernel32')
}
[IO.File]::WriteAllText((Join-Path $outputDirectory ($name + '.build.json')), ($metadata | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Write-Host "Native build complete: $destination"
