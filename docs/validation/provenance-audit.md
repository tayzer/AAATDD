# Supported provenance audit

Status is **SUPPORTED TRUSTED-TOOLCHAIN BOUNDARY REVIEWED; FINAL CANDIDATE VALIDATION OPEN**. The 2026-11-09 review covers the alpha.12 v4/v2/v2 policies below and the existing controls. Historical alpha.6/alpha.7 findings remain preserved. Exhaustive automatic I/O observation and protected assurance remain outside the supported claim. See the [release checklist](../release-readiness.md).

## Alpha.12 supported-boundary review

The release profile uses inventory schema 6, `evaluated-dotnet-build-v4`, `msbuild-tasks-and-declared-inputs-v2` and `dotnet-loader-observation-v2`. Review covered `BuildInputs.Capture/Verify/CheckCurrent`, `TaskJournal.CheckEvaluation/Audit/Verify`, `ExecutionEnvironment.Capture/Tools`, `RunEngine.Run/Verify`, `RuntimeInputs.CapturePlatform/Verify` and the managed/native observer. The 139-test local suite passed after the checkpoint request/acknowledgement repairs. Full Orders and installed-package controls remain required for the selected candidate.

The explicit trusted-toolchain policy has these obligations.

- The user trusts the local host, verifier and SDK 10.0.403 installation. Capture records observed SDK, reference-pack and package tool files, their hashes and their contribution to environment identity. Exact package versions come from reviewed lock files, project SDK declarations or `global.json`. NuGet content-hash enforcement during locked restore remains a trusted NuGet responsibility.
- Repository-controlled harness code, imports, compiler inputs, fixture resources, dependency declarations and custom-task contracts belong to the approved oracle. Selected application source belongs to the implementation identity. Before/after capture and final checks reject drift. Generated inputs use fresh run-owned intermediates and output directories.
- SDK/package tasks remain trusted executable tools. Repository custom tasks and detected opaque evaluation require exact origin/target/task contracts and complete reviewed read roots. Those declarations include indirect file reads and invoked helper inputs. Standard structured file parameters are audited independently of a declaration. Arbitrary custom I/O and every descendant executable are outside automatic tracing.
- Runtime observation covers the configured managed harness and separate managed application, their located assemblies, ordinary Windows loader events and final snapshots. Loaded files must match captured output or the platform policy. The pre-run platform inventory covers the selected host, .NET 10 shared frameworks and core Windows DLLs. Other observed native modules beneath Windows System32 or WinSxS are explicitly trusted operating-system components with their observed hashes retained. CLR dynamic methods use the documented generated-code exception.
- Local files must remain stable through execution. Manual image mapping, mutable mapped-memory attacks, external network/service state, arbitrary business-data reads and non-.NET child execution require reviewed fixture/trust assumptions. The supported policy provides no independent protection against an actor controlling local records or tools.

Relevant executable controls cover unselected linked source/imports, unlocked packages, stale intermediates, changed tool bytes, undeclared file-reading/opaque tasks, missing task lifecycle/input records, dependency-role tampering, unlocated/unidentified managed loads, transient native loads, dropped loader events, missing terminal snapshots and damaged retained reports. `BuildInputTests`, `TaskJournalTests`, `EnvironmentTests`, `RuntimeInputTests`, `NativeLoaderTests` and the Orders workflow exercise these cases. The checkpoint regression additionally covers a real request-writer sharing conflict.

The supported-boundary review found no additional defect within these declared assumptions. Complete automatic execution-input discovery remains unproven. Final candidate controls and exact-artifact correspondence remain publication gates. This review grants no business approval and changes no accepted feature or original ADR.

## What the evidence establishes

| Boundary | Implementation and evidence | Trust or remaining limit |
| --- | --- | --- |
| Agreed business expectations | Exact-byte feature/support snapshots, review/approval records and retained scenario identities; oracle-drift and stale-review controls. | The human judges expectation adequacy. A controlling local actor can forge local approval records. |
| Selected build inputs and imports | Evaluated items, preprocessed imports, before/after hashes, fresh intermediates and locked restore. Unselected linked source/import controls block PASS. | SDK/package code is trusted. A successful build alone does not establish complete semantic dependency discovery. |
| Actual task code and file parameters | Task origins, actual task assembly hashes, known structured file parameters and lifecycle/command linkage. Custom file-read and opaque-task controls exercise rejection and declared trust. | Opaque tasks/evaluation honour reviewed complete read-root declarations. Arbitrary I/O and descendant tool execution are not automatically traced. |
| Built harness and application | Full run-output artifact inventory, distinct source/implementation identities and before/after stability checks. | Inputs must remain stable; hashing does not capture an immutable mapped-memory image. |
| Managed/native runtime loads | Managed assembly observation, ordinary Windows loader events, initial/final native snapshots, observer identities and raw report verification. Transient DLL and missing-checkpoint controls exercise the boundary. | Cooperative configured .NET processes, ordinary loader activity, trusted platform locations and stable files. Manual mapping, arbitrary file/network reads and non-.NET child execution are not covered automatically. |
| Results and retained history | Actual native Messages, expected-case coverage, assertion-failure interpretation and rerun history controls. | Local producer records are advisory L1. Successful steps do not prove assertion adequacy without reviewed bindings. |
| Failure and cancellation | Deadlines, process-tree termination, retained stdout/stderr, explicit cancellation and incomplete-evidence outcomes. | Successful process cleanup does not turn incomplete business evidence into PASS. |

Sources: [baseline storage](../../src/Tada.Core/BaselineStore.cs), [build capture](../../src/Tada.Core/BuildInputs.cs), [task audit](../../src/Tada.Core/TaskJournal.cs), [runtime verification](../../src/Tada.Core/RuntimeInputs.cs), [execution/history](../../src/Tada.Core/RunEngine.cs) and [process handling](../../src/Tada.Core/Processes.cs). The [task contract guide](../build-task-contracts.md) describes the declared trust obligation.

## Corrected finding: task-only environment identity

The run engine constructs its environment's `tools` identity from evaluated `ProjectBuildInputs.After`. Actual task journals retain additional trusted files that are absent from that evaluated list. The real alpha.6 Orders run contains 16 distinct such paths, including `Microsoft.Build.dll`, `NuGet.Build.Tasks.dll`, Reqnroll generation code and MTP build-task code.

Alpha.6 hashed and stability-checked these files during a run but omitted them from cross-run environment identity. Replacing task-only code at the same trusted cache path could leave that identity unchanged.

Alpha.7 includes observed task-only SDK/package inputs in environment identity. A private-cache control changes the exact Reqnroll task DLL bytes using a PE overlay while preserving functional behaviour. The real task journal captures the changed digest; execution passes with the same implementation source and a different environment. The control restores the original cache bytes. Equivalent development/rerun executions retain one environment despite fresh run-owned paths.

## Corrected finding: fresh output paths changed implementation identity

An unchanged real Echo rerun under the earlier profile produced different DLL, PDB and apphost hashes because compiler output paths differed. Including those hashes in the implementation identity could evade earlier-failure matching without any source fix. This was a genuine history-policy gap that synthetic history controls did not expose.

Alpha.7 identifies selected implementation source separately from exact per-run artifact hashes. It also maps temporary compiler paths consistently. Artifact tampering still blocks verification; nondeterministic output bytes cannot discard a failure for the same identified source/environment.

Real installed-package controls now show an unchanged rerun retaining its implementation/environment identities and identical SUT artifact hashes. Another control first exposes a business assertion failure, then allows native assertions to pass without changing source. Native runner exit is zero, while TADA remains FAIL with `unresolved-earlier-failure`. Restoring corrected source produces PASS. These controls are recorded in the [alpha.7 checkpoint](increment-07.md).

Inventory schema 6 and task policy `msbuild-tasks-and-declared-inputs-v2` distinguish the corrected semantics. Historical schema-5 baselines remain reviewable, but new execution/verification does not infer current identity coverage or carry approval forward.

## Release decision

The observed identity gaps have executable controls. Keep the complete automatic execution-boundary claim unproven: arbitrary I/O, non-.NET child/helper execution and manual image mapping are not automatically observed. Reviewed custom code must honour its declared input/trust obligations. A contract is not evidence of arbitrary I/O tracing. Retain unsupported cases as limitations rather than extending the assurance claim.

[Hosted Windows CI](hosted-ci.md) now validates public checkpoint `13c71f1`, including installation in isolated caches and real API controls. Downloaded artifacts identify the exact source commit and pass a separate correspondence audit. This closes the earlier hosted-validation gap for that checkpoint; it does not prove complete automatic input discovery. Revalidate the selected publication SHA before release. Actual requester installation, fresh-agent context use and requester/Codex API development remain separate experience gates. Internal and scripted controls do not replace them.
