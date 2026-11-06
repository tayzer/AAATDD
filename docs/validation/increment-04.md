# Alpha.4 runtime observation and ambient-input checkpoint

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-04.json) identifies the final local package, tested source, run identities and retained integration reports. Earlier packages and checkpoints remain historical evidence.

## Implemented boundary

The tool packages a BCL-only .NET startup observer and copies it into the identified run artifacts. It observes the harness and scenario-owned .NET API processes without application references to TADA. Each stream records run/process/start identity, sequence numbers, timestamps, located managed assemblies and native module snapshots with SHA-256 byte identities. Memory-only assemblies, unsupported dynamic assemblies and module paths outside captured outputs or the supported platform policy prevent verification. CLR anonymous dynamic methods remain explicitly trusted generated runtime code.

Before execution, TADA captures installed .NET 10 shared-framework files, host components and core Windows DLL identities. This stable platform snapshot and the recorded allowed environment contribute to environment identity; varying load order does not define a different environment. Loaded platform bytes must match the pre-run snapshot. Other native Windows system locations remain an explicit trusted OS policy with their observed hashes retained. The acceptance harness cannot load a SUT artifact into its own process, or vice versa.

The observer records graceful exits automatically. Before forcefully terminating an API, reviewed bindings call the generated `TadaRuntimeCheckpoint.Complete(api)` helper and wait for a flushed final-snapshot acknowledgement. Cleanup still terminates/disposes processes and removes scenario databases if observation fails. An abruptly terminated process without a final snapshot is INDETERMINATE. Application source needs no TADA dependency or shutdown endpoint.

Offline verification requires the environment, runtime manifest, exact raw report set and captured observer identity. It re-evaluates stream continuity, supported inputs, entry/observer coverage and final snapshots. Cached summaries cannot supply missing producer evidence. Current execution also rechecks captured loaded-file and platform hashes; offline verification does not require the old SDK/OS installation to remain present.

## Ambient build inputs

Build and harness processes inherit a recorded allowlist of OS locations and supported .NET/NuGet cache locations. Arbitrary ambient compiler/MSBuild properties, startup hooks, additional dependency paths and profiling injection variables are excluded. The one observer hook is supplied explicitly for execution. All build/restore/evaluation commands disable automatic MSBuild response files. Custom compiler response files are rejected; signing/resource/manifest/ruleset/source-link/configuration file properties join the evaluated-input audit.

Inventory schema 3 explicitly records `evaluated-dotnet-build-v2` and `dotnet-startup-observation-v1`. Earlier baseline schemas remain readable, but cannot acquire the new provenance by default. New execution requires explicit review/approval of a new revision.

## Validation

- Release build and package creation pass. All 80 focused/replay/CLI/history/Git/environment tests pass with zero skips. Controls include missing terminal evidence, unlocated/unidentified assemblies, harness/SUT loading boundaries, cross-run streams, native path casing and ambient environment isolation.
- All 31 real Orders controls pass. The observer identifies native SQLite in the harness and both API processes. A deliberately omitted API checkpoint blocks PASS. Changed environment/manifests, truncated runtime streams and earlier build/native-message controls produce the expected outcomes. Restored evidence verifies offline.
- An invalid automatic `Directory.Build.rsp` demonstrably fails raw MSBuild. The same workspace passes TADA execution because every applicable command disables automatic response files.
- A fresh-cache installation of the final local package establishes assertion RED against HTTP 501, implements the Echo API with a separate library and establishes GREEN, then refactors under the unchanged oracle and passes again. All three implementation identities differ while the baseline and environment remain identical. Maintenance and Git context readback pass.
- An isolated copy of an actual alpha.3 schema-2 baseline remains reviewable with alpha.4; execution returns INDETERMINATE rather than inventing new provenance. The original historical workspace is unchanged.

The first real runtime control found native Windows module filenames using `.DLL` where build artifacts used `.dll`. Windows path lookup now preserves the same identity across casing, with a regression test. That failed assessment was an observation/integrity gap, not business RED. An initial parallel packaging attempt failed silently under the Windows sandbox; unchanged sources packed successfully with one MSBuild worker. Historical failed attempts remain local evidence.

## Remaining release gates

Native capture uses periodic/final snapshots, not a native loader-event feed. A short-lived native module can load and unload between snapshots. Arbitrary custom build-task file reads are also not exhaustively traced. Neither gap is silently reclassified as complete provenance; the [release checklist](../release-readiness.md) keeps that boundary open.

Complete the supported provenance boundary, remaining robustness checks, final-source remote CI, clean-machine validation and actual requester/Codex use with a genuine new-session handoff. Automated approval records here are explicitly isolated controls, not requester business acceptance. No public release is claimed.
