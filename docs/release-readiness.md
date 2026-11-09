# First release checklist

Status is **EXPERIENCE ACCEPTED; PUBLICATION PENDING**. The active goal supersedes the earlier design-only workflow under [ADR 0041](decisions/0041-authorise-autonomous-release-implementation.md). Final publication requires the exact-source and artifact closeout below.

## User experience and evidence

- [x] Install a versioned repository-local `tada` tool from a packaged artifact on a fresh supported automated environment; requester-led installation remains separate.
- [x] Prepare a development download bundle with install guidance, licences, notices and checksums; validate extracted-feed installation and corruption rejection.
- [x] Initialise a new repository without overwriting existing instructions or work.
- [x] After project enablement, an ordinary project/change request automatically follows the TADA workflow: collaborative discovery, human-approved Gherkin where appropriate, meaningful RED/GREEN and regression verification. No workflow-specific user prompt or reminder is supplied. Requester reported this experience passed on 2026-11-09.
- [x] Native plugin enable, disable and re-enable controls govern automatic workflow selection in fresh sessions, including projects migrated from earlier generated instruction blocks. The requester confirmed both activation trials passed on 2026-11-09. Migration controls and isolated native CLI state controls also pass.
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
- [x] Validate the complete execution/import/loaded-runtime input boundary against reviewed files or explicit trusted-toolchain policy. The [alpha.12 supported-boundary review](validation/provenance-audit.md) records the explicit policy and control coverage. Exhaustive automatic observation remains unproven.
- [x] Context survives a new agent session using concise Git-native records with provenance and supersession, recovered from an ordinary continuation request without handoff-reading instructions. The requester confirmed the fresh-chat continuation trial passed on 2026-11-09.
- [x] Quickstart reproduces a useful install-to-API workflow, including quality checks and failure recovery. The requester accepted ordinary use. Fresh-package onboarding covers API development, native tests, maintenance and retained failure recovery. Candidate bundle installation and corruption rejection also pass, as recorded in [candidate validation](validation/release-candidate-12.md).
- [x] The same acceptance scenarios run through native `dotnet test` in Debug/Release and Visual Studio Test Explorer Run/Debug, with scenario-owned resources and no wrapper or manual TADA run variables.
- [x] Test path handling, canonicalisation, approval races, process failure, cancellation, timeouts, teardown and evidence integrity.
- [x] Hosted CI validates the recorded source/package checkpoint; licence and dependency notices match redistributed content. Revalidate the selected publication SHA before release.
- [x] Document local advisory L1 limitations and actual supported runner/protocol/platform versions.
- [x] User acceptance: requester installs TADA and uses it with Codex to build a simple API from scratch. Requester reported this experience passed on 2026-11-09.

Unchecked means incomplete or not yet verified. The [earlier audit](architecture/alpha-delivery-readiness.md) is a design-stage snapshot; it must not be mistaken for current implementation evidence. The completed [compatibility probe](research/compatibility-probe-results.md) remains tooling evidence only.

## Current implementation increment

The [alpha.7 checkpoint](validation/increment-07.md) records 111 passing focused tests, 46 Orders controls and installed-package API development with unchanged-rerun identity and real failure-history controls. A private task-cache byte change produces a distinct environment; restored-cache evidence verifies offline. Cancellation and deadline controls terminate owned child processes and preserve diagnostics. Checked items describe bounded control demonstrations, not requester business approval or whole-release completion. The [alpha.6 checkpoint](validation/increment-06.md), [alpha.5 checkpoint](validation/increment-05.md) and earlier increments remain historical evidence.

Alpha.8 corrects the activation/trial approach: `init` supplies default workflow instructions even when root agent instructions already exist, and a new context records the invoking SDK host. The [ordinary-request trial](validation/default-workflow-trial.md) supersedes the earlier coached starter prompt. Existing alpha.7 CI and scripted API controls remain evidence for their recorded source/package only; they do not prove spontaneous workflow adoption by an agent or certify alpha.8.

Alpha.9 moves workflow activation to the [TADA plugin](plugin.md). The CLI prepares project scaffolding and migrates recognised generated instructions from earlier alphas. Package/setup checks and native host enable/disable trials are tracked separately.

The requester reported alpha.9 asking for a project folder during installation. Alpha.10 removes its optional installation onboarding hook and defers verifier preparation until ordinary project work requires business verification. The skill uses the active workspace and asks for a folder only when the target is genuinely ambiguous. Packaging checks enforce the absence of the hook. The corrected native installation and uncoached project workflow still need requester acceptance.

Alpha.11 supports native acceptance execution through the generated fixture context and build-only API dependency. The [native execution checkpoint](validation/increment-11.md) records the actual TODO project's 14 passing scenarios in Debug, Release and Visual Studio Test Explorer, including a successful debugger breakpoint in `DeleteAMissingTodo`. New recorded builds retain dependency roles under `evaluated-dotnet-build-v4`; historical v3 evidence remains verifiable. Migrated harnesses require renewed human baseline approval.

Alpha.12 adds elapsed-time progress for run and retained-evidence verification, including ten-second updates during long phases. Structured JSON mode suppresses progress. The [CI and PR workflow guide](ci-pr-workflows.md) distinguishes existing execution support from the proposed trusted approval and required-check integration for application repositories.

Alpha.7 uses schema-6 baselines with `evaluated-dotnet-build-v3`, `msbuild-tasks-and-declared-inputs-v2` and `dotnet-loader-observation-v2`. Implementation source identity and exact per-run artifact identities remain separate; observed task-only tools join environment identity. The [provenance audit](validation/provenance-audit.md) records the corrected gaps and remaining observation limits. [Contracts](build-task-contracts.md) trust reviewed opaque code to honour complete declarations; they do not trace arbitrary I/O or every possible child/helper execution. Complete automatic execution-boundary coverage remains unproven. Alpha.5's ordinary transient-DLL observation and compiler reproducibility evidence remain applicable historical controls.

The [local source/distribution checkpoint](validation/release-packaging.md) adds an isolated source rebuild, fresh compiler bootstrap, full API controls and installable download ZIP. Windows Git long-path support is explicit and validation avoids global-tool PATH updates. This host remains distinct from a clean supported machine.

The [hosted checkpoint](validation/hosted-ci.md) records successful push and proposed-merge validation for public head `13c71f1`. Its downloaded package identifies that exact source commit. The correspondence audit checks 23 retained run inventories and 3,159 file digests, the redistributed DLL set, notices and bundle checksums. Hosted Windows installation is automated supported-environment evidence; it does not establish requester experience or a genuinely fresh Codex session. Historical local packaging reports retain their original source/package identities.

Requester updates on 2026-11-09 confirm that ordinary-use acceptance, plugin disable/re-enable and genuinely fresh-chat continuation all passed. The experience gates are accepted by the requester.

PR #1 merged on 2026-11-09. Its head `673d6f733922d0eb12466dc7af04f7777a65ae11` passed both recorded Windows validation runs. The merged `main` commit is `f3ccef8aa40a4f2f0c8656fd9daedec3b3389ab4`. Final publication must identify and validate its selected commit and artifacts separately. The CI workflow includes the pinned native-toolchain cache, unchanged-rerun/real-history controls, task-byte environment control and download-bundle installation.

Git round-trip validation passes, including generated attributes and exact-byte approval checks after clone. The requester also accepted actual new-agent context recovery.

## Release plan for 2026-11-09

The user activated a release goal for `0.1.0-alpha.12`, Windows x64, SDK 10.0.403 and local advisory L1 scope. The target is a first public alpha today, subject to the remaining checks and approval of the concrete publication candidate. See [candidate validation](validation/release-candidate-12.md) and [release notes](releases/0.1.0-alpha.12.md).

1. Completed by requester verdict. Disabled-plugin and re-enabled fresh-chat continuation tests both passed.
2. Completed. The requester explicitly approved the new checkpoint helper review. Approval `d43f7299e2154b0580309c55b1e1cc1d` records that decision. New run `fa6adcc5f7534ca5a626438e03a9fb83` and retained verification passed all 14 cases.
3. Review completed against implementation, existing controls and the explicit trusted-toolchain policy. Unsupported observation cases and the unproven complete automatic execution-boundary claim remain documented.
4. Completed at implementation/packaging commit `892a2aa`. The isolated checkout passes locked restores, 139 focused/replay tests, 46 Orders controls, fresh-cache onboarding, task-byte environment identity, plugin setup, actual native CLI installation/state controls, bundle installation and corruption rejection. Both hosted Windows runs pass. Failed attempts and unrelated local work remain preserved.
5. Validate the selected candidate commit in hosted Windows CI. Audit the exact downloadable package and plugin against that commit, notices and checksums. Exercise the published installation instructions against the candidate artifacts, including a documented failure-recovery path.
6. Present the concrete candidate, evidence, remaining limitations and downloadable artifacts for publication approval. The proposed first distribution is a GitHub prerelease containing the CLI package, download bundle, matching plugin ZIP and checksums. Confirm any NuGet publication target, package access and authority separately. Publish only the approved validated artifacts, then check their downloaded bytes and installation path.

Release blockers are failed experience controls, unresolved supported-boundary defects, missing required human approval, failing candidate validation or artifact correspondence, and missing publication authority. Proposed application-repository approval adapters remain separately scoped work in the CI guide.

## Publication closeout

- [ ] Validate the selected final documentation/source checkpoint in hosted Windows CI and audit its exact downloadable assets. The release manifest identifies the selected SHA, runs and asset hashes.
- [ ] Obtain explicit approval for the concrete merge and GitHub prerelease candidate.
- [ ] Publish the approved source tag and audited artifacts, then verify downloaded bytes and installation.
