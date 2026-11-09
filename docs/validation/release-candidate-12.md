# Alpha.12 release candidate

Status is **EXPERIENCE ACCEPTED; PUBLICATION CLOSEOUT PENDING**. The target is the first public alpha on 2026-11-09, Windows x64 with SDK 10.0.403 and local advisory L1 scope.

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

## Remaining validation and publication

The remaining source changes record the accepted experience and approval closeout. The selected final checkpoint requires hosted Windows validation and an audit of its exact package, notices, plugin and bundle. The release manifest will identify that selected source and the actual assets. The [release notes](../releases/0.1.0-alpha.12.md) describe the proposed distribution.

Publication needs the requester's explicit approval of the concrete validated candidate. The candidate's source commit and artifact hashes will be presented together with the CI evidence. Downloaded release assets and installation must be checked after publication. No approval is inferred from tests or automated control fixtures.
