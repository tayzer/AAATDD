# ADR 0017: Validate fixture readiness before submission

Status: **ACCEPTED**. Decision date: 2026-10-28. Resolves Q18's initial fixture-readiness policy; concrete setup validation and reusable provider contracts remain open.

## Context

The sample cannot meaningfully assess an order operation if its required starting state is unestablished. A faulty seed can make a behavioural check fail for the wrong reason or pass without exercising the agreed case. Setup validation and product outcome assertions need distinct evidence.

## Decision

Before submitting the sample order, validate the seeded scenario-owned database state and public stock view against the agreed Gherkin. Validate the intended store/schema identity, product/quantity and initial order population. Expected values come from the feature rather than being inferred from the measured database.

Keep these setup checks separate from outcome assertions. If required preconditions cannot be established, block submission and completion rather than claim meaningful behavioural RED. Apply ADR 0009: missing or invalid setup evidence yields `INDETERMINATE` unless a separately established trustworthy requirement failure warrants `FAIL`. Preserve independently established product failures; labelling a defect as setup cannot erase it.

Use a deliberately wrong seed as a targeted control to demonstrate that readiness validation blocks the action for the intended cause. This refines ADR 0014's required setup/evidence-validity control class without selecting the concrete faulty seed or implementation.

## Alternatives considered

- Trusting successful seed execution does not establish that the intended data or store was used.
- Relying only on later outcome assertions can confuse invalid setup with a demonstrated behavioural defect.
- Deriving expected values from the measured database can reproduce a fixture error in the oracle.

## Consequences and limits

Concrete schema, validation queries, seed fault, code, versions and evidence format remain pending review. This policy does not establish complete independence between setup and observer code, approve a privileged outcome-observation exception, define reusable provider APIs or authorise implementation/execution. General fixture and teardown policies remain open.

## Approval reference

On 2026-10-28, the user replied **“yes”** to “Should we adopt this readiness policy for the sample?” The preceding recommendation required database/public-stock validation against Gherkin before submission, distinct setup and outcome checks, blocked submission for unestablished preconditions, a deliberately wrong-seed control, and preservation of independently established product failures.

Related: [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0014](0014-require-targeted-first-harness-validation-controls.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
