# ADR 0029: Use explicit SQLite connection roles

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q24's initial open-mode/pooling profile; concrete configuration, dependency versions and the observer exception remain open.

## Context

The fixture owns creation of each scenario database. The SUT and read-only observer must use that prepared store without silently creating another one. Required cleanup needs deterministic release of owned connections and files.

## Decision

Use explicit role-specific connection modes against the established scenario-owned database path:

| Role | Mode |
| --- | --- |
| Fixture creation/seed | `ReadWriteCreate` |
| SUT persistence | `ReadWrite` |
| Outcome observer | `ReadOnly` |

Only fixture setup creates the database. Set `Pooling=False` for connections in all three sample roles to simplify handle release before cleanup. Preserve distinct observer connections and the post-completion committed-read policy.

Missing or unreadable storage must not become empty observation evidence. Resolve errors under ADR 0009 according to their established cause and trustworthy evidence, preserving any separately established failure.

## Alternatives considered

- Default create-capable modes could mask a missing or misdirected store.
- Pooling can be supported later, but adds handle-lifetime concerns without a demonstrated need in this small serial experiment.

## Consequences and limits

Connection modes do not replace ownership/path validation, prove that supplied paths refer to the intended file, restrict all SQL capabilities of a read-write connection or establish filesystem isolation. Exact connection construction, locking/transaction/timeouts, provider/native versions and resolved behaviour still require review. The concrete privileged-observer exception remains unapproved. No configuration file, database, implementation, installation or execution is authorised.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should we adopt this connection profile?” The preceding recommendation specified fixture `ReadWriteCreate`, SUT `ReadWrite`, observer `ReadOnly`, fixture-only creation and disabled pooling throughout the sample, leaving exact configuration, versions and the observer exception open.

Related: [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0016](0016-observe-completed-sample-operations.md), [ADR 0019](0019-require-confirmed-scenario-teardown.md), [ADR 0027](0027-use-microsoft-data-sqlite-with-direct-sql.md), [provider research](../research/2026-11-03-sqlite-provider.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
