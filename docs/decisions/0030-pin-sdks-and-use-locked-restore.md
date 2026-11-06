# ADR 0030: Pin SDKs and use locked restore

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves Q4's initial pinning/restore controls; exact versions, configuration and runtime identities remain open.

## Context

ADR 0012 includes material SDK, build and resolved dependency inputs in the initial harness boundary. Ordinary SDK selection or dependency restore can change those inputs. The reviewed configuration needs repeatable selection and explicit updates.

## Decision

- Pin the exact stable .NET 10 SDK in reviewed `global.json`, disabling SDK roll-forward and excluding prereleases. Pin `MSTest.Sdk` separately.
- Review and retain per-project NuGet lock files, direct package selections and relevant restore configuration. Use locked restore during verification without force-evaluation or automatic lock regeneration.
- Prevent later build/test stages from silently restoring another graph. Reject dependency drift rather than treating updated inputs as already approved.
- Prepare updates as explicit reviewed changes. Changes affecting approved harness/toolchain inputs follow baseline amendment rules; SUT dependencies retain separate implementation/environment identity and do not automatically become oracle amendments.

Capture actual runtime and resolved managed/native library identities separately. SDK pinning does not pin every running process, and a matching lock file does not authenticate arbitrary loaded binaries or cover every imported/project-SDK input.

## Alternatives considered

- Floating SDK/package selection can change verification inputs without a corresponding review.
- Automatically regenerating lock files during verification defeats comparison with the reviewed graph.
- Treating all SUT dependency changes as oracle amendments would contradict the separately assessed implementation boundary.

## Consequences and limits

Exact versions, file paths, package-source policy, identity encodings, restore/build/test commands and enforcement mechanisms remain pending review. Pinning supports explicit maintenance updates; it does not justify indefinite use of obsolete patches. These controls do not establish complete input discovery, deterministic binaries, producer authenticity or L2 isolation. No configuration/lock file, installation, restore, build, implementation or test execution is authorised.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Should we adopt these pinning and restore controls?” The preceding recommendation specified exact .NET/project-SDK pins, reviewed NuGet lock files, locked restore without automatic regeneration, rejected dependency drift, explicit update review and separate runtime/native identity checks, leaving exact versions unselected.

Related: [ADR 0012](0012-bound-the-initial-oracle-to-harness-inputs.md), [ADR 0013](0013-separate-baseline-authority-from-verification.md), [ADR 0025](0025-use-mstest-sdk-for-acceptance-harness.md), [pinning research](../research/2026-11-03-reproducible-inputs.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
