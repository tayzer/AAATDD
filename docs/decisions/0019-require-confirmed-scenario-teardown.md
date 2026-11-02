# ADR 0019: Require confirmed scenario teardown

Status: **ACCEPTED**. Decision date: 2026-10-28. Resolves Q18's initial teardown policy; concrete ownership checks, cleanup mechanics and retention remain open.

## Context

A passing behavioural assertion does not establish that a scenario released its resources. Cleanup failure must remain visible without overwriting evidence about product behaviour. ADR 0008 assigns fixture ownership of scenario creation, seed and cleanup.

## Decision

Attempt cleanup after execution or setup failure for resources whose scenario ownership has been established. Stop owned processes and release SUT/observer connections, including pooled handles, before deleting owned scenario database files. Do not stop unrelated processes or delete unrelated files.

Require confirmed cleanup for the verification assessment to be `PASS`. Preserve behavioural results and diagnostics separately:

| Behavioural evidence | Required cleanup | Assessment |
| --- | --- | --- |
| Valid complete satisfaction | Confirmed | Eligible for `PASS`, subject to every other approved requirement |
| Satisfied | Failed or unconfirmed | `INDETERMINATE`, with cleanup reason |
| Trustworthy demonstrated failure | Failed or unconfirmed | `FAIL`, retaining the additional cleanup gap |
| Missing or invalid | Confirmed | Cleanup cannot convert missing or invalid verification into `PASS` |

Apply ADR 0009's globally invalid oracle and mixed-evidence rules. Teardown does not rewrite individual assertion results. Retain collected observation/error evidence; cleanup failure cannot erase already established behavioural evidence or diagnostics.

## Alternatives considered

- Treating cleanup as best effort without affecting completion can hide resource leaks behind green scenarios.
- Classifying every cleanup problem as a product failure conflates infrastructure validity with demonstrated unmet behaviour.
- Discarding behavioural results after teardown failure loses trustworthy evidence and diagnostic value.

## Consequences and limits

Concrete process ownership, path validation, handle/pooling settings, teardown timeouts and evidence storage remain pending review. Raw-database preservation, retention periods and any authorised retention exception remain open. This sample policy authorises no actual deletion, process termination, implementation or execution during design.

## Approval reference

On 2026-10-28, the user replied **“yes”** to “Should we adopt this teardown policy for the sample?” The preceding proposal required cleanup after execution/setup failure, release of owned processes/connections before owned-file deletion, confirmed cleanup for `PASS`, separate behavioural/diagnostic preservation, and ADR 0009's `INDETERMINATE`/`FAIL` treatment of cleanup failures.

Related: [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0017](0017-validate-fixture-readiness-before-submission.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
