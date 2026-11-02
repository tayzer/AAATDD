# Design status

Updated: 2026-10-28. Phase: **DESIGN ONLY**.

The 2026-10-26 audit found the existing nine Markdown files and accepted ADRs 0001–0003, with no code, tests, dependencies or runner integration. The directory is still not a Git repository. Reconciliation adds/updates design documents only; no framework was installed, forked, scaffolded or implemented.

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

## Current decision

**Open: Q18, initial component model.** Recommendation: use ordinary scenario-scoped fixture, HTTP-action, observation and assertion components. Keep measurement separate from acceptance, share reviewed technical plumbing where useful, and defer provider registries/published extension APIs. This is responsibility separation, not a trust boundary. The [baseline proposal](architecture/first-experiment-baseline-proposal.md) describes the proposed split and its research basis; exact APIs and implementation remain unapproved.

The baseline document also groups remaining wire-validation details, fixture/observer lifecycle, scoped-exception prerequisites, evidence and concrete sensitivity controls. Apart from explicitly agreed directions and policies, those details remain proposed. Compatible versions, concrete artifacts and exception approval must still precede a runnable baseline; implementation remains unauthorised.

Q17's general direction is now agreed: classify by type and risk, require an evaluable objective, and prevent agent-driven verification downgrades. Exact sufficiency rules, classification enforcement, manual-only evidence, and Q16's minimum review contents/readiness remain open.
