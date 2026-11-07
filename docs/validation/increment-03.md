# Alpha.3 evaluated build-input checkpoint

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-03.json) identifies the package, tested source, retained integration reports and run evidence. Earlier increments and their packages remain historical evidence.

## Implemented boundary

New inventory schema-2 baselines explicitly record the `evaluated-dotnet-build-v1` policy. Human-readable review displays its installed-toolchain trust assumption. The tool records supported evaluated compiler, analyzer, resource, content and reference inputs, project references and the full filenames emitted in MSBuild preprocessed import boundaries. `MSBuildAllProjects` is deliberately not used as a complete import inventory.

Repository inputs must be selected by the appropriate oracle or implementation roots. Harness inputs cannot implicitly borrow unselected application sources. Arbitrary external/shared source is rejected. SDK, reference-pack and workload-manifest inputs are classified from the pinned installation reported by the actual dotnet host. Package inputs must belong to the restored locked graph or an explicitly versioned NuGet SDK. This policy trusts installed SDK/cache contents; it does not claim that a lock's NuGet content hash independently authenticates the installed file bytes.

Each run uses fresh SDK intermediate directories. Reviewed project references are rebuilt in dependency order, with duplicate project filenames and cycles rejected. An intermediate-directory override outside the owned build area is rejected. Existing generated files in a project's old `obj` directory do not acquire trust as fresh build inputs.

Before/after evaluated inputs and import reports are retained. Previously observed non-generated inputs must retain their identities through the build; captured post-build inputs are checked before and after harness execution. Toolchain/package hashes contribute to environment identity. A single path observed with inconsistent identities across project builds blocks execution. Offline verification requires the captured manifest, project coverage and exact report digests. It does not require the current SDK/cache to remain installed.

Schema-1 baselines remain readable, but alpha.3 refuses to infer missing provenance when running or verifying them. Inspect historical alpha.2 results with the matching tool, or explicitly prepare, review and approve a new baseline. New nullable model fields are omitted from historical canonical encoding.

## Validation

- Release build and locked restore pass with zero warnings and errors. All 70 focused/replay/CLI/history/Git tests pass with zero skips.
- All 23 real Orders controls pass, including unselected linked source, an unselected imported targets file, changed build-input evidence and changed raw evaluation reports. Restored retained evidence verifies offline. Existing business-defect, setup, cleanup, stale-review and native-evidence controls continue to behave as expected.
- The final package installs into a fresh cache and new repository. The HTTP 501 starting point produces behavioural FAIL; implementation and a behaviour-preserving refactor both produce PASS under the same approved oracle.
- The Echo API uses a separate reviewed implementation library through `ProjectReference`. Both projects have locks and captured build inputs. The acceptance harness remains separate from that library. A warn-as-error maintenance build and Git-committed context readback pass.
- The original authoritative Orders feature retains SHA-256 `24c3c0b4b54f5bdd191e51b9577fbfc22f0b2522f95779e5acb903e29f3a2c5d`.
- A disposable copy of the actual alpha.2 schema-1 baseline remains reviewable with alpha.3. Running it returns INDETERMINATE for the missing evaluated-build-input policy, without inventing provenance or modifying the original historical workspace.

The initial unrestricted Orders run exposed SDK workload-manifest imports outside the versioned SDK directory; those files are now identified from the same installation. SDK analyzer-config discovery also lists absent ancestor candidates such as a drive-root `.globalconfig`; absent candidates are not file inputs. Unit tests cover missing candidates, selected linked files, spoofed SDK roots and input drift. The final extended Orders checker required a PowerShell property-selection fix before all 23 controls passed. Restricted Git execution still fails at the Windows MSYS boundary; the unchanged Git test passes outside that sandbox. None of these infrastructure/checker failures is reported as business RED.

## Remaining release gates

This is evaluated-input capture, not exhaustive file-read tracing. Arbitrary custom-task reads, ambient compiler/build configuration and dynamically loaded managed/native runtime modules still require a complete supported-boundary policy and validation. Source generators and reviewed build tools remain trusted executable components. Complete execution-input provenance is therefore still unchecked on the [release checklist](../release-readiness.md).

Final-source remote CI, clean-machine validation, actual new-agent context use and requester/Codex install-to-API acceptance remain incomplete. The automated approvals here are labelled controls, not requester business acceptance. No public package release or source publication is claimed by this checkpoint.
