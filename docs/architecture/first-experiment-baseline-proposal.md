# First experiment: executable-baseline proposal

Status: **PARTIALLY DECIDED**. Prepared: 2026-10-27; updated: 2026-11-05. ADRs 0009–0031 accept verification outcomes, identity/baseline policies, evidence composition, the .NET 10 sample stack and direct validation targets, minimal database schema, explicit connection roles, pinning/restore controls and sample verification policies described below. Remaining choices and concrete controls are proposals unless explicitly marked agreed. This document grants no implementation or privileged-observation authority. The [Gherkin feature](../requirements/order-stock-validation.feature) remains the single business specification.

## Agreed foundation

[ADR 0005](../decisions/0005-validate-l1-before-protected-l2.md) selects L1 first and L2 next. [ADR 0006](../decisions/0006-bound-the-initial-orders-example.md) agrees the bounded business intent. [ADR 0007](../decisions/0007-use-public-outcomes-with-scoped-order-observation.md) agrees the observation split. [ADR 0008](../decisions/0008-use-disposable-sqlite-per-scenario.md) selects a disposable named SQLite file per scenario.

The core outcomes, initial retry policy and identity roles below are agreed in [ADR 0009](../decisions/0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0010](../decisions/0010-preserve-failures-across-explicit-reruns.md) and [ADR 0011](../decisions/0011-separate-logical-baseline-and-run-identities.md). ADRs 0021–0027 select evidence composition, the .NET 10 sample stack with MSTest/MTP and the direct-SQL provider approach. Compatible release versions and resolved dependencies remain unselected; the [reuse research](../research/execution-boundaries.md) records capabilities rather than tested compatibility.

[ADR 0012](../decisions/0012-bound-the-initial-oracle-to-harness-inputs.md) also accepts the dedicated-harness execution-input boundary. That boundary does not approve the concrete inventory or its implementation.

## Agreed first-experiment stack

Status: **AGREED** in [ADR 0022](../decisions/0022-select-dotnet-reqnroll-for-first-experiment.md), resolving Q14's initial sample-stack choice. Use C#/.NET with Reqnroll for the dedicated acceptance harness and a small separately running ASP.NET Core HTTP SUT. This is a sample choice, not an AAATDD-wide implementation-language decision.

The [reuse research](../research/execution-boundaries.md#existing-capabilities) identifies Reqnroll's native Cucumber Messages support and scenario-lifetime hooks/context, together with standard .NET HTTP capabilities. That fits the agreed synchronous HTTP/SQLite sample and evidence composition. Share reviewed technical plumbing where appropriate without referencing the SUT implementation assembly or sharing its business-rule implementation with assertions. The SUT still runs separately under ADRs 0005 and 0012; choosing the same ecosystem establishes no independence guarantee.

Cucumber.js remains a credible later portability candidate; it was not selected for the first adapter. Select exact SDK, Reqnroll/test-framework/provider/native versions and assess compatibility before executable-baseline approval. The framework/verifier language, packaging and public APIs remain open. This decision authorises no scaffolding, restore, installation or execution.

### Agreed MSTest/MTP and serial scenario scheduling

Status: **AGREED** framework/platform in [ADR 0023](../decisions/0023-use-mstest-with-microsoft-testing-platform.md): use MSTest with Microsoft.Testing.Platform. The earlier NUnit recommendation was not selected. [Official-source follow-up research](../research/2026-11-03-mstest-mtp.md) documents support and the remaining end-to-end compatibility review. ADR 0025 selects the SDK arrangement below; exact versions, concrete MTP selection and command-line/configuration details remain unapproved.

**Agreed scheduling in [ADR 0024](../decisions/0024-run-initial-scenarios-serially.md):** explicitly disable scenario parallelism for the first experiment, while retaining independent per-scenario setup and no order dependency. This narrows the first lifecycle/evidence exercise, not the future framework's concurrency capabilities. Serial execution supplies no independence or L2 guarantee. Exact scheduling configuration remains unapproved; no package installation or execution is authorised.

### Agreed MSTest SDK project arrangement

Status: **AGREED** in [ADR 0025](../decisions/0025-use-mstest-sdk-for-acceptance-harness.md), resolving Q14's initial harness project arrangement. Use `MSTest.Sdk` for the acceptance harness, with the Reqnroll MSTest integration (`Reqnroll.MSTest`). The [MSTest/MTP research](../research/2026-11-03-mstest-mtp.md) records Microsoft's SDK-based MTP route and the manual-configuration alternative. The SDK route centralises MSTest/MTP dependency alignment; it does not itself establish Reqnroll build/discovery or formatter compatibility.

Keep the separately running SUT on its ASP.NET Core project SDK. Reqnroll generated-test imports, SDK/package versions, MTP invocation and native-message production must be reviewed as one coherent pinned configuration. Manual `Microsoft.NET.Sdk` plus explicit MSTest/MTP properties/packages remains a possible amendment if evidence shows the SDK arrangement cannot meet the approved baseline. Do not silently change the selected MTP platform. No concrete SDK/project file, dependency graph or implementation is approved by this decision.

### Agreed .NET 10 LTS target

Status: **AGREED** in [ADR 0026](../decisions/0026-target-net10-for-first-sample.md), resolving Q14's initial target-framework choice. Target both the acceptance harness and separate sample API at `net10.0` using a stable .NET 10 SDK. Microsoft's [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) lists .NET 10 as active LTS through 2028-11-14; .NET 8 and 9 support ends on 2026-11-10. Reqnroll's [compatibility documentation](https://docs.reqnroll.net/latest/installation/compatibility.html) lists .NET 10. These are documentation findings, not a tested dependency graph.

This supplies a supported common sample target and a native MTP CLI path without adopting a preview target. It does not choose the future framework/verifier's target or require multi-targeting. Exact SDK/runtime patches, package versions, platform settings and compatibility evidence remain pending review. No SDK installation, project creation or execution is authorised.

### Agreed SQLite provider and direct SQL

Status: **AGREED** in [ADR 0027](../decisions/0027-use-microsoft-data-sqlite-with-direct-sql.md), resolving Q24's sample provider approach. Use `Microsoft.Data.Sqlite` with direct parameterised SQL for SUT persistence, fixture setup/validation and the distinct read-only observer. [Current provider research](../research/2026-11-03-sqlite-provider.md) records the documented capabilities and alternatives. This is a bounded sample choice, not a universal provider API or an approved privileged exception.

Keep SUT persistence code separate from harness observation/assertion SQL. Sharing the technical provider does not establish independence. Exact schema, commands, provider/native versions, dependency graph and transaction/pooling/open settings remain pending review. No package installation or execution is authorised.

## Agreed sample HTTP mapping

Status: **AGREED** in [ADR 0015](../decisions/0015-agree-initial-sample-http-mapping.md), resolving Q23's initial HTTP mapping. Use the following paths, fields and status/body combinations for the two agreed scenarios. HTTP `409` expresses a conflict with current stock; the alternative `422` was not selected. No broader API or DTO-validation specification is implied.

| Operation | Agreed interface | Agreed interpretation |
| --- | --- | --- |
| Submit the order | `POST /orders`, fields `requestId`, `customerId`, `productSku`, `quantity` | Correlate the operation to the scenario; `requestId` is trace information and adds no idempotency promise |
| Accepted response | HTTP `201`, fields `outcome: accepted`, `requestId`, `orderId` | Recognise acceptance only from this explicit status/body combination |
| Stock rejection | HTTP `409`, fields `outcome: rejected`, `reason: insufficient_stock`, `requestId` | Recognise the intended rejection explicitly; an arbitrary non-success status is not a stock rejection |
| Observe stock | `GET /products/{sku}/stock`, HTTP `200`, fields `productSku`, `available` | Check identity and measured availability against the feature |

Remaining error/evidence handling must follow ADR 0009. HTTP `500`, or a complete correlated response whose body violates the mapping, is an observed response failure when established by a valid oracle. Distinguish malformed application output from incomplete capture or a collector that cannot interpret an unsupported format. A transport timeout or unusable capture leaves the relevant observation unavailable. Do not silently retry the order submission, infer acceptance from a successful send, or derive persisted-order existence from the response alone. Concrete validation and collection mechanisms remain review items; exact DTO validation outside the two agreed scenarios remains outside the experiment's acceptance scope.

Require response identities to match the request/product being observed. Concrete wire types, schema validation and endpoint configuration still need executable-baseline review. ADR 0016 agrees the completion/read-consistency policy below; concrete transaction settings remain open. The mapping decision does not approve a complete executable baseline.

## Proposed fixture, persistence and observation

Use a scenario-owned absolute path beneath a dedicated test directory. Record ownership and environment identity before creating the file; never use a supplied production connection string. Concrete path/ownership settings and SQL remain review items rather than implemented migrations.

### Agreed minimal two-table schema

Status: **AGREED** in [ADR 0028](../decisions/0028-use-minimal-inventory-and-orders-schema.md), resolving Q24's initial table/column structure. Use these two tables in each disposable scenario store:

| Table | Agreed columns |
| --- | --- |
| `inventory` | `product_sku TEXT PRIMARY KEY NOT NULL`, `available INTEGER NOT NULL` |
| `orders` | `order_id TEXT PRIMARY KEY NOT NULL`, `request_id TEXT NOT NULL`, `customer_id TEXT NOT NULL`, `product_sku TEXT NOT NULL`, `quantity INTEGER NOT NULL` |

These columns map the already agreed observations to explicit stored fields. Keep `request_id` non-unique: correlation adds no idempotency promise. No separate customer/product catalogue, event history or ORM mapping is proposed for this bounded sample. Do not infer additional request-validation behaviour from the field types.

This decision selects the minimal table/column structure and stated constraints, not a complete production schema or validation suite. Additional constraints, indexes, SQLite strict-table mode, identity-generation rules, schema-identity encoding and actual DDL/queries remain review items; they must not silently change acceptance. Parameterise runtime values in seed, persistence and observation commands. The Gherkin remains the single business specification; concrete provider/native versions and the observer exception remain pending review.

### Agreed explicit connection roles without pooling

Status: **AGREED** in [ADR 0029](../decisions/0029-use-explicit-sqlite-connection-roles.md), resolving Q24's initial open-mode/pooling profile. Use the same established absolute scenario-owned database path with explicit role-specific open modes:

| Role | Agreed mode | Purpose |
| --- | --- | --- |
| Fixture creation/seed | `ReadWriteCreate` | Only fixture setup creates the owned database |
| SUT persistence | `ReadWrite` | Open the prepared store without silently creating a missing one |
| Outcome observer | `ReadOnly` | Inspect the prepared store without write/create authority |

Set `Pooling=False` for these sample connections to simplify deterministic handle release before file cleanup. Microsoft's [connection-string documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) documents these modes and pooling controls; defaults permit file creation and pooling, so record explicit settings in the eventual baseline. Opening read-write does not itself restrict SQL privileges within the file; read-only connection mode is not an operating-system isolation boundary.

This profile interprets the already agreed fixture ownership, distinct read-only observation and required teardown. It does not replace ownership/path validation, establish that two supplied paths identify the same store, or approve the privileged-observer exception. Verify the actual resolved provider/native behaviour under the selected versions. Connection builders, SQL/transaction settings, locking/timeouts and raw configuration identity remain review items.

### Agreed fixture readiness before the action

Status: **AGREED** in [ADR 0017](../decisions/0017-validate-fixture-readiness-before-submission.md), resolving Q18's initial setup-readiness policy. Seed from the Given steps, then validate the scenario-owned store and public stock view before submitting the order. Validate the intended store/schema identity, product/quantity and initial order population against the agreed feature. Expected values come from that feature, not from reconstructing expectations from the measured database.

Keep setup validation distinct from outcome assertions. If required preconditions cannot be established, do not submit the order or claim a meaningful behavioural RED. Apply ADR 0009: invalid/missing setup evidence is `INDETERMINATE` unless a separately established trustworthy requirement failure warrants `FAIL`. A defect in the public stock view must not be erased merely by labelling it a fixture problem.

Use a deliberately wrong seed as a targeted control to demonstrate that readiness checking blocks the action for the intended cause. The concrete seed fault, schema/queries, validation implementation and evidence format remain pending review. This policy does not establish full independence between fixture and observer code or approve a privileged outcome-observation exception.

### Agreed completion and read consistency

Status: **AGREED** in [ADR 0016](../decisions/0016-observe-completed-sample-operations.md), resolving Q19's initial synchronous sample policy. Concrete SQL, transaction settings and execution evidence remain pending review.

For acceptance, commit the order and stock update together before returning HTTP `201`. For insufficient stock, finish the rejection without order or stock changes before returning HTTP `409`, leaving no pending order work. These completion points interpret the agreed feature rather than adding asynchronous behaviour.

After receiving the response, observe committed state in the scenario-owned store: the public stock endpoint must reflect that committed state, and the database observer must use a fresh read transaction rather than a snapshot opened before the action. With one request, no concurrent actors and no pending background order work in the sample, sequential post-response observations can assess the same completed operation. Record the store/read point and collection identity. Do not use dirty reads or polling that hides a premature response.

A successful empty query establishes no persisted order at its declared post-completion read point. It does not prove unlimited future absence. A failed or unavailable read supplies no absence evidence; apply ADR 0009 and retain the gap. This sample policy does not define eventual-consistency, bounded-event-stream or general temporal-provider semantics. Concrete SQL, transaction settings, timeout values and evidence encodings remain open.

### Agreed outcome-observer scope

Status: **AGREED** in [ADR 0018](../decisions/0018-inspect-all-scenario-owned-orders.md), resolving Q18's initial outcome-observer scope. Use the distinct read-only observer connection agreed in ADR 0008. Inspect the complete order population in the scenario-owned store before applying request/customer filters. This is bounded synthetic data with one request per scenario, not permission to inspect unrelated databases.

Compare the population and relevant fields against the Gherkin expectations. Validate expected counts, customer, product, quantity and request identity; for an accepted response, also match stored order identity to the response's `orderId`. These comparisons are the agreed observation interpretation, not a second business specification. Checking only rows matching the expected `requestId` could conceal an unexpected order with the wrong identity, especially in the rejection case.

Missing or unreadable storage is unavailable evidence, never an empty population. Proposed remaining mechanics: open read-only without creating a missing database and record source, correlation, collection time, consistency and availability. Concrete queries, field types, provider identity and the privileged-observation exception remain unapproved.

### Agreed teardown with separate evidence

Status: **AGREED** in [ADR 0019](../decisions/0019-require-confirmed-scenario-teardown.md), resolving Q18's initial teardown policy. Always attempt cleanup after scenario execution or setup failure for resources whose ownership has been established. Stop the scenario-owned SUT and close SUT/observer connections, including pooled handles, before deleting the scenario-owned database and associated owned files. Do not delete unrelated files or stop unrelated processes. Concrete ownership/path checks and teardown timeouts remain review items.

Retain collected behavioural evidence and report cleanup separately. Required cleanup must be confirmed before the verification assessment can be `PASS`. If behaviour is satisfied but required teardown fails or cannot be confirmed, report `INDETERMINATE` with the cleanup reason. If a trustworthy behavioural failure is already established, retain `FAIL` and the additional cleanup gap under ADR 0009. Do not rewrite an assertion's result merely because teardown failed.

Capture observation/error evidence before disposal where possible; teardown failure must not erase already collected evidence. Exact diagnostic storage, retention, raw-database preservation and any authorised retention exception remain open. This policy does not make successful cleanup evidence of behavioural correctness or authorise actual deletion during design.

### Agreed scenario-scoped component model

Status: **AGREED** in [ADR 0020](../decisions/0020-use-scenario-scoped-harness-components.md), resolving Q18's initial component model. Follow the [responsibility-separation research](../research/execution-boundaries.md#is-fixtureactionobservationassertion-separation-useful) with ordinary scenario-scoped classes or functions in the dedicated harness:

| Responsibility | Agreed owner |
| --- | --- |
| Establish and validate starting state; own teardown | Fixture code |
| Send the HTTP action and capture transport details | Thin technical HTTP helper |
| Collect measured outcomes, correlation and availability | Observation code |
| Apply approved business expectations to observations | Gherkin bindings/assertion code |

An observer reports measured values or evidence unavailability; it does not decide business acceptance. Bindings/assertions interpret observations against the approved expectations, while verification policy accounts for approval, integrity and evidence completeness. A setup assertion remains a readiness check, not a product outcome assertion.

These responsibilities can share reviewed technical plumbing. They do not require separate processes, published extension APIs, provider registries or a universal assertion DSL in the first experiment. This is an organisation boundary, not an independence or L2 trust boundary. Exact classes, APIs, wire/result types and selected package versions remain unapproved.

### Privileged-observation exception proposal

| Field | Proposed scope or remaining prerequisite |
| --- | --- |
| Requirement/scenarios | The two cases in the agreed Orders feature; durable identity mapping still needs review |
| Capability | Read persisted-order existence/count and relevant fields in the scenario-owned SQLite store |
| Rationale | The minimal sample has no adequate public order query; reassess if such an interface becomes available |
| Provider/version | Microsoft.Data.Sqlite with direct SQL is selected in ADR 0027; the local observer implementation/source digest and resolved provider/native versions still need review before exception approval |
| Environment/permissions | Synthetic local L1 test data only; read-only database connection; no unrelated stores or fixture write authority |
| Limits | Schema coupling, declared read point, no future-absence proof, and no protection against same-user file tampering |
| Authorisation | Pending explicit executable-baseline review; this table is not an approved exception |

## Proposed approval and evidence boundary

### Agreed pinned SDKs and locked dependency restore

Status: **AGREED** in [ADR 0030](../decisions/0030-pin-sdks-and-use-locked-restore.md), resolving Q4's initial reproducible-input mechanisms. Pin the exact stable .NET 10 SDK with SDK roll-forward disabled and prereleases excluded, and pin the selected `MSTest.Sdk` version separately. Review and retain per-project NuGet lock files and use locked restore without force-evaluation or automatic lock regeneration during verification. Prevent later build/test steps from silently restoring a different graph.

The [pinning/restore research](../research/2026-11-03-reproducible-inputs.md) records documented mechanisms and limits. Capture actual resolved managed/native dependencies and runtime identity separately; SDK pinning does not pin every running process, and a lock file does not authenticate loaded binaries or cover every imported/project-SDK input. This decision does not establish L2 isolation or deterministic binary output.

Prepare updates as reviewed changes. Changes affecting approved harness/toolchain inputs follow baseline amendment rules; SUT dependencies remain separately assessed implementation/environment inputs. Exact versions, files, package-source policy, identity encodings and commands remain unapproved. This selects no executable baseline and authorises no restore or installation.

### Agreed direct-version validation targets

Status: **AGREED** in [ADR 0031](../decisions/0031-select-direct-version-validation-targets.md), resolving Q14's first direct-version targets. The [version review](../research/2026-11-05-version-candidates.md) records stable .NET SDK `10.0.403`, `MSTest.Sdk` `4.5.2`, aligned Reqnroll/MSTest integration and generation `3.3.4`, and `Microsoft.Data.Sqlite` `10.0.14`. Primary listings establish availability and dependency constraints, not tested compatibility.

The complete transitive/native graph, shipped SDK imports, actual runtime and Cucumber Messages schema remain unresolved. The decision chooses validation targets, not approved lock files, the privileged exception, implementation or a runnable baseline. The review lists the concrete evidence still needed under the selected MTP path.

### Completed bounded compatibility probe

Status: **EXPLICITLY AUTHORISED AND COMPLETED**, 2026-11-05. The user approved the [bounded plan](compatibility-probe-plan.md), and the [evidence report](../research/compatibility-probe-results.md) records candidate-graph discovery/locked restore, generation/build, serial native MTP discovery and controls, message relationships and SQLite role/cleanup checks. The technical harness implements no Orders API/business bindings or AAATDD runtime.

The user's separate approval authorised only the probe and reporting paths. Its candidate graph and bounded compatibility observations do not approve Orders coverage, the full oracle or the privileged exception. Execution stopped after reporting; broader implementation remains subject to the design-only handover.

### Remaining approval and evidence details

Version the agreed feature and design records in Git before selecting the executable baseline. Git metadata was observed at probe start on 2026-11-05, but the probe performed no staging or commits and did not establish an approved Git baseline. Pin the reviewed feature, bindings, assertions, hooks, fixture/observer code, technical helper, runner/configuration, relevant dependencies and policy. Record the identified SUT artifact and environment separately. Do not treat a branch name or runner-generated case ID as a complete identity.

ADR 0021 agrees native Cucumber Messages plus a small AAATDD envelope linking baseline/policy and SUT/harness/environment identities, expected-case inventory, fixture/observation provenance and gate reasons to execution evidence. Establish the approved expected inventory before execution filters under ADR 0011; do not derive it solely from the cases reported as run. Concrete field/schema encodings remain design candidates. Retain original statuses rather than reducing missing, skipped or unbound work to a passing scenario count. L1 approvals and integrity checks remain advisory.

### Agreed evidence composition

Status: **AGREED** in [ADR 0021](../decisions/0021-retain-native-messages-with-aaatdd-envelope.md), resolving Q12's first-experiment evidence composition. Retain the native Cucumber Messages stream as the primary runner evidence, including original statuses, source/case links and attempts. Add a small AAATDD envelope linking that evidence to the approved baseline/policy, assessed implementation/environment, expected inventory, fixture/observation provenance and gate reasons. Approval and completeness cannot be inferred from native runner success alone.

This follows the [runner-evidence research](../research/execution-boundaries.md#existing-capabilities): the native format supplies execution relationships, while AAATDD must establish approval, expected coverage and evidence validity. Validate supported versions and stream completeness before accepting evidence as sufficient for `PASS`; unsupported or truncated evidence cannot silently become success. Apply ADR 0009 when trustworthy failures coexist with evidence gaps.

An optional CTRF projection may support generic reports. It is derived output, not a substitute for the retained native stream or AAATDD approval/coverage checks. Defer the exact envelope schema, compatible versions, artifact-link/digest encoding and projection rules until their review. This is a composition decision, not a claim that native messages are authentic or that L1 producers are protected. The envelope references approval; it cannot grant it.

## Agreed verification-outcome semantics

Status: **AGREED** in ADR 0009, resolving Q7's core outcome semantics. Use `PASS`, `FAIL` and `INDETERMINATE`, with completion permitted only by `PASS`.

| Outcome | Agreed meaning |
| --- | --- |
| `PASS` | Required checks are accounted for, the approved baseline/evidence is valid for the identified implementation/environment, and all applicable requirements are satisfied |
| `FAIL` | Trustworthy evidence establishes at least one unmet approved behavioural or fitness requirement |
| `INDETERMINATE` | Verification cannot establish a valid result because required approval, integrity, execution or usable evidence is missing/invalid, without a separately established trustworthy requirement failure |

Both `FAIL` and `INDETERMINATE` block completion. Preserve a separate reason for the actual cause, including known integrity-policy violations: `INDETERMINATE` does not mean that an unapproved oracle edit is uncertain or acceptable.

A globally invalid oracle invalidates its run's behavioural claims. Where a valid independent check already demonstrates failure but another check lacks evidence, report `FAIL` and preserve the evidence gaps. Never turn a mixed run into full coverage or use an untrusted assertion failure as proof of a product defect.

The alternative of classifying known approval/integrity-policy violations as `FAIL` was not selected. Exact status schemas, reason/exit codes, waiver rules and general retry/flake policy remain open; the initial experiment's retry policy is agreed below.

## Agreed retries and reruns for the first experiment

Status: **AGREED** in ADR 0010, resolving Q7's initial-experiment retry policy. Use one evaluated scenario attempt per required case in each verification run, with automatic scenario retries disabled. This does not authorise silently retrying the order submission. It is a scenario/harness-level policy, not a claim about lower-level network retransmissions.

An explicit rerun has a new run identity and retains the earlier evidence. A routine fix within approved implementation scope does not need a new approval merely to rerun; a changed oracle still follows its amendment rules. A later passing run must not silently erase an unresolved trustworthy failure for the same assessed implementation, baseline and relevant environment.

When an unchanged assessed implementation produces both a trustworthy failure and a pass, retain the failure and treat the inconsistency as unresolved. A fix with a newly identified implementation or an explicitly justified invalidation of defective earlier evidence can resolve the finding; a green rerun alone cannot. Evidence invalidation must follow an authorised policy/approval path: the implementation agent may propose it but cannot unilaterally discard a trusted failure. An earlier `INDETERMINATE` can be resolved by correcting the evidence/environment problem and obtaining a valid fresh run without necessarily changing the SUT. Preserve the cause and history.

The alternative of bounded automatic retries with all attempts retained and an explicit flaky-result policy was not selected for the initial experiment. Broader policy remains open. Temporal observation policies may legitimately poll; those are separately approved measurement semantics rather than retries that replace a failed scenario.

## Agreed logical identities and baseline revisions

Status: **AGREED** in ADR 0011, resolving Q6's identity model. Exact naming, encoding and generated-case rules remain open.

| Identity | Agreed role |
| --- | --- |
| Requirement/scenario identity | Explicit, project-unique logical IDs in Gherkin tags; keep the current sample tags as the initial candidates |
| Approved baseline revision | Content-based identity of the reviewed artifact set and policy, with Git provenance; approval refers to this exact revision |
| Implementation/environment identity | Identified SUT artifact/source and relevant environment/configuration, recorded separately from the oracle |
| Run identity | A fresh identity for every explicit verification run, linked to the assessed identities and any prior finding/resolution |
| Native runner identities | Preserve for within-run traceability and map them to the logical cases; do not substitute generated IDs or line numbers for durable identity |

Let logical IDs survive file moves and display-name changes while content and provenance identify the reviewed revision. Retaining an ID never carries approval over changed authoritative content. Added/deleted cases change the expected inventory; derive that inventory before execution filters.

The alternative of identity derived from source paths, line numbers or scenario text was not selected. Reusing runner-generated identifiers also cannot by itself establish continuity across runners or runs; see the [Gherkin/Messages research](../research/execution-boundaries.md).

The exact tag grammar, cross-project namespace, digest/canonicalisation rules and approval carry-forward for non-semantic edits remain open. Scenario Outline examples need durable case keys before outline support is claimed; there are no outlines in the first sample. The accepted identity model supplies no concrete ID schema or computed baseline today.

## Agreed baseline boundary for the first harness

Status: **AGREED** in ADR 0012, resolving Q4's initial boundary. Use one dedicated acceptance harness plus identified external execution inputs, with the SUT tracked separately. The actual inventory, project layout and implementation remain unapproved.

| Boundary | Agreed scope; actual artifacts pending review |
| --- | --- |
| Reviewed oracle | Gherkin, bindings/assertions, hooks/transformations, fixture/schema/seed inputs, observers, technical helpers, runner configuration and verification policy |
| External execution inputs | Referenced helpers/resources, inherited build imports, restore/launch settings, resolved package/native dependencies and relevant toolchain identities |
| Implementation under test | SUT source/artifact and build identity, assessed separately; no SUT project reference from the initial acceptance harness |
| Generated state and evidence | Scenario database, reports, logs and build outputs are execution artifacts, not changes to the approved source-input baseline merely because a run creates them |

All material harness inputs must either be captured in the reviewed set or supplied through an explicitly identified trusted toolchain/environment policy. A shared helper that can affect assertions remains an oracle dependency wherever its source lives. A detected unmatched material input must block verification rather than inherit approval from a directory name. This is not a claim of complete automatic dependency discovery.

Generated harness executables, adapters and loaded dependencies are checked execution artifacts: record their identities and association with the approved source/build inputs before using them as evidence producers. Excluding generated files from the source-input baseline does not permit arbitrary or altered binaries. Their trust still needs validation, with L1's advisory limits reported.

The folder alone is insufficient: [MSBuild can import parent-directory build configuration](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=visualstudio), and a normal [NuGet restore can update its lock graph](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies). Propose controlled imports and locked dependency resolution for the harness, with the resolved identities captured. These mechanisms help define inputs; they do not establish semantic correctness or L2 isolation.

For the first sample, drive the separately running SUT over HTTP without referencing its implementation assembly from the harness. Keep the verifier and approval-policy identity under trusted control rather than accepting replacements supplied by a candidate SUT. L1 checking remains advisory; independent enforcement is still the later L2 task.

The alternatives considered were a whole-repository oracle baseline, which couples ordinary SUT fixes to baseline revision, and fine-grained per-binding dependency capture, which can reduce reapproval but adds dependency-analysis complexity. The project-level execution-input boundary is accepted as the initial compromise. Exact inventory, hashing, generated-input validation, exceptions and non-semantic-change approval rules remain open.

## Agreed baseline authority and revision lifecycle

Status: **AGREED** in [ADR 0013](../decisions/0013-separate-baseline-authority-from-verification.md), resolving Q8's conceptual lifecycle. Use three conceptual revision states, with verification outcomes tracked separately:

| Revision state | Agreed meaning |
| --- | --- |
| `DRAFT` | Proposed content without approval authority; submission, review requests, rejection or withdrawal can be recorded as events |
| `APPROVED` | Human-authorised immutable revision, authoritative within its approved change/scope even before the required behaviour passes |
| `SUPERSEDED` | Previously approved revision explicitly replaced for the relevant scope by another approved revision; retained for historical evidence |

Approval pins the reviewed content and scope. A valid executable baseline may be approved while a business assertion fails: it states the target for the approved change; implementation authorisation remains separate. Its verification result remains `FAIL`, blocking completion under ADR 0009. If the current SUT already satisfies it, no artificial RED failure is required. Missing bindings, broken setup or unavailable evidence are not meaningful behavioural RED results.

An edited copy of an approved revision becomes a new draft; it does not mutate the earlier approval. Draft amendments inherit no authority from an existing logical ID or a passing native run. The previous approved snapshot remains authoritative for its existing scope until a replacement is explicitly approved. Supersession preserves the historical approval/results and must not silently remove obligations outside the replaced scope.

This is a per-change/per-scope authority model, not automatic project-wide activation or permission to merge failing code. Selecting the approved baseline and recording `PASS`/`FAIL`/`INDETERMINATE` are separate operations. Verification success cannot promote a draft without approval, and approval cannot turn a failing result into a pass.

The alternative of separate `PROPOSED`, `APPROVED_PENDING` and `ACTIVE` states was not selected. Those can expose review/execution progress, but risk conflating approval authority with verification satisfaction. The agreed three-state model keeps review activity as events and uses the agreed outcome model for execution. Exact storage/schema, approval identity and supersession mechanics, including partial replacement, remain open.

## Agreed first-harness validation policy

Status: **AGREED** in [ADR 0014](../decisions/0014-require-targeted-first-harness-validation-controls.md), resolving Q5's first-experiment policy. Before accepting the first harness's validation evidence, require a correct-SUT positive control plus targeted negative controls. Exercise representative behavioural faults, an unapproved oracle edit and failed setup/observation. Keep their claims separate: assertion sensitivity, integrity rejection and evidence validity are different checks.

The table below is a candidate control inventory, not an approved exhaustive mutation suite. Each selected control needs an identified fault, expected result and observed reason so that a setup failure cannot masquerade as detecting a behavioural defect. Approval of the conceptual baseline can precede this execution; accepting the harness's validation evidence requires the selected controls to demonstrate their expected results. Control results assess the harness and must not be mixed into the normal SUT acceptance result.

This policy is specific to validating the first harness. It does not require a mutation run for every product change, establish universal no-op detection or select concrete faulty implementations. Retain uncovered sensitivity limits explicitly. Broader adequacy policy and later revalidation triggers remain open.

## Proposed concrete validation controls

The concrete control inventory remains proposed. Its required classes are agreed in ADR 0014; outcome labels use the agreed ADR 0009 semantics.

| Control | Expected verification result |
| --- | --- |
| Correct SUT, intact approved oracle, complete evidence | `PASS` |
| SUT rejects every order or permits oversubscription | `FAIL` from the relevant trusted scenario |
| Incorrect order persistence or stock effects | `FAIL` from the relevant trusted observation/assertion |
| Approved binding replaced by an unapproved no-op | Completion blocked; `INDETERMINATE` with integrity rejection, even if the native runner passes |
| Wrong seed, failed setup, unavailable observer, or failed required cleanup | Completion blocked; `INDETERMINATE` with the relevant infrastructure/evidence reason unless a separate trusted requirement failure already establishes `FAIL` |
| Required case skipped, unbound or absent; stale, mismatched or truncated evidence | Completion blocked; preserve the exact validity/completeness reason |
| Passing rerun with an unresolved trustworthy failure for the same assessed identities | Preserve per-run results; the later pass cannot clear the assessment's earlier failure without resolution |

These controls demonstrate selected failure sensitivity, not a universal semantic no-op detector. A complete executable-baseline approval still needs reviewed implementation artifacts, compatible pinned versions, concrete exception identity and a reproducible execution plan. No such approval or implementation authorisation is requested by this draft.
