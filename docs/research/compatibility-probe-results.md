# Compatibility probe results

Executed: 2026-11-05, Windows x64, under the user's explicit approval of the [bounded probe plan](../architecture/compatibility-probe-plan.md). Status: **COMPLETED — bounded technical compatibility demonstrated**. This is technical probe evidence, not Orders acceptance, an approved executable baseline or L2 assurance. Probe execution began at 12:22:49 UTC; measured smoke runs finished by 12:29:57 UTC, within the 30-minute budget.

## Result and scope

The selected direct versions restored and built together, generated Reqnroll cases and executed through native MTP. Discovery found exactly the two synthetic technical scenarios. The positive run passed both; the deliberately faulty run failed at its intended assertion while the SQLite scenario still passed. Both raw native streams contain complete run/case/step lifecycle records for the expected technical cases, with no scenario retries observed.

Sources and generated evidence are in the [isolated probe](../../experiments/compatibility-probe/README.md). No Orders API/business bindings, AAATDD runtime/CLI, CI workflow or public artifact was created. Existing Git metadata was already present when the probe began; no repository initialisation, staging, commit or push occurred. The previously recorded claim that this directory was not a Git repository is therefore stale and has been corrected in active design documents.

## Checks and retained outcomes

| Check | Observed result | Evidence |
| --- | --- | --- |
| Exact SDK acquisition | SDK `10.0.403` was absent locally; downloaded official Windows x64 archive and matched Microsoft's published SHA-512 before extraction | [Archive verification](../../experiments/compatibility-probe/evidence/sdk-archive-verification.json), [official release metadata](../../experiments/compatibility-probe/evidence/releases-10.0.json) |
| Exploratory restore | Succeeded using the probe-local official NuGet configuration and caches; candidate graph generated | [Restore command and logs](../../experiments/compatibility-probe/evidence/restore-20261105T122726Z-56396ec112d34237be69a0653098fc78/command.json) |
| Locked restore | Succeeded with no warnings/errors; frozen lock hash remained unchanged through execution | [Locked-restore command](../../experiments/compatibility-probe/evidence/locked-restore-20261105T122758Z-b9828f82f7aa432ca718a231da195910/command.json), [candidate lock](../../experiments/compatibility-probe/packages.lock.json) |
| First build, sandboxed | Failed in Reqnroll generation with `MSB4216`/`MSB4027`: task-host creation/connection unavailable | [Retained failed-build output](../../experiments/compatibility-probe/evidence/build-20261105T122811Z-8c3470592cfe4fc58f78099e456dc1ab/stdout.txt) |
| Unchanged build outside sandbox restrictions | Succeeded, zero warnings/errors; generated feature code and native message resources | [Successful-build output](../../experiments/compatibility-probe/evidence/build-20261105T122856Z-9d96eef62911434fb8a442b6f3b1877b/stdout.txt) |
| Native MTP discovery | Exactly `Observe isolated execution context` and `Exercise SQLite connection roles` | [Discovery output](../../experiments/compatibility-probe/evidence/discover-20261105T122925Z-bc223521aaeb444d8e1c9e879860d19a/stdout.txt) |
| Positive control | 2 succeeded, 0 failed/skipped; executable exit `0`; 10 native step results `PASSED` | [Positive raw Messages](../../experiments/compatibility-probe/evidence/positive-20261105T122940Z-847b822a78934b7ab5ccca1b9d3fc02d/native-messages.ndjson) |
| Deliberate assertion failure | 1 failed, 1 succeeded, 0 skipped; executable exit `2`; native failure contains `PROBE_EXPECTED_ASSERTION_FAILURE` | [Negative raw Messages](../../experiments/compatibility-probe/evidence/negative-20261105T122955Z-68932621b5784032bee8791efaa03730/native-messages.ndjson), [negative output](../../experiments/compatibility-probe/evidence/negative-20261105T122955Z-68932621b5784032bee8791efaa03730/stdout.txt) |
| Context/hooks and retries | Two distinct contexts per run, ordered `before/after/before/after`, active count never above 1; attempt 0 for both cases, `willBeRetried=false` | [Control summary](../../experiments/compatibility-probe/evidence/control-summary.json), [positive hook/provider events](../../experiments/compatibility-probe/evidence/positive-20261105T122940Z-847b822a78934b7ab5ccca1b9d3fc02d/probe-events.jsonl) |
| SQLite roles | Fixture created the file; `ReadWrite` changed and committed a value; fresh `ReadOnly` observed it; missing-file opens rejected with SQLite code `14`; attempted read-only write rejected with code `8` | Same retained provider events and native step results |
| Required teardown | Non-pooled disposed connections permitted deletion; no owned scenario databases remained. One residual probe-owned `VBCSCompiler.exe` was stopped by validated executable path; no probe-owned process remained | [Cleanup record](../../experiments/compatibility-probe/evidence/probe-cleanup.json) |

The initial failed build remains preserved. No package or probe source was changed between the failed and successful build; relaxing sandbox child-process restrictions was sufficient for the latter. This establishes that the combination works in the observed execution environment, not that it works under the sandbox restriction. The PowerShell tool wrapper reports a generic shell failure for the negative stage; the child's exact MTP exit `2` is retained in its `command.json` and output.

## Resolved identities and provenance

| Input or observed component | Version/identity |
| --- | --- |
| Task-local .NET SDK | `10.0.403`, MSBuild `18.9.15+5a2162bea` |
| Actual test runtime | `.NET 10.0.14`, x64 |
| MSTest project SDK/framework/adapter | `4.5.2` |
| Microsoft.Testing.Platform and MSBuild integration | `2.5.2` |
| Reqnroll core/MSTest/generation | `3.3.4` |
| Cucumber.Messages package and emitted protocol | `30.1.0` |
| Gherkin | `35.0.0` |
| Microsoft.Data.Sqlite/Core | `10.0.14` |
| SQLitePCLRaw bundle/core/provider/native package | `2.1.13` |
| Observed native SQLite engine | `3.53.3` |
| Loaded `e_sqlite3.DLL` SHA-256 | `B7385D722C83FB52142A00477A726723745916D22A555711EE89834C1111FB2E` |

The [dependency inventory](../../experiments/compatibility-probe/evidence/dependency-inventory.json) records 24 graph packages with resolved versions/content hashes plus the separately hashed MSTest SDK package. The SDK archive hash is recorded independently. The full [preprocessed project](../../experiments/compatibility-probe/evidence/imports-20261105T122838Z-0bfc8bc2de024aaf83b2ffa319548c5a/project-preprocessed.xml) and [129 captured import-file hashes](../../experiments/compatibility-probe/evidence/import-inventory.json) expose the evaluated SDK/build inputs. All captured import paths were inside the probe directory. This is not a claim of complete automatic material-input discovery.

The [source/output inventory](../../experiments/compatibility-probe/evidence/source-output-inventory.json) associates retained source/configuration hashes with built executable/DLL/dependency/runtimeconfig identities and the successful build command. Both run-time assembly inventories report the same probe DLL hash, matching the retained built DLL. Each run also records loaded managed assemblies and SQLite-related module paths/hashes in its runtime identity record. These are L1 provenance observations, not signed attestation or tamper protection.

Both native streams contain 40 parseable message envelopes: one source/document, two compiled pickles/cases/starts/finishes, ten step starts/finishes and a linked run finish. The inspection script checked case/pickle and step/result references, expected technical names, one attempt per case, intended failure reason and context/cleanup events. It did not perform full JSON-schema conformance or implement a general adapter/verifier. Native run success is used only alongside the probe's explicit technical inventory and control observations.

## Authority and remaining work

The candidate lock graph and local sources are now reviewable evidence; they are not automatically approved oracle inputs. Remaining baseline work includes actual Orders bindings and separate HTTP SUT behaviour, request/response types and field validation, concrete transaction/SQL implementation, Orders fixture/readiness controls, the scoped privileged-observer exception, evidence-envelope schema/identity rules, negative controls for business faults/integrity/evidence gaps and the complete L1 contract. No second platform, hostile-code protection or L2 isolation was exercised.

The limited probe is complete and execution has stopped. Broader implementation remains unauthorised. Further work should follow review of these findings and the remaining baseline decisions rather than silently promoting the technical probe into the product harness.
