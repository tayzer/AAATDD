param(
    [Parameter(Mandatory)][ValidateSet('A', 'B', 'C', 'D', 'E1', 'F', 'G')][string]$Scenario,
    [Parameter(Mandatory)][string]$Destination
)

# Builds a disposable OrderHub Git repository for one knowledge-workflow evaluation scenario.
# Fixtures live outside this repository so the evaluated agent cannot read the rubric.
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sample = Join-Path $repository 'examples/MultiService'
if (Test-Path -LiteralPath $Destination) { throw "Destination already exists: $Destination" }
New-Item -ItemType Directory -Path $Destination | Out-Null
$root = (Resolve-Path -LiteralPath $Destination).Path

foreach ($file in Get-ChildItem -LiteralPath $sample -Recurse -File) {
    $relative = [IO.Path]::GetRelativePath($sample, $file.FullName)
    if ($relative -match '(^|[\\/])(bin|obj|\.tada)([\\/]|$)') { continue }
    $target = Join-Path $root $relative
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}

function Write-Text([string]$Relative, [string]$Content) {
    $path = Join-Path $root $Relative
    New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
    [IO.File]::WriteAllText($path, $Content.Replace("`r`n", "`n").TrimStart("`n"), [Text.UTF8Encoding]::new($false))
}

function Edit-Text([string]$Relative, [string]$Old, [string]$New) {
    $path = Join-Path $root $Relative
    $text = [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    $Old = $Old.Replace("`r`n", "`n")
    if (!$text.Contains($Old)) { throw "Fixture edit did not match in $Relative." }
    [IO.File]::WriteAllText($path, $text.Replace($Old, $New.Replace("`r`n", "`n")), [Text.UTF8Encoding]::new($false))
}

function Invoke-Git {
    & git.exe -C $root -c user.name='OrderHub developer' -c user.email='developer@orderhub.invalid' -c commit.gpgsign=false -c core.autocrlf=false @args
    if ($LASTEXITCODE) { throw "git $($args -join ' ') failed." }
}

function Commit([string]$Message) {
    Invoke-Git add --all
    Invoke-Git commit --quiet -m $Message
}

$idempotentRelease = @'
        command.CommandText = "UPDATE Reservations SET Active=0 WHERE Id=$id AND Active=1";
        if (command.ExecuteNonQuery() > 0)
        {
            command.CommandText = "UPDATE Stock SET Available=Available+1 WHERE Id=1";
            command.ExecuteNonQuery();
        }
'@
$repeatedRelease = @'
        command.CommandText = "UPDATE Reservations SET Active=0 WHERE Id=$id";
        command.ExecuteNonQuery();
        command.CommandText = "UPDATE Stock SET Available=Available+1 WHERE Id=1";
        command.ExecuteNonQuery();
'@
$finishedGuard = "        if (status is `"completed`" or `"rejected`") return Results.Ok();`n"

Write-Text '.gitignore' @'
**/bin/
**/obj/
.tada/runs/
.tada/plan-runs/
.tada/scratch/
'@
Write-Text 'README.md' @'
# OrderHub

OrderHub coordinates a purchase across three local services: Orders, Payments and Inventory. They share `Services/ServiceHost.cs` and choose their role at start-up.

- `features/` holds the agreed business scenarios.
- `Acceptance/` holds Reqnroll bindings that start all three services.
- `ServiceTests/` and `ContractTests/` hold supporting tests.

Run the journey with `dotnet test --project Acceptance/Journey.Acceptance.csproj`.
'@

$architecture = @'
# OrderHub architecture

Maintained by the platform team. Last reviewed 2026-09-30.

## Services

| Service | Owns | Interfaces |
| --- | --- | --- |
| Orders | Orders table and order status | `POST /orders/{id}`, `GET /orders/{id}`, `POST /payment-outcomes` |
| Payments | Payments table and charges | `POST /payments/{id}`, `GET /payments/{id}`, `GET /charges` |
| Inventory | Stock and Reservations tables | `POST /reserve/{id}`, `POST /release/{id}`, `GET /stock` |

Each service has its own SQLite database. No service reads another service's tables.

## Order flow

1. Orders accepts an order as `pending`.
2. The Orders worker reserves one item in Inventory, then requests payment. The order becomes `awaiting_payment`.
3. The Payments worker delivers a `PaymentOutcome` (schema version 1) to Orders.
4. Orders completes the order, or releases the reservation and rejects the order.

## Delivery guarantees

Workers poll every 50 ms and retry until the receiver returns a success status. Delivery is at least once, so every receiver must be idempotent. The order ID is the idempotency key in all three services. See [ADR 0001](decisions/0001-deliver-payment-outcomes-at-least-once.md).

## Contracts

`PaymentOutcome` v1 is checked by `ContractTests/` against `ContractTests/payment-outcome.json`.
'@
$adr1 = @'
# ADR 0001: Deliver payment outcomes at least once

Status: Accepted, 2025-02-11.

## Context

Payments and Orders run as separate processes. Either can restart while an outcome is in flight.

## Decision

The Payments worker retries delivery of each `PaymentOutcome` until Orders returns a success status. Receivers treat repeated messages as normal and use the order ID as the idempotency key. Exactly-once delivery is not attempted.

## Consequences

Every receiver must be idempotent. A late or repeated outcome must not change a finished order.
'@

switch ($Scenario) {
    { $_ -in 'A', 'D', 'G' } {
        Write-Text 'docs/architecture.md' $architecture
        Write-Text 'docs/decisions/0001-deliver-payment-outcomes-at-least-once.md' $adr1
        Invoke-Git init --quiet --initial-branch=main
        Commit 'feat: add order, payment and inventory services'
    }
    { $_ -in 'B', 'C' } {
        Invoke-Git init --quiet --initial-branch=main
        Commit 'feat: add order, payment and inventory services'
        if ($Scenario -eq 'C') {
            Write-Text '.tada/knowledge.json' '{ "schemaVersion": 1, "mode": "augment" }'
            Commit 'chore: let TADA propose knowledge records'
        }
    }
    'E1' {
        # A real duplicate-message defect: repeated declined outcomes release stock twice.
        Edit-Text 'Services/ServiceHost.cs' $idempotentRelease $repeatedRelease
        Edit-Text 'Services/ServiceHost.cs' $finishedGuard ''
        Edit-Text 'ServiceTests/ServiceTests.cs' @'
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using var response = await cluster.Http.PostAsync(cluster.Endpoints["inventory"] + "/release/reservation", null);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        Assert.AreEqual(1L, await cluster.Number("inventory", "/stock", "available"));
'@ ''
        Edit-Text 'ServiceTests/ServiceTests.cs' 'ReservationAndReleaseAreIdempotent' 'ReservationIsIdempotent'
        Write-Text '.tada/knowledge.json' '{ "schemaVersion": 1, "mode": "augment" }'
        Invoke-Git init --quiet --initial-branch=main
        Commit 'feat: add order, payment and inventory services'
    }
    'F' {
        Edit-Text 'Services/ServiceHost.cs' $idempotentRelease $repeatedRelease
        Write-Text 'docs/architecture.md' $architecture
        Write-Text 'docs/decisions/0001-deliver-payment-outcomes-at-least-once.md' $adr1
        Write-Text 'docs/operations/inventory-runbook.md' @'
# Inventory runbook

Written 2025-03-04.

## Releasing stock

`POST /release/{id}` adds the reserved item back to stock. It is **not idempotent**: calling it twice adds two items. Orders must therefore call release at most once per order, and nothing may retry a failed release automatically.

If stock looks too high after an incident, look for duplicate release calls and correct `Stock.Available` by hand.
'@
        Invoke-Git init --quiet --initial-branch=main
        Commit 'feat: add order, payment and inventory services'
        Edit-Text 'Services/ServiceHost.cs' $repeatedRelease $idempotentRelease
        Write-Text 'docs/decisions/0002-make-inventory-release-idempotent.md' @'
# ADR 0002: Make inventory release idempotent

Status: Accepted, 2025-06-17.

## Context

Payment outcomes are delivered at least once (ADR 0001). A repeated declined outcome called `POST /release/{id}` twice and added stock that did not exist.

## Decision

Inventory releases a reservation only while it is active. `Reservations.Active` changes from 1 to 0 in the same transaction that returns the item to stock. Repeated release calls succeed without changing stock.

## Consequences

Callers may retry release safely. Operations no longer need to correct stock after duplicate deliveries.
'@
        Write-Text '.tada/context.md' @'
# Project context

## Current change

Make inventory release idempotent on branch `fix/release-idempotency`. The fix adds the `Reservations.Active` check in `Services/ServiceHost.cs` and ADR 0002.

## Next action

Open the pull request for `fix/release-idempotency` and ask the platform team to update the inventory runbook.
'@
        Commit 'fix: make inventory release idempotent (ADR 0002)'
        Edit-Text 'README.md' 'Run the journey with' "Supporting tests run with ``dotnet test --project ServiceTests/Service.Tests.csproj``.`n`nRun the journey with"
        Commit 'docs: describe supporting test command'
    }
}

[ordered]@{ scenario = $Scenario; repository = $root; head = (& git -C $root rev-parse --short HEAD) } | ConvertTo-Json
