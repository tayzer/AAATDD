# ADR 0036: Bind approval to the reviewed baseline revision

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha review-to-approval binding within Q8; identifiers, digests, schemas and flags remain open.

## Context

The alpha CLI requires approval of unchanged reviewed content, distinct from execution. It needs a record connecting the review presentation to the exact baseline receiving approval without implying authenticated human review in cooperative L1 operation.

## Decision

Use a review record tied to an immutable baseline revision:

1. `tada baseline prepare` creates a draft snapshot of oracle inputs, expected cases and policy.
2. `tada baseline review` shows that snapshot, changes from the previous baseline and unresolved prerequisites. It produces a review record identifying the exact revision.
3. `tada baseline approve` requires explicit selection of that review record. Before recording approval, it checks that the proposed inputs still match the reviewed revision.
4. Any material change requires a new draft and review. Preserve earlier snapshots, reviews and approvals.
5. `tada run` requires explicit selection of an approved revision. Changed or missing oracle inputs block execution and yield `INDETERMINATE`.

Keep these records distinct:

| Record | Meaning |
| --- | --- |
| Baseline revision | The captured expectations and execution inputs. |
| Review record | The revision presented for review, including comparison context. |
| Approval record | Who declared approval, for which revision and scope. |

Replacement approval must explicitly identify superseded scope. Approving a new revision must never silently retire earlier obligations.

## Alternatives considered

- Approving the current workspace without selecting a review record would not reliably link approval to the content presented for review.
- Treating a generated review record as approval would conflate presentation with human authority.
- Updating an existing approved snapshot or implicitly replacing earlier scope would obscure the reviewed revision and retained obligations.

## Consequences and limits

A generated review record identifies what TADA presented; it cannot prove someone read it. In cooperative L1 operation, approver identity remains a declaration rather than authenticated human approval against a controlling local actor.

Exact identifiers, hashes, schemas and flags remain open. Concrete capture/storage, comparison selection, material-input rules and source-to-execution integrity checks still need design. These record roles do not add conceptual baseline lifecycle states beyond ADR 0013.

The pre-execution integrity gap above yields `INDETERMINATE`; it does not erase separately established trustworthy failures retained under ADRs 0009–0010. This decision does not approve implementation, further runtime work or publication. Prior accepted ADRs remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“yes”** to “Shall we adopt this review-to-approval binding for the alpha?” The preceding proposal specified the five-step flow, three distinct record roles, explicit supersession scope, L1 limits and deferred concrete encodings recorded here.

Related: [CLI boundary](0035-adopt-five-operation-alpha-cli.md), [baseline lifecycle](0013-separate-baseline-authority-from-verification.md), [oracle boundary](0012-bound-the-initial-oracle-to-harness-inputs.md), [design status](../design-status.md), [open questions](../open-questions.md).
