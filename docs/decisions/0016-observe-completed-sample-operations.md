# ADR 0016: Observe completed sample operations

Status: **ACCEPTED**. Decision date: 2026-10-28. Resolves Q19's initial sample completion/read-consistency policy; concrete execution settings and general temporal semantics remain open.

## Context

ADR 0006 agrees synchronous, atomic business intent, and ADR 0015 defines the HTTP result mapping. A response followed by a stale read could still produce misleading acceptance evidence. The sample needs a clear completion point and bounded observation claim.

## Decision

For the initial sample:

- Commit order creation and stock reduction atomically before returning HTTP `201`.
- Complete insufficient-stock rejection without order or stock changes before returning HTTP `409`, with no pending order work.
- After the response, observe committed stock and persisted orders in the scenario-owned store. The public stock view must reflect that committed state; use a fresh observer read transaction rather than a snapshot opened before the action.
- Do not use dirty reads or polling that hides incomplete work or a premature response.

The sample has one request per scenario, no concurrent actors and no pending background order work. Within that scope, sequential post-response observations can assess the completed operation. Identify the store/read point and collection provenance in the eventual evidence design.

A successful empty query establishes no persisted order at the declared read point. It does not prove unlimited future absence. A failed or unavailable query establishes no absence; preserve the gap and apply ADR 0009. A separately established trustworthy failure remains a failure even when another observation is unavailable.

## Alternatives considered

- Responding before completion and polling for eventual state would change the sample's synchronous interpretation and could conceal premature responses.
- Reusing a pre-action snapshot can miss completed writes.
- Treating an unavailable query as empty creates false absence evidence.

## Consequences and limits

This is a sample-specific observation policy, not a general temporal-provider API or an eventual-consistency model. Concrete SQL, transaction settings, timeout values, evidence encodings, fixture validation and the privileged-observation exception remain pending review. The policy does not authorise implementation or execution, and does not strengthen L1 into protected L2 assurance.

## Approval reference

On 2026-10-28, the user replied **“yes”** to “Should we adopt this completion and read-consistency policy for the sample?” The preceding recommendation required atomic commit before `201`, completed rejection without changes before `409`, fresh post-response committed reads, no polling to hide incomplete work, and read-point limits on absence claims.

Related: [ADR 0006](0006-bound-the-initial-orders-example.md), [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0015](0015-agree-initial-sample-http-mapping.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
