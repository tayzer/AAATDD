# ADR 0018: Inspect all scenario-owned orders

Status: **ACCEPTED**. Decision date: 2026-10-28. Resolves Q18's initial outcome-observer scope; concrete queries, field types and the privileged-observation exception remain open.

## Context

A query filtered only by the expected request identity can hide an erroneous order carrying another identity. The initial sample has one request per scenario and a dedicated disposable store, so the complete synthetic order population is small and bounded.

## Decision

Inspect all orders in the scenario-owned database before applying expected request/customer filters. Compare the population against the Gherkin expectations and check the expected counts, customer, product, quantity and request identity. For an accepted order, also match the stored order identity to the response's `orderId`.

Use the separately agreed read-only observation connection and post-completion read policy. This scope covers only the isolated scenario-owned store; it confers no authority to inspect unrelated databases. It refines the executable observation interpretation without replacing the Gherkin business specification.

Missing or unreadable storage cannot establish an empty population under ADR 0009. Concrete read-only open settings and missing-file handling still require review as part of the observer implementation.

## Alternatives considered

- Filtering only by the expected `requestId` can falsely report no orders when an incorrect row exists under a different identity.
- Counting only a filtered population omits identity/field correctness and cannot establish response-to-persistence correspondence.
- Inspecting unrelated stores would exceed the sample's isolated observation boundary and is unnecessary.

## Consequences and limits

Whole-population observation is appropriate for this bounded sample, not a default requirement to scan production databases. Concrete schema, SQL, field types, provider/version, provenance encoding and the narrowly scoped privileged-observation exception remain unapproved. This agreement does not authorise implementation, execution or privileged access.

## Approval reference

On 2026-10-28, the user replied **“Yes”** to “Should we adopt this observation scope for the sample?” The preceding recommendation required inspecting all scenario-owned orders, checking expected counts and customer/product/quantity/request identities, and matching accepted-order identity to response `orderId`, to avoid hiding rows behind an expected-request filter.

Related: [ADR 0007](0007-use-public-outcomes-with-scoped-order-observation.md), [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0015](0015-agree-initial-sample-http-mapping.md), [ADR 0016](0016-observe-completed-sample-operations.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
