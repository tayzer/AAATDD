param(
    [Parameter(Mandatory)]
    [ValidateSet('restore','locked-restore','build','imports','help','discover','positive','negative')]
    [string]$Stage
)
$ErrorActionPreference = 'Stop'
$probeRoot = $PSScriptRoot
$dotnetPath = Join-Path $probeRoot '.tools/dotnet/dotnet.exe'
$stageId = '{0}-{1}-{2}' -f $Stage, [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'), [Guid]::NewGuid().ToString('N')
$stageDirectory = Join-Path $probeRoot "evidence/$stageId"
New-Item -ItemType Directory -Path $stageDirectory -Force | Out-Null
foreach ($folder in @('.cli','.packages','.tmp/http-cache','.tmp/plugins','.tmp/scratch')) {
    New-Item -ItemType Directory -Path (Join-Path $probeRoot $folder) -Force | Out-Null
}
$stageArguments = switch ($Stage) {
    'restore' { @('restore','CompatibilityProbe.csproj','--configfile','NuGet.Config','--verbosity','normal','-p:RestoreForceEvaluate=false') }
    'locked-restore' { @('restore','CompatibilityProbe.csproj','--configfile','NuGet.Config','--locked-mode','--verbosity','normal','-p:RestoreForceEvaluate=false') }
    'build' { @('build','CompatibilityProbe.csproj','--no-restore','--verbosity','normal') }
    'imports' { @('msbuild','CompatibilityProbe.csproj',"/pp:$stageDirectory/project-preprocessed.xml") }
    'help' { @('test','--help') }
    'discover' { @('test','--project','CompatibilityProbe.csproj','--no-build','--no-restore','--list-tests') }
    default { @('test','--project','CompatibilityProbe.csproj','--no-build','--no-restore','--results-directory',$stageDirectory) }
}
$timeoutMilliseconds = if ($Stage -in @('restore','locked-restore','build','imports')) { 300000 } else { 60000 }
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $dotnetPath
$startInfo.WorkingDirectory = $probeRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
foreach ($argument in $stageArguments) { $startInfo.ArgumentList.Add($argument) }
$processSettings = @{
    DOTNET_ROOT = (Join-Path $probeRoot '.tools/dotnet')
    DOTNET_CLI_HOME = (Join-Path $probeRoot '.cli')
    NUGET_PACKAGES = (Join-Path $probeRoot '.packages')
    NUGET_HTTP_CACHE_PATH = (Join-Path $probeRoot '.tmp/http-cache')
    NUGET_PLUGINS_CACHE_PATH = (Join-Path $probeRoot '.tmp/plugins')
    NUGET_SCRATCH = (Join-Path $probeRoot '.tmp/scratch')
    TEMP = (Join-Path $probeRoot '.tmp')
    TMP = (Join-Path $probeRoot '.tmp')
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_NOLOGO = '1'
    DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
    MSBuildEnableWorkloadResolver = 'false'
    AAATDD_PROBE_EVIDENCE_DIR = $stageDirectory
    AAATDD_PROBE_NEGATIVE = $(if ($Stage -eq 'negative') { '1' } else { '0' })
    REQNROLL_FORMATTERS_MESSAGE = "outputFilePath=$stageDirectory/native-messages.ndjson"
}
foreach ($entry in $processSettings.GetEnumerator()) { $startInfo.Environment[$entry.Key] = $entry.Value }
$startInfo.Environment['PATH'] = (Join-Path $probeRoot '.tools/dotnet') + [IO.Path]::PathSeparator + $env:PATH
$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo
$startedUtc = [DateTimeOffset]::UtcNow
[void]$process.Start()
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
$timedOut = -not $process.WaitForExit($timeoutMilliseconds)
if ($timedOut) {
    $process.Kill($true)
    $process.WaitForExit()
}
$stdout = $stdoutTask.GetAwaiter().GetResult()
$stderr = $stderrTask.GetAwaiter().GetResult()
[IO.File]::WriteAllText((Join-Path $stageDirectory 'stdout.txt'), $stdout)
[IO.File]::WriteAllText((Join-Path $stageDirectory 'stderr.txt'), $stderr)
$record = [ordered]@{
    stage = $Stage
    stageId = $stageId
    executable = $dotnetPath
    arguments = $stageArguments
    workingDirectory = $probeRoot
    processEnvironment = $processSettings
    startedUtc = $startedUtc.ToString('O')
    finishedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    processId = $process.Id
    timeoutMilliseconds = $timeoutMilliseconds
    timedOut = $timedOut
    exitCode = $process.ExitCode
}
$record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stageDirectory 'command.json')
[pscustomobject]@{ stage = $Stage; exitCode = $process.ExitCode; timedOut = $timedOut; evidence = $stageDirectory } | ConvertTo-Json
if ($stdout) { $stdout }
if ($stderr) { $stderr }
if ($timedOut) { exit 124 }
exit $process.ExitCode
