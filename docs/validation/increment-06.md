# Alpha.6 build-task input checkpoint

Status: **DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-06.json) identifies the tested package, sources and control reports. Earlier checkpoints remain historical evidence.

## Implemented boundary

The packaged MSBuild logger observes actual locked restore and rebuild commands. It requests structured task inputs at diagnostic verbosity, records task origins/targets, actual task assembly paths/hashes, known file parameters, sequence/timestamps and successful completion. It references the selected SDK's MSBuild API without redistributing SDK MSBuild binaries. Each run copies the logger to its own observer directory before loading it, avoiding a self-rebuild file lock.

Known standard file-reading tasks must consume approved repository inputs, fresh generated files or trusted toolchain files. Opaque custom code, including `Exec`, requires exact reviewed origin/target/task contracts. Detected file/opaque project evaluation requires an explicit evaluation contract. Contract origins and declared read roots join the immutable oracle snapshot. The [contract guide](../build-task-contracts.md) describes configuration, review and limitations.

Inventory schema 5 records `evaluated-dotnet-build-v3` and `msbuild-tasks-and-declared-inputs-v1`. Runtime policy remains `dotnet-loader-observation-v2`. Retained `task-inputs.json`, the logger artifact and raw task journals are linked to actual command identities. Offline verification checks hashes, task lifecycle, required scalar parameters and report/command coverage. TEMP/TMP use fresh run-owned directories; environment identity normalises the run path so equivalent executions retain the same environment identity.

## Validation and fixes

The focused suite passes 98 tests with zero skips. It covers unselected task data, captured declarations, approval invalidation after input changes, exact contracts, opaque evaluation, task assembly changes, missing task completion and missing required scalar inputs. Empty item-array controls cover `Copy`, `Csc`, `GetFileHash` and `GetAssemblyIdentity`.

All 46 real Orders controls pass. A reviewed target reading an unselected data file blocks PASS; selecting its declared read root and approving the new baseline permits PASS. Undeclared `Exec` blocks PASS, while its exact reviewed contract permits the isolated no-input control. Corrupted task manifests and raw journals block offline PASS; restored evidence verifies successfully. Existing business-defect, transient DLL, missing-checkpoint, seed, cleanup and evidence controls retain their intended outcomes.

Package inspection confirms one task logger, one native observer and third-party notices, with no SDK MSBuild binary or native test fixture. The first 40 ADRs and original Orders feature retain their checkpoint hashes.

Real SDK evidence exposed a false rejection: empty static-web-assets `Copy` tasks legitimately omit `SourceFiles`. Required-input checks now distinguish scalar `ReadLinesFromFile.File` from item arrays that MSBuild omits when empty. Task parameters still undergo file-identity checks whenever supplied.

Installed-package validation exposed .NET's local-tool resolver cache retaining an earlier development build of the same package version despite a fresh package cache. The onboarding control now isolates both `DOTNET_CLI_HOME` and `NUGET_PACKAGES`, and compares executed logger bytes with the newly installed package. Fresh-package Echo development establishes assertion RED, implementation GREEN and refactor GREEN under one unchanged baseline/environment and three distinct implementation identities. The maintenance build and Git-committed handoff pass.

An actual schema-4 alpha.5 baseline is copied without modifying its bytes. Alpha.6 can review it, but execution returns INDETERMINATE until a new baseline is prepared, reviewed and approved. No historical approval or observation coverage is inferred.

The initial restricted runs encountered Git MSYS, Reqnroll task-host and NuGet access failures. Unchanged controls ran with the required permissions. One unrestricted focused run also encountered a transient snapshot directory rename denial; the subsequent full suite passed without changing publication semantics. None of these infrastructure failures counted as behavioural RED.

## Remaining release gates

Custom-task contracts are explicit trust declarations, not arbitrary I/O tracing or a sandbox. Reviewed opaque code is trusted to honour complete read-root declarations. Installed SDK/package contents, stable file bytes and cooperative local producers remain L1 assumptions. These controls do not prove complete automatic discovery of all possible execution inputs.

The supported provenance boundary still needs a consolidated review against its documented trust assumptions. Remaining work includes robustness/release audit, final-source remote CI, clean-machine validation, genuine fresh-agent context use and actual requester/Codex API acceptance. Automated fixture approvals are not requester acceptance. No public package release is claimed.
