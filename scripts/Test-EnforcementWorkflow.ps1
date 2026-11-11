param(
    [string]$Dotnet = 'dotnet',
    [string]$Cli = (Join-Path $PSScriptRoot '../src/Tada.Cli/bin/Release/net10.0/Tada.Cli.dll')
)
# End-to-end lifecycle controls for approved candidates, active merge obligations and protected changes.
# Runs real HTTP/SQLite Orders scenarios in a disposable Git repository. Every approval below is an
# explicitly labelled automated control; none represents requester or business acceptance.
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repository 'examples/Orders'
$workspace = Join-Path $repository ('.artifacts/enforcement-workflow/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workspace -Force | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File -Force) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
    if (($relative -split '[/\\]') | Where-Object { $_ -in @('bin','obj','.tada') }) { continue }
    $target = Join-Path $workspace $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
$checks = [Collections.Generic.List[object]]::new()
$controlApprover = 'automated enforcement control; not human acceptance'
$utf8 = [Text.UTF8Encoding]::new($false)
$today = [DateTime]::UtcNow.ToString('yyyy-MM-dd')

function Invoke-Tada([string[]]$Arguments) {
    $output = & $Dotnet $Cli @Arguments --repo $workspace --json
    $code = $LASTEXITCODE
    $parsed = ($output -join "`n") | ConvertFrom-Json
    return @{ Code = $code; Result = $parsed }
}
function Check([string]$Name, [int]$Expected, $Actual) {
    if ($Actual.Code -ne $Expected) { throw "$Name expected exit $Expected, got $($Actual.Code): $($Actual.Result | ConvertTo-Json -Depth 8)" }
    $checks.Add([pscustomobject]@{ name=$Name; exitCode=$Actual.Code; result=$Actual.Result })
    Write-Host "$Name passed (exit $Expected)"
}
function Assert-That([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Invoke-Git([string[]]$Arguments) {
    $output = & git.exe -C $workspace -c core.autocrlf=false -c commit.gpgsign=false -c core.hooksPath=NUL -c user.name=tada-control -c user.email=control@invalid @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed: $output" }
    return $output
}
function Commit([string]$Message) { Invoke-Git @('add','-A') | Out-Null; Invoke-Git @('commit','-q','-m',$Message) | Out-Null }
function Merge([string]$Branch) { Invoke-Git @('checkout','-q','main') | Out-Null; Invoke-Git @('merge','-q','--ff-only',$Branch) | Out-Null }
function Edit([string]$Relative, [string]$Old, [string]$New) {
    $path = Join-Path $workspace $Relative
    $text = [IO.File]::ReadAllText($path)
    if (!$text.Contains($Old)) { throw "Control edit did not find its intended text in $Relative." }
    [IO.File]::WriteAllText($path, $text.Replace($Old, $New), $utf8)
}
function Approve-Control([string]$Name, [string]$Previous) {
    $prepared = Invoke-Tada @('baseline','prepare')
    Check "$Name prepare" 0 $prepared
    $baseline = $prepared.Result.baseline
    $reviewArguments = @('baseline','review','--baseline',$baseline)
    if ($Previous) { $reviewArguments += @('--against',$Previous) }
    $review = Invoke-Tada $reviewArguments
    Check "$Name review" 0 $review
    $approveArguments = @('baseline','approve','--review',$review.Result.review.id,'--by',$controlApprover)
    if ($Previous) { $approveArguments += @('--replaces',$Previous) }
    Check "$Name control approval" 0 (Invoke-Tada $approveArguments)
    return @{ Baseline = $baseline; Review = $review.Result }
}
function Write-Ledger([string]$Baseline, [string[]]$Active, [object[]]$Candidates, $Policy) {
    $ledger = [ordered]@{ schemaVersion = 1; baseline = $Baseline; active = @($Active); candidates = @($Candidates) }
    if ($Policy) { $ledger.policy = $Policy }
    New-Item -ItemType Directory -Path (Join-Path $workspace '.tada') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $workspace '.tada/enforcement.json'), ($ledger | ConvertTo-Json -Depth 5), $utf8)
}
function Candidate([string]$Scenario, [string]$Reason) { [ordered]@{ scenario = $Scenario; since = $today; reason = $Reason } }
function Run([string]$Name, [string]$Baseline, [int]$Expected) {
    $result = Invoke-Tada @('run','--baseline',$Baseline,'--dotnet',$Dotnet)
    Check $Name $Expected $result
    return $result.Result.runId
}
function Gate([string]$Name, [int]$Expected, [string]$RunId, [switch]$NoBase) {
    $arguments = @('gate')
    if ($RunId) { $arguments += @('--run',$RunId) }
    if (!$NoBase) { $arguments += @('--base','main') }
    $result = Invoke-Tada $arguments
    Check $Name $Expected $result
    return $result.Result
}
function State($Gate, [string]$Scenario) { ($Gate.obligations | Where-Object { $_.scenario -eq $Scenario }).state }

$active = @('AC_DEMO_STOCK_001_01','AC_DEMO_STOCK_001_02')
$limit = 'AC_DEMO_LIMIT_001_01'
$boundary = 'AC_DEMO_LIMIT_001_02'

# Main: approved Orders obligations are active. Contract and fitness inputs are declared protected obligations.
[IO.File]::WriteAllText((Join-Path $workspace '.gitattributes'), "* -text`n", $utf8)
[IO.File]::WriteAllText((Join-Path $workspace '.gitignore'), "**/bin/`n**/obj/`n.tada/runs/`n.tada/scratch/`n", $utf8)
New-Item -ItemType Directory -Path (Join-Path $workspace 'Acceptance/contracts'), (Join-Path $workspace 'Acceptance/fitness') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $workspace 'Acceptance/contracts/order-rejection.json'), "{`"status`":409,`"outcome`":`"rejected`",`"fields`":[`"requestId`",`"reason`"]}`n", $utf8)
[IO.File]::WriteAllText((Join-Path $workspace 'Acceptance/fitness/order-latency.json'), "{`"p95Milliseconds`":2000}`n", $utf8)
Edit 'tada.json' '"implementationRoots": ["Api"]' ('"implementationRoots": ["Api"],' + "`n" + '  "obligations": [{ "kind": "contract", "roots": ["Acceptance/contracts"] }, { "kind": "fitness", "roots": ["Acceptance/fitness"] }]')
Invoke-Git @('init','-q','-b','main') | Out-Null
$main = Approve-Control 'main obligations' $null
Write-Ledger $main.Baseline $active @() @{ candidateMaxDays = 30 }
$mainRun = Run 'main obligations pass' $main.Baseline 0
$gate = Gate 'main gate without base comparison' 0 $mainRun -NoBase
Assert-That ($gate.baseBaseline -eq 'not-compared') 'A gate without --base must say it did not compare.'
Commit 'main: approved Orders obligations'
$gate = Gate 'main gate against itself' 0 $mainRun
Assert-That ($gate.protectedChanges.Count -eq 0) 'Unchanged main must report no protected changes.'

# Chore: no execution evidence is never a behavioural PASS while active obligations exist.
$gate = Gate 'chore without execution is not PASS' 2 $null
Assert-That (@($gate.reasons | Where-Object code -eq 'not-executed').Count -eq 1) 'Unexecuted active obligations need an explicit reason.'

# Feature, stage 1: agreed Gherkin exists before bindings or production code (spec-only change).
Invoke-Git @('checkout','-q','-b','spec-limit') | Out-Null
[IO.File]::WriteAllText((Join-Path $workspace 'features/order-quantity-limit.feature'), @'
@REQ_DEMO_LIMIT_001
Feature: Order quantity limit
  Automated control specification for TADA lifecycle tests; not requester-approved behaviour.

  Rule: One order cannot request more than 5 units

    @AC_DEMO_LIMIT_001_01
    Scenario: Reject an order above the per-order limit
      Given product "ABC123" has 10 units available
      And the customer has no existing orders
      When the customer orders 6 units of "ABC123"
      Then the order is rejected for exceeding the quantity limit
      And no order is created for that request
      And product "ABC123" still has 10 units available
'@.Replace("`r`n", "`n"), $utf8)
$spec = Approve-Control 'specification amendment' $main.Baseline
Assert-That (@($spec.Review.protectedChanges | Where-Object { $_.kind -eq 'specification' -and $_.change -eq 'added' }).Count -eq 1) 'Review must identify the added specification.'
Write-Ledger $spec.Baseline $active @() @{ candidateMaxDays = 30 }
$specRun = Run 'spec-only run has pending scope' $spec.Baseline 2
$gate = Gate 'unclassified approved scenario blocks' 2 $specRun
Assert-That (@($gate.reasons | Where-Object { $_.code -eq 'unclassified-obligation' -and $_.caseId -eq $limit }).Count -eq 1) 'New scenario must need explicit enforcement status.'
Write-Ledger $spec.Baseline @('AC_DEMO_STOCK_001_01') @((Candidate 'AC_DEMO_STOCK_001_02' 'Attempted demotion control'), (Candidate $limit 'Agreed; bindings pending')) @{ candidateMaxDays = 30 }
$gate = Gate 'demoting unchanged active obligation blocks' 2 $specRun
Assert-That (@($gate.reasons | Where-Object { $_.code -eq 'active-obligation-demoted' -and $_.caseId -eq 'AC_DEMO_STOCK_001_02' }).Count -eq 1) 'Demotion must be rejected.'
Write-Ledger $spec.Baseline $active @((Candidate $limit 'Agreed; bindings pending')) @{ candidateMaxDays = 30 }
$gate = Gate 'spec-only candidate is visible but non-blocking' 0 $specRun
Assert-That ((State $gate $limit) -eq 'incomplete') 'Unbound candidate must be incomplete, not behavioural RED.'
Assert-That (@($gate.protectedChanges | Where-Object kind -eq 'specification').Count -eq 1) 'Spec PR must list its protected specification change.'
Commit 'spec: agree order quantity limit'
Merge 'spec-limit'

# Fresh-session recovery: committed records expose pending work without a run or special instructions.
$gate = Gate 'fresh session recovers pending candidate' 2 $null
Assert-That ((State $gate $limit) -eq 'not-executed' -and @($gate.next).Count -gt 0) 'Gate status must expose the pending candidate and next action.'

# Feature, stage 2: executable interpretation is reviewed; behavioural RED comes from the intended assertion.
Invoke-Git @('checkout','-q','-b','evidence-limit') | Out-Null
Edit 'Acceptance/OrderBindings.cs' '    [Then("the order is accepted")]' @'
    [Then("the order is rejected for exceeding the quantity limit")]
    public void RejectedForQuantityLimit()
    {
        Assert.AreEqual(HttpStatusCode.Conflict, response!.StatusCode);
        Assert.AreEqual("rejected", Text("outcome"));
        Assert.AreEqual("quantity_limit", Text("reason"));
        Assert.AreEqual(requestId, Text("requestId"));
    }

    [Then("the order is accepted")]
'@.Replace("`r`n", "`n")
$evidence = Approve-Control 'executable interpretation' $spec.Baseline
Assert-That (@($evidence.Review.protectedChanges | Where-Object { $_.kind -eq 'executable-interpretation' -and $_.subject -eq 'Acceptance/OrderBindings.cs' }).Count -eq 1) 'Binding change must be presented for review.'
Write-Ledger $evidence.Baseline $active @((Candidate $limit 'Agreed; bindings pending')) @{ candidateMaxDays = 30 }
$redRun = Run 'meaningful behavioural RED' $evidence.Baseline 1
$red = Invoke-Tada @('verify','--run',$redRun)
Assert-That (@($red.Result.reasons | Where-Object { $_.code -eq 'requirement-failure' -and $_.caseId -eq $limit }).Count -eq 1) 'RED must come from the intended outcome assertion.'
$gate = Gate 'RED candidate does not block active gate' 0 $redRun
Assert-That ((State $gate $limit) -eq 'behavioural-red') 'Candidate must report behavioural RED.'
Commit 'test: bind quantity-limit expectation'
Merge 'evidence-limit'

# Feature, stage 3: small implementation increment, then promotion before completion.
Invoke-Git @('checkout','-q','-b','implement-limit') | Out-Null
Edit 'Api/Program.cs' '        return Results.BadRequest();' @'
        return Results.BadRequest();
    if (request.Quantity > 5)
        return Results.Json(new { outcome = "rejected", reason = "quantity_limit", requestId = request.RequestId }, statusCode: 409);
'@.Replace("`r`n", "`n")
$greenRun = Run 'implementation GREEN' $evidence.Baseline 0
$gate = Gate 'passing candidate must be promoted' 2 $greenRun
Assert-That ((State $gate $limit) -eq 'satisfied-unpromoted') 'Passing candidate must be identified as stale.'
Write-Ledger $evidence.Baseline ($active + $limit) @() @{ candidateMaxDays = 30 }
$gate = Gate 'promoted scope is enforced' 0 $greenRun
Assert-That ($gate.protectedChanges.Count -eq 0) 'Promotion alone is not a protected obligation change.'
Commit 'feat: enforce per-order quantity limit'
Merge 'implement-limit'
$enforced = $active + $limit

# Protected-input weakening: an unapproved assertion edit cannot reach a passing gate.
Invoke-Git @('checkout','-q','-b','weaken') | Out-Null
Edit 'Acceptance/OrderBindings.cs' '        Assert.AreEqual("quantity_limit", Text("reason"));' '        // reason no longer checked'
$gate = Gate 'unapproved assertion weakening blocks' 2 $greenRun
Assert-That (@($gate.reasons | Where-Object code -eq 'oracle-changed').Count -eq 1) 'Weakened oracle must be detected.'
Invoke-Git @('checkout','-q','--','.') | Out-Null
Invoke-Git @('checkout','-q','main') | Out-Null

# Contract and fitness changes are protected obligation changes, not ordinary test edits.
Invoke-Git @('checkout','-q','-b','contract') | Out-Null
Edit 'Acceptance/contracts/order-rejection.json' '"status":409' '"status":422'
Edit 'Acceptance/fitness/order-latency.json' '2000' '20000'
New-Item -ItemType Directory -Path (Join-Path $workspace 'UnitTests') -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $workspace 'UnitTests/LimitTests.cs'), "// Supporting engineering test outside the approved oracle.`n", $utf8)
$gate = Gate 'unapproved contract/fitness change blocks' 2 $greenRun
Assert-That (@($gate.reasons | Where-Object code -eq 'oracle-changed').Count -eq 1) 'Contract/fitness edits must invalidate the approved oracle.'
$amended = Approve-Control 'contract and fitness amendment' $evidence.Baseline
foreach ($kind in @('contract','fitness')) {
    Assert-That (@($amended.Review.protectedChanges | Where-Object kind -eq $kind).Count -eq 1) "Review must classify the $kind change."
}
Assert-That (@($amended.Review.protectedChanges | Where-Object { $_.subject -like 'UnitTests/*' }).Count -eq 0) 'Supporting tests outside the oracle are not protected obligations.'
Write-Ledger $amended.Baseline $enforced @() @{ candidateMaxDays = 30 }
$contractRun = Run 'amended obligations run' $amended.Baseline 0
$gate = Gate 'approved contract/fitness change is flagged for independent review' 0 $contractRun
foreach ($kind in @('contract','fitness')) {
    Assert-That (@($gate.protectedChanges | Where-Object kind -eq $kind).Count -eq 1) "Gate must flag the $kind change against main."
}
Invoke-Git @('checkout','-q','-f','main') | Out-Null
Invoke-Git @('clean','-q','-fd') | Out-Null # Abandoned branch records never reached main.

# Enforcement-policy weakening is flagged; reset candidate history and unapproved lineage block.
Invoke-Git @('checkout','-q','-b','policy') | Out-Null
Write-Ledger $evidence.Baseline $enforced @() @{ candidateMaxDays = 365 }
$gate = Gate 'policy weakening is flagged' 0 $greenRun
Assert-That (@($gate.protectedChanges | Where-Object { $_.kind -eq 'enforcement-policy' -and $_.change -eq 'weakened' }).Count -eq 1) 'Policy weakening must be visible.'
Invoke-Git @('checkout','-q','-f','main') | Out-Null

# Business defect: a shipped off-by-one is reproduced by an agreed regression scenario, fixed and kept.
Edit 'Api/Program.cs' 'if (request.Quantity > 5)' 'if (request.Quantity >= 5)'
Commit 'defect: shipped off-by-one limit'
Invoke-Git @('checkout','-q','-b','fix-boundary') | Out-Null
Edit 'features/order-quantity-limit.feature' '      And product "ABC123" still has 10 units available' @'
      And product "ABC123" still has 10 units available

    @AC_DEMO_LIMIT_001_02
    Scenario: Accept an order at the per-order limit
      Given product "ABC123" has 10 units available
      And the customer has no existing orders
      When the customer orders 5 units of "ABC123"
      Then the order is accepted
      And exactly one order is created for 5 units of "ABC123"
      And product "ABC123" has 5 units available
'@.Replace("`r`n", "`n")
$regression = Approve-Control 'regression expectation' $evidence.Baseline
Write-Ledger $regression.Baseline $enforced @((Candidate $boundary 'Regression for shipped off-by-one defect')) @{ candidateMaxDays = 30 }
$defectRun = Run 'defect reproduced' $regression.Baseline 1
$gate = Gate 'regression RED is pending while existing obligations gate' 0 $defectRun
Assert-That ((State $gate $boundary) -eq 'behavioural-red') 'Defect must be reproduced by the intended assertion.'
Edit 'Api/Program.cs' 'if (request.Quantity >= 5)' 'if (request.Quantity > 5)'
$fixedRun = Run 'defect repaired' $regression.Baseline 0
Write-Ledger $regression.Baseline ($enforced + $boundary) @() @{ candidateMaxDays = 30 }
$gate = Gate 'regression protection enforced' 0 $fixedRun
Commit 'fix: accept orders at the quantity limit'
Merge 'fix-boundary'
$enforced = $enforced + $boundary

# Refactor: existing approved obligations are reused; no new scenario or approval is needed.
Invoke-Git @('checkout','-q','-b','refactor') | Out-Null
Edit 'Api/Program.cs' 'if (request.Quantity > 5)' 'if (request.Quantity > MaxUnitsPerOrder)'
[IO.File]::AppendAllText((Join-Path $workspace 'Api/Program.cs'), "partial class Program { const int MaxUnitsPerOrder = 5; }`n", $utf8)
$refactorRun = Run 'refactor preserves obligations' $regression.Baseline 0
$gate = Gate 'refactor gate reuses approval' 0 $refactorRun
Assert-That ($gate.protectedChanges.Count -eq 0 -and $gate.baseline -eq $regression.Baseline) 'Refactor must not change protected obligations.'

# Invalid evidence: tampered native evidence cannot produce a passing gate.
$messages = Join-Path $workspace ".tada/runs/$refactorRun/harness/tada-messages.ndjson"
$original = [IO.File]::ReadAllBytes($messages)
try {
    [IO.File]::WriteAllText($messages, '{"testRunFinished":', $utf8)
    $gate = Gate 'tampered evidence blocks gate' 2 $refactorRun
    Assert-That (@($gate.obligations | Where-Object { $_.enforcement -eq 'active' -and $_.state -eq 'satisfied' }).Count -eq 0) 'No obligation may be satisfied by tampered evidence.'
} finally { [IO.File]::WriteAllBytes($messages, $original) }
Gate 'restored evidence passes gate' 0 $refactorRun | Out-Null

$summary = Join-Path $workspace 'enforcement-workflow.json'
$checks | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summary -Encoding utf8
Write-Host "Enforcement workflow controls passed: $($checks.Count). Evidence: $summary"
