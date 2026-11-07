param(
    [string]$Dotnet = 'dotnet',
    [string]$Cli = (Join-Path $PSScriptRoot '../src/Tada.Cli/bin/Release/net10.0/Tada.Cli.dll')
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repository 'examples/Orders'
$workspace = Join-Path $repository ('.artifacts/orders-workflow/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workspace -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File -Force) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
    if (($relative -split '[/\\]') | Where-Object { $_ -in @('bin','obj','.tada') }) { continue }
    $target = Join-Path $workspace $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
$checks = [Collections.Generic.List[object]]::new()
function Invoke-Tada([string[]]$Arguments) {
    $output = & $Dotnet $Cli @Arguments --repo $workspace --json
    $code = $LASTEXITCODE
    $parsed = ($output -join "`n") | ConvertFrom-Json
    return @{ Code = $code; Result = $parsed }
}
function Check([string]$Name, [int]$Expected, $Actual) {
    if ($Actual.Code -ne $Expected) {
        throw "$Name expected exit $Expected, got $($Actual.Code): $($Actual.Result | ConvertTo-Json -Depth 8)"
    }
    $checks.Add([pscustomobject]@{ name=$Name; exitCode=$Actual.Code; result=$Actual.Result })
    Write-Host "$Name passed (exit $Expected)"
}
pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Build-NativeObserver.ps1') -Fixture
if ($LASTEXITCODE -ne 0) { throw 'Native provenance fixture build failed.' }
$nativeFixture = Join-Path $repository '.artifacts/native/tests/tada_fixture.dll'
$responseFile = Join-Path $workspace 'Directory.Build.rsp'
[IO.File]::WriteAllText($responseFile, '-TADA_UNREVIEWED_RESPONSE_CONTROL')
$responseOutput = & $Dotnet msbuild (Join-Path $workspace 'Acceptance/Orders.Acceptance.csproj') -getProperty:TargetFramework 2>&1
$responseExit = $LASTEXITCODE
Check 'ambient response-file poison is active without isolation' 1 @{ Code=$responseExit; Result=@{ diagnostic=($responseOutput -join "`n") } }
if (($responseOutput -join "`n") -notmatch 'TADA_UNREVIEWED_RESPONSE_CONTROL') { throw 'Response-file control did not load the intended poison.' }
$prepared = Invoke-Tada @('baseline','prepare')
Check 'prepare draft' 0 $prepared
$baseline = $prepared.Result.baseline
$review = Invoke-Tada @('baseline','review','--baseline',$baseline)
Check 'review snapshot' 0 $review
Check 'explicit control approval' 0 (Invoke-Tada @('baseline','approve','--review',$review.Result.review.id,'--by','automated integration control; not human acceptance'))
$positive = Invoke-Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
Check 'correct Orders' 0 $positive
$runId = $positive.Result.runId
Check 'offline verification' 0 (Invoke-Tada @('verify','--run',$runId))
$positiveDirectory = Join-Path $workspace ".tada/runs/$runId"
$nativeInputs = @(Get-ChildItem (Join-Path $positiveDirectory 'runtime') -Filter '*.ndjson' -File | ForEach-Object {
    [IO.File]::ReadLines($_.FullName) | ForEach-Object { $_ | ConvertFrom-Json } | Where-Object { $_.kind -eq 'native' -and $_.path -match '[/\\]e_sqlite3\.dll$' }
})
if ($nativeInputs.Count -lt 3) { throw 'Observer did not identify native SQLite in the harness and both API processes.' }
Check 'real native SQLite module identities captured' 0 @{Code=0;Result=@{observations=$nativeInputs.Count;processIds=@($nativeInputs.processId | Select-Object -Unique)}}
$positiveRecord = Get-Content -Raw (Join-Path $positiveDirectory 'run.json') | ConvertFrom-Json
if (@($positiveRecord.commands | Where-Object { $_.arguments[0] -in @('restore','msbuild') -and '-noAutoResponse' -notin $_.arguments }).Count -gt 0) { throw 'A build command accepted ambient response files.' }

# Reviewed projects must not pull unselected oracle source/imports into an otherwise passing suite.
$harnessProject = Join-Path $workspace 'Acceptance/Orders.Acceptance.csproj'
$originalProject = [IO.File]::ReadAllBytes($harnessProject)
$configPath = Join-Path $workspace 'tada.json'
$originalConfig = [IO.File]::ReadAllBytes($configPath)
$shared = Join-Path $workspace 'shared-control'
New-Item -ItemType Directory -Path $shared -Force | Out-Null
foreach ($control in @(
    @{ name='unselected linked source'; file='Linked.cs'; content='// Unselected compiler-input control'; xml='<ItemGroup><Compile Include="../shared-control/Linked.cs" /></ItemGroup>' },
    @{ name='unselected imported targets'; file='Custom.targets'; content='<Project><PropertyGroup><TadaInputControl>true</TadaInputControl></PropertyGroup></Project>'; xml='<Import Project="../shared-control/Custom.targets" />' }
)) {
    try {
        [IO.File]::WriteAllText((Join-Path $shared $control.file), $control.content)
        $changedProject = [Text.Encoding]::UTF8.GetString($originalProject).Replace('</Project>', $control.xml + '</Project>')
        [IO.File]::WriteAllText($harnessProject, $changedProject, [Text.UTF8Encoding]::new($false))
        $inputBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
        $inputReview = (Invoke-Tada @('baseline','review','--baseline',$inputBaseline)).Result.review.id
        Check ($control.name + ' control approval') 0 (Invoke-Tada @('baseline','approve','--review',$inputReview,'--by','automated unselected-input control; not human acceptance'))
        $blocked = Invoke-Tada @('run','--baseline',$inputBaseline,'--dotnet',$Dotnet)
        Check ($control.name + ' blocks pass') 2 $blocked
        if (!($blocked.Result.reasons | Where-Object { $_.code -eq 'execution-invalid' -and $_.detail.Contains('Unreviewed harness build input:') -and $_.detail.Contains($control.file) })) {
            throw ($control.name + ' was not rejected by the intended input boundary.')
        }
    } finally { [IO.File]::WriteAllBytes($harnessProject, $originalProject) }
}

$api = Join-Path $workspace 'Api/Program.cs'
$originalApi = [IO.File]::ReadAllBytes($api)
try {
    $text = [IO.File]::ReadAllText($api)
    $faulty = $text.Replace('AND available >= @quantity','AND available >= 0')
    if ($faulty -eq $text) { throw 'Oversubscription control did not change its intended guard.' }
    [IO.File]::WriteAllText($api, $faulty, [Text.UTF8Encoding]::new($false))
    $negative = Invoke-Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    Check 'oversubscription business defect' 1 $negative
    if (!($negative.Result.reasons | Where-Object { $_.code -eq 'requirement-failure' -and $_.caseId -eq 'AC_DEMO_STOCK_001_01' })) {
        throw 'Fault was not detected by the intended rejection assertion.'
    }
} finally { [IO.File]::WriteAllBytes($api, $originalApi) }

try {
    $text = [IO.File]::ReadAllText($api)
    $faulty = $text.Replace('insert.Parameters.AddWithValue("@quantity", request.Quantity);', 'insert.Parameters.AddWithValue("@quantity", request.Quantity + 1);')
    if ($faulty -eq $text) { throw 'Persistence control did not change its intended quantity write.' }
    [IO.File]::WriteAllText($api, $faulty, [Text.UTF8Encoding]::new($false))
    $negative = Invoke-Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    Check 'wrong persisted quantity business defect' 1 $negative
    if (!($negative.Result.reasons | Where-Object { $_.code -eq 'requirement-failure' -and $_.caseId -eq 'AC_DEMO_STOCK_001_02' })) {
        throw 'Incorrect persistence was not detected by the intended accepted-order assertion.'
    }
} finally { [IO.File]::WriteAllBytes($api, $originalApi) }

$binding = Join-Path $workspace 'Acceptance/OrderBindings.cs'
$originalBinding = [IO.File]::ReadAllBytes($binding)
$transientSource = Join-Path $workspace 'Acceptance/TransientNativeControl.cs'
$nativeDirectory = Join-Path $workspace 'unreviewed-native-control'
New-Item -ItemType Directory -Path $nativeDirectory -Force | Out-Null
Copy-Item -LiteralPath $nativeFixture -Destination (Join-Path $nativeDirectory 'tada_fixture.dll')
$transientHook = @'
using System.Diagnostics;
using System.Runtime.InteropServices;
using Reqnroll;
[Binding]
public sealed class TransientNativeControl
{
    [BeforeTestRun]
    public static void LoadAndUnload()
    {
        string file = CONTROL_PATH;
        nint loaded = NativeLibrary.Load(file);
        NativeLibrary.Free(loaded);
        using var process = Process.GetCurrentProcess();
        if (process.Modules.Cast<ProcessModule>().Any(module => string.Equals(module.FileName, file, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Native control must be unloaded before business execution.");
    }
}
'@
try {
    [IO.File]::WriteAllText($transientSource, $transientHook.Replace('CONTROL_PATH','Path.Combine(Environment.GetEnvironmentVariable("TADA_REPOSITORY")!, "unreviewed-native-control", "tada_fixture.dll")'))
    $transientBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
    $transientReview = (Invoke-Tada @('baseline','review','--baseline',$transientBaseline)).Result.review.id
    Check 'unreviewed transient native control approval' 0 (Invoke-Tada @('baseline','approve','--review',$transientReview,'--by','automated transient-input control; not human acceptance'))
    $transientGap = Invoke-Tada @('run','--baseline',$transientBaseline,'--dotnet',$Dotnet)
    Check 'unreviewed DLL unloaded before assertions blocks pass' 2 $transientGap
    if (!($transientGap.Result.reasons | Where-Object { $_.detail.Contains('Unidentified loaded runtime input:') -and $_.detail.Contains('tada_fixture.dll') })) { throw 'Transient native input was not rejected by its intended runtime boundary.' }
    $transientRecord = Get-Content -Raw (Join-Path $workspace ".tada/runs/$($transientGap.Result.runId)/run.json") | ConvertFrom-Json
    if ($transientRecord.commands[-1].exitCode -ne 0) { throw 'Unreviewed transient control did not leave business execution successful.' }

    $capturedDirectory = Join-Path $workspace 'Acceptance/NativeControl'
    New-Item -ItemType Directory -Path $capturedDirectory -Force | Out-Null
    Copy-Item -LiteralPath $nativeFixture -Destination (Join-Path $capturedDirectory 'tada_fixture.dll')
    [IO.File]::WriteAllText($transientSource, $transientHook.Replace('CONTROL_PATH','Path.Combine(AppContext.BaseDirectory, "NativeControl", "tada_fixture.dll")'))
    [IO.File]::WriteAllText($harnessProject, [Text.Encoding]::UTF8.GetString($originalProject).Replace('</Project>','<ItemGroup><None Update="NativeControl/tada_fixture.dll" CopyToOutputDirectory="PreserveNewest" /></ItemGroup></Project>'))
    $capturedBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
    $capturedReview = (Invoke-Tada @('baseline','review','--baseline',$capturedBaseline)).Result.review.id
    Check 'captured transient native control approval' 0 (Invoke-Tada @('baseline','approve','--review',$capturedReview,'--by','automated captured-input control; not human acceptance'))
    $capturedResult = Invoke-Tada @('run','--baseline',$capturedBaseline,'--dotnet',$Dotnet)
    Check 'reviewed transient DLL retains a passing load identity' 0 $capturedResult
    $capturedEvents = @(Get-ChildItem (Join-Path $workspace ".tada/runs/$($capturedResult.Result.runId)/runtime") -Filter '*.ndjson' -File | ForEach-Object {
        [IO.File]::ReadLines($_.FullName) | ForEach-Object { $_ | ConvertFrom-Json } | Where-Object { $_.kind -eq 'native-loaded' -and $_.path -match '[/\\]tada_fixture\.dll$' }
    })
    if ($capturedEvents.Count -ne 1) { throw 'Captured transient DLL has no unique retained native loader event.' }
} finally {
    [IO.File]::WriteAllBytes($harnessProject, $originalProject)
    if (Test-Path -LiteralPath $transientSource) { [IO.File]::Delete($transientSource) }
    $capturedFixture = Join-Path $workspace 'Acceptance/NativeControl/tada_fixture.dll'
    if (Test-Path -LiteralPath $capturedFixture) { [IO.File]::Delete($capturedFixture) }
    $capturedDirectory = Join-Path $workspace 'Acceptance/NativeControl'
    if (Test-Path -LiteralPath $capturedDirectory) { [IO.Directory]::Delete($capturedDirectory, $false) }
}
try {
    $text = [Text.Encoding]::UTF8.GetString($originalBinding)
    $faulty = $text.Replace('if (api != null) TadaRuntimeCheckpoint.Complete(api);', '// control: omit the required runtime checkpoint')
    if ($faulty -eq $text) { throw 'Missing-checkpoint control did not modify its intended guard.' }
    [IO.File]::WriteAllText($binding, $faulty, [Text.UTF8Encoding]::new($false))
    $runtimeBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
    $runtimeReview = (Invoke-Tada @('baseline','review','--baseline',$runtimeBaseline)).Result.review.id
    Check 'missing runtime checkpoint control approval' 0 (Invoke-Tada @('baseline','approve','--review',$runtimeReview,'--by','automated runtime-gap control; not human acceptance'))
    $runtimeGap = Invoke-Tada @('run','--baseline',$runtimeBaseline,'--dotnet',$Dotnet)
    Check 'missing final API runtime snapshot blocks pass' 2 $runtimeGap
    if (!($runtimeGap.Result.reasons | Where-Object detail -like '*complete final snapshot*')) { throw 'Missing runtime checkpoint was not caught by the intended evidence guard.' }
} finally { [IO.File]::WriteAllBytes($binding, $originalBinding) }
try {
    [IO.File]::AppendAllText($binding, "`n// unapproved oracle edit`n")
    Check 'oracle edit blocks execution' 2 (Invoke-Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet))
    Check 'reviewed revision cannot approve changed bytes' 3 (Invoke-Tada @('baseline','approve','--review',$review.Result.review.id,'--by','automated control'))
} finally { [IO.File]::WriteAllBytes($binding, $originalBinding) }

# Deliberately reviewed faulty-fixture controls validate readiness and cleanup guards,
# separately from the unapproved-oracle-edit control above.
New-Item -ItemType Directory -Path (Join-Path $workspace 'task-data-control') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $workspace 'task-data-control/expected.txt'), 'control assertion data')
foreach ($control in @(
    @{name='custom file-read task';task='ReadLinesFromFile';target='TaskInputControl';inputs=@('task-data-control');expected='Unreviewed harness task input';xml='<Target Name="TaskInputControl" BeforeTargets="CoreCompile"><ReadLinesFromFile File="../task-data-control/expected.txt"><Output TaskParameter="Lines" ItemName="TaskControlLines" /></ReadLinesFromFile><WriteLinesToFile File="$(IntermediateOutputPath)TaskInputControl.txt" Lines="@(TaskControlLines)" Overwrite="true" /></Target>'},
    @{name='opaque custom task';task='Exec';target='OpaqueTaskControl';inputs=@();expected='requires a reviewed buildTaskContracts entry';xml='<Target Name="OpaqueTaskControl" BeforeTargets="CoreCompile"><Exec Command="echo TADA_OPAQUE_CONTROL" /></Target>'}
)) {
    try {
        [IO.File]::WriteAllText($harnessProject, [Text.Encoding]::UTF8.GetString($originalProject).Replace('</Project>', $control.xml + '</Project>'))
        $taskBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
        $taskReview = (Invoke-Tada @('baseline','review','--baseline',$taskBaseline)).Result.review.id
        Check ($control.name + ' unreviewed-input control approval') 0 (Invoke-Tada @('baseline','approve','--review',$taskReview,'--by','automated task-input boundary control; not human acceptance'))
        $blockedTask = Invoke-Tada @('run','--baseline',$taskBaseline,'--dotnet',$Dotnet)
        Check ($control.name + ' blocks pass without declared inputs') 2 $blockedTask
        if (!($blockedTask.Result.reasons | Where-Object { $_.detail.Contains($control.expected) })) { throw ($control.name + ' did not fail at its intended task-input boundary.') }
        $declared = [Text.Encoding]::UTF8.GetString($originalConfig) | ConvertFrom-Json
        $declared | Add-Member -NotePropertyName buildTaskContracts -NotePropertyValue @([pscustomobject]@{origin='Acceptance/Orders.Acceptance.csproj';target=$control.target;task=$control.task;inputRoots=@($control.inputs)})
        [IO.File]::WriteAllText($configPath, ($declared | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
        $declaredBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
        $declaredReview = (Invoke-Tada @('baseline','review','--baseline',$declaredBaseline)).Result.review.id
        Check ($control.name + ' declared-input control approval') 0 (Invoke-Tada @('baseline','approve','--review',$declaredReview,'--by','automated declared-task control; not human acceptance'))
        Check ($control.name + ' passes under explicit reviewed input contract') 0 (Invoke-Tada @('run','--baseline',$declaredBaseline,'--dotnet',$Dotnet))
    } finally {
        [IO.File]::WriteAllBytes($harnessProject, $originalProject)
        [IO.File]::WriteAllBytes($configPath, $originalConfig)
    }
}
try {
    $text = [Text.Encoding]::UTF8.GetString($originalBinding)
    $faulty = $text.Replace('command.Parameters.AddWithValue("@stock", stock);', 'command.Parameters.AddWithValue("@stock", stock + 1);')
    if ($faulty -eq $text) { throw 'Wrong-seed control did not modify its seed.' }
    [IO.File]::WriteAllText($binding, $faulty, [Text.UTF8Encoding]::new($false))
    $seedBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
    $seedReview = (Invoke-Tada @('baseline','review','--baseline',$seedBaseline)).Result.review.id
    Check 'faulty-fixture control approval' 0 (Invoke-Tada @('baseline','approve','--review',$seedReview,'--by','automated faulty-fixture control; not human acceptance'))
    $seedResult = Invoke-Tada @('run','--baseline',$seedBaseline,'--dotnet',$Dotnet)
    Check 'wrong seed blocks behaviour submission' 2 $seedResult
    $seedRecord = Get-Content -Raw (Join-Path $workspace ".tada/runs/$($seedResult.Result.runId)/run.json") | ConvertFrom-Json
    if ($seedRecord.commands[-1].stdout -notmatch 'Seed readiness mismatch') { throw 'Wrong seed was not caught by the intended readiness guard.' }
} finally { [IO.File]::WriteAllBytes($binding, $originalBinding) }

try {
    $text = [Text.Encoding]::UTF8.GetString($originalBinding)
    $faulty = $text.Replace('if (File.Exists(database)) throw new IOException("Required owned database cleanup failed.");', 'if (File.Exists(database)) throw new IOException("Required owned database cleanup failed."); throw new IOException("CONTROL_REQUIRED_CLEANUP_GAP");')
    if ($faulty -eq $text) { throw 'Cleanup control did not modify its intended guard.' }
    [IO.File]::WriteAllText($binding, $faulty, [Text.UTF8Encoding]::new($false))
    $cleanupBaseline = (Invoke-Tada @('baseline','prepare')).Result.baseline
    $cleanupReview = (Invoke-Tada @('baseline','review','--baseline',$cleanupBaseline)).Result.review.id
    Check 'cleanup control approval' 0 (Invoke-Tada @('baseline','approve','--review',$cleanupReview,'--by','automated cleanup-gap control; not human acceptance'))
    Check 'required cleanup gap blocks pass' 2 (Invoke-Tada @('run','--baseline',$cleanupBaseline,'--dotnet',$Dotnet))
} finally { [IO.File]::WriteAllBytes($binding, $originalBinding) }

$runDirectory = Join-Path $workspace ".tada/runs/$runId"
$messages = Join-Path $runDirectory 'harness/tada-messages.ndjson'
$originalMessages = [IO.File]::ReadAllBytes($messages)
try {
    [IO.File]::WriteAllText($messages, '{"testRunFinished":', [Text.UTF8Encoding]::new($false))
    Check 'truncated evidence cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($messages, $originalMessages) }
Check 'restored retained evidence verifies' 0 (Invoke-Tada @('verify','--run',$runId))
$recordPath = Join-Path $runDirectory 'run.json'
$originalRecord = [IO.File]::ReadAllBytes($recordPath)
try {
    [IO.File]::WriteAllText($recordPath, '{"id":"incomplete"}', [Text.UTF8Encoding]::new($false))
    Check 'incomplete metadata cannot default to success' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($recordPath, $originalRecord) }
$buildInputs = Join-Path $runDirectory 'build-inputs.json'
$originalBuildInputs = [IO.File]::ReadAllBytes($buildInputs)
try {
    [IO.File]::WriteAllText($buildInputs, '{}', [Text.UTF8Encoding]::new($false))
    Check 'changed build-input evidence cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($buildInputs, $originalBuildInputs) }
$buildEvidence = Get-Content -Raw $buildInputs | ConvertFrom-Json
$reportInput = ($buildEvidence.projects[0].reports.PSObject.Properties | Select-Object -First 1).Name
$buildReport = Join-Path $runDirectory $reportInput
$originalBuildReport = [IO.File]::ReadAllBytes($buildReport)
try {
    [IO.File]::AppendAllText($buildReport, ' ')
    Check 'changed evaluated report cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($buildReport, $originalBuildReport) }
Check 'restored build-input evidence verifies offline' 0 (Invoke-Tada @('verify','--run',$runId))
foreach ($target in @('runtime-inputs.json','execution-environment.json')) {
    $evidence = Join-Path $runDirectory $target
    $originalEvidence = [IO.File]::ReadAllBytes($evidence)
    try {
        [IO.File]::WriteAllText($evidence, '{}', [Text.UTF8Encoding]::new($false))
        Check ("changed $target cannot pass") 2 (Invoke-Tada @('verify','--run',$runId))
    } finally { [IO.File]::WriteAllBytes($evidence, $originalEvidence) }
}
$runtimeReport = Get-ChildItem (Join-Path $runDirectory 'runtime') -Filter '*.ndjson' -File | Select-Object -First 1
$originalRuntimeReport = [IO.File]::ReadAllBytes($runtimeReport.FullName)
try {
    [IO.File]::WriteAllText($runtimeReport.FullName, '{', [Text.UTF8Encoding]::new($false))
    Check 'truncated runtime observation cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($runtimeReport.FullName, $originalRuntimeReport) }
Check 'restored runtime evidence verifies offline' 0 (Invoke-Tada @('verify','--run',$runId))
$taskInputs = Join-Path $runDirectory 'task-inputs.json'
$originalTaskInputs = [IO.File]::ReadAllBytes($taskInputs)
try {
    [IO.File]::WriteAllText($taskInputs, '{}', [Text.UTF8Encoding]::new($false))
    Check 'changed task-input evidence cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($taskInputs, $originalTaskInputs) }
$taskEvidence = Get-Content -Raw $taskInputs | ConvertFrom-Json
$taskReport = Join-Path $runDirectory $taskEvidence.reports[0].path
$originalTaskReport = [IO.File]::ReadAllBytes($taskReport)
try {
    [IO.File]::AppendAllText($taskReport, ' ')
    Check 'changed raw task journal cannot pass' 2 (Invoke-Tada @('verify','--run',$runId))
} finally { [IO.File]::WriteAllBytes($taskReport, $originalTaskReport) }
Check 'restored task-input evidence verifies offline' 0 (Invoke-Tada @('verify','--run',$runId))
$report = [pscustomobject]@{ workspace=$workspace; cliSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $Cli).Hash; checks=@($checks) }
$reportPath = Join-Path $workspace 'validation.json'
[IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
Write-Host "Evidence retained: $reportPath"
exit 0
