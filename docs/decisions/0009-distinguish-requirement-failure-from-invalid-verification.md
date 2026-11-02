# ADR 0009: Distinguish requirement failure from invalid verification

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q7's core outcome semantics; retry, waiver and encoding details remain open.

## Context

A behavioural defect and an inability to verify are different findings. Unapproved assertions, missing execution or unavailable observations cannot establish a trustworthy product verdict. These conditions must still block completion, with their actual causes visible.

## Decision

Use three verification outcomes:

| Outcome | Meaning |
| --- | --- |
| `PASS` | Valid, complete evidence accounts for all required checks and satisfies the applicable approved requirements for the identified implementation and environment |
| `FAIL` | Trustworthy evidence demonstrates at least one unmet approved behavioural or fitness requirement |
| `INDETERMINATE` | Missing or invalid approval, integrity, execution or usable evidence prevents verification, without a separately established trustworthy requirement failure |

Only `PASS` permits completion. Both `FAIL` and `INDETERMINATE` block it.

Known approval or integrity-policy violations are recorded as explicit rejection reasons under `INDETERMINATE`, rather than being presented as proof of a behavioural defect. The cause is known; the requirement verdict is not established. A passing native runner cannot override that rejection.

A trustworthy failure alongside an evidence gap remains `FAIL`, with the gap retained. A globally invalid oracle cannot establish trustworthy behavioural results from that run; its native failure messages do not become proof of a product defect.

## Alternative considered

Include known approval/integrity violations within `FAIL`. This makes the label describe both requirement satisfaction and verification-policy compliance. Keeping them distinct makes the evidence's practical meaning clearer while preserving blocking behaviour and explicit policy-rejection reasons.

## Consequences and limits

Do not interpret `INDETERMINATE` as acceptable, partially passing or a confidence score. Do not claim complete coverage for mixed runs. Preserve native execution statuses and detailed reasons separately from the overall outcome.

Exact schemas, reason codes, exit codes, retry/flake policy, waivers and specialised aggregation cases remain open. No runnable verifier, executable baseline, privileged exception or implementation is authorised by this decision.

## Approval reference

On 2026-10-27, the user replied **“Yes”** to: “Should FAIL mean a demonstrated unmet requirement, with invalid or incomplete verification reported as INDETERMINATE and both blocking completion?” The preceding proposal included the mixed-result and globally invalid-oracle cases recorded here.

Related: [baseline proposal](../architecture/first-experiment-baseline-proposal.md), [design status](../design-status.md).
