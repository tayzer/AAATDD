# ADR 0007: Use public outcomes with scoped order observation

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q23 at the observation-plan level.

## Context

The agreed Orders feature needs evidence for customer-visible outcomes and persisted orders. Public observations preserve the black-box-first direction, while a narrowly scoped database observation can establish persistence when the sample exposes no adequate public order query.

## Decision

- Observe customer-facing acceptance/rejection through the public HTTP response.
- Observe remaining stock through a public HTTP read.
- When no adequate public order query exists, use a read-only database observer limited to the isolated test store for persisted-order existence/count and relevant values.
- Prefer an adequate existing public order query over privileged inspection. An unavailable public observation does not silently authorise privileged fallback.
- Report database/query failures as unavailable evidence, never as an empty order population or a passing business assertion.

This accepts the observation split for the bounded first example. It does not grant the fixture's setup permissions to the observer or create a global privileged-observation allowance.

## Alternative considered

Use or add a public order query sufficient for the agreed claim. This offers greater black-box portability, but can add application surface solely to inspect persistence in the minimal sample. It remains preferable when an adequate interface exists; demonstrating exception handling is not sufficient justification for privileged access by itself.

## Consequences and limits

The concrete observer/provider version, scenario scope, environment, permissions, schema/query, completion/consistency boundary, limitations and authorising exception must still be made reviewable within the executable baseline. No concrete exception or implementation is approved here.

HTTP response classification and interface shapes also remain open. The feature and its scenario bodies do not change. The first experiment remains L1/advisory under ADR 0005; this observation choice does not establish stronger assurance.

## Approval reference

On 2026-10-27, the user replied **“yes”** to: “Do you agree with this observation split for the first example?” The proposal specified public HTTP observations for acceptance/rejection and stock, with a read-only database query within the isolated test store for persisted orders when no adequate public query exists.

Related: [ADR 0006](0006-bound-the-initial-orders-example.md), [observation plan](../architecture/2026-10-26-design-reconciliation.md), [business feature](../requirements/order-stock-validation.feature).
