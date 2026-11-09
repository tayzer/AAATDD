# Alpha.11 native acceptance checkpoint

Status **VERIFIED DEVELOPMENT PROGRESS. RELEASE INCOMPLETE**.

## Execution support

Generated acceptance projects now support Visual Studio Test Explorer Run/Debug and ordinary MTP `dotnet test` in Debug and Release. The shared fixture context selects either staged native API output or TADA's captured execution paths. Native sessions own separate temporary report directories and scenario storage. Partial managed-run settings fail with a setup diagnostic.

A build-only project reference builds the separate API without exposing its assemblies to acceptance compilation. The target stages the complete API runtime output for native tests. Recorded execution suppresses this staging and retains the separately captured application build. The reference remains in both restore graphs.

New baselines use `evaluated-dotnet-build-v4` and build evidence schema 2. Dependency roles and reference metadata are checked against hashed before/after evaluation reports. Historical v3/schema-1 evidence retains its original verification policy. Oracle hashing remains byte-exact.

## Local validation on 2026-11-09

- Locked restore, Release build and all **134 focused tests** pass, with zero skips. Controls cover native setup, missing output, path traversal, partial managed context, configured API metadata, native-as-recorded rejection, historical evidence and altered dependency summaries/raw reports.
- All **46 Orders controls** pass, including intended assertion failures, unselected build/task/runtime inputs, missing checkpoints, setup/cleanup gaps and damaged/restored evidence.
- Fresh-package-cache onboarding passes real Echo RED/GREEN/refactor, transitive application dependencies, native Debug/Release execution, unchanged rerun identity, retained failure history and Git context handoff.
- The requester's TODO API passes all **14 scenarios** through direct `dotnet test --project tests/Acceptance/Acceptance.csproj` in both Debug and Release, using SDK 10.0.403.
- Two simultaneous native sessions each pass all **14 scenarios**. An isolated implementation defect produces the intended missing-delete assertion failure, expected 404 and actual 204, while teardown releases the API process. Original control source bytes were restored afterward.
- Visual Studio Professional 2026, version 18.11.12328.437, Test Explorer Run All passes all **14 scenarios**. Debug Selected Test stops at `TadaAcceptance.Create()` inside the `DeleteAMissingTodo` binding, then completes with **1 passed** after continuing. The temporary breakpoint was removed.
- An isolated TODO copy passes all **14 scenarios** through recorded TADA v4 execution with an explicitly labelled automated control approval. The real project's retained v3 run `8c9b3b5132094462b59fa77f03919b4b` also verifies with the new CLI.

Local logs and disposable control evidence are retained under the delivery checkout's `.artifacts/alpha11-*`, `.artifacts/orders-workflow/86482542075749fa918bee64f2973c8f/validation.json` and `.artifacts/onboarding/21239a055f0043adba4d920c9cdfe0f8/validation.json`. The TODO CLI logs are in its `.local/native-alpha11-Debug.log` and `.local/native-alpha11-Release.log`. These pre-commit package controls identify their tested binary hashes. Final packaging records the committed source identity separately.

## Migration and remaining gates

The actual TODO harness was migrated while preserving its feature bytes and business assertions. Harness, target, helper and project changes require a revised immutable baseline review and human approval. Old approvals and runs remain retained. Native runner success describes the scenarios executed. TADA PASS additionally requires the approved baseline and verified execution evidence.

Initial template embedding treated `.cs.txt` as a Czech satellite resource. `WithCulture=false` fixes packaging and installed initialization. A control also exposed SDK content copying from an unbuilt API apphost during recorded builds. `Private=false` prevents that copying, with explicit metadata validation and regression coverage. Failed setup evidence is retained as INDETERMINATE.

Verification remains local advisory L1 with the existing declared-trust and runtime-observation limits. Final-source hosted CI, requester plugin activation and fresh-session acceptance, merge and release publication retain their applicable gates.
