# Architecture update reconciliation

Assessed: 2026-10-26; decision status updated 2026-11-05. Source of updated authority: the user-supplied **AAATDD — Architecture & Design Update** and subsequent explicit decisions. This assessment continues the existing design; it does not authorise implementation.

**Agreed** means expressly established in the update or existing ADRs. **Provisional** means a preferred model or recommendation requiring validation and agreement. **Open** means an unresolved decision. Source research is linked separately and does not become a decision by inclusion here.

## Audit of existing progress

Before reconciliation, the working directory contained nine Markdown files: README, charter, design status, lifecycle discussion, open-question register, initial research and accepted ADRs 0001–0003. At that audit it had no runtime code, test projects, dependencies or execution evidence and was not a Git repository. These are historical findings. On 2026-11-05, Git metadata already existed before the separately authorised technical probe, which added only its scoped sources/dependencies/evidence. See the [probe report](../research/compatibility-probe-results.md); no AAATDD framework or Orders API was implemented and no Git operations changed repository state.

The existing work already establishes two approval scopes, escalation of acceptance-changing uncertainty, change-appropriate artifacts, protection of the effective oracle, a common verification direction, portable AI collaboration and risk-proportional evidence. The initial research already identifies overlap with TEA and other workflows. That reuse question remains open.

ADRs 0001–0003 remain compatible and are preserved byte-for-byte. No approved ADR requires supersession. The charter's equal-DSL direction is the substantive conflict; [ADR 0004](../decisions/0004-gherkin-business-specification-authority.md) explicitly revises that direction. Historical research remains unchanged and must be read as the 2026-10-23 snapshot.

## Focused design delta

| Area | Existing approach | Updated direction | Impact | Recommendation |
| --- | --- | --- | --- | --- |
| Business authority | Multiple executable DSLs treated as peers | **Agreed:** Gherkin is authoritative for business behaviour; other tools complement it | Revises charter; preserves common verification | Record ADR 0004; avoid duplicate Markdown functional specs |
| Approvals and ambiguity | Accepted ADRs 0001–0003 | **Agreed:** retain them; meaningful human decisions and proportional workflow | Compatible; no historical rewrite | Reference existing decisions; keep exact approval mechanics **Open** |
| Execution layers | Test case/DSL/driver/SUT sketch | **Provisional:** Gherkin, project bindings, shared protocol capabilities, SUT; separate runner adapters | Makes domain interpretation and runner concerns explicit | Exercise one runner and HTTP path before publishing provider APIs |
| Observation boundary | Effective oracle protected, no explicit observation policy | **Agreed:** black-box first; scoped privileged exceptions | Adds approval scope and boundary reporting | Make setup privilege distinct from outcome-observation privilege |
| Fixtures and observers | Implicit harness dependencies | **Provisional:** first-class conceptual contracts with provenance, limitations and failure states | Makes false-green setup/observation failures reviewable | Use project-local implementations and existing libraries first |
| Integrity and sensitivity | Broad oracle integrity; adequacy question open | **Agreed:** protect hooks, transformations, fixtures, observers, drivers, dependencies and policy; investigate sensitivity | Hash equality alone cannot establish a useful oracle | Demonstrate tamper rejection separately from assertion sensitivity; assertion-evidence mechanism **Open** |
| Temporal checks | No explicit model | **Open:** deadlines, stable intervals, bounded absence and meaningful watermarks | Immediate empty reads cannot prove future absence | Keep technical timing in approved bindings/policy; do not add a broker just for the first slice |
| Change workflow | Q17 proposed impact/risk selection | **Agreed:** classify by type and risk; no verification downgrade through labels; every change has an evaluable objective | General direction accepted; category table and sufficiency remain **Provisional/Open** | Adapt existing lifecycle; avoid a new approval event for every classification |
| Project knowledge | Versioned, human-readable context preferred | **Agreed:** Git is authoritative; MCP sources are context and indexes are projections | Git metadata now exists, but the probe established no approved baseline | Use existing artifacts and tracked evidence references; review approval/evidence provenance before baseline selection |
| Evidence and portability | Manifest/adapters/evidence; JSON Schema and CTRF candidates | **Agreed:** preserve common contract and required-check accounting; reuse standards where suitable | Adds fixture/observer provenance and durable case identities | **Accepted in ADR 0021:** native Cucumber Messages plus a small AAATDD envelope; CTRF optional and derived; concrete schemas/versions open |
| Protection and dimensions | Proposed L1/L2/L3 | **Agreed:** retain local, protected-team and future-attested direction; report observation boundary and portability separately | No automatic L2 downgrade for an approved privileged observer | Exact enforcement/attestation design **Open**; no numerical confidence score |

## Architecture challenge and smallest model

See [execution-boundary research](../research/execution-boundaries.md) for current primary sources. The following design recommendations are **Provisional**.

[ADR 0020](../decisions/0020-use-scenario-scoped-harness-components.md) accepts ordinary scenario-scoped fixture, HTTP-helper, observer and binding/assertion responsibilities for the first harness, with shared reviewed plumbing where useful. Keep measurement separate from acceptance and defer provider registries/published extension APIs until demonstrated reuse. Concrete APIs and runner-native hook wiring remain open. This responsibility separation does not establish isolation.

| Responsibility | What it establishes | Important limit |
| --- | --- | --- |
| Fixture | Isolated, repeatable starting conditions and validation/cleanup evidence | Successful seeding is not an outcome assertion; bypassed invariants may create an unrealistic state |
| Action | The intended SUT operation with run/scenario/operation correlation | A successful send or transport response is not the whole business result |
| Observation | Measured data, source/version, boundary, correlation, collection timing and availability | Unavailable is not absent; permissions and consistency affect what can be claimed |
| Assertion | Comparison of measured data with the approved business expectation | Counts and a green runner do not prove a meaningful predicate |

The same HTTP transport can support actions and observations; conceptual separation does not require duplicated implementations. Keep domain vocabulary in project bindings and business helpers. Keep protocol helpers free of order/customer policy. Reuse Reqnroll DI/hooks, native Cucumber Messages, standard HTTP clients, and established fixture infrastructure. These are reuse recommendations, not dependency approvals.

### False confidence to address

1. **Unchanged source can still be wrong.** Approved no-ops, stale caches, dynamic dependencies/configuration and shared setup/observer mistakes can survive integrity checking. Preserve resolved inputs and environment identity; independently review expectations and use targeted sensitivity controls.
2. **Fixture/observer correlation can hide defects.** If both use the same mistaken schema mapping, they may agree on an incorrect state. Keep expected values rooted in the scenario. Validate realistic setup and use an external outcome observation where adequate.
3. **Negative observations need coverage.** A timeout, dropped subscription or empty query is not a trustworthy absence claim. Define correlation, observer readiness before the action, relevant interval and coverage/completion. A finite interval only establishes the approved bounded claim.
4. **L2 requires build and runtime isolation.** Protected feature files are insufficient if implementation build scripts or the SUT can modify the harness, policy, credentials or evidence. Define and test that boundary before claiming independent verification.
5. **Portability has limits.** Reuse Gherkin and business identity across runners. Replace or reapprove language-specific bindings and helpers. An observer depending on a database schema has different portability from an external HTTP observation, regardless of assurance level.
6. **Approval friction needs bounded reuse.** Reuse existing approved exceptions only within their scenario/capability/provider/version/environment/permission scope. Changes to observer implementation or semantics require reassessment. The policy for other harness changes remains open; do not require a new business approval for every unrelated file edit.

The four observation categories in the update are **Provisional** metadata, not an ordered confidence scale. Whether every test-only/internal observation follows exactly the privileged-exception process is still **Open**; those categories must not become a silent privilege bypass. A privileged observer can retain L2 assurance when the oracle and execution are independently protected and the specific capability is authorised.

### Minimal evidence direction

The common contract should distinguish specification/scenario/example identity, approved baseline and policy, SUT artifact/source identity, harness/runner/dependency identity, environment, expected discovery inventory, actual attempts/results, fixture and observation provenance, and assurance. Preserve raw failure reasons: behavioural mismatch, unapproved/invalid infrastructure, missing execution, unbound steps, skipped required work, unavailable observation and environment failure. Core outcomes and the initial retry policy are agreed in [ADR 0009](../decisions/0009-distinguish-requirement-failure-from-invalid-verification.md) and [ADR 0010](../decisions/0010-preserve-failures-across-explicit-reruns.md); exact schemas, general retry policy, waivers and specialised aggregation details remain **Open**.

**Agreed composition in [ADR 0021](../decisions/0021-retain-native-messages-with-aaatdd-envelope.md):** retain native Cucumber Messages and raw attempts, plus a small envelope linking approved inventory, baseline and provenance that the native stream does not establish. CTRF is optional derived reporting; it must not replace native relationships or approval/coverage checks. Unsupported/incomplete evidence blocks `PASS`. Concrete compatible versions, schemas, validation and projection rules remain open. Runtime-generated case IDs must not substitute for stable requirement/scenario identities; Scenario Outline example encoding remains open.

Keep assurance, observation boundary and portability as separate reported dimensions. Large evidence may live in content-addressed artifact storage with Git-tracked digest references, retention and redaction policy. No new evidence store or reporting platform is selected.

## Validation sequence and proposed slice contents

The L1-first, L2-next assurance sequence is **Agreed** in [ADR 0005](../decisions/0005-validate-l1-before-protected-l2.md), and the bounded business contract is **Agreed** in [ADR 0006](../decisions/0006-bound-the-initial-orders-example.md). Remaining harness details and slice acceptance criteria below are **Provisional, not implementation authorisation**. The agreed feature's executable interpretation and exact interfaces still require review.

| Slice | Smallest useful scope | Reviewable acceptance evidence |
| --- | --- | --- |
| 0. Complete experiment approval | Use the agreed Orders Gherkin; settle the executable-baseline scope, runtime/storage, expected inventory, observer exception and evidence policy | Reviewable executable interpretation and acceptance plan; no assumed package choices or privileged approvals |
| 1. Complete L1 local vertical slice | Reqnroll; a small .NET Orders API in a separate process; scenario-isolated inventory fixture; HTTP action; public observation; one specifically approved privileged observation; native messages; minimal baseline/evidence evaluation | Good SUT succeeds; targeted bad SUT fails; missing setup/observation or required execution cannot produce verification success; unapproved weakened binding is rejected; privileged observation without its scoped approval is rejected; claims remain advisory |
| 2. Protected L2 execution | Execute the same approved oracle against an isolated build/runtime with independently controlled CI policy and evidence publication | Test that candidate code/build steps cannot rewrite the baseline, verifier or evidence; changed/forged/stale artifacts are rejected; only then claim L2 |
| 3. Second Gherkin runner | Cucumber.js against the same identified HTTP SUT; same approved feature content and scenario/example identity rules; new approved JS bindings | Equivalent observed outcomes and gate reasons for good/bad SUT, missing/unbound/skipped work, observation failure and unauthorised oracle change; no expectation of C# binding reuse |
| 4. Temporal extension, only when needed | One approved asynchronous requirement using one established event/observation mechanism | Delayed relevant event is detected; unavailable or interrupted capture cannot satisfy an absence claim; bounded interval/watermark limits are visible |

For slice 1, [the Orders Gherkin](../requirements/order-stock-validation.feature) records the agreed business intent and [ADR 0007](../decisions/0007-use-public-outcomes-with-scoped-order-observation.md) records the agreed observation split. The concrete observer and exception remain pending review. Select privileged observation only when an adequate public query does not exist; a failed public observation is not permission to switch sources silently. Do not invent a privileged dependency merely to fill a demonstration checklist. Approve the provider/version, permissions, environment, correlation and limitations before execution. Keep outcome-observation authority distinct from fixture setup authority.

A database absence query has the same temporal limits as an empty event stream. Define the relevant completion point and read consistency before accepting a “no order” claim. A point-in-time read cannot exclude a later background write. If the required completion or observation coverage cannot be established, the broader claim remains unverified.

Use a disposable scenario-owned database and validated seed. [ADR 0008](../decisions/0008-use-disposable-sqlite-per-scenario.md) selects a named SQLite file per scenario for the first sample. Testcontainers remains a later candidate when engine fidelity warrants it; concrete dependencies remain unapproved. No messaging/UI platform, general provider registry, universal assertion DSL, or second SUT implementation belongs in the first slice.

### What the weakened-assertion demonstration can honestly prove

- **Integrity case:** replace an approved assertion with a no-op without approval. The native runner may pass; AAATDD must reject the oracle mismatch. This is the minimum slice-1 demonstration.
- **Sensitivity case:** test an approved assertion against controlled relevant incorrect behaviour. It must detect that defect to support the corresponding sensitivity claim. If it does not, record the gap and improve the proposed oracle through approval.
- **Open mechanism:** requiring structured evidence for selected outcome assertions may catch missing instrumentation, but a no-op can fabricate a record. No universal semantic no-op detector or assertion-count guarantee is promised.

## Accepted assurance staging

**Agreed in ADR 0005:** validate the first experiment as explicitly L1/advisory, with the SUT out of the harness process to preserve a path to L2. Make protected L2 execution the next slice and a prerequisite for independent-assurance claims. This minimises initial work while exercising the contract end to end. It is not implementation authorisation.

The alternative is to require L2 isolation in the first demonstration. That tests the central adversarial guarantee earlier, but adds trusted-build, credential, workflow and runtime-boundary work before the runner/evidence contract has been exercised. A separate process alone is not enough for L2.

## Accepted business scope

**Agreed in ADR 0006 on 2026-10-27:** use the bounded synchronous, atomic example specified in [the Orders feature](../requirements/order-stock-validation.feature). The existing scenario bodies are unchanged. The first intent checkpoint is satisfied; the feature has no bindings or execution evidence, and its tags do not establish a universal identity schema.

The scope and deferred concerns are recorded in ADR 0006. Synchronous handling is an example-product choice, not a framework restriction. The second checkpoint still needs the executable interpretation, fixture/observation evidence, exact consistency/completion point and any justified privileged-observer exception. Implementation remains unauthorised.

## Accepted observation split

Status: **Agreed** in [ADR 0007](../decisions/0007-use-public-outcomes-with-scoped-order-observation.md), resolving Q23 at the observation-plan level. [ADR 0015](../decisions/0015-agree-initial-sample-http-mapping.md) subsequently accepts the sample paths, fields, status/body combinations and correlation checks. Fixture implementation, detailed wire validation, provider versions and the concrete exception remain open. The following describes responsibilities rather than a second business specification.

| Responsibility | Mechanism direction | Evidence boundary |
| --- | --- | --- |
| Establish preconditions | Proposed: validated seed in a scenario-owned test database using established fixture infrastructure | Privileged setup; it does not authorise outcome inspection |
| Perform the action | Public HTTP order operation | External interaction correlated to this request and scenario |
| Observe result and stock | Public HTTP response and stock interface | External observation; a result/stock read failure is unavailable evidence |
| Observe persisted orders | Read-only queries within the isolated scenario database | Privileged observation, requiring a narrowly scoped approved exception |

Use the agreed split for the initial example when its public interface has no adequate persisted-order query. It accommodates both external observation and controlled privileged observation without requiring a general database-driver platform. Existing setup credentials must not silently confer outcome-observation permission.

The alternative is to use or add a public order query sufficient for the agreed claim. It offers a more portable black-box observation, but can add application surface to the minimal example and remove the need for this particular privileged exception. If an adequate public query already exists, prefer it; the desire to demonstrate exception handling is not itself a reason to bypass black-box-first policy.

[ADR 0018](../decisions/0018-inspect-all-scenario-owned-orders.md) accepts complete scenario-owned order-population observation and expected count/field checks, including accepted-response order identity correspondence. Read-only scope is agreed in ADR 0008; the concrete privileged-observation exception remains unapproved. Do not hide a wrongly correlated order behind a submitted-request filter or trust only a count returned by the action response. Concrete database schema, queries, provider/version and permissions remain open.

Synchronous business intent alone did not establish the storage completion point. [ADR 0016](../decisions/0016-observe-completed-sample-operations.md) now accepts completion before the response and fresh post-response committed reads. Concrete settings remain pending. [ADR 0017](../decisions/0017-validate-fixture-readiness-before-submission.md) accepts separate starting-state validation before submission and a wrong-seed control; concrete validation remains open. Query/setup failures cannot become empty results. A snapshot still does not establish an unlimited future-absence claim.

This decision does not authorise a concrete privileged exception before its provider/version, scenario scope, environment, permissions, limitations and approval reference are reviewable. No observer has been implemented or executed.

## Accepted storage direction for the first example

Status: **Agreed** in [ADR 0008](../decisions/0008-use-disposable-sqlite-per-scenario.md), resolving Q24 for the sample. SQLite/file topology is selected; no provider version, concrete store or dependency has been installed. [Storage research](../research/execution-boundaries.md) records the relevant capabilities and limits.

Use a uniquely named disposable SQLite file for each scenario, reachable by the separately running sample SUT and by a distinct read-only observer connection. The fixture owns creation, validated seed and cleanup. An explicit file path is required for sharing: `:memory:` databases and SQLite's automatically managed empty-filename temporary databases do not provide the cross-process store this plan needs.

| Alternative | Benefit | Cost or limit |
| --- | --- | --- |
| Disposable SQLite file per scenario | Small local setup, supports separate processes and an explicit read-only observation connection | Demonstrates SQLite behaviour; it does not establish parity with another database engine |
| Server database via Testcontainers | Can exercise the selected server engine's actual behaviour and permissions | Requires a compatible container runtime plus image, readiness and cleanup management |

The current sample has no existing production database to match, and concurrency is deferred. SQLite therefore offers a smaller first contract experiment. This is a sample implementation choice, not a restriction on AAATDD fixtures or observers. Reconsider it if an approved sample requirement depends on another engine's semantics.

ADRs 0017 and 0019 accept readiness validation and required teardown: release owned processes/connections before owned-file deletion, confirm cleanup for `PASS` and preserve behavioural evidence/diagnostics. Proposed concrete mechanics include scenario-owned absolute paths and read-only observer opening without missing-file creation. ADR 0016 accepts the completion/read-consistency policy; concrete schema, transaction settings, ownership checks, pooling/lifecycle details, package/native dependency versions and the narrowly scoped observation exception still require review.

Read-only connection mode describes the observer's intended database operation. It does not prevent a same-user L1 process from changing files through other means or establish an L2 boundary.

## Verification outcomes and next policy decision

The [executable-baseline proposal](first-experiment-baseline-proposal.md) groups remaining technical choices without changing the agreed feature or approving the Orders harness. ADRs 0009–0031 accept outcomes, initial retries, identities, oracle boundary, conceptual revision states, initial controls, evidence composition, the sample stack/targets and database/pinning directions. The user did not accept NUnit. The separately authorised checkpoint-4 [technical probe completed](../research/compatibility-probe-results.md), recording one candidate graph and bounded runtime evidence; execution stopped after reporting. Full Orders baseline/graph approval, protected execution, framework/verifier language/target, actual Orders DDL/queries, wire validation, controls and identity encodings remain open.
