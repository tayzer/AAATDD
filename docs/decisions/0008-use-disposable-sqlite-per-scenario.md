# ADR 0008: Use a disposable SQLite file per scenario

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q24 for the first sample.

## Context

The first sample runs its SUT separately from the harness and needs an isolated store reachable by a scoped read-only observer. It has no existing production database to match, and its accepted scope defers concurrency. A server database and container runtime would add setup before the verification contract is exercised.

## Decision

- Use one uniquely named disposable SQLite file per scenario for the first Orders sample.
- Let the fixture own creation, validated seed and cleanup of that scenario's store.
- Let the separate sample SUT access the same named file for its application operations.
- Use a distinct read-only observer connection within the observation split accepted in ADR 0007.

Use an explicit file path. SQLite's in-memory and anonymous temporary database modes do not supply the cross-process store this topology requires.

## Alternative considered

Run a server database through Testcontainers. This is useful when an approved requirement depends on that engine's semantics or production parity. The initial sample has no such target, so a disposable SQLite file provides the smaller experiment.

## Consequences and limits

This is a sample-specific storage/topology decision, not a restriction on AAATDD's fixture or observer capabilities. It establishes no parity with another database engine and no L2 isolation guarantee.

Concrete schema, provider/package versions, connection lifecycle, transaction/read semantics, cleanup behaviour and the privileged-observer exception still need executable-baseline review. No dependency installation, runtime code or implementation is authorised by this decision.

## Approval reference

On 2026-10-27, the user replied **“yes”** to: “Should we use one disposable SQLite file per scenario for the first sample?” The proposal described a named file accessible to the separately running SUT and a read-only observer, with fixture-owned seed and cleanup.

Related: [ADR 0007](0007-use-public-outcomes-with-scoped-order-observation.md), [storage research](../research/execution-boundaries.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
