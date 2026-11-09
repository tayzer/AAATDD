# Alpha.12 release candidate

Status is **IN VALIDATION; PUBLICATION PENDING**. The target is the first public alpha on 2026-11-09, Windows x64 with SDK 10.0.403 and local advisory L1 scope.

## Accepted experience

The requester confirmed ordinary-use acceptance passed on 2026-11-09, including installation and automatic workflow use for API development. Disabled-plugin and re-enabled fresh-session recovery checks are pending the requester's result. The trial procedure remains in [ordinary-request acceptance](default-workflow-trial.md).

The TODO migration gate was already completed in the application repository, `D:\Dev\TADATestDelete`. Its context records human approval of native-compatible review `4f7b60dc0cb44b04af7e760bfdab8155`, approval `3f0182d4c16a4592b4f9bb5e93035010`, and the later missing-DELETE amendment. The current alpha.12 tool-version review `fd88ab1f52704e83881a6a1ad7e89e1c` has approval `b63a3d76140042d3b728000143eb53fc` by Protayne. Current baseline is `sha256:2cf2e970fa38b7d77d0567e634cdfa0ba53cb91505c75d6f727298ad4fb8f685`.

Run `86ab495683274aadb5bae6b4f3170c7c` passed all 14 cases. This candidate reverified the retained evidence as PASS with 14 cases and no reasons. Earlier approvals, failed runs and business changes remain retained in the application repository. This closes the previously stale request for migration approval.

The acknowledgement-reader repair below changes captured helper code. Comparative review `1be09e4807dc4afdb31d6d9a4127f4ce` proposes baseline `sha256:8cf369cb654fa08290a76d309032a7831037042cbc1e1464c566712ccb55f15c` against the current approved baseline. Only `tests/Acceptance/TadaRuntimeCheckpoint.cs` changes. A byte-exact helper backup is retained in the delivery checkout. Human approval of this new helper-only revision is pending.

## Checkpoint race and repair

PR #1 merged as `f3ccef8aa40a4f2f0c8656fd9daedec3b3389ab4`. Both validation runs at its head `673d6f733922d0eb12466dc7af04f7777a65ae11` passed. The merged-main [run 42690796228](https://github.com/tayzer/AAATDD/actions/runs/42690796228) then failed during an opaque-task positive control. Runtime observation retained an IOException while reading a checkpoint request still held by its writer, producing INDETERMINATE.

`CheckpointWaitsForLegacyRequestWriterBeforeCompletingObservation` reproduced that error before the repair. The observer now defers Windows sharing/lock violations from that read to its next timer tick. It continues observation and preserves mismatched-identity and final-snapshot checks. Existing helper and oracle bytes are unchanged.

The first local Orders attempt then exposed a separate acknowledgement-read sharing conflict in teardown. Run `0ead90752e204681b088dc50be844244` is retained as INDETERMINATE. A two-case regression reproduced the helper failure for both correct and mismatched acknowledgement identities. The generated helper and Orders helper now retry only Windows sharing/lock violations within the existing ten-second deadline. A completed acknowledgement still requires the matching run identity.

After both repairs, the full focused/replay suite passed 139 tests with zero failures and skips. The generated and tested sample helper text also matches. Logs are retained in the isolated delivery checkout's `.artifacts/`, including both failing regressions and the failed Orders attempt. The initial restricted restore failure is setup evidence. Locked restores and the unchanged-source build succeeded with the required network/process permissions.

An attempted local repack overlapped the first Orders run and failed because Windows held the active CLI DLLs open. Its diagnostics are retained. Packaging and CLI execution are serialized for the final committed candidate.

## Remaining validation and publication

The candidate is prepared in an isolated checkout. Local Orders, installed-package onboarding, task-environment identity, plugin and bundle controls must finish. The committed source then requires hosted Windows validation and an audit of its exact package, notices, plugin and bundle. The [release notes](../releases/0.1.0-alpha.12.md) describe the proposed assets.

Publication needs the requester's explicit approval of the concrete validated candidate. The candidate's source commit and artifact hashes will be presented together with the CI evidence. Downloaded release assets and installation must be checked after publication. No approval is inferred from tests or automated control fixtures.
