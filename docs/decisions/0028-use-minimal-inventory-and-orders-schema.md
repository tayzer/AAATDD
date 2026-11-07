# ADR 0028: Use a minimal inventory and orders schema

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q24's initial table/column structure; actual DDL/queries, additional constraints and concrete dependency versions remain open.

## Context

The sample has an agreed disposable SQLite topology, direct-SQL provider approach and observation scope. Explicit stored fields make the fixture and observer interpretation reviewable without adding a general data model or duplicating the business specification.

## Decision

Use these two tables in each scenario-owned database:

| Table | Columns and agreed constraints |
| --- | --- |
| `inventory` | `product_sku TEXT PRIMARY KEY NOT NULL`, `available INTEGER NOT NULL` |
| `orders` | `order_id TEXT PRIMARY KEY NOT NULL`, `request_id TEXT NOT NULL`, `customer_id TEXT NOT NULL`, `product_sku TEXT NOT NULL`, `quantity INTEGER NOT NULL` |

Identities are text; measured quantities are integers; every listed field is non-null. Keep `request_id` non-unique: it supplies correlation without an idempotency promise. Apply ADR 0018's complete scenario-owned order-population observation and accepted-response order identity comparison.

These columns map the agreed observations to persisted fields. The Gherkin remains authoritative for business behaviour; field declarations do not establish additional request-validation behaviour outside the two agreed scenarios.

## Alternatives considered

- Additional customer/catalogue/history tables would expand the model without a demonstrated need in this bounded experiment.
- Opaque payload storage would make the fields required by the agreed observer less explicit.
- A unique request constraint would introduce database uniqueness beyond the agreed correlation role.

## Consequences and limits

Actual DDL, seed/persistence/observation queries, additional constraints/indexes, SQLite strict-table mode, identity-generation rules and schema-identity encoding remain review items. They must not silently redefine acceptance. Provider/native versions, connection/transaction settings and the privileged-observer exception remain pending review. No migration file, database, implementation, installation or execution is authorised by this design decision.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should we adopt this two-table schema for the sample?” The preceding recommendation named both tables/columns, primary keys on product SKU and order identity, text identities, integer quantities, non-null fields and a non-unique request correlation ID, leaving actual SQL/additional constraints and versions open.

Related: [ADR 0006](0006-bound-the-initial-orders-example.md), [ADR 0018](0018-inspect-all-scenario-owned-orders.md), [ADR 0027](0027-use-microsoft-data-sqlite-with-direct-sql.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
