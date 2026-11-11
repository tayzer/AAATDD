# ADR 0044: Separate approval authority from merge enforcement

Status: **ACCEPTED — IMPLEMENTATION DECISION UNDER [ADR 0041](0041-authorise-autonomous-release-implementation.md)**. Decision date: 2026-11-11. Refines the concrete mechanics left open by [ADR 0013](0013-separate-baseline-authority-from-verification.md).

## Context

Teams need to agree a specification before implementing it. A spec-only change can therefore introduce approved scenarios that the current implementation does not yet satisfy. If every approved scenario must pass on the mainline, agreeing a specification breaks CI. If approved scenarios can be excluded with a mutable label such as `@skip`, an agent can bypass verification by changing that label.

ADR 0013 deliberately kept approval (`DRAFT`, `APPROVED`, `SUPERSEDED`) separate from verification outcomes (`PASS`, `FAIL`, `INDETERMINATE`), and rejected an `ACTIVE` baseline state that would conflate them. A third dimension is still needed: whether an approved obligation is currently mandatory for merge.

## Decision

Model **enforcement status** separately from baseline authority and verification outcome.

| Dimension | Values | Changed by |
| --- | --- | --- |
| Baseline revision authority | `DRAFT`, `APPROVED`, `SUPERSEDED` | Human review and approval records (unchanged) |
| Enforcement status of an approved scenario | `candidate` (approved, may be intentionally RED) or `active` (mandatory for merge) | `.tada/enforcement.json`, constrained by `tada gate` rules |
| Verification outcome | `PASS`, `FAIL`, `INDETERMINATE` | Retained run evidence (unchanged) |

"Draft" and "superseded" remain baseline authority states. They are not enforcement states.

The enforcement ledger names one approved baseline and classifies every one of its scenarios as active or candidate. Each candidate records a `since` date and a visible reason. An optional policy limits how long candidates may remain pending.

`tada gate` assesses the ledger, the approved baseline and, optionally, a run and a base revision. It applies these rules:

1. The ledger's baseline must be approved, not superseded, and match the current oracle bytes.
2. Every approved scenario must be classified. Unknown entries are rejected.
3. Every active scenario must have a verified passing result in the supplied run, with no unresolved earlier failure for the same implementation and environment. An active assertion failure is `FAIL`. Missing or invalid evidence is `INDETERMINATE`.
4. Candidates are reported as `behavioural-red`, `incomplete` or `not-executed` and do not block.
5. A candidate that passes must be promoted to active in the same change. A completed increment therefore cannot leave its satisfied scope unenforced.
6. Against the base revision's ledger, read from Git:
   - an active obligation cannot become a candidate or disappear, unless an approved replacement chain from the base baseline changed that scenario's Gherkin content;
   - a changed baseline must supersede the base baseline through approved, compared replacements;
   - a candidate's `since` date cannot be reset, or set in the future;
   - weakened candidate policy and withdrawn candidates are reported as protected changes.
7. Candidates past the policy limit block the gate.
8. Without a run, active obligations are `INDETERMINATE` (`not-executed`). With no active obligations and no run, the result is `NO_CHECKS`, which is not a behavioural PASS.

Gate outcomes are `PASS` and `NO_CHECKS` (exit 0), `FAIL` (exit 1) and `INDETERMINATE` (exit 2). `tada verify` keeps its existing meaning: the complete assessment of every scenario in a run, so a run with a RED candidate still verifies as `FAIL`.

## Protection boundary

No single mutable file is the protection boundary. Enforcement relies on:

- approval-bound baseline content, which an agent cannot change without new human approval;
- monotonic comparison with the target branch's committed ledger, which the pull request cannot alter;
- the rule that passing candidates must be promoted;
- scenario-level fingerprints of steps, background and tags, so only a scenario whose agreed Gherkin behaviour was amended can return to candidate;
- visible ages and policy limits for pending candidates;
- independent human review of protected changes listed by the gate.

## Consequences

- Spec-only changes can merge approved candidates without breaking mainline CI. Implementation changes promote their scope when it passes.
- CI runs one baseline. Candidates run in the same execution and appear as non-blocking diagnostics.
- A partial increment can merge with candidates still pending. The gate lists them, with ages and next actions, so incomplete scope is visible rather than hidden.
- Removing an approved scenario remains unsupported, as before. Amending an active scenario requires an approved replacement. That replacement may return the scenario to candidate until its implementation passes.
- Approval records introduced in a pull request remain local L1 declarations. The gate identifies protected changes, but independent authority must come from repository controls such as required code-owner review. A trusted approval adapter remains future work.

## Alternatives considered

- **Tags such as `@candidate` or `@skip` in Gherkin.** These would be approval-bound, but promotion would then require re-approval of unchanged behaviour, and tag semantics would mix business content with delivery status.
- **Separate feature branches for unimplemented specifications.** This keeps mainline green but hides agreed expectations from the next session and from other increments.
- **Adding `ACTIVE`/`PENDING` baseline states.** ADR 0013 rejected this because it conflates authority with satisfaction.
