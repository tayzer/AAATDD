# Execution boundaries and reuse research

Researched: 2026-10-26; sample-storage and baseline-input follow-ups 2026-10-27. Status: **Research findings** and explicitly marked **Proposals**. Sources are primary project documentation and schemas. Nothing was installed or executed; no compatibility, security or performance claim has been validated experimentally.

## Existing capabilities

| Source | Finding | Design implication, not an approved integration |
| --- | --- | --- |
| [Reqnroll formatters](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html) | Reqnroll v3+ includes a Cucumber Messages NDJSON formatter. Custom formatter APIs carry a warning about changes even in minor releases. | Prefer consuming the native stream over writing a formatter. Pin runner, formatter and supported message-schema versions during an authorised spike. |
| [Reqnroll context injection](https://docs.reqnroll.net/latest/automation/context-injection.html) and [hooks](https://docs.reqnroll.net/latest/automation/hooks.html) | Scenario-lifetime objects can be injected into bindings. Hooks support setup/teardown; same-type hook order is undefined unless explicitly specified. | Reuse scenario-scoped state and controlled hooks. DI scope alone does not isolate a database or external environment. |
| [Reqnroll driver pattern](https://docs.reqnroll.net/latest/guides/driver-pattern.html) | The project already recommends thin bindings backed by helper classes and context injection. Its driver examples include UI-specific assertions. | Reuse the organisation pattern. AAATDD's protocol-driver term is narrower: domain interpretation and business assertions stay above technical transport helpers. |
| [Cucumber Messages](https://github.com/cucumber/messages) and its [schema](https://github.com/cucumber/messages/blob/main/jsonschema/messages.md) | Messages link sources, compiled cases, steps, attempts, results and attachments. `TestRunFinished.success` permits passed or skipped steps. | Use messages for native evidence, then independently reconcile required execution. A successful stream is not an approved requirement-coverage verdict. |
| [Gherkin library](https://github.com/cucumber/gherkin) | Existing parsers compile Gherkin to executable cases. Examples use generated UUIDs or incrementing IDs. Messages relate Scenario Outline cases back to scenario and example-row AST nodes. | Reuse the parser/compiler. Keep durable requirement/scenario/example identities distinct from run-local IDs and source line numbers. The exact mapping is open. |
| [Testcontainers readiness](https://dotnet.testcontainers.org/api/wait_strategies/) and [cleanup](https://dotnet.testcontainers.org/api/resource_reaper/) | Container-running status alone is insufficient readiness for many images; explicit waits and resource cleanup exist. | Reuse them when a real database container is justified. Still validate the seeded business preconditions, scenario ownership and cleanup failures. |
| [.NET HttpClient](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) | The standard client already sends/receives HTTP and manages connection pooling. | Start with a thin wrapper if needed for correlation and evidence. Avoid a new HTTP stack; isolate scenario authentication and do not silently retry non-idempotent actions. |
| [ASP.NET Core integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) | `WebApplicationFactory` and `TestServer` simplify hosting and test configuration. | Useful for ordinary integration tests. Loading the SUT into the test host is not hostile-code isolation; it cannot establish the proposed L2 trust boundary. |
| [Cucumber.js World](https://github.com/cucumber/cucumber-js/blob/main/docs/support_files/world.md) and [formatters](https://github.com/cucumber/cucumber-js/blob/main/docs/formatters.md) | World supplies per-scenario state, including fresh retry instances. The native `message` formatter emits Cucumber Messages. | A credible second runner for a later portability experiment. JavaScript bindings and technical helpers still need their own implementation and approval. |

The reviewed [Cucumber Messages licence](https://github.com/cucumber/messages/blob/main/LICENSE) and [Testcontainers for .NET licence](https://github.com/testcontainers/testcontainers-dotnet/blob/develop/LICENSE) are MIT. Reqnroll's BSD-3-Clause source was checked in the [initial research](initial-landscape.md); recheck the selected versions and transitive dependencies before adoption. No dependency choice is approved here.

## Is fixture/action/observation/assertion separation useful?

**Assessment:** yes as a separation of responsibilities. It exposes whether a test established the intended starting state, exercised the application, measured an actual result, and compared that result with an approved expectation. It is not itself an assurance boundary or proof that each component is correct.

**Proposal:** begin with ordinary scenario-scoped classes or functions. A fixture prepares and validates preconditions; a technical HTTP helper sends the action; observation code produces a measured value with provenance; a binding or assertion helper applies the expectation. These responsibilities may share technical plumbing. They do not need separate registries, processes, universal object models or published extension APIs in the first slice.

Preserve the distinction between measurement and acceptance. An observer may report connection failure, missing data, collection gaps or timeout. It must not turn those into an empty result or a business pass. It may raise infrastructure errors without deciding business acceptance.

## Sensitivity and unchanged-source limits

An unapproved binding edit can be rejected by baseline integrity checks even if the native runner passes. That validates tamper detection only. An already approved no-op, weak predicate, wrong fixture, stale data source or environment-dependent helper may retain the same source hashes and still give false confidence.

**Proposal:** use targeted wrong responses and a known-bad SUT variant to check newly introduced assertions where practical. Keep expected values derived from the approved scenario, not reconstructed from the same database representation used by setup. Test fixture validation separately from outcome assertions. Record incomplete sensitivity evidence rather than inventing artificial RED history when existing behaviour already satisfies the requirement.

Small structured observation/assertion records may explain which values and expectations were compared. Counters, attachment presence and self-reported success cannot prove meaningful assertions or establish authenticity outside a protected producer. Review and sensitivity checks remain necessary.

## Temporal limits

[Awaitility](https://github.com/awaitility/awaitility/wiki/Usage#assert-that-a-value-is-maintained) already supports conditions maintained over a duration. This illustrates reusable temporal primitives; it is a Java library, not a selected .NET dependency. Polling tools alone cannot establish complete observation of transient events.

**Proposal:** distinguish immediate, eventually-within-deadline, stable-over-interval, bounded absence and observation after a meaningful processing watermark. A sampled stable state is different from continuous event coverage. For absence checks, start capture before the action, establish readiness, correlate the operation and retain the observation interval, coverage and completion status. Observer failure or lost coverage invalidates an absence claim. A watermark must actually cover the work and event stream in question; generic “system idle” is insufficient. Controlled clocks help only when relevant components use them.

Timing that is part of the business promise belongs in Gherkin. Technical polling, readiness and collection budgets belong in approved bindings/policy. If only bounded absence is demonstrated, evidence must state that limit. No unbounded absence proof follows from a sleep or empty queue.

## L2 trust boundary

[GitHub's security guidance](https://docs.github.com/en/actions/reference/security/secure-use) documents risks from privileged workflows processing untrusted code and from jobs sharing files, caches or Docker access. This is evidence about a possible CI platform, not a GitHub-only AAATDD requirement.

**Proposal:** distinguish the untrusted implementation build and runtime from the trusted harness, baseline and verifier. Untrusted build scripts must not execute with approval/signing credentials or access to trusted oracle output paths. An isolated build produces an identified SUT artifact; approved harness code exercises it across a restricted interface. Candidate reports and attachments are data until checked, not authority. Protect workflow selection, policy, dependency resolution, caches and evidence publication as well as feature files.

A separate process or container is only one control. Shared privileged mounts, writable harness paths, host/container-management sockets, or common credentials can invalidate the separation. The platform and adversary assumptions need explicit review before any L2 claim. L1 can exercise the same contract locally with advisory integrity checks, but cannot prevent a same-user process from replacing its own baseline and evidence.

## Sample-storage follow-up: 2026-10-27

The [Microsoft.Data.Sqlite overview](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) identifies it as a lightweight ADO.NET provider usable independently of Entity Framework Core. Its [connection-string documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) supports a database-file path and `Mode=ReadOnly`; the default instead permits creation. Its [transaction documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions) covers atomic commits and rollback, with locking/concurrency limits. These capabilities make it a candidate for the isolated synchronous sample, not evidence that the future implementation will satisfy the business contract.

[SQLite's own documentation](https://www.sqlite.org/inmemorydb.html) distinguishes ordinary named files from `:memory:` and empty-filename temporary databases. Shared in-memory connections must be in the same process; each empty-filename temporary database is private to its creating connection. **Proposal:** a uniquely named, explicitly managed disposable file per scenario can serve the separate SUT and observer processes. This does not imply that an anonymous temporary database is shareable or that cleanup is automatic.

[Testcontainers for .NET](https://dotnet.testcontainers.org/) requires a Docker-API-compatible container runtime. A server database managed through it remains a candidate when actual engine semantics matter. No local Docker availability, startup cost or package compatibility was tested.

The reviewed Microsoft.Data.Sqlite source repository's [licence](https://github.com/dotnet/efcore/blob/main/LICENSE.txt) is MIT. The resolved package and native dependency set still need review before adoption. No package version, storage engine, fixture implementation or privileged observer is approved by this research.

## Harness folders and effective execution inputs: 2026-10-27

[MSBuild's directory customisation documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=visualstudio) describes upward discovery of `Directory.Build.props` and imports of shared targets. A harness directory boundary therefore does not by itself contain every build input. The document also describes explicit import controls; no build settings have been selected or changed here.

[NuGet's dependency-lock documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies) describes a lock file for the resolved graph, ordinary restore updating that graph when inputs change, and locked restore failing instead of accepting changed dependencies. A helper library's lock file does not determine the consuming project's resolved graph. Locked package resolution alone does not pin every build, runtime, configuration or native input.

**Proposal:** capture the acceptance project's execution inputs plus identified shared/imported inputs and resolved dependency/toolchain identities. Use controlled imports and locked restore when the .NET harness is implemented. Track the SUT separately and avoid a direct reference to its implementation assembly in the first acceptance harness. This is a boundary-design proposal, not a completed dependency inventory or a proof of hermetic execution. See the [baseline-scope discussion](../architecture/first-experiment-baseline-proposal.md).

## Source anchors

Reviewed Git blob IDs: Cucumber Messages README `769530fdf7dcd2534183848c460098f5641d5d4d`; message schema description `c987d6377b9bc926fc72c03abe5a2d9eef03b9f7`; Gherkin README `9b28b04acff029bc0b0c612ac4ebf8ceae50cab4`; Cucumber.js World `76bb2a817943d7e94626c713e707a12345649e0d`; Cucumber.js formatters `60c787cf0c04b550df8775e92c51e467bad2f6c7`. These identify reviewed source content, not an approved mutually compatible release set. Website references are retrievals on the research date and may change.

The 2026-10-27 follow-up read the dotnet/efcore licence blob `a616ed188dfce68ee308f674350ad242d8588c2b`. That identifies reviewed licence content, not a selected package release.
