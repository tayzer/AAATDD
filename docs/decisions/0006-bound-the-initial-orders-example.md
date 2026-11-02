# ADR 0006: Bound the initial Orders example

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q22 at the intent checkpoint.

## Context

The local experiment needs a small, contrasting set of business expectations before its executable interpretation can be reviewed. Rejection-only coverage could admit an application that rejects every order. Asynchronous completion would introduce temporal-observation requirements before the first synchronous contract has been exercised.

## Decision

Use synchronous, atomic order handling for the first example: the customer receives a final outcome with the corresponding order and stock effects settled. The accepted business expectations are defined once in [order-stock-validation.feature](../requirements/order-stock-validation.feature). Both the oversubscription rejection and exact-stock success scenarios are included.

The example is bounded to one product and one isolated customer making one request per scenario. Concurrency, reservations, payments and fulfilment are deferred from this experiment. These boundaries do not restrict AAATDD's eventual support for other application behaviours.

Promote the existing Gherkin proposal to the requirements directory without changing its scenario bodies. This records **intent agreement only**. The identifier-schema design remains open; the current tags do not establish a universal naming policy.

## Alternative considered

Model asynchronous order completion immediately. This is a valid later example, but would require approved completion signals, observation deadlines and consistency semantics in the first experiment. The synchronous example provides a smaller starting point while retaining contrasting outcomes.

## Consequences and limits

The first intent checkpoint is satisfied for this bounded business contract. The second checkpoint still needs approved bindings, fixtures, observations, assertions, completion/consistency semantics, expected execution inventory and verification policy.

No privileged-observer exception, API/storage/package choice, runtime contract or implementation is approved by this decision. The feature is not bound or executed. The project remains in DESIGN ONLY mode.

## Approval reference

On 2026-10-27, the user replied **“yes”** to: “Do you agree with this synchronous, atomic two-scenario scope for the first example?” The preceding proposal showed the two Gherkin scenarios and the bounded/deferred scope recorded above.

Related: [ADR 0001](0001-separate-intent-and-baseline-approval.md), [ADR 0005](0005-validate-l1-before-protected-l2.md), [experiment design](../architecture/2026-10-26-design-reconciliation.md).
