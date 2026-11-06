$ErrorActionPreference = 'Stop'
$probeRoot = $PSScriptRoot
function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
$summaries = @()
foreach ($kind in @('positive','negative')) {
    $runDirectories = @(Get-ChildItem -LiteralPath (Join-Path $probeRoot 'evidence') -Directory | Where-Object Name -Like "$kind-*")
    Require ($runDirectories.Count -eq 1) "Expected one retained $kind control run, without silent reruns."
    $runDirectory = $runDirectories[0].FullName
    $command = Get-Content -LiteralPath (Join-Path $runDirectory 'command.json') -Raw | ConvertFrom-Json
    $nativePath = Join-Path $runDirectory 'native-messages.ndjson'
    $messages = @(Get-Content -LiteralPath $nativePath | ForEach-Object { $_ | ConvertFrom-Json })
    $events = @(Get-Content -LiteralPath (Join-Path $runDirectory 'probe-events.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
    $pickles = @($messages | Where-Object pickle | ForEach-Object pickle)
    $cases = @($messages | Where-Object testCase | ForEach-Object testCase)
    $starts = @($messages | Where-Object testCaseStarted | ForEach-Object testCaseStarted)
    $finishes = @($messages | Where-Object testCaseFinished | ForEach-Object testCaseFinished)
    $stepStarts = @($messages | Where-Object testStepStarted | ForEach-Object testStepStarted)
    $stepFinishes = @($messages | Where-Object testStepFinished | ForEach-Object testStepFinished)
    $runStarts = @($messages | Where-Object testRunStarted | ForEach-Object testRunStarted)
    $runFinishes = @($messages | Where-Object testRunFinished | ForEach-Object testRunFinished)
    $sources = @($messages | Where-Object source | ForEach-Object source)
    $documents = @($messages | Where-Object gherkinDocument | ForEach-Object gherkinDocument)
    Require ($sources.Count -eq 1 -and $documents.Count -eq 1) 'Expected source and Gherkin document evidence.'
    Require ($sources[0].uri -eq $documents[0].uri) 'Source/document URI mismatch.'
    Require ($pickles.Count -eq 2 -and $cases.Count -eq 2 -and $starts.Count -eq 2 -and $finishes.Count -eq 2) 'Required technical-case accounting is incomplete.'
    Require (($pickles.name | Sort-Object) -join '|' -eq 'Exercise SQLite connection roles|Observe isolated execution context') 'Unexpected technical-case inventory.'
    foreach ($case in $cases) { Require ($case.pickleId -in $pickles.id) 'Case/pickle link missing.' }
    foreach ($start in $starts) {
        Require ($start.testCaseId -in $cases.id) 'Case-start link missing.'
        Require ($start.attempt -eq 0) 'Unexpected retry attempt.'
    }
    foreach ($finish in $finishes) {
        Require ($finish.testCaseStartedId -in $starts.id) 'Case-finish link missing.'
        Require (-not $finish.willBeRetried) 'Unexpected planned retry.'
    }
    Require ($stepStarts.Count -eq $stepFinishes.Count) 'Step lifecycle incomplete.'
    foreach ($finish in $stepFinishes) {
        $matchingStart = @($stepStarts | Where-Object { $_.testCaseStartedId -eq $finish.testCaseStartedId -and $_.testStepId -eq $finish.testStepId })
        Require ($matchingStart.Count -eq 1) 'Step result lacks one matching start.'
        $caseStart = $starts | Where-Object id -EQ $finish.testCaseStartedId
        $case = $cases | Where-Object id -EQ $caseStart.testCaseId
        Require ($finish.testStepId -in $case.testSteps.id) 'Step result does not belong to its test case.'
    }
    Require ($runStarts.Count -eq 1 -and $runFinishes.Count -eq 1) 'Run lifecycle incomplete.'
    Require ($runFinishes[0].testRunStartedId -eq $runStarts[0].id) 'Run-finish link missing.'
    $failedSteps = @($stepFinishes | Where-Object { $_.testStepResult.status -eq 'FAILED' })
    $passedSteps = @($stepFinishes | Where-Object { $_.testStepResult.status -eq 'PASSED' })
    Require ($failedSteps.Count + $passedSteps.Count -eq $stepFinishes.Count) 'Skipped or unavailable technical steps found.'
    if ($kind -eq 'positive') {
        Require ($command.exitCode -eq 0 -and $runFinishes[0].success -and $failedSteps.Count -eq 0) 'Positive control did not satisfy its expected result.'
    } else {
        Require ($command.exitCode -eq 2 -and -not $runFinishes[0].success -and $failedSteps.Count -eq 1) 'Negative control did not produce its expected failure.'
        Require ($failedSteps[0].testStepResult.message -match 'PROBE_EXPECTED_ASSERTION_FAILURE') 'Negative control failed for the wrong reason.'
    }
    $before = @($events | Where-Object kind -EQ 'before')
    $after = @($events | Where-Object kind -EQ 'after')
    Require ($before.Count -eq 2 -and $after.Count -eq 2) 'Scenario hooks incomplete.'
    Require (@($before.contextId | Sort-Object -Unique).Count -eq 2) 'Scenario contexts were reused.'
    Require (@($before | Where-Object { $_.data.active -ne 1 }).Count -eq 0) 'Scenarios overlapped.'
    Require (@($after | Where-Object { $_.data.active -ne 0 -or @($_.data.cleanupErrors).Count -ne 0 }).Count -eq 0) 'Hook teardown did not finish cleanly.'
    $lifecycle = @($events | Where-Object { $_.kind -in @('before','after') })
    Require (($lifecycle.kind -join '|') -eq 'before|after|before|after') 'Unexpected scenario lifecycle interleaving.'
    $missing = @($events | Where-Object kind -EQ 'missing-file-rejected')
    Require ($missing.Count -eq 2 -and @($missing | Where-Object { $_.data.SqliteErrorCode -ne 14 }).Count -eq 0) 'Missing-file controls incomplete.'
    Require (@($events | Where-Object { $_.kind -eq 'read-only-write-rejected' -and $_.data.SqliteErrorCode -eq 8 }).Count -eq 1) 'Read-only write control missing.'
    Require (@($events | Where-Object { $_.kind -eq 'provider-complete' -and $_.data.databaseDeleted -and -not $_.data.pooling }).Count -eq 1) 'Provider cleanup control missing.'
    $runtime = ($events | Where-Object kind -EQ 'runtime').data
    $runtime | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runDirectory 'runtime-identity.json')
    $meta = ($messages | Where-Object meta).meta
    $summaries += [ordered]@{
        kind = $kind; runDirectory = Split-Path $runDirectory -Leaf; nativeRunId = $runStarts[0].id
        executableExitCode = $command.exitCode; nativeSuccess = $runFinishes[0].success
        discoveredCaseCount = $cases.Count; caseAttempts = $starts.attempt
        passedSteps = $passedSteps.Count; failedSteps = $failedSteps.Count
        messageCount = $messages.Count; messageProtocol = $meta.protocolVersion
        reqnrollVersion = $meta.implementation.version; runtime = $runtime.runtime
        sqliteVersion = ($events | Where-Object kind -EQ 'sqlite-version').data.version
        nativeSqlite = @($runtime.native | Where-Object name -EQ 'e_sqlite3.DLL')
        independentContexts = $before.contextId; lifecycle = $lifecycle.kind
        cleanupErrors = @($after | ForEach-Object { $_.data.cleanupErrors })
        nativeMessagesSHA256 = Hash $nativePath
    }
}
$lockPath = Join-Path $probeRoot 'packages.lock.json'
$lockBefore = Get-Content -LiteralPath (Join-Path $probeRoot 'evidence/candidate-lock-hash-before.json') -Raw | ConvertFrom-Json
Require ((Hash $lockPath) -eq $lockBefore.Hash) 'Frozen candidate lock graph changed.'
$lock = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$packageRows = @($lock.dependencies.'net10.0'.PSObject.Properties | ForEach-Object {
    [ordered]@{ id = $_.Name; version = $_.Value.resolved; kind = $_.Value.type; contentHash = $_.Value.contentHash }
})
$sdkPackage = Join-Path $probeRoot '.packages/mstest.sdk/4.5.2/mstest.sdk.4.5.2.nupkg'
$inventory = [ordered]@{
    targetFramework = 'net10.0'; sdk = '10.0.403'; candidateLockSHA256 = Hash $lockPath
    projectSdk = @{ id = 'MSTest.Sdk'; version = '4.5.2'; packageSHA512 = (Get-FileHash -LiteralPath $sdkPackage -Algorithm SHA512).Hash }
    packages = $packageRows
}
$inventory | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $probeRoot 'evidence/dependency-inventory.json')
$importCapture = Get-ChildItem -LiteralPath (Join-Path $probeRoot 'evidence') -Recurse -Filter 'project-preprocessed.xml' | Select-Object -First 1
Require ($null -ne $importCapture) 'Preprocessed SDK/import capture missing.'
$importText = Get-Content -LiteralPath $importCapture.FullName -Raw
$inputPaths = @($importText -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^[A-Z]:[\\/]' -and $_ -match '\.(props|targets|csproj)$' } | Sort-Object -Unique | Where-Object { Test-Path -LiteralPath $_ })
$importRows = @($inputPaths | ForEach-Object { @{ path = $_; sha256 = Hash $_ } })
Require ($importRows.Count -gt 0) 'No imported input paths were captured.'
$importRows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $probeRoot 'evidence/import-inventory.json')
$summaries | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $probeRoot 'evidence/control-summary.json')
[pscustomobject]@{ ControlsValidated = $summaries.Count; Packages = $packageRows.Count; ImportedFilesCaptured = $importRows.Count; FrozenLockUnchanged = $true } | ConvertTo-Json
