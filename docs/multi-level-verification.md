# Change-aware verification

Alpha.13 adds discovery-time verification selection, supporting MSTest/MTP checks and named local .NET services. The scope is one Git repository on Windows x64 with SDK 10.0.403, MSTest 4.5.2, MTP 2.5.2 and Reqnroll 3.3.4. Reports retain local advisory L1 limits.

## Select evidence during discovery

Classify actual effects using `feat`, `fix`, `refactor`, `chore` or a mixture. Record the objective, affected services, interfaces, state and journeys, adequate existing coverage, coverage gaps, selected checks and omission rationale. Branch names supply a hint. Declared business changes require business acceptance even when labelled `chore`.

Features add missing coverage. Fixes reproduce the intended failing assertion with existing or new regression coverage. Refactors begin with passing preservation checks and add characterisation where coverage is inadequate. Chores use relevant existing checks and can justify no test execution. An evaluable objective and recorded reasoning still apply.

Choose unit, service, contract and journey checks according to impact. Every level has its own scope. A contract check exercises the actual producer and consumer against independent captured expectations. A journey check establishes the combined business outcome. Technical checks can use ordinary MSTest without Gherkin. Gherkin remains the business authority.

Keep lightweight selections in `.tada/context.md` and execute existing tools directly when coordination adds no value. Installing or enabling TADA requires no application folder or verifier installation.

## Capture a coordinated selection

Write a plan when several suites need coordinated execution and evidence. The following supporting-only plan checks an existing internal rule. Replace the test name with the exact unique name from the pinned runner's `--list-tests` output.

```json
{
  "schemaVersion": 1,
  "objective": "Preserve stock reservation idempotency during extraction",
  "categories": ["refactor"],
  "businessChange": false,
  "impact": ["Inventory reservation implementation"],
  "checks": [
    {
      "id": "inventory",
      "level": "service",
      "purpose": "supporting",
      "obligations": ["Repeated reservation consumes stock once"],
      "rationale": "Existing service checks exercise the affected state transition",
      "engineering": {
        "configuration": {
          "schemaVersion": 1,
          "oracleRoots": ["tests/Inventory.Tests"],
          "implementationRoots": ["src/Inventory"],
          "execution": {
            "project": "tests/Inventory.Tests/Inventory.Tests.csproj",
            "assembly": "Inventory.Tests.dll",
            "messagesFile": "supporting.trx",
            "timeoutSeconds": 120
          }
        },
        "requiredTests": ["RepeatedReservationConsumesStockOnce"]
      }
    }
  ],
  "omissions": ["Public contracts and deployed topology are unchanged"],
  "timeoutSeconds": 600
}
```

Supporting test projects use the pinned MSTest SDK and set `EnableMicrosoftTestingExtensionsTrxReport` to `true`. Restore their locked package graphs before capture. Include fixtures, linked source, contracts and imported files in the selected roots. Supporting unit tests may reference selected implementation assemblies. Their source and dependencies receive engineering capture, without business approval.

An acceptance check has `purpose` equal to `acceptance` and a `baseline` containing the digest printed by `tada baseline prepare`. It has no `engineering` property. The human reviews and approves that executable baseline through the existing operations. Preparing or executing a plan grants no approval. A business-change plan needs at least one acceptance check. Required business obligations still need substantive coverage review.

```powershell
dotnet tool run tada plan prepare --file .tada/verification-plan.json
dotnet tool run tada run --plan sha256:<printed-plan-digest> --dotnet <selected-host>
dotnet tool run tada verify --run <printed-run-id>
```

Use actual printed identities in commands. `run` accepts either `--plan` or `--baseline`. `--json` suppresses progress and returns structured output. Readable output identifies every selected suite, scope and evidence run.

The plan inventory records selection and captured check identities under `.tada/plans/`. Supporting source snapshots use the existing immutable capture machinery and carry explicit engineering metadata. They cannot receive business approval. Engineering inputs shared with an acceptance baseline remain subject to that baseline's byte-integrity checks.

Revisions identify `previous` and explain `changeReason`. Preparation reports changed and removed checks. Existing plans require explicit lineage for a changed selection. Commit useful plans, approvals and snapshots through normal review. Runtime records remain under ignored `.tada/runs/` and `.tada/plan-runs/` directories.

## Several owned services

Schema-2 `tada.json` replaces the legacy `sutProject`/`sutAssembly` pair with `execution.services`. Each service has a unique lowercase ID, project and assembly filename. The verifier builds each service into a distinct `sut/<id>/` directory and captures the complete local combination. Implementation roots must include every selected service and shared implementation input.

Native build-only references retain `ReferenceOutputAssembly="false"`, `Private="false"` and `OutputItemType="TadaSutOutput"`, and add `TadaServiceId="<id>"`. Import `Tada.Acceptance.targets`. Fixtures access `TadaAcceptance.Create().Services[id]`. The existing single-service API and configuration continue to work.

Each scenario owns service launch, readiness, ports, storage and cleanup. Propagate the supplied environment and selected .NET host to owned service processes. Capture meaningful public observations and correlation IDs. Checkpoint every service before termination and perform cleanup even after failures. Every configured service needs complete runtime observations for the suite to pass.

Business deadlines belong in the agreed scenarios. Polling must retain the distinction between a valid observed breach and unavailable observation. Read-point assertions establish bounded claims. They provide no unlimited-future absence guarantee.

## Results and limits

The coordinator runs suites serially, once, and continues independent suites after failures within the overall deadline. Native discovery and TRX results must agree. Missing, duplicated, ambiguous, skipped or stale cases block a passing supporting assessment. Exact native names must be unique within each selected project, including data rows. The adapter targets the pinned runner's discovery format.

PASS requires every selected check to pass with complete evidence. FAIL reports a verified failed check, labelled with its acceptance or supporting purpose. Supporting failures establish no business-RED claim. INDETERMINATE identifies evidence, infrastructure or execution gaps. Verified failures remain visible alongside other suites' gaps. Offline verification re-reads native evidence and existing failure history. Cached projections supply no authority.

An empty selection needs `noChecksReason` and reports NO_CHECKS with exit 0, recording its rationale without a passing-test claim. The no-check result does not establish business acceptance.

Historical schema-6 records remain verifiable. New multi-service and supporting inventories use schema 7. Adopting multi-service acceptance changes captured inputs and requires a fresh human-approved baseline. Existing records and owner files are preserved.

Container orchestration, remote deployments, cross-repository identities, other runners and protected independent assurance remain future work. The existing SDK/package trust and cooperative runtime-observation limits apply.

## Executable example and controls

`examples/MultiService/` contains Orders, Payments and Inventory as separate processes with scenario-owned SQLite storage. Orders reserve stock and request payment. Payment outcomes are delivered asynchronously over HTTP from a persisted outbox, with idempotent receivers. Service tests cover reservation/release and charging, contract tests check the real producer and consumer, and three Gherkin journeys cover completion, compensation and repeated delivery.

`scripts/Test-MultiServiceWorkflow.ps1` installs the packaged CLI into an isolated project/cache, runs native Debug/Release acceptance and all three recorded levels, rejects tampered supporting evidence, injects a real compensation defect, restores implementation bytes and checks a no-test chore revision. Automated approval declarations are explicitly isolated controls. Requester business acceptance remains separate.
