# Reproducible SDK and restore inputs

Researched: 2026-11-03; decision status updated: 2026-11-05. Status: **Research findings**, with initial pinning/restore controls subsequently **ACCEPTED** in ADR 0030. ADR 0012 agrees the execution-input boundary. No SDK/project/lock file was created and no restore/build/test was run.

## Findings

- Microsoft's [global.json documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) documents an exact SDK version with `rollForward: disable`; `allowPrerelease: false` excludes prerelease SDKs. SDK selection is distinct from the runtime actually hosting a process.
- The [NuGet lock-file documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies) describes `packages.lock.json` and locked restore: restore the reviewed graph or fail rather than silently update it.
- Microsoft's [NU1512 documentation](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1512) explains that `RestoreForceEvaluate` overrides `RestoreLockedMode`. The newer warning's availability is SDK-dependent; the approved path must not rely on a warning to prevent the override.

These mechanisms do not establish complete material-input discovery, runtime/native-library identity, deterministic binaries, producer authenticity or L2 isolation. Project SDKs such as MSTest SDK must also have explicit reviewed versions; do not assume that a project's ordinary package lock covers every SDK/import input.

## Initial controls subsequently accepted

- Pin the exact stable .NET 10 SDK in reviewed `global.json`, with SDK roll-forward disabled and prereleases excluded. Pin `MSTest.Sdk` separately.
- Review and retain per-project NuGet lock files, exact direct package selections and relevant restore configuration. Use locked restore during verification, without force-evaluation or automatic lock regeneration. Keep subsequent build/test steps from silently performing an uncontrolled restore.
- Capture actual resolved managed/native dependencies and runtime identities as required by ADR 0012. A matching lock file is not sufficient evidence that arbitrary loaded binaries are approved.

Prepare dependency updates as explicit reviewed changes. Harness/toolchain input changes follow baseline amendment rules; SUT dependencies retain their separate implementation/environment identity and do not automatically become oracle amendments. Concrete versions, paths, restore/build/test invocation, package-source policy and identity encodings remain open. Pinning must support explicit maintenance updates rather than imply indefinite use of an obsolete patch.

**Outcome:** The user accepted these controls in [ADR 0030](../decisions/0030-pin-sdks-and-use-locked-restore.md). The [direct-version candidate review](2026-11-05-version-candidates.md) remains proposed; exact configuration and the resolved dependency graph are not approved by this research.
