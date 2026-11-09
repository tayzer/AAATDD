# Alpha.12 release candidate

Status is **PUBLIC ALPHA RELEASED AND VERIFIED**. The first public alpha was published on 2026-11-09, Windows x64 with SDK 10.0.403 and local advisory L1 scope.

## Accepted experience

The requester confirmed ordinary-use acceptance passed on 2026-11-09, including installation and automatic workflow use for API development. The requester then confirmed both disabled-plugin and re-enabled fresh-chat continuation tests passed. The trial procedure remains in [ordinary-request acceptance](default-workflow-trial.md).

The TODO migration gate was already completed in the application repository, `D:\Dev\TADATestDelete`. Its context records human approval of native-compatible review `4f7b60dc0cb44b04af7e760bfdab8155`, approval `3f0182d4c16a4592b4f9bb5e93035010`, and the later missing-DELETE amendment. The current alpha.12 tool-version review `fd88ab1f52704e83881a6a1ad7e89e1c` has approval `b63a3d76140042d3b728000143eb53fc` by Protayne. Current baseline is `sha256:2cf2e970fa38b7d77d0567e634cdfa0ba53cb91505c75d6f727298ad4fb8f685`.

Run `86ab495683274aadb5bae6b4f3170c7c` passed all 14 cases. This candidate reverified the retained evidence as PASS with 14 cases and no reasons. Earlier approvals, failed runs and business changes remain retained in the application repository. This closes the previously stale request for migration approval.

The acknowledgement-reader repair below changes captured helper code. Comparative review `1be09e4807dc4afdb31d6d9a4127f4ce` proposes baseline `sha256:8cf369cb654fa08290a76d309032a7831037042cbc1e1464c566712ccb55f15c` against the previously approved baseline. Only `tests/Acceptance/TadaRuntimeCheckpoint.cs` changes. Byte-exact backups are retained in the delivery checkout and application `.local/` directory. The requester explicitly approved this helper-only revision. Approval `d43f7299e2154b0580309c55b1e1cc1d`, by Protayne, records that decision and replaces the prior baseline. Run `fa6adcc5f7534ca5a626438e03a9fb83` and retained verification passed all 14 cases using candidate code `892a2aa`.

## Checkpoint race and repair

PR #1 merged as `f3ccef8aa40a4f2f0c8656fd9daedec3b3389ab4`. Both validation runs at its head `673d6f733922d0eb12466dc7af04f7777a65ae11` passed. The merged-main [run 42690796228](https://github.com/tayzer/AAATDD/actions/runs/42690796228) then failed during an opaque-task positive control. Runtime observation retained an IOException while reading a checkpoint request still held by its writer, producing INDETERMINATE.

`CheckpointWaitsForLegacyRequestWriterBeforeCompletingObservation` reproduced that error before the repair. The observer now defers Windows sharing/lock violations from that read to its next timer tick. It continues observation and preserves mismatched-identity and final-snapshot checks. Existing helper and oracle bytes are unchanged.

The first local Orders attempt then exposed a separate acknowledgement-read sharing conflict in teardown. Run `0ead90752e204681b088dc50be844244` is retained as INDETERMINATE. A two-case regression reproduced the helper failure for both correct and mismatched acknowledgement identities. The generated helper and Orders helper now retry only Windows sharing/lock violations within the existing ten-second deadline. A completed acknowledgement still requires the matching run identity.

After both repairs, the full focused/replay suite passed 139 tests with zero failures and skips. The generated and tested sample helper text also matches. Logs are retained in the isolated delivery checkout's `.artifacts/`, including both failing regressions and the failed Orders attempt. The initial restricted restore failure is setup evidence. Locked restores and the unchanged-source build succeeded with the required network/process permissions.

An attempted local repack overlapped the first Orders run and failed because Windows held the active CLI DLLs open. Its diagnostics are retained. Packaging and CLI execution are serialized for the final committed candidate.

## Local code validation and standalone installation

At code commit `398c4bc028b187179926a53639854b88085a678c`, all 46 Orders controls and fresh-package onboarding passed. Onboarding includes meaningful Echo RED/GREEN/refactor, native Debug/Release tests, stable rerun identities, retained real failure history and a Git-committed context handoff. The task-byte control passed with the same implementation, a distinct environment and original private-cache bytes restored. Plugin preparation, bundle installation and deliberate bundle-corruption rejection also passed.

Both hosted Windows runs at that code commit passed, [push 42695961110](https://github.com/tayzer/AAATDD/actions/runs/42695961110) and [proposed merge 42695970708](https://github.com/tayzer/AAATDD/actions/runs/42695970708). These runs precede the final standalone-catalog packaging change.

Download-install review then found that the standalone plugin ZIP omitted the marketplace catalog required by its installation instructions. A control against the old archive failed on the missing catalog. Packaging now includes a root-resolving `.agents/plugins/marketplace.json` and README. Validation extracts the ZIP before installing its feed. Actual Codex CLI marketplace registration and plugin installation from that extracted ZIP passed in an isolated child-process Codex home. These controls grant no fresh-agent or requester acceptance.

Implementation/packaging commit `892a2aac05d3ef5dd223a841a7e1132c0d918091` passed both hosted Windows runs, [push 42698714310](https://github.com/tayzer/AAATDD/actions/runs/42698714310) and [proposed merge 42698723504](https://github.com/tayzer/AAATDD/actions/runs/42698723504). The exact local package also passed 139 focused/replay tests, 46 Orders controls, full onboarding and task-environment controls, extracted plugin/bundle installation and artifact correspondence. Actual native CLI controls confirmed installation and disabled/re-enabled configuration states in an isolated home. Local logs and machine-readable control reports remain in the delivery checkout's `.artifacts/`.

## Publication verified on 2026-11-09

Publication source `a5b4570e27e46cb334546b10ce8bb2fc7b71ae7b` passed [push 42701848029](https://github.com/tayzer/AAATDD/actions/runs/42701848029) and [proposed merge 42701857412](https://github.com/tayzer/AAATDD/actions/runs/42701857412). The downloaded push artifact, ID `13712801737`, passed exact-byte correspondence checks across 23 runs, 13 baseline inventories and 3,675 file digests. Its package and embedded plugin/bundle copies match. The bundled licence matches Git's exact Windows checkout expansion of the selected source. Oracle and evidence hashes use exact bytes.

The requester explicitly approved merging PR #2 and publishing the pinned candidate. PR #2 merged as `0aa2f25be57736f9a8e17b0b389bfea376b129d4`. Its source tree matches the selected release source. [GitHub prerelease `v0.1.0-alpha.12`](https://github.com/tayzer/AAATDD/releases/tag/v0.1.0-alpha.12), release ID `439694450`, was published at `2026-11-09T21:57:14Z` with that tag targeting the approved source.

All five release assets were downloaded again. The manifest SHA-256 is `78e227abd25f542aee94d1d7dc8934bcfb3b355fadf772e231b2a7044877e242`. Asset hashes and GitHub's uploaded digest receipts match approval. Fresh-cache installation from the downloaded CLI and plugin ZIPs passed, as did native Codex installation and activation-state controls in an isolated home.

The actual downloaded package SHA-256 is `798727249d09c8e49135b9ab1d8f1be9ce52e69f352b64e0fa4497e50d495426`. The TODO project's normal wrapper ran approved baseline `sha256:8cf369cb654fa08290a76d309032a7831037042cbc1e1464c566712ccb55f15c` with that package. Run `2dcf562d45b542fa922fd46a42ec9b99` and retained verification passed all 14 cases. Captured observer/logger bytes match the downloaded package. Manifest bytes, previous caches and earlier approvals/runs remain preserved.

The delivery checkout retains publication approval, release/API receipts, checksum verification, installation reports, failed diagnostics and recovered cache paths under `.artifacts/`. This documentation closeout records the published source and assets. The release manifest and tag identify the immutable publication checkpoint.
