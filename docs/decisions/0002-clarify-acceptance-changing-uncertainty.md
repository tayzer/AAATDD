# ADR 0002: Clarify acceptance-changing uncertainty

Status: **ACCEPTED**. Decision date: 2026-10-23.

## Context

Discovery cannot resolve every possible detail upfront. Asking a human to decide every routine technical choice creates excessive interruption. Allowing an agent to silently choose business rules or material trade-offs undermines human ownership of acceptance.

The first approval checkpoint therefore needs an explicit boundary between decisions requiring human clarification and routine choices an agent may make within approved constraints.

## Decision

- Ask the human when plausible answers would change observable behaviour, scope, required compatibility, material security or data risks, fitness targets, or the interpretation of an acceptance check.
- Make routine technical choices within approved constraints, following existing conventions where applicable.
- Escalate when the agent cannot establish that a choice stays within that boundary.
- Record candidate answers as proposals until agreed; do not present an assumption as an approved fact.
- Defer unanswered acceptance-changing questions only with the affected scope explicitly excluded from the agreed slice. Record the deferral and do not silently narrow the requested outcome.

Cancellation eligibility requires human agreement. Test-file naming may follow repository conventions when it does not affect acceptance or violate an approved constraint. Ease of reversing an implementation does not make a behavioural choice routine.

## Alternatives considered

1. Require human input for every unresolved choice. This limits delegated choices but creates excessive interruption and cannot establish exhaustive completeness.
2. Let the agent choose defaults and seek retrospective approval. This reduces interruption but risks building behaviour that the human never intended.

## Rationale and consequences

Human attention goes to decisions that affect acceptance and material risks. Agents retain room for routine engineering choices within established constraints.

Applying the boundary requires judgement. Uncertainty about whether a choice falls inside it is itself a reason to ask. Agreement covers a bounded slice and does not claim that all possible requirements have been discovered.

This decision resolves the clarification boundary only. The minimum contents and format of an intent agreement, approval mechanics, and the rest of the lifecycle remain open. It grants no authorisation to implement AAATDD.

## Approval reference

In the design conversation on 2026-10-23, the user replied **“adopt the boundary”** after the proposed rule to clarify acceptance-changing uncertainty, permit routine choices within approved constraints, explicitly defer affected scope, and escalate uncertainty about the boundary itself.

Related: [ADR 0001](0001-separate-intent-and-baseline-approval.md), [lifecycle discussion](../architecture/change-lifecycle.md).
