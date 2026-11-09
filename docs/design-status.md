# Design status

Phase: **IMPLEMENTATION IN PROGRESS; RELEASE INCOMPLETE**. The active autonomous-release instruction explicitly authorises implementation and routine engineering choices under [ADR 0041](decisions/0041-authorise-autonomous-release-implementation.md). Earlier design-only boundaries below are historical and superseded. Human business acceptance and oracle integrity remain required.

Current tool name: **TADA — Test-Assured Development Assistance**, command prefix `tada`, accepted in [ADR 0034](decisions/0034-rename-tool-to-tada.md). Earlier AAATDD references in accepted ADRs, research snapshots and probe evidence retain their historical meaning and authority.

The design-stage audit and completed bounded compatibility probe remain historical evidence. Product implementation now lives under `src/`, tests under `tests/`, and the Orders API/harness under `examples/Orders/`. See the [quickstart](quickstart.md) and [release checklist](release-readiness.md) for current executable behaviour and incomplete release obligations. No staging, commit, push or public publication is implied.

## Authority and reading order

1. Read the [charter](vision.md) for directions already established in the user's handover.
2. Read [initial research](research/initial-landscape.md) for sourced findings and their limits.
3. Read the [2026-10-26 reconciliation](architecture/2026-10-26-design-reconciliation.md) for the audit, labelled design delta, challenges, proposed slices and next decision.
4. Read [execution-boundary research](research/execution-boundaries.md) for current integration capabilities and limitations. The initial research remains a historical snapshot, including the earlier equal-DSL framing.
5. Continue the [change-lifecycle discussion](architecture/change-lifecycle.md) and consult [open questions](open-questions.md) as their checkpoints are reached.

Document labels mean:

| Label | Meaning |
| --- | --- |
| Established direction | Supplied as agreed in the user's handover; precise mechanisms may remain open. |
| Accepted decision | Explicitly agreed by the user and recorded in an ADR; applies only to its stated scope. |
| Research finding | Supported by a cited source; not automatically a project decision. |
| Proposal | Recommendation awaiting explicit agreement. |
| Open question | Unresolved product or engineering choice. |
| Assumption to validate | Working hypothesis requiring evidence. |

The reconciliation uses **Agreed**, **Provisional** and **Open** to distinguish explicit user direction from recommendations and unresolved details. A preferred implementation slice or runner does not authorise code changes.

After explicit agreement, create a concise ADR under `docs/decisions/` recording the decision, alternatives, rationale, consequences, and approval reference. Agreed business specifications live under `docs/requirements/`; proposed specifications can live under `docs/requirements/proposals/`. Approval applies only to the recorded scope; business intent agreement does not approve the executable harness.

## Recommended decision sequence

This order is a proposal. Revisit earlier decisions when later evidence exposes a conflict.

| Checkpoint | Decision and reviewable output |
| --- | --- |
| 1. Collaborative change lifecycle | Agree how intent becomes executable acceptance criteria, approval scopes, ambiguity handling, and shortcuts for small changes. Walk through a feature, bug fix, and refactor. |
| 2. Guarantees and reuse | Choose the initial experiment's assurance target and define actors, authority, attacks, build/runtime isolation, and L1/L2 trust boundaries. Compare extending TEA or existing tooling with a separate verification core. Include brownfield baselines and amendments. |
| 3. Verification semantics | Agree identities, requirement/check relationships, discovery completeness, gate outcomes, stale evidence, retries, waivers, and environment identity using concrete examples. Only then propose schemas. |
| 4. Adapter feasibility | Exercise favoured Reqnroll and native Cucumber Messages first, subject to approval. Evaluate discovery, bindings, fixture/observation provenance, reporting/version fidelity and licensing; later test a second Gherkin runner. |
| 5. Incremental execution and fitness | Agree slice size, scope control, regression selection, stop rules, non-functional thresholds, and manual evidence policies. |
| 6. Context and collaboration interfaces | Define authoritative knowledge, retrieval, updates and retirement; source-neutral MCP provenance; portable roles and platform entry points. |
| 7. Product and validation | Choose language, CLI UX, packaging, versioning, project licence, contribution model, and example. Approve measurable acceptance criteria before separately authorising implementation. |

The threat model starts during checkpoint 1; checkpoint 2 formalises it before any security claims or contracts become accepted.

## Accepted decisions

[ADR 0001](decisions/0001-separate-intent-and-baseline-approval.md) resolves Q1: normally agree intent first, then approve its executable baseline. A clear small change may combine both scopes in one review. Intent agreement alone does not authorise product implementation.

[ADR 0002](decisions/0002-clarify-acceptance-changing-uncertainty.md) resolves Q15: clarify acceptance-changing uncertainty, permit routine technical choices within approved constraints, explicitly defer affected scope, and escalate uncertainty about the boundary itself.

[ADR 0003](decisions/0003-support-change-appropriate-specification-artifacts.md) resolves the format-support part of Q16: support DSL scenarios where appropriate and allow changes such as chores without a mandatory behavioural DSL scenario. Detailed review contents and selection rules remain open.

These decisions were accepted on 2026-10-23. They accept the approval and clarification boundaries and conditional artifact support; the complete lifecycle is not yet approved.

[ADR 0004](decisions/0004-gherkin-business-specification-authority.md), accepted from the 2026-10-26 update, makes Gherkin authoritative for business behaviour, with complementary verification tools. It explicitly revises the old charter direction and supersedes no prior ADR. ADRs 0001–0003 and the initial research are preserved unchanged.

The update also establishes black-box-first observation with scoped privileged exceptions, Git-authoritative knowledge, type/risk-adaptive workflow, retained L1/L2/future-L3 protection, and separate reporting of assurance, observation boundary and portability. See the [active charter](vision.md) and the reconciliation for scope and unresolved mechanics.

The hybrid provider model, exact observation categories and interfaces, assertion-evidence mechanism, temporal semantics, evidence schema, package versions and slice implementation remain provisional or open. A business `.feature` specification now has intent agreement, but no executable baseline or runtime contract is approved.

[ADR 0005](decisions/0005-validate-l1-before-protected-l2.md), accepted 2026-10-26, resolves Q20: validate the complete local contract as explicitly L1/advisory with a separately running SUT, then validate protected L2 execution before making independent-assurance claims. This approves staging only; implementation remains unauthorised.

[ADR 0006](decisions/0006-bound-the-initial-orders-example.md), accepted 2026-10-27, resolves Q22 at the first checkpoint. The [initial Orders feature](requirements/order-stock-validation.feature) is agreed within its bounded synchronous, atomic scope. Its scenario bodies are unchanged from the proposal; only its location and status comment changed. Bindings, observer exceptions and implementation remain unapproved.

[ADR 0007](decisions/0007-use-public-outcomes-with-scoped-order-observation.md), accepted 2026-10-27, resolves Q23's observation split: public HTTP for the result and stock, plus a scoped read-only database observer for persisted orders when no adequate public order query exists. The concrete provider and exception remain pending executable-baseline review.

[ADR 0008](decisions/0008-use-disposable-sqlite-per-scenario.md), accepted 2026-10-27, resolves Q24: one disposable named SQLite file per scenario, fixture-owned seed/cleanup and a distinct read-only observer connection. Package versions, concrete schema/exception and implementation remain unapproved.

[ADR 0009](decisions/0009-distinguish-requirement-failure-from-invalid-verification.md), accepted 2026-10-27, resolves Q7's core semantics: `PASS` requires valid complete evidence; `FAIL` means a trustworthy demonstrated requirement failure; missing/invalid verification is `INDETERMINATE`. Both non-PASS outcomes block completion. Exact schemas, general retry rules and waivers remain open; the initial retry policy is settled below.

[ADR 0010](decisions/0010-preserve-failures-across-explicit-reruns.md), accepted 2026-10-27, resolves the first experiment's retry policy: no automatic scenario retries, retained explicit reruns and no silent clearing of unresolved trustworthy failures. Evidence invalidation requires an authorised path. General retry, waiver and encoding details remain open.

[ADR 0011](decisions/0011-separate-logical-baseline-and-run-identities.md), accepted 2026-10-27, resolves Q6's identity model: stable explicit logical IDs, separate content-based baseline revision, separate implementation/environment identity and a fresh ID per run. Tag grammar, digest rules, namespaces and generated-case encodings remain open.

[ADR 0012](decisions/0012-bound-the-initial-oracle-to-harness-inputs.md), accepted 2026-10-27, resolves Q4's initial boundary: a dedicated acceptance harness and identified external execution inputs, with the SUT tracked separately. The concrete inventory, validation/amendment mechanics and executable baseline remain unapproved.

[ADR 0013](decisions/0013-separate-baseline-authority-from-verification.md), accepted 2026-10-27, resolves Q8's conceptual lifecycle: `DRAFT`, `APPROVED` and `SUPERSEDED`, with scoped approval authority separate from verification. Approved expectations can return `FAIL`; only `PASS` permits completion. Amendments create new drafts and supersession preserves history and obligations outside the replaced scope. Exact mechanics and broader starting-evidence policy remain open.

[ADR 0014](decisions/0014-require-targeted-first-harness-validation-controls.md), accepted 2026-10-27, resolves Q5's first-harness validation policy: require a positive control and targeted negative controls for behavioural faults, oracle tampering and failed setup/observation before accepting that harness's validation evidence. The intended assertion must detect each selected behavioural fault. Keep control results separate from product acceptance; concrete variants and broader adequacy policy remain open.

[ADR 0015](decisions/0015-agree-initial-sample-http-mapping.md), accepted 2026-10-27, resolves Q23's initial HTTP mapping: `POST /orders` with explicit `201` acceptance and `409` insufficient-stock bodies, plus `GET /products/{sku}/stock` returning `200`, with the agreed fields and matching identities. `requestId` supplies correlation without idempotency. Wire validation, consistency mechanics and executable-baseline approval remain open.

[ADR 0016](decisions/0016-observe-completed-sample-operations.md), accepted 2026-10-28, resolves Q19's initial sample policy: commit accepted order/stock changes atomically before `201`, or complete rejection without changes before `409`; then observe committed state with a fresh observer read transaction and a public stock view of that state. Do not poll to hide incomplete work. Absence claims are bounded to the declared read point. Concrete settings and general temporal semantics remain open.

[ADR 0017](decisions/0017-validate-fixture-readiness-before-submission.md), accepted 2026-10-28, resolves Q18's initial readiness policy: validate seeded store/public stock against the feature before submission, distinguish setup from outcomes, block unestablished preconditions, demonstrate the guard with a wrong-seed control and preserve separately trustworthy failures. Concrete schema/queries, seed fault and evidence format remain open.

[ADR 0018](decisions/0018-inspect-all-scenario-owned-orders.md), accepted 2026-10-28, resolves Q18's initial outcome-observer scope: inspect all scenario-owned orders and validate expected counts, customer/product/quantity/request identities and accepted-order correspondence with response `orderId`. Do not hide unexpected rows behind an expected-request filter. Concrete queries, types, provider/version and the observer exception remain unapproved.

[ADR 0019](decisions/0019-require-confirmed-scenario-teardown.md), accepted 2026-10-28, resolves Q18's initial teardown policy: attempt owned-resource cleanup after execution or setup failure, release owned processes/connections before deleting owned files, and require confirmed cleanup for `PASS`. Preserve behavioural results and diagnostics separately. Satisfied behaviour with failed/unconfirmed required cleanup is `INDETERMINATE`; a trustworthy behavioural `FAIL` stays `FAIL` with the cleanup gap. Concrete ownership, cleanup and retention mechanics remain open.

[ADR 0020](decisions/0020-use-scenario-scoped-harness-components.md), accepted 2026-11-03, resolves Q18's initial component model: ordinary scenario-scoped fixture, HTTP-helper, observer and binding/assertion responsibilities, with measurement separate from acceptance and shared reviewed plumbing where useful. Defer provider registries/published extension APIs until demonstrated reuse. This is not an isolation boundary; exact APIs and implementation remain unapproved.

[ADR 0021](decisions/0021-retain-native-messages-with-aaatdd-envelope.md), accepted 2026-11-03, resolves Q12's initial evidence composition: retained native Cucumber Messages plus a small AAATDD envelope linking approval, baseline/policy, assessed identities, expected cases, observation provenance and gate reasons. CTRF is optional derived reporting. Native success alone cannot establish approved coverage; unsupported/incomplete evidence blocks `PASS`. Concrete schemas, versions and producer validation remain open.

[ADR 0022](decisions/0022-select-dotnet-reqnroll-for-first-experiment.md), accepted 2026-11-03, resolves Q14's initial sample stack: C#/.NET with a Reqnroll acceptance harness and a separately running ASP.NET Core API. This does not choose the framework/verifier language, exact versions or underlying test execution framework. Implementation remains unauthorised.

[ADR 0023](decisions/0023-use-mstest-with-microsoft-testing-platform.md), accepted 2026-11-03 from the user's explicit selection, chooses MSTest and Microsoft.Testing.Platform for the first Reqnroll harness. The unaccepted NUnit recommendation is retired. Exact versions, SDK arrangement and MTP configuration remain open; this choice does not approve serial or parallel execution.

[ADR 0024](decisions/0024-run-initial-scenarios-serially.md), accepted 2026-11-03, resolves Q14's initial scenario scheduling: disable parallelism, keep independent per-scenario setup and ownership, and permit no execution-order dependency. This changes neither the no-automatic-retry policy nor L1 assurance. Concrete scheduling configuration remains pending review.

[ADR 0025](decisions/0025-use-mstest-sdk-for-acceptance-harness.md), accepted 2026-11-03, resolves Q14's initial project arrangement: `MSTest.Sdk` with `Reqnroll.MSTest` for the harness, with the ASP.NET Core SUT separate. Exact versions, generated-test integration, discovery, MTP invocation and native-message compatibility remain unvalidated and require review.

[ADR 0026](decisions/0026-target-net10-for-first-sample.md), accepted 2026-11-03, resolves Q14's initial sample target: both the acceptance harness and separate API use .NET 10 LTS (`net10.0`) with a stable .NET 10 SDK. Exact SDK/runtime patches, packages and compatibility evidence remain pending review; the framework/verifier target remains open.

[ADR 0027](decisions/0027-use-microsoft-data-sqlite-with-direct-sql.md), accepted 2026-11-03, resolves Q24's initial provider approach: Microsoft.Data.Sqlite with direct parameterised SQL for SUT persistence, fixture setup/validation and distinct read-only observation. Keep SUT persistence logic separate from harness queries/assertions. Schema, exact provider/native versions, settings and the concrete observer exception remain unapproved.

[ADR 0028](decisions/0028-use-minimal-inventory-and-orders-schema.md), accepted 2026-11-03, resolves Q24's minimal schema: `inventory` and `orders` with the agreed text identity/integer quantity columns, non-null fields and SKU/order primary keys. Request correlation stays non-unique. Actual DDL/queries, additional constraints and dependency versions remain open; no database or implementation was created.

[ADR 0029](decisions/0029-use-explicit-sqlite-connection-roles.md), accepted 2026-11-03, resolves Q24's initial connection profile: fixture `ReadWriteCreate`, SUT `ReadWrite`, observer `ReadOnly`, with `Pooling=False` throughout. Only fixture setup creates the database. Concrete construction, ownership/path checks, transaction settings, versions and the observer exception remain pending review.

[ADR 0030](decisions/0030-pin-sdks-and-use-locked-restore.md), accepted 2026-11-05, resolves Q4's initial pinning/restore controls: exact SDK/project-SDK pins, reviewed NuGet lock files and locked restore without automatic graph updates or force-evaluation. Review updates explicitly and capture actual runtime/managed/native identities separately. Concrete versions/configuration, input inventory and commands remain unapproved.

[ADR 0031](decisions/0031-select-direct-version-validation-targets.md), accepted 2026-11-05, selects .NET SDK `10.0.403`, `MSTest.Sdk` `4.5.2`, aligned Reqnroll packages `3.3.4` and `Microsoft.Data.Sqlite` `10.0.14` as direct compatibility-validation targets. They remain untested; the full graph, shipped SDK inputs, runtime/schema identities and executable baseline are unapproved.

## Current review

**Current implementation checkpoint:** [alpha.2 validation](validation/increment-02.md) records 57 passing tests, 16 Orders controls, exact-byte Git round trips, native failure-history re-evaluation, resource integrity and readable CLI review/results. The installed package drives a new Echo API through assertion RED, implementation GREEN, a preservation refactor and a Git-committed handoff under one unchanged oracle. Complete import/runtime provenance, final-source CI, clean-machine and requester/Codex acceptance remain open. These are bounded development checks, not a completed release.

**New release-input research:** the [retained candidate licence inventory](research/compatibility-candidate-licences.md) statically inspects 25 cached package archives and records declared licences, embedded notice hashes and separate SDK distribution terms. Raw archive hashes and NuGet lock identities are recorded separately. Final graph, release payload, project licence and attribution review remain open; no further probe execution or implementation occurred.

**Active release goal:** deliver the useful agent-neutral specify/verify/proceed workflow, proportionate maintenance checks, Git-native session context, real application controls, second-project reuse and clean-machine installation. The [release checklist](release-readiness.md) tracks current evidence and gaps. The earlier [delivery audit](architecture/alpha-delivery-readiness.md) and [file inventory proposal](architecture/alpha-file-inventory-proposal.md) are retained design-stage context, not requests for routine architecture approval under the new implementation authority.

**Accepted scope and expected cases:** [ADR 0040](decisions/0040-define-alpha-scope-and-expected-cases.md) defines `scope.requirementIds`, `scope.scenarioIds` and expected-case entries containing `caseId`, `scenarioId`, `requirementIds` and `sourcePath`. IDs are explicit case-sensitive Gherkin tag identities matching `[A-Z][A-Z0-9_]*`, without `@` in inventory values. ID sets and cases sort ordinally; coverage is established before filters. The alpha supports ordinary scenarios only and explicitly rejects Scenario Outlines. The Orders feature remains byte-for-byte unchanged; its historical proposed-ID comment predates this decision. Remaining field schemas and concrete parser/evidence mappings stay open.

**Accepted inventory structure:** [ADR 0039](decisions/0039-define-alpha-inventory-structure-and-ordering.md) adopts `schemaVersion`, `scope`, `files`, `expectedCases`, `execution`, `externalInputs` and `policy`. Keyed sets use case-sensitive ordinal sorting; meaningful sequences retain order. References identify exact content/revisions, repository-file paths are relative with `/` separators, and unresolved required inputs or unapproved observation exceptions block approval. Detailed field schemas, unique keys and path-resolution mechanics remain open.

**Accepted hashing and encoding:** [ADR 0037](decisions/0037-hash-oracle-input-files-as-exact-bytes.md) requires exact-byte hashing of captured oracle input files, without whitespace, line-ending, comment or encoding normalisation. Formatting changes require a new draft and review. [ADR 0038](decisions/0038-use-sha256-and-rfc8785-for-baseline-inventories.md) selects SHA-256, `sha256:` plus 64 lowercase hexadecimal characters, and RFC 8785 canonical JSON for the generated baseline inventory. Canonicalisation does not alter captured source-file bytes. ADR 0039 supplies the inventory structure and ordering policy; detailed field schemas and other identity encodings remain open.

**Accepted review-to-approval binding:** [ADR 0036](decisions/0036-bind-approval-to-reviewed-baseline.md) requires a review record tied to an immutable baseline revision, explicit selection of that record for approval, unchanged proposed inputs and explicit superseded scope. `tada run` selects an approved revision explicitly; changed or missing oracle inputs block execution with `INDETERMINATE`. Review presentation and declared approval remain distinct under L1. Exact identifiers, digests, schemas and flags remain open.

**Accepted CLI boundary:** [ADR 0035](decisions/0035-adopt-five-operation-alpha-cli.md) adopts `tada baseline prepare`, `tada baseline review`, `tada baseline approve`, `tada run` and `tada verify`, with explicit baseline selection, unchanged reviewed content for approval, retained evidence and shared verification rules. Numeric exit codes, flags, configuration and approval/review records remain open. The commands are not implemented.

**Accepted release boundary:** [ADR 0032](decisions/0032-bound-the-first-publishable-alpha.md) targets `0.1.0-alpha` as a local L1/advisory approval and evidence-verification tool for one developer and Git repository, supporting Reqnroll/MSTest/MTP on Windows x64/.NET 10. It records the minimum workflow, release criteria and deferred scope. [ADR 0033](decisions/0033-use-dotnet-local-tool-for-alpha-verifier.md) selects C#/.NET 10 for the verifier, distributed as a NuGet-packaged repository-local .NET tool with a pinned manifest version. Verification logic stays separate from the CLI and runner integration. Package identity, dependencies, licence, detailed command contracts, storage formats and detailed acceptance procedures remain open. These decisions authorise continued design, not implementation or publication.

**Completed: checkpoint 4's bounded technical compatibility probe.** The user separately authorised the [plan](architecture/compatibility-probe-plan.md). The [results](research/compatibility-probe-results.md) show exact SDK acquisition/integrity, candidate restore/locked restore, generation/build, two-case MTP discovery, a passing positive run, an intended assertion-failure control, native message relationships and SQLite role/cleanup checks. The retained candidate graph resolves MSTest `4.5.2`, MTP `2.5.2`, Messages `30.1.0` and SQLite engine `3.53.3`. The initial sandbox task-host build failure remains visible alongside the unchanged successful build outside that restriction.

Review the report and candidate graph before selecting further baseline work. This is not Orders acceptance, approved oracle inputs or L2 protection. Execution stopped after the approved probe; broader implementation and additional runtime work remain unauthorised.

The baseline document also groups remaining Orders wire-validation details, fixture/observer lifecycle, scoped-exception prerequisites, evidence and business sensitivity controls. Apart from explicitly agreed directions and policies, those details remain proposed. The probe supplies candidate compatibility evidence; the actual Orders artifacts, full baseline/graph review and exception approval must still precede the runnable baseline. Broader implementation remains unauthorised.

Q17's general direction is now agreed: classify by type and risk, require an evaluable objective, and prevent agent-driven verification downgrades. Exact sufficiency rules, classification enforcement, manual-only evidence, and Q16's minimum review contents/readiness remain open.
