# ADR 0013: Separate baseline authority from verification

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q8's conceptual baseline revision lifecycle; concrete mechanics and broader starting-evidence policy remain open.

## Context

An executable baseline describes the approved target for a change. Requiring passing implementation before it becomes authoritative would obscure the acceptance-driven development cycle. Review activity, approval authority and verification satisfaction need separate meanings.

## Decision

Use three conceptual baseline revision states:

| State | Meaning |
| --- | --- |
| `DRAFT` | Proposed revision without approval authority; review, rejection and withdrawal can be recorded as events |
| `APPROVED` | Immutable human-authorised revision, authoritative within its approved change/scope even before the implementation satisfies it |
| `SUPERSEDED` | Previously approved revision explicitly replaced within the relevant scope by another approved revision; retained historically |

Track verification separately using ADR 0009's `PASS`, `FAIL` and `INDETERMINATE`. An approved valid baseline can produce `FAIL` while implementation is being developed. Only `PASS` permits completion; approval cannot change a verification result. A passing draft does not acquire approval authority.

Editing approved content creates another draft without inherited approval. The previous approved snapshot remains authoritative for its existing scope until explicitly replaced. Preserve approval and evidence history, and do not drop obligations outside the replaced scope. The representation of partial supersession remains open.

Already-satisfied expectations need no artificial RED history. Missing bindings, broken setup and unavailable evidence do not establish meaningful behavioural RED. Detailed sensitivity and starting-evidence policy remain open.

This authority is scoped to the approved change. It does not automatically activate a project-wide baseline, authorise implementation or permit merging failing code.

## Alternatives considered

Additional `PROPOSED`, `APPROVED_PENDING` and `ACTIVE` states could expose review and execution progress, but risk conflating authority with satisfaction. Keep review activity as events and use verification outcomes to express execution results.

## Consequences and limits

Concrete schemas, approval identities, authorisation controls, amendment validation and supersession mechanics remain unselected. Brownfield characterisation, refactor sufficiency and general RED/sensitivity requirements still need treatment. This decision approves the conceptual lifecycle only, not an executable baseline or implementation.

## Approval reference

On 2026-10-27, the user replied **“Yes”** to “Should we adopt these three lifecycle states?” The preceding proposal defined `DRAFT`, `APPROVED` and `SUPERSEDED`, scoped authority before passing verification, immutable approved revisions, draft amendments and separate verification outcomes with only `PASS` permitting completion.

Related: [ADR 0001](0001-separate-intent-and-baseline-approval.md), [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0011](0011-separate-logical-baseline-and-run-identities.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
