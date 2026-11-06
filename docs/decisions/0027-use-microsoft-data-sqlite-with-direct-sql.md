# ADR 0027: Use Microsoft.Data.Sqlite with direct SQL

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q24's initial SQLite provider approach; concrete schema, versions and privileged-observer exception remain open.

## Context

ADR 0008 selects one disposable SQLite file per scenario, and the sample now targets .NET 10. The fixture and outcome observer need reviewable database access with distinct write/read-only responsibilities. A concrete provider approach is needed before proposing the schema and dependency graph.

## Decision

Use `Microsoft.Data.Sqlite` with direct parameterised SQL for sample SUT persistence, fixture setup/validation and the distinct read-only order observer.

Keep SUT persistence logic separate from harness observation queries and assertions. Shared low-level provider code does not establish independence and must not lead to copying SUT business-rule implementation into the oracle. Expected results remain grounded in Gherkin.

This is a bounded sample choice, not a mandatory framework-wide provider API. Fixture write authority does not confer privileged outcome-observation authority.

## Alternatives considered

- An ORM adds a mapping layer without a demonstrated need in the small sample and can obscure the exact observation queries under review.
- Another ADO.NET provider is possible, but the documented direct-SQL and read-only capabilities of Microsoft.Data.Sqlite suit the agreed sample boundary.

## Consequences and limits

Exact schema, commands, provider/native package versions, resolved dependencies and transaction/pooling/open settings remain unapproved. Concrete observer source identity, permissions, environment and limitations still need explicit exception review before execution. Selecting a library does not approve that exception or prove the implementation correct. No installation, restore, implementation or execution is authorised.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should we adopt this provider approach for the sample?” The preceding proposal selected Microsoft.Data.Sqlite with direct parameterised SQL for SUT persistence, fixture setup and read-only observation, kept SUT/harness logic separate, and left schema, versions and privileged-observer approval open.

Related: [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0018](0018-inspect-all-scenario-owned-orders.md), [ADR 0026](0026-target-net10-for-first-sample.md), [provider research](../research/2026-11-03-sqlite-provider.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
