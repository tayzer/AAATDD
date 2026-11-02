# ADR 0010: Preserve failures across explicit reruns

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q7's initial-experiment retry policy; general retry/waiver rules remain open.

## Context

A retry that eventually turns green can conceal an intermittent product defect or a defective oracle. The initial experiment should exercise the basic verification contract without adding automatic-retry and flaky-result acceptance rules.

## Decision

- Disable automatic scenario retries in the first experiment: evaluate one attempt per required case in each verification run.
- Retain every explicit rerun as separate evidence with a new run identity.
- Do not let a later pass erase an unresolved trustworthy failure for the same assessed implementation, baseline and relevant environment.
- Resolve earlier failures through an identified fix or justified evidence invalidation under an authorised policy/approval path. The implementation agent may propose invalidation but cannot unilaterally discard a trusted failure.
- An earlier `INDETERMINATE` may be resolved by correcting the evidence/environment problem and obtaining a valid fresh run, without necessarily changing the application. Keep the original cause and history.

Routine fixes and reruns may stay within existing implementation authorisation; they do not require a fresh approval solely because another run is needed. Oracle changes still follow their amendment rules. This decision provides no implementation authorisation itself.

This is a scenario-level policy for the first experiment, not a restriction on lower-level network retransmission or separately approved temporal observation/polling semantics. It does not authorise silently retrying a mutating order action.

## Alternative considered

Bounded automatic retries with all attempts retained and an explicit flaky-result acceptance policy. This may reduce transient interruptions, but adds acceptance semantics before the initial contract is exercised. Projects may consider such a policy later through approval; it is not the initial default.

## Consequences and limits

Preserve native per-run results and the unresolved assessment history. A later run's passing steps do not automatically clear a prior trustworthy failure. The finding and its resolution must remain traceable to implementation, baseline, environment and run identities.

General retry policy, waivers, evidence-resolution encoding and retention mechanics remain open. No runtime, dependency installation, concrete privileged exception or executable baseline is approved here.

## Approval reference

On 2026-10-27, the user replied **“yes”** to: “Should the first experiment disable automatic retries and preserve unresolved failures across explicit reruns?” The preceding proposal specified retained evidence, resolution of earlier failures and authorised evidence invalidation.

Related: [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
