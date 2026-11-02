# ADR 0001: Separate intent agreement from executable-baseline approval

Status: **ACCEPTED**. Decision date: 2026-10-23.

## Context

Translating a request into acceptance criteria introduces behavioural decisions. Translating those criteria into executable checks introduces further interpretation through assertions, bindings, fixtures, drivers and configuration. Agreeing intended behaviour does not establish that its executable interpretation is correct.

The process should catch misunderstandings before substantial harness work while remaining practical for small changes.

## Decision

AAATDD normally uses two checkpoints:

1. **Intent agreement:** a human agrees the intended behaviour, concrete examples, scope and consequential assumptions.
2. **Executable-baseline approval:** a human approves the executable interpretation, including the relevant checks, bindings and harness that determine success.

A clear small change may combine both approval scopes in one review. Combining the review does not remove either scope.

Intent agreement alone does not authorise product implementation. Implementation requires the approved executable baseline and authorisation to implement. This decision does not grant that authorisation for AAATDD itself.

## Alternative considered

Always present one complete intent-and-execution bundle for approval. This reduces review interruptions, but risks constructing a substantial executable interpretation before discovering that the intended behaviour is disputed. It remains available as a combined review for a clear small change rather than the mandatory default.

## Rationale and consequences

- Business misunderstandings can be resolved before expensive test and harness construction.
- Review of executable interpretation remains explicit rather than inheriting authority from a readable scenario.
- Small changes need not incur two separate review sessions.
- Approval identity, evidence, amendment mechanics, criteria for combined review, and enforcement remain to be designed.
- The surrounding lifecycle remains provisional; this ADR accepts only these approval boundaries.

## Approval reference

In the design conversation on 2026-10-23, the user answered **“the former”** to: “Should AAATDD normally use these two checkpoints, allowing a combined review for small changes, or should it always present one complete behaviour-and-execution bundle for approval?”

Related: [lifecycle discussion](../architecture/change-lifecycle.md), [design status](../design-status.md).
