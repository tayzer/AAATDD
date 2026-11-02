# ADR 0015: Agree the initial sample HTTP mapping

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q23's initial HTTP mapping; concrete wire validation, consistency mechanics and executable-baseline approval remain open.

## Context

ADR 0007 selects public HTTP observations for the order result and stock. The two agreed Gherkin scenarios need an explicit interface interpretation so that an arbitrary error cannot count as the intended insufficient-stock rejection. This is a sample protocol contract, not another business specification.

## Decision

Use the following mapping for the initial sample:

| Operation or result | HTTP interface | Required fields |
| --- | --- | --- |
| Submit order | `POST /orders` | Request: `requestId`, `customerId`, `productSku`, `quantity` |
| Accepted order | HTTP `201` | Response: `outcome: accepted`, `requestId`, `orderId` |
| Insufficient stock | HTTP `409` | Response: `outcome: rejected`, `reason: insufficient_stock`, `requestId` |
| Observe stock | `GET /products/{sku}/stock`, HTTP `200` | Response: `productSku`, `available` |

Recognise acceptance and stock rejection from the explicit status/body combinations. Response request/product identities must match the request or product being observed. `requestId` provides correlation without promising idempotency. An arbitrary error does not satisfy the insufficient-stock expectation.

The Gherkin feature remains authoritative for business behaviour. Response fields alone do not demonstrate persisted-order correctness; the separately agreed observation split still applies.

## Alternatives considered

- HTTP `422` could represent a rejected request; `409` was selected to express conflict with current stock.
- Treating every non-success response as rejection could hide application failures and does not identify the approved business outcome.
- Status-only recognition omits the explicit outcome, rejection reason and correlation checks.

## Consequences and limits

Concrete wire types/serialization, schema validation, endpoint configuration, completion/read-consistency mechanics, fixture/schema implementation and the privileged-observation exception remain pending executable-baseline review. This approves only the initial sample mapping, not a general API design, full DTO-validation suite, executable baseline or implementation. It adds no retry, idempotency, concurrency or asynchronous-processing promise.

## Approval reference

On 2026-10-27, the user replied **“Yes”** to “Should we adopt this HTTP mapping for the initial sample?” The preceding recommendation listed the request fields, `201` acceptance, `409` insufficient-stock response, `200` stock observation, matching response identities, correlation without idempotency, and exclusion of arbitrary errors as stock rejection.

Related: [ADR 0006](0006-bound-the-initial-orders-example.md), [ADR 0007](0007-use-public-outcomes-with-scoped-order-observation.md), [Orders feature](../requirements/order-stock-validation.feature), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
