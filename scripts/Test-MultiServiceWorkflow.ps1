param([string]$Dotnet = 'dotnet')

$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
[xml]$properties = Get-Content -LiteralPath (Join-Path $repository 'Directory.Build.props') -Raw
$version = [string]$properties.Project.PropertyGroup.Version
$workspace = Join-Path $repository ('.artifacts/multi-service/' + [guid]::NewGuid().ToString('N'))
$sample = Join-Path $repository 'examples/MultiService'
New-Item -ItemType Directory -Path $workspace -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $sample -Recurse -File) {
    $relative = [IO.Path]::GetRelativePath($sample, $file.FullName)
    if ($relative -match '(^|[\\/])(bin|obj|\.tada)([\\/]|$)') { continue }
    $destination = Join-Path $workspace $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
$previousHome = $env:DOTNET_CLI_HOME
$previousCache = $env:NUGET_PACKAGES
$previousPathSetup = $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$previousCertificate = $env:DOTNET_GENERATE_ASPNET_CERTIFICATE
$env:DOTNET_CLI_HOME = Join-Path $workspace 'cli-home'
$env:NUGET_PACKAGES = Join-Path $workspace 'package-cache'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$checks = [Collections.Generic.List[object]]::new()
Push-Location -LiteralPath $workspace
try {
    & $Dotnet new tool-manifest | Out-Host
    if ($LASTEXITCODE) { throw 'Cannot create the local tool manifest.' }
    $feed = [Security.SecurityElement]::Escape((Join-Path $repository '.artifacts/packages'))
    [IO.File]::WriteAllText((Join-Path $workspace 'NuGet.Local.Config'), '<configuration><packageSources><clear/><add key="local" value="' + $feed + '"/></packageSources></configuration>')
    & $Dotnet tool install Tada.Cli --version $version --configfile NuGet.Local.Config | Out-Host
    if ($LASTEXITCODE) { throw 'Cannot install the packaged verifier.' }
    function Tada([string[]]$Arguments, [int]$ExpectedExit = 0) {
        $output = & $Dotnet tool run tada @Arguments --json
        $code = $LASTEXITCODE
        if ($code -ne $ExpectedExit) { throw "Expected exit $ExpectedExit, received $code for $($Arguments -join ' '): $($output -join [Environment]::NewLine)" }
        $result = ($output -join [Environment]::NewLine) | ConvertFrom-Json
        $checks.Add([pscustomobject]@{ arguments=$Arguments; exit=$code; result=$result })
        return $result
    }
    foreach ($project in @('Acceptance/Journey.Acceptance.csproj','ServiceTests/Service.Tests.csproj','ContractTests/Contract.Tests.csproj')) {
        & $Dotnet restore $project --locked-mode -p:NuGetAudit=false --disable-build-servers | Out-Host
        if ($LASTEXITCODE) { throw "Locked restore failed for $project." }
    }
    foreach ($configuration in @('Debug','Release')) {
        & $Dotnet test --project Acceptance/Journey.Acceptance.csproj -c $configuration --no-restore --no-ansi | Out-Host
        if ($LASTEXITCODE) { throw "Native $configuration journey execution failed." }
    }
    $baseline = (Tada @('baseline','prepare')).baseline
    $review = Tada @('baseline','review','--baseline',$baseline)
    Tada @('baseline','approve','--review',$review.review.id,'--by','isolated multi-service control; not requester business acceptance') | Out-Null
    $configuration = Get-Content -LiteralPath tada.json -Raw | ConvertFrom-Json
    function Engineering([string]$Folder, [string]$Assembly, [string[]]$RequiredTests) {
        $selected = $configuration | ConvertTo-Json -Depth 20 | ConvertFrom-Json
        $selected.oracleRoots = @($Folder,'Support')
        $selected.execution.project = "$Folder/$Assembly.csproj"
        $selected.execution.assembly = "$Assembly.dll"
        $selected.execution.messagesFile = 'supporting.trx'
        return @{ configuration=$selected; requiredTests=$RequiredTests }
    }
    $plan = [ordered]@{
        schemaVersion=1
        objective='Verify asynchronous ordering and its supporting boundaries.'
        categories=@('feat')
        businessChange=$true
        impact=@('Orders','Payments','Inventory','PaymentOutcome v1')
        checks=@(
            @{ id='services'; level='service'; purpose='supporting'; obligations=@('Reservation and payment idempotency'); rationale='Exercise service state through public HTTP outcomes.'; engineering=(Engineering 'ServiceTests' 'Service.Tests' @('ReservationAndReleaseAreIdempotent','RepeatedPaymentCreatesOneCharge')) },
            @{ id='contracts'; level='contract'; purpose='supporting'; obligations=@('PaymentOutcome producer/consumer compatibility'); rationale='Check actual producer output and consumer acceptance against independent contract fixtures.'; engineering=(Engineering 'ContractTests' 'Contract.Tests' @('ProducerOutcomeIsAcceptedByTheConsumer','ConsumerRejectsUnsupportedOutcomeVersion')) },
            @{ id='journey'; level='journey'; purpose='acceptance'; obligations=@('REQ_ORDER_JOURNEY'); rationale='Verify completion, compensation and repeated delivery across the three services.'; baseline=$baseline }
        )
        omissions=@('No separate unit suite is selected for this integration control. Service rules are exercised through HTTP.')
        timeoutSeconds=900
    }
    New-Item -ItemType Directory -Path .tada -Force | Out-Null
    function SavePlan { [IO.File]::WriteAllText((Join-Path $workspace '.tada/verification-plan.json'), ($plan | ConvertTo-Json -Depth 30)) }
    SavePlan
    $prepared = Tada @('plan','prepare','--file','.tada/verification-plan.json')
    $passing = Tada @('run','--plan',$prepared.plan,'--dotnet',$Dotnet)
    $verified = Tada @('verify','--run',$passing.runId)
    if ($verified.outcome -ne 'PASS' -or $verified.checks.Count -ne 3) { throw 'The coordinated three-level run did not pass.' }
    foreach ($check in $verified.checks) {
        if ($check.result.outcome -ne 'PASS') { throw "Required check $($check.id) did not pass." }
    }
    # Cached projections are replaceable. Verification must read the retained native reports.
    $serviceRun = ($passing.checks | Where-Object id -eq 'services').result.runId
    $report = Join-Path $workspace ".tada/runs/$serviceRun/harness/supporting.trx"
    $originalReport = [IO.File]::ReadAllBytes($report)
    try {
        [IO.File]::AppendAllText($report, '<tampered/>')
        $tampered = Tada -Arguments @('verify','--run',$passing.runId) -ExpectedExit 2
        if ($tampered.outcome -ne 'INDETERMINATE') { throw 'Tampered supporting evidence was accepted.' }
    }
    finally { [IO.File]::WriteAllBytes($report, $originalReport) }
    # A real implementation defect must fail supporting tests and the business journey.
    $implementation = Join-Path $workspace 'Services/ServiceHost.cs'
    $originalImplementation = [IO.File]::ReadAllBytes($implementation)
    try {
        $source = [Text.Encoding]::UTF8.GetString($originalImplementation)
        [IO.File]::WriteAllText($implementation, $source.Replace('UPDATE Stock SET Available=Available+1 WHERE Id=1', 'UPDATE Stock SET Available=Available+0 WHERE Id=1'))
        $failed = Tada -Arguments @('run','--plan',$prepared.plan,'--dotnet',$Dotnet) -ExpectedExit 1
        if ($failed.outcome -ne 'FAIL' -or !($failed.checks | Where-Object { $_.purpose -eq 'supporting' -and $_.result.outcome -eq 'FAIL' })) {
            throw 'The real compensation defect did not fail a supporting check.'
        }
        if (!(($failed.checks | Where-Object id -eq 'journey').result.reasons | Where-Object code -eq 'requirement-failure')) {
            throw 'The compensation defect did not establish behavioural RED.'
        }
    }
    finally { [IO.File]::WriteAllBytes($implementation, $originalImplementation) }
    $restored = Tada @('run','--plan',$prepared.plan,'--dotnet',$Dotnet)
    if ($restored.outcome -ne 'PASS') { throw 'Restored implementation did not pass.' }
    # A revised selection must retain its predecessor and explicitly account for removals.
    $plan.previous = $prepared.plan
    $plan.changeReason = 'The remaining change is a documentation heading correction.'
    $plan.categories = @('chore')
    $plan.businessChange = $false
    $plan.checks = @()
    $plan.noChecksReason = 'Only a documentation heading changes. Review the diff.'
    SavePlan
    $empty = Tada @('plan','prepare','--file','.tada/verification-plan.json')
    if ($empty.removedChecks.Count -ne 3) { throw 'Plan revision concealed removed obligations.' }
    $noChecks = Tada @('run','--plan',$empty.plan)
    if ($noChecks.outcome -ne 'NO_CHECKS') { throw 'Empty selection claimed a passing test result.' }
    [ordered]@{
        version=$version; workspace=$workspace; baseline=$baseline; plan=$prepared.plan
        passingRun=$passing.runId; failedRun=$failed.runId; restoredRun=$restored.runId
        checks=$checks; requesterAcceptance='pending'
    } | ConvertTo-Json -Depth 35 | Set-Content -LiteralPath (Join-Path $workspace 'validation.json') -Encoding utf8
    Write-Host "Multi-service validation saved at $workspace/validation.json"
}
finally {
    Pop-Location
    $env:DOTNET_CLI_HOME = $previousHome
    $env:NUGET_PACKAGES = $previousCache
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = $previousPathSetup
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = $previousCertificate
}
