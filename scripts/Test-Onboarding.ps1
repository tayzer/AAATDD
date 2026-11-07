param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
[xml]$buildProperties = Get-Content -Raw (Join-Path $repository 'Directory.Build.props')
$version = [string]$buildProperties.Project.PropertyGroup.Version
$workspace = Join-Path $repository ('.artifacts/onboarding/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workspace -Force | Out-Null
$feed = Join-Path $repository '.artifacts/packages'
$config = Join-Path $workspace 'NuGet.Local.Config'
[IO.File]::WriteAllText($config, '<configuration><packageSources><clear/><add key="local" value="'+[Security.SecurityElement]::Escape($feed)+'"/></packageSources></configuration>')
$previousCache = $env:NUGET_PACKAGES
$previousCliHome = $env:DOTNET_CLI_HOME
$previousGlobalToolPath = $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$previousCertificate = $env:DOTNET_GENERATE_ASPNET_CERTIFICATE
$checks = [Collections.Generic.List[object]]::new()
Push-Location $workspace
try {
    $env:NUGET_PACKAGES = Join-Path $workspace 'tool-cache'
    # Local-tool resolver entries are keyed by package/version, not package-cache root.
    # A reused CLI home can execute an earlier development build of the same version.
    $env:DOTNET_CLI_HOME = Join-Path $workspace 'dotnet-home'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    & $Dotnet new tool-manifest | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Tool manifest creation failed.' }
    & $Dotnet tool install Tada.Cli --version $version --configfile $config | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Package installation failed.' }
    # Keep this fresh cache for tool and harness to exercise actual package acquisition.
    function Tada([string[]]$Arguments, [int]$ExpectedExit = 0) {
        $output = & $Dotnet tool run tada @Arguments --json
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne $ExpectedExit) { throw "Installed tool expected exit $ExpectedExit, got $exitCode : $($output -join "`n")" }
        $result = ($output -join "`n") | ConvertFrom-Json
        $checks.Add([pscustomobject]@{ operation=$Arguments[0]; expectedExit=$ExpectedExit; result=$result })
        return $result
    }
    Tada @('init') | Out-Null
    $hooks=Join-Path $workspace '.tada/git-control-hooks'
    New-Item -ItemType Directory -Path $hooks -Force | Out-Null
    function ControlGit([string[]]$Arguments) {
        $output = & git "--git-dir=$workspace/.git" "--work-tree=$workspace" -c core.longpaths=true -c "core.hooksPath=$hooks" -c commit.gpgsign=false -c 'user.name=TADA integration control' -c 'user.email=tada-control@example.invalid' @Arguments
        if ($LASTEXITCODE) { throw "Local control Git operation failed: $($output -join "`n")" }
        return $output
    }
    ControlGit @('init','--quiet','--initial-branch=main',"--template=$hooks") | Out-Host
    Copy-Item -LiteralPath (Join-Path $repository 'examples/Echo/echo.feature') -Destination features/echo.feature
    Copy-Item -LiteralPath (Join-Path $repository 'examples/Echo/EchoBindings.cs') -Destination tests/Acceptance/Bindings.cs
    New-Item -ItemType Directory -Path Api -Force | Out-Null
    # New API starting state: transport is available, but the business endpoint is not implemented.
    [IO.File]::WriteAllText((Join-Path $workspace 'Api/Program.cs'), 'var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.MapPost("/echo", () => Results.StatusCode(501)); app.Run();', [Text.UTF8Encoding]::new($false))
    Copy-Item -LiteralPath (Join-Path $repository 'examples/Echo/Api/Echo.Api.csproj') -Destination Api/Echo.Api.csproj
    # A separate implementation library exercises the evaluated ProjectReference graph without
    # sharing business-rule code with the acceptance harness.
    New-Item -ItemType Directory -Path Domain -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $workspace 'Domain/Echo.Domain.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><RestorePackagesWithLockFile>true</RestorePackagesWithLockFile></PropertyGroup></Project>')
    [IO.File]::WriteAllText((Join-Path $workspace 'Domain/EchoText.cs'), 'public static class EchoText { public static string Keep(string text) => text; }')
    $apiProject = [IO.File]::ReadAllText((Join-Path $workspace 'Api/Echo.Api.csproj')).Replace('</Project>', '<ItemGroup><ProjectReference Include="../Domain/Echo.Domain.csproj" /></ItemGroup></Project>')
    [IO.File]::WriteAllText((Join-Path $workspace 'Api/Echo.Api.csproj'), $apiProject)
    $tadaConfig = Get-Content -Raw tada.json | ConvertFrom-Json
    $tadaConfig.execution | Add-Member -NotePropertyName sutProject -NotePropertyValue 'Api/Echo.Api.csproj' -Force
    $tadaConfig.execution | Add-Member -NotePropertyName sutAssembly -NotePropertyValue 'Echo.Api.dll' -Force
    $tadaConfig | Add-Member -NotePropertyName implementationRoots -NotePropertyValue @('Api','Domain') -Force
    [IO.File]::WriteAllText((Join-Path $workspace 'tada.json'), ($tadaConfig | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    & $Dotnet restore tests/Acceptance/Acceptance.csproj -p:NuGetAudit=false --ignore-failed-sources | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Harness restore failed.' }
    & $Dotnet restore Api/Echo.Api.csproj -p:NuGetAudit=false --ignore-failed-sources | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'API restore failed.' }
    $baseline = (Tada @('baseline','prepare')).baseline
    $review = Tada @('baseline','review','--baseline',$baseline)
    Tada @('baseline','approve','--review',$review.review.id,'--by','automated onboarding control; not human acceptance') | Out-Null
    $red = Tada -Arguments @('run','--baseline',$baseline,'--dotnet',$Dotnet) -ExpectedExit 1
    if ($red.outcome -ne 'FAIL' -or !($red.reasons | Where-Object code -eq 'requirement-failure')) { throw 'Starting RED was not a meaningful business assertion failure.' }
    Copy-Item -LiteralPath (Join-Path $repository 'examples/Echo/Api/Program.cs') -Destination Api/Program.cs
    $greenProgram = [IO.File]::ReadAllText((Join-Path $workspace 'Api/Program.cs')).Replace('message = request.Message', 'message = EchoText.Keep(request.Message)')
    [IO.File]::WriteAllText((Join-Path $workspace 'Api/Program.cs'), $greenProgram, [Text.UTF8Encoding]::new($false))
    $run = Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    $verified = Tada @('verify','--run',$run.runId)
    if ($verified.outcome -ne 'PASS' -or $verified.passedCases.Count -ne 2) { throw 'Second project did not establish both cases.' }
    # Behaviour-preserving refactor: extract the response function, keeping the oracle unchanged.
    $refactoredProgram = @'
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapPost("/echo", Echo);
app.Run();
static IResult Echo(EchoRequest request) => string.IsNullOrWhiteSpace(request.Message)
    ? Results.BadRequest(new { error = "message_required" })
    : Results.Ok(new { message = EchoText.Keep(request.Message) });
public sealed record EchoRequest(string Message);
'@
    [IO.File]::WriteAllText((Join-Path $workspace 'Api/Program.cs'), $refactoredProgram, [Text.UTF8Encoding]::new($false))
    $refactorRun = Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    if ($refactorRun.outcome -ne 'PASS' -or $refactorRun.passedCases.Count -ne 2) { throw 'Refactor did not preserve the approved behaviour.' }
    function RunRecord($Result) { Get-Content -LiteralPath (Join-Path $workspace ".tada/runs/$($Result.runId)/run.json") -Raw | ConvertFrom-Json }
    $refactorRecord = RunRecord $refactorRun
    $repeat = Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    $repeatRecord = RunRecord $repeat
    if ($repeatRecord.implementation -ne $refactorRecord.implementation -or $repeatRecord.environment -ne $refactorRecord.environment) { throw 'Unchanged rerun manufactured a new implementation/environment identity.' }

    # Isolated implementation fault: mutable runtime state exposes a failure once, then lets
    # native assertions pass without changing the code. TADA must preserve the earlier failure.
    $historyMarker = Join-Path $workspace '.tada/history-fault-control'
    $programPath = Join-Path $workspace 'Api/Program.cs'
    $originalProgram = [IO.File]::ReadAllBytes($programPath)
    try {
        $faultyProgram = $refactoredProgram.Replace('static IResult Echo(EchoRequest request) =>', 'static IResult Echo(EchoRequest request) => File.Exists(Path.Combine(Environment.GetEnvironmentVariable("TADA_REPOSITORY")!, ".tada", "history-fault-control")) ? Results.StatusCode(501) :')
        if ($faultyProgram -eq $refactoredProgram) { throw 'History control did not modify its intended endpoint.' }
        [IO.File]::WriteAllText($programPath, $faultyProgram, [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText($historyMarker, 'automated failure-history control; not a business expectation')
        $historyFailure = Tada -Arguments @('run','--baseline',$baseline,'--dotnet',$Dotnet) -ExpectedExit 1
        if (!($historyFailure.reasons | Where-Object code -eq 'requirement-failure')) { throw 'History control did not establish an actual business assertion failure.' }
        [IO.File]::Delete($historyMarker)
        $historyRepeat = Tada -Arguments @('run','--baseline',$baseline,'--dotnet',$Dotnet) -ExpectedExit 1
        $failedRecord = RunRecord $historyFailure
        $rerunRecord = RunRecord $historyRepeat
        if ($failedRecord.implementation -ne $rerunRecord.implementation -or $failedRecord.environment -ne $rerunRecord.environment -or $rerunRecord.commands[-1].exitCode -ne 0 -or !($historyRepeat.reasons | Where-Object code -eq 'unresolved-earlier-failure')) { throw 'A native passing rerun discarded a real earlier failure or changed its assessed identities.' }
    } finally {
        [IO.File]::WriteAllBytes($programPath, $originalProgram)
        if (Test-Path -LiteralPath $historyMarker) { [IO.File]::Delete($historyMarker) }
    }
    $restored = Tada @('run','--baseline',$baseline,'--dotnet',$Dotnet)
    $restoredRecord = RunRecord $restored
    if ($restoredRecord.implementation -ne $refactorRecord.implementation -or $restoredRecord.environment -ne $refactorRecord.environment) { throw 'Restored implementation did not recover its identified source/environment.' }
    $installedLogger = Join-Path $env:NUGET_PACKAGES "tada.cli/$version/tools/net10.0/any/Tada.BuildLogger.dll"
    $loggerDigest = (Get-FileHash -LiteralPath $installedLogger -Algorithm SHA256).Hash
    foreach ($executed in @($red, $run, $refactorRun, $repeat, $historyFailure, $historyRepeat, $restored)) {
        $capturedLogger = Join-Path $workspace ".tada/runs/$($executed.runId)/observer/Tada.BuildLogger.dll"
        if ((Get-FileHash -LiteralPath $capturedLogger -Algorithm SHA256).Hash -ne $loggerDigest) { throw 'Executed logger differs from the freshly installed package.' }
    }
    # A documentation chore uses its appropriate check rather than a new business feature.
    $context = "# Current handoff`n`nGoal: echo API demonstration. Acceptance: features/echo.feature. Baseline: $baseline. Starting RED: $($red.runId), HTTP 501 detected by outcome assertions. Implemented endpoint GREEN: $($run.runId). Behaviour-preserving refactor GREEN: $($refactorRun.runId), current implementation passes both cases under the same approved oracle. Approvals are automated demo controls, not requester business acceptance. Next: requester acceptance and residual release gaps.`n"
    [IO.File]::WriteAllText((Join-Path $workspace '.tada/context.md'), $context, [Text.UTF8Encoding]::new($false))
    & $Dotnet build Api/Echo.Api.csproj --no-restore --disable-build-servers --warnaserror | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Maintenance build check failed.' }
    if (!(Select-String -LiteralPath .tada/context.md -SimpleMatch $run.runId)) { throw 'Context handoff lost run provenance.' }
    $manifest = if (Test-Path dotnet-tools.json) { 'dotnet-tools.json' } else { '.config/dotnet-tools.json' }
    ControlGit @('add','--','.gitattributes','.gitignore','Directory.Build.props','Directory.Build.targets','global.json','NuGet.Config','tada.json',$manifest,'Api','Domain','features','tests/Acceptance','.tada/context.md','.tada/WORKFLOW.md','.tada/.gitattributes','.tada/baselines','.tada/snapshots','.tada/reviews','.tada/approvals') | Out-Host
    ControlGit @('commit','--quiet','-m','test: preserve API workflow handoff') | Out-Host
    $contextCommit = (ControlGit @('rev-parse','HEAD') -join '').Trim()
    $persistedContext = (ControlGit @('show','HEAD:.tada/context.md')) -join "`n"
    if (!$persistedContext.Contains($red.runId) -or !$persistedContext.Contains($run.runId) -or !$persistedContext.Contains($refactorRun.runId)) { throw 'Git context did not retain RED/GREEN/refactor provenance.' }
    $report = [pscustomobject]@{ workspace=$workspace; packageSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $feed "Tada.Cli.$version.nupkg")).Hash; checks=@($checks); developmentRuns=@($red.runId,$run.runId,$refactorRun.runId); unchangedRerun=@{before=$refactorRun.runId;after=$repeat.runId;sameImplementation=$true;sameEnvironment=$true}; failureHistory=@{failure=$historyFailure.runId;nativePassingRerun=$historyRepeat.runId;nativeRerunExit=0;tadaRerunOutcome=$historyRepeat.outcome;restoredImplementation=$restored.runId}; maintenance='Behaviour-preserving refactor and warn-as-error API build passed without new business scenarios'; contextCommit=$contextCommit; context='handoff committed and read back through Git with RED/GREEN/refactor run identities'; limitation='Fresh package cache and new project on this host; not a clean-machine or requester/Codex acceptance test.' }
    [IO.File]::WriteAllText((Join-Path $workspace 'validation.json'), ($report | ConvertTo-Json -Depth 25), [Text.UTF8Encoding]::new($false))
    Write-Host "Installed-package onboarding evidence: $workspace/validation.json"
} finally { $env:NUGET_PACKAGES=$previousCache; $env:DOTNET_CLI_HOME=$previousCliHome; $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=$previousGlobalToolPath; $env:DOTNET_CLI_TELEMETRY_OPTOUT=$previousTelemetry; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE=$previousCertificate; Pop-Location }
exit 0
