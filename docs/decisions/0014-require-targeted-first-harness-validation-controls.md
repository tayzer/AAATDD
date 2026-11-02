# ADR 0014: Require targeted first-harness validation controls

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q5's first-harness validation policy; concrete control inventory and general adequacy policy remain open.

## Context

A passing sample alone does not demonstrate that the harness detects relevant defects. Integrity checks can detect an unapproved oracle edit without establishing that an approved assertion is meaningful. Missing setup or observations must not produce apparent success or masquerade as detecting a product defect.

## Decision

Before accepting the first harness's validation evidence, require a correct-sample positive control and targeted negative controls covering these classes:

| Control class | Required demonstration |
| --- | --- |
| Correct sample | An intact approved oracle with complete valid evidence produces `PASS` |
| Deliberate behavioural defects | Relevant trusted assertions produce `FAIL` for the identified incorrect behaviour |
| Unapproved oracle edits | Integrity checking rejects the changed oracle, blocking completion |
| Broken setup or unavailable observations | Missing or invalid required evidence blocks completion rather than being treated as success |

Apply ADR 0009's outcome semantics. An invalid oracle produces `INDETERMINATE` with the integrity reason. Setup or observation failures produce `INDETERMINATE` unless a separately established trustworthy requirement failure warrants `FAIL`; retain the evidence gaps. A globally invalid oracle cannot establish a trustworthy behavioural failure.

Identify each selected control's fault, expected result and observed reason. A behavioural negative control is successful only when the intended assertion detects its fault; a crash or setup failure does not establish that sensitivity. Keep harness-validation control results distinct from the ordinary SUT acceptance result. An expected `FAIL` in a faulty variant is evidence for the harness's selected detection claim, not a passing product acceptance result.

Approval of baseline expectations may precede execution under ADR 0013. Accepting the first harness's validation evidence requires the selected controls to demonstrate their expected results. This decision does not require mutation runs for every subsequent product change.

## Alternatives considered

- A passing correct sample alone leaves false-green paths unexercised.
- Integrity-only validation detects unapproved edits but can preserve approved weak assertions.
- An exhaustive mutation campaign would add scope before the first contract is exercised and still would not prove universal correctness.

Targeted controls provide bounded evidence for the first experiment.

## Consequences and limits

These controls demonstrate selected fault detection, not universal semantic no-op detection, complete assertion coverage or independent L2 assurance. Retain uncovered sensitivity limits explicitly. The concrete variants, artifact identities, evidence format and execution plan require review; the candidate inventory remains proposed. Broader adequacy policy and when later changes require revalidation remain open. No implementation, dependency installation, execution or privileged-observation exception is authorised.

## Approval reference

On 2026-10-27, the user replied **“Yes”** to: “Should we require these positive and targeted negative controls before accepting the first harness’s validation evidence?” The preceding recommendation described a passing correct sample, behavioural defects detected by their intended assertion, oracle-tamper rejection, and completion blocked by broken setup or unavailable observations, limited to first-harness validation.

Related: [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0012](0012-bound-the-initial-oracle-to-harness-inputs.md), [ADR 0013](0013-separate-baseline-authority-from-verification.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
