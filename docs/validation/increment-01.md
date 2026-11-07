# First executable implementation increment

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-01.json) identifies the actual package, source-file hashes, test counts and retained validation reports. It is a snapshot of this increment, not a promise that later builds have the same identity.

## Demonstrated

- Core and CLI build without warnings/errors; 40 focused, real-Messages replay and owned-process deadline tests pass with zero skips.
- The CLI captures exact-byte inputs and logical Gherkin cases, preserves snapshots/reviews/approvals, runs a locked restore and fresh build, executes Reqnroll/MSTest/MTP and verifies retained evidence without rerunning the application.
- Both agreed Orders scenarios pass against the separate HTTP API. The controlled oversubscription fault fails at the rejection assertion. Wrong seed and required cleanup gaps produce INDETERMINATE, as do changed oracle inputs, truncated Messages and incomplete metadata. Changed reviewed bytes cannot be approved.
- The versioned local tool package installs into a fresh package cache, initialises a new project and runs/verifies a separate public-HTTP Echo API with two scenarios without TADA core changes. A maintenance build and context handoff are exercised without adding an unnecessary business scenario.
- The package contains a readme and full notices for its two bundled third-party libraries. Packaged core/CLI bytes match the validated Release binaries.

Automated approvals are explicitly labelled integration controls in disposable projects. They are not human business acceptance or approval of a production oracle. Original accepted ADRs and the agreed Orders feature remain historical/accepted authority.

## Reproduce

Use the [quickstart](../quickstart.md). Build/execute `tests/Tada.Core.Tests`, pack `src/Tada.Cli`, then run [Orders controls](../../scripts/Test-OrdersWorkflow.ps1) and [installed-package onboarding](../../scripts/Test-Onboarding.ps1). Each script retains a unique workspace and evidence report under `.artifacts/`. Reqnroll task-host execution and local HTTP processes require permissions unavailable in some restricted Windows sandboxes; failed attempts remain INDETERMINATE, not behavioural RED.

An initial linked-feature setup compiled but ran zero tests. The evaluator blocked PASS; the generator input was corrected. A subsequent sandbox task-host failure was retained, and unchanged sources ran successfully with the needed permission. A wrapper originally inherited an intentional negative-control exit code despite successful checks; explicit script completion status fixed that and both scripts subsequently completed with exit zero.

## Still required

The [release checklist](../release-readiness.md) remains open. This increment does not establish complete external/import/loaded-runtime provenance, fully validated failure-history/interruption policy, protected assurance, a clean-machine CI result, public package availability or the requester's actual Codex/API-from-scratch experience. UI clarity and onboarding also remain subject to that real user acceptance. Source edits are uncommitted; no remote/publication action is claimed.
