# Bounded compatibility probe: execution approval proposal

Prepared: 2026-11-05. Status: **EXPLICITLY AUTHORISED AND COMPLETED**. The user replied **“Yes”** to the request to authorise this bounded probe, including minimal code and restore/build/test execution. ADR 0031 alone selected direct targets; this later approval granted the limited execution scope below. See the [completed evidence report](../research/compatibility-probe-results.md). Broader implementation remains unauthorised.

## Objective and boundary

Determine whether the selected SDK, MSTest SDK, Reqnroll/MSTest/MTP and SQLite provider can build and run a small technical harness and produce usable native Cucumber Messages. Resolve and report its actual dependency inputs. This probe is not the Orders acceptance baseline or a framework implementation.

Use a new directory `experiments/compatibility-probe/` within the current workspace. If that path already contains unrelated work, stop and report rather than overwrite it. Keep every created project, technical scenario/binding, configuration, task-local SDK/cache, lock file, database, log and report inside that directory. Record the evidence summary in `docs/research/compatibility-probe-results.md` and update active design status to reflect findings. Retain reviewable probe sources and reports after completion.

## Authorised actions and limits

1. Inspect installed SDKs and use exact .NET SDK `10.0.403`. If absent, download official SDK binaries to the probe's task-local tools directory, verify published integrity information and use them through process-local settings. Do not install globally or change persistent environment/IDE settings.
2. Create a minimal `net10.0` harness using `MSTest.Sdk` `4.5.2`, aligned Reqnroll integration/core/generation `3.3.4`, and `Microsoft.Data.Sqlite` `10.0.14`. Use explicit MTP selection and disabled scenario parallelism. Create only synthetic technical scenarios for this probe; preserve the agreed Orders feature byte-for-byte.
3. Use task-local NuGet/CLI caches and a probe-local configuration restricted to the official NuGet feed. An initial exploratory restore may generate the candidate graph because no lock files exist. Label it unapproved dependency discovery, then freeze the generated candidate locks and perform locked restore before measured smoke execution. Record SDK imports, feeds, exact resolved managed/native package versions and observed runtime/native identities. Do not bypass conflicts with other versions or switch to VSTest.
4. Build and discover the technical cases, then run one positive smoke assessment and one explicit negative control. Keep run identities and original outcomes distinct. Verify serial scenario hooks/context, native Messages source/case/status links and identifiable run completion. Inject one intended assertion failure for the negative control; a setup crash does not count as detecting that fault. No automatic scenario retries.
5. Exercise the selected SQLite provider against probe-owned disposable files: fixture creation, SUT-style `ReadWrite` opening, distinct `ReadOnly` observation, missing-file/no-create behaviour, read-only write rejection and disposal/deletion with pooling disabled. These are provider-capability checks on synthetic probe data, not an approved privileged observer for Orders.
6. Preserve sources, locks, logs and raw messages. Clean only established probe-owned disposable resources, record cleanup status and report exact successes, failures, gaps and commands. Stop after the compatibility report; propose amendments for blockers.

## Scope and limits

No Orders business bindings or API implementation, AAATDD verifier/CLI, general provider framework, protected CI, Git initialisation/commits, public repository, external publication or unrelated cleanup belongs in this probe. It does not approve business-case discovery or full Orders coverage. Report these gaps explicitly: separate HTTP SUT execution, atomic order/stock behaviour, the concrete observer exception, full evidence-envelope schema and L2 protection remain unvalidated.

The proposed working limit is 30 minutes of probe execution, with a five-minute bound per download/restore/build stage and a 60-second bound per smoke run. Stop on exhausted bounds, incompatible dependencies, unavailable exact SDK, unverifiable SDK download integrity or a required platform substitution. Do not extend budgets or introduce alternative versions automatically. A routine focused source/configuration correction within the approved probe scope may be attempted within the original limits, preserving earlier evidence.

## Reviewable outputs and result interpretation

Deliver a Markdown compatibility report, a dependency/runtime/native identity inventory, explicit launch/build/restore/discovery commands, candidate project-SDK imports and locks, original positive/negative Messages streams and a table relating expected technical controls to observed results and cleanup. Do not label synthetic probe results as Orders `PASS`, infer approved coverage from platform success or treat the candidate graph as an approved oracle baseline.

The user's approval granted limited implementation/dependency/execution authority for this probe and its stated reporting paths only. The main project retains its design-only restrictions, and broader implementation still needs separate authorisation. Preserve unresolved compatibility and acceptance questions honestly rather than treating this technical result as a complete executable baseline.

**Approval reference:** On 2026-11-05, the user replied **“Yes”** to “Do you authorise this probe, including minimal code and restore/build/test execution?” The preceding summary linked this plan and explicitly included task-local SDK/dependency downloads, isolated technical checks, retained evidence and the 30-minute stop limit. Execution is now complete; this approval is not continuing authority for further probes or product implementation.
