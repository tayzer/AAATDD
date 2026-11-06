# Alpha.7 rerun identity and cancellation checkpoint

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-07.json) links the tested package/source, control reports and discovered identity defects. The [provenance audit](provenance-audit.md) distinguishes observed inputs from declared trust.

## Corrected behaviour

Cancellation terminates the owned process tree while retaining stdout/stderr and command metadata. Records distinguish cancellation from deadline expiry; cancelled execution cannot use complete successful Messages to obtain PASS. An operation cancelled before startup launches no command. Real child-process controls exercise both cancellation and timeout.

Environment identity now includes SDK/package task assemblies and supported file parameters absent from evaluated compiler inputs. Duplicate uses share an identity; contradictory observed hashes are rejected. Run-owned generated paths and implementation edits do not manufacture changes to the tool environment.

The audit also found that fresh compiler output paths changed DLL/PDB/apphost bytes for unchanged source. Using those bytes in rerun implementation identity could discard a real earlier failure. Alpha.7 identifies selected implementation source separately from its exact per-run artifact inventory and maps temporary compiler paths consistently. Artifact hashes remain byte-exact and integrity checked. A source-identical rerun cannot count as an implementation fix merely because output paths or generated artifact bytes differ.

Inventory schema 6 and `msbuild-tasks-and-declared-inputs-v2` record the corrected identity policy. Build policy remains `evaluated-dotnet-build-v3`; runtime policy remains `dotnet-loader-observation-v2`. An actual schema-5 baseline remains readable/reviewable, while current execution and verification return INDETERMINATE rather than inferring new coverage or approval. Historical evidence needs its matching tool.

## Validation

- Locked restore, release build and package creation pass. All **111 focused/replay/CLI/history/Git/native/process tests** pass with zero skips. Concurrent baseline preparation publishes one consistent snapshot; eight real oracle-edit/approval races cannot authorise changed-byte execution. Changing oracle bytes after approval prevents every execution command. Required task-input checks also handle case variants and qualified task names.
- All **46 real Orders controls** pass, including intended business failures, unselected build/task/runtime inputs, missing runtime checkpoints, setup/cleanup gaps and damaged/restored evidence.
- Fresh-package Echo development establishes RED, GREEN and refactor GREEN under one baseline/environment and distinct implementation source identities. An unchanged rerun retains source/environment identities and identical SUT artifact hashes.
- A real fault control first produces an assertion FAIL, then native PASS without changing source/environment. TADA retains FAIL with `unresolved-earlier-failure`. Restoring corrected source passes. The warn-as-error maintenance build and Git-committed handoff pass.
- A private-cache PE overlay changes the exact Reqnroll task DLL bytes without changing its functional behaviour. The actual task journal records the changed digest; the run passes with unchanged implementation source and a different environment. Original cache bytes are restored, and retained changed-environment evidence still verifies offline.

The package contains the task logger, native observer and dependency notices, without SDK MSBuild binaries, compiler or native test fixture. The first 40 ADRs and original Orders feature retain their hashes. No owned API/acceptance processes or scenario databases remain after validation.

Initial restricted restore attempts could not query NuGet vulnerability data. Locked restoration succeeded with required network permission; validation was not weakened. Earlier development runs and the unchanged-rerun discovery are retained as diagnostic evidence, not substituted for final-source controls.

## Remaining release gates

Observed identity defects are fixed. Complete automatic execution-input observation remains unproven: arbitrary I/O, non-.NET child/helper execution and manual image mapping are outside the automatic boundary. Custom code honours reviewed declarations; SDK/package contents and cooperative stable-file local execution remain explicit L1 assumptions.

Remaining gates include final release/provenance review, final-source remote CI, clean-machine installation, genuine fresh-agent context use and requester/Codex API acceptance. Automated approvals establish isolated controls only. No public package release is claimed.
