# First release checklist

Status: **IMPLEMENTATION AUTHORISED; RELEASE INCOMPLETE**. The active goal supersedes the earlier design-only workflow under [ADR 0041](decisions/0041-authorise-autonomous-release-implementation.md).

## User experience and evidence

- [ ] Install a versioned repository-local `tada` tool from a packaged artifact on a fresh supported environment.
- [x] Prepare a development download bundle with install guidance, licences, notices and checksums; validate extracted-feed installation and corruption rejection.
- [x] Initialise a new repository without overwriting existing instructions or work.
- [ ] Agent-neutral guidance supports collaborative discovery and human-approved Gherkin, meaningful RED/GREEN, small increments and regression verification.
- [x] Prepare/review/approve an immutable exact-byte oracle baseline with explicit scope and preserved history.
- [x] Build and run the actual Reqnroll/MSTest/MTP harness against a separate API; retain identities, native Messages and command evidence.
- [x] Verify retained evidence without re-execution, detecting missing/skipped/stale/truncated evidence and altered oracle inputs.
- [x] Correct Orders behaviour passes; selected business defects fail for the intended assertions; infrastructure failures are distinguished.
- [x] Preserve unresolved trustworthy failures across reruns and amendments without silently dropping obligations.
- [x] A second project uses the same package/core without project-specific changes.
- [x] Ordinary maintenance follows appropriate checks without mandatory business scenarios or weakening existing obligations.
- [x] Identify supported evaluated build inputs/imports, reject unselected linked source/imports, isolate fresh intermediates and preserve hashed reports for offline verification.
- [x] Record located managed loads, cooperative native snapshots and a pre-run platform identity; reject missing terminal snapshots, unidentified modules and damaged runtime/environment evidence.
- [x] Retain ordinary Windows loader notifications for transient DLLs, validate continuous event counts and reject queue loss/overflow; package the native observer without an end-user C compiler or additional C runtime library.
- [x] Record actual restore/rebuild task code and supported file parameters; reject unselected file inputs and require reviewed declarations for opaque custom tasks/evaluation.
- [ ] Validate the complete execution/import/loaded-runtime input boundary against reviewed files or explicit trusted-toolchain policy.
- [ ] Context survives a new agent session using concise Git-native records with provenance and supersession.
- [ ] Quickstart reproduces a useful install-to-API workflow, including quality checks and failure recovery.
- [x] Test path handling, canonicalisation, approval races, process failure, cancellation, timeouts, teardown and evidence integrity.
- [ ] CI validates the final release source and package; licence and dependency notices match redistributed content.
- [x] Document local advisory L1 limitations and actual supported runner/protocol/platform versions.
- [ ] User acceptance: requester installs TADA and uses it with Codex to build a simple API from scratch.

Unchecked means incomplete or not yet verified. The [earlier audit](architecture/alpha-delivery-readiness.md) is a design-stage snapshot; it must not be mistaken for current implementation evidence. The completed [compatibility probe](research/compatibility-probe-results.md) remains tooling evidence only.

## Current implementation increment

The [alpha.7 checkpoint](validation/increment-07.md) records 111 passing focused tests, 46 Orders controls and installed-package API development with unchanged-rerun identity and real failure-history controls. A private task-cache byte change produces a distinct environment; restored-cache evidence verifies offline. Cancellation and deadline controls terminate owned child processes and preserve diagnostics. Checked items describe bounded control demonstrations, not requester business approval or whole-release completion. The [alpha.6 checkpoint](validation/increment-06.md), [alpha.5 checkpoint](validation/increment-05.md) and earlier increments remain historical evidence.

Alpha.7 uses schema-6 baselines with `evaluated-dotnet-build-v3`, `msbuild-tasks-and-declared-inputs-v2` and `dotnet-loader-observation-v2`. Implementation source identity and exact per-run artifact identities remain separate; observed task-only tools join environment identity. The [provenance audit](validation/provenance-audit.md) records the corrected gaps and remaining observation limits. [Contracts](build-task-contracts.md) trust reviewed opaque code to honour complete declarations; they do not trace arbitrary I/O or every possible child/helper execution. Complete automatic execution-boundary coverage remains unproven. Alpha.5's ordinary transient-DLL observation and compiler reproducibility evidence remain applicable historical controls.

The [local source/distribution checkpoint](validation/release-packaging.md) adds an isolated source rebuild, fresh compiler bootstrap, full API controls and installable download ZIP. Windows Git long-path support is explicit and validation avoids global-tool PATH updates. This host remains distinct from a clean supported machine.

Next work: final release/provenance review, final CI and clean-machine/user acceptance. The CI workflow includes the pinned native-toolchain cache, unchanged-rerun/real-history controls, task-byte environment control and download-bundle installation, but has not yet run on the final release source.

Git round-trip validation now passes, including generated attributes and exact-byte approval checks after clone. Actual new-agent context use and requester/Codex acceptance remain separate unverified experience gates.
