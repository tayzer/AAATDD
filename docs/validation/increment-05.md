# Alpha.5 transient native-load checkpoint

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-05.json) identifies the tested package/source, native bootstrap, integration reports and migration evidence. Earlier checkpoints and packages remain historical records.

## Implemented boundary

A small native-only Windows callback now retains ordinary DLL load notifications in a bounded, preallocated queue. It calls no CLR, allocator, file I/O or imported routine while under the loader callback. Managed code drains the queue and identifies file bytes after the callback returns. Initial/final module snapshots remain useful for pre-existing modules. See the [implementation research and source references](../research/native-loader-observation.md).

Every process stream requires the captured native helper's startup identity, continuous load-event numbers and a matching final loader count. Unsupported registration, missing events, queue overflow, oversized paths or unavailable files prevent PASS. Queue limits are 4,096 events and 2,047 UTF-16 path characters. A final API checkpoint is still mandatory before forceful termination.

Inventory schema 4 explicitly records `dotnet-loader-observation-v2`; runtime manifests use schema 2. The managed/native observer artifacts and raw stream hashes remain linked to the run. Older baselines cannot acquire loader-event coverage by default. An isolated copy of a real alpha.4 schema-3 baseline remains reviewable, while new execution returns INDETERMINATE until a new baseline is prepared/reviewed/approved.

## Native build and packaging

Source builds bootstrap checksum-pinned Zig 0.15.2 under `.artifacts/`, using PowerShell 7 and no system installation. The fresh download/checksum/extraction/build path was executed in a new compiler cache. That build and the existing compiler cache produced identical helper bytes. Native build metadata records source/compiler/output identities. Unneeded debug identifiers are stripped during linking; hashing still uses exact produced bytes.

The package includes TADA's native helper, with no compiler or native test fixture. PE inspection requires Windows x64 and only `KERNEL32.dll` imports; no additional C runtime DLL is required. The application has no TADA project/package reference. A new NuGet cache and application repository exercised the final package.

## Validation

- Release build, locked graph restoration and package creation pass. All 86 focused/replay/CLI/history/Git/native tests pass with zero skips.
- A real native fixture loads, returns 42 and unloads immediately. It is absent from `Process.Modules` before polling, but its retained loader event remains available. A separate 4,100-load control overflows the bounded queue and cannot complete successfully.
- All 35 Orders controls pass. An unselected fixture DLL loads and unloads before business assertions: native execution succeeds, no module snapshot contains it, and the retained loader event causes TADA INDETERMINATE. A captured/reviewed copy also has no snapshot occurrence, retains exactly one loader event and produces PASS. Existing business-defect, missing-checkpoint, setup, cleanup and integrity controls retain their expected outcomes.
- Fresh-package Echo development produces assertion RED, implementation GREEN and refactor GREEN under one unchanged baseline/environment, with three distinct implementation identities. The separate implementation library, maintenance build and Git handoff pass.

The initial compiler invocation was restricted from writing its workspace-owned cache; unchanged compilation succeeded outside the Windows sandbox. Initial isolated native builds differed because of unneeded debug identifiers; explicit stripping made the output identical across output directories and fresh/cached toolchains. Four synthetic runtime tests initially inserted earlier timestamps after their synthetic stop marker; the fixtures were corrected without relaxing the verifier. Real loader, overflow and existing Orders controls already passed. These were build/test-fixture failures, not behavioural RED.

## Remaining release gates

The native boundary covers the ordinary Windows loader from registration through the final checkpoint/graceful exit. It is not immutable capture of mapped memory or protection against manual image mapping and a controlling local actor. Supported file bytes must remain stable. These are explicit local advisory assumptions, not protected assurance claims.

Arbitrary custom build-task file reads remain an open provenance boundary. Complete that boundary, remaining robustness checks, final-source remote CI, clean-machine validation and actual requester/Codex API development with a genuine new-session handoff. Automated fixture approvals are not requester business acceptance. No public release is claimed.
