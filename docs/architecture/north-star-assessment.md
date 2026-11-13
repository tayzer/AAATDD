# North Star architecture assessment

Assessment date: 2026-11-11, against `main` at `943486f` (release `0.1.0-alpha.12`) and the [PR #5](https://github.com/tayzer/TADA/pull/5) branch at `c7e4683`. PR #5 merged into `main` during this work (`beace74`) and this change was reconciled with it. The North Star is recorded in [ADR 0043](../decisions/0043-adopt-the-tada-north-star.md).

## Summary

TADA's verifier was already strong. Exact-byte capture, approval bound to reviewed content, real runner execution, retained native evidence and distinct `PASS`, `FAIL` and `INDETERMINATE` results are all implemented and tested. The main gaps were elsewhere:

- The README and charter presented TADA as a verifier rather than a development workflow.
- There was no supported way to agree a specification before implementing it without failing mainline CI.
- Contract and fitness obligations were protected only implicitly, as anonymous captured bytes.
- CI guidance assumed one fully passing baseline.

This change addresses the first three and the CI model. It does not attempt independent L2 approval authority.

## Findings

### Already aligned and implemented on `main`

- Gherkin is the business specification authority. Bindings, fixtures, observers and configuration form a captured oracle.
- Approval binds to an immutable, content-addressed baseline. Prepare and review never approve. Edited inputs invalidate approval.
- Replacements must be reviewed against the replaced revision and cannot drop obligations.
- Real Reqnroll/MSTest/MTP execution against a separately built API. Native success alone is not PASS.
- `PASS`, `FAIL` and `INDETERMINATE` are distinct. A behavioural FAIL requires an outcome-step assertion failure. Unbound steps and setup errors are INDETERMINATE.
- Earlier failures for the same baseline, implementation and environment survive passing reruns.
- The plugin applies the workflow to ordinary requests. `.tada/context.md` supports session handoff.
- Honest local L1 claims, with documented build-input and runtime observation limits.

### Described in guidance but not mechanically supported

- Change-type paths for feature, defect, refactor and chore work exist in the skill and generated workflow. Selection is agent judgement.
- NFR and fitness discovery was a single line in the generated workflow, and had no representation in captured inputs.
- Targeted questions for acceptance-critical unknowns, and delegation of routine choices.
- Small increments and reviewable pull requests.
- Independent approval authority for CI is documented as proposed work.

### Partially implemented, or present in draft work

- **Change-aware verification ([ADR 0042](../decisions/0042-add-change-aware-multi-level-verification.md), merged from PR #5, unreleased alpha.13):** discovery-time verification selection, content-addressed verification plans, supporting MSTest suites with TRX reconciliation, multi-service local systems, and `NO_CHECKS` for justified no-test changes. Its contract checks can have `supporting` purpose, which does not require approval. This change defines obligation kinds that PR #5's plans can adopt for protected contract and fitness checks.
- Lifecycle states: ADR 0013 accepted `DRAFT`, `APPROVED` and `SUPERSEDED` conceptually. Concrete enforcement of approved-but-unimplemented scope was open.

### Missing and necessary (addressed by this change)

| Gap | Resolution |
| --- | --- |
| Product identity and lifecycle presentation | New README, charter update, lifecycle guide and [ADR 0043](../decisions/0043-adopt-the-tada-north-star.md) |
| Spec-before-implementation without breaking CI | Enforcement ledger and `tada gate` ([ADR 0044](../decisions/0044-separate-approval-authority-from-merge-enforcement.md)) |
| Bypass by relabelling or demoting obligations | Base-branch comparison, scenario fingerprints, mandatory promotion, visible candidate ages and policy |
| Contract and fitness changes indistinguishable from test edits | Declared obligation roots and classified protected changes ([ADR 0045](../decisions/0045-classify-protected-verification-obligations.md)) |
| Missing execution reported as success | Gate without a run is `INDETERMINATE` while active obligations exist, and `NO_CHECKS` otherwise |
| Realistic lifecycle validation | `scripts/Test-EnforcementWorkflow.ps1`, run in CI |

### Longer-term capabilities that remain deferred

- Independent approval authority for team CI (L2), such as a trusted approval workflow or GitHub review integration that binds approval to a baseline digest.
- Protected contract and fitness suites executed outside the acceptance harness. This builds on ADR 0042's verification plans.
- Other DSLs, runners, languages and operating systems.
- Stronger runtime provenance beyond the current observer.

## Prioritised roadmap

| Priority | Item | Depends on | Acceptance criteria |
| --- | --- | --- | --- |
| 1 | Reconcile verification plans with enforcement | ADR 0042, ADR 0044 | Done for coexistence: plans and the gate run side by side. Next, the gate can consume plan runs so supporting checks stay delegated while active obligations stay mandatory. |
| 2 | Protected plan checks | 1, ADR 0045 | A plan check with `contract` or `fitness` purpose must reference approved captured inputs. Changing them is reported by `gate --base`. Supporting checks remain unapproved. |
| 3 | Independent approval adapter for CI | ADR 0044 | In CI, a baseline approval introduced by a pull request counts only when it is bound to a review by an authorised human outside the pull request's content, such as a required code-owner review of the exact digest. The gate reports which approvals are independently authorised. |
| 4 | Scoped amendment and withdrawal of scenarios | ADR 0013, ADR 0044 | A human-approved amendment can retire a scenario identity with retained history. The gate reports the withdrawal as a protected change. |
| 5 | Shared CI history for failure preservation | 3 | Earlier failed CI attempts for the same baseline and implementation are supplied to later gate runs. |
| 6 | Gate summary for pull requests | ADR 0044 | CI publishes active, candidate and protected-change tables in the job summary without parsing raw JSON by hand. |
| 7 | Additional adapters | Common verification contract | A second runner implements discovery, execution and evidence without changing authority, enforcement or assessment rules. |

## Validation evidence

- Focused tests in `tests/Tada.Core.Tests/EnforcementTests.cs` cover gate rules, ledger validation, lineage, obligation classification and scenario fingerprints.
- `scripts/Test-EnforcementWorkflow.ps1` exercises a disposable Git repository with real HTTP/SQLite Orders runs. It covers: a spec-only candidate before bindings, rejected demotion, rejected unclassified scenarios, fresh-session status, meaningful behavioural RED, implementation GREEN with mandatory promotion, rejected assertion weakening, contract and fitness amendments, supporting tests outside the oracle, policy weakening, defect reproduction and repair, a refactor without new approval, a chore without execution, and tampered evidence. Its approvals are labelled automated controls, not human acceptance.

## Discovery and continuous learning assessment

Assessment date: 2026-11-11, against `main` at `cf267fa`. [ADR 0046](../decisions/0046-investigate-before-agreeing-acceptance.md) and [ADR 0047](../decisions/0047-add-reference-and-augment-knowledge-modes.md) record the resulting decisions.

| Capability | State before this change | Gap | Resolution |
| --- | --- | --- | --- |
| Investigation before agreement | Guidance only. One Understand paragraph in the skill: inspect the request, obligations, components, constraints and tests. | No depth guide, stopping rule, distributed-system checklist or reporting format. Agents could propose Gherkin after reading a few files. | Risk-proportionate depth guide, stopping rule, retrieval by relevance and a conversational change impact brief in the skill, its reference and the generated workflow. |
| Verified, inferred and unknown findings | Guidance only: "never silently adopt an assumption as agreed". | No convention distinguishing evidence from inference, or code behaviour from intended behaviour. | Brief separates Verified (with references), Inferred and Unknown. Authority order documented. |
| Session handoff | Implemented as a file convention: `tada init` creates `.tada/context.md`, the skill updates it. | Mixed current work with history and went stale. This repository's handoff still named a merged branch and a completed next action. | Handoff restricted to current work, finished work replaced, and `tada knowledge check` reports how many commits landed after the handoff last changed. |
| Persistent engineering knowledge | Absent. The charter stated Git authority and relevance indexing; Q11 left mechanics open. | No defined home for root causes, invariants or pitfalls; no index; no provenance. | Optional pointer index in reference mode; focused records only in augment mode, with front-matter provenance. |
| Knowledge creation policy | Absent. | No way for a team to allow or forbid agent-authored knowledge documents. | `.tada/knowledge.json` with `reference` (default) and `augment` modes, outside the captured oracle. |
| Staleness and supersession | Absent. | Recorded facts could be trusted after the code moved on. | Records carry a `checked` commit and `sources`; the check flags changed or missing sources, unknown checkpoints, broken links and missing successors. Supersession retains history. |
| Agent behaviour validation | Requester trial for ordinary use; no discovery or learning trials. | No evidence of how agents investigate or preserve knowledge. | Realistic multi-session trials in disposable repositories, recorded in the [knowledge workflow evaluation](../validation/knowledge-workflow-evaluation.md). |

Deferred:

- Semantic retrieval. Conventional search and Markdown links work across models and need no new infrastructure.
- Retrieval from issue trackers and pull requests is agent behaviour using whatever access the host provides. TADA has no adapter.
- Semantic contradiction detection between documents and code. The check flags structural staleness only.
- Retention and redaction rules for knowledge (Q11).
