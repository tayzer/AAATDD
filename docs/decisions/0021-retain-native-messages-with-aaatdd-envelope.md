# ADR 0021: Retain native messages with an AAATDD envelope

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q12's initial evidence composition; concrete schemas, compatible versions and producer-validation mechanisms remain open.

## Context

Native Cucumber Messages preserve useful execution relationships, but native runner success does not establish approval authority or complete required coverage. AAATDD needs to link execution evidence to the assessed identities, approved obligations and observation provenance without discarding runner detail.

## Decision

For the first experiment, retain:

| Evidence component | Role |
| --- | --- |
| Native Cucumber Messages | Primary runner evidence, retaining original statuses, attempts and source/case links |
| Small AAATDD envelope | Link approval, baseline/policy, implementation/environment, expected cases, fixture/observation provenance and gate reasons to the retained evidence |
| Optional CTRF projection | Derived generic reporting, without replacing native evidence or AAATDD approval/coverage checks |

Native runner success alone cannot establish approved coverage. Unsupported or incomplete evidence blocks `PASS`; apply ADR 0009 when trustworthy failures coexist with evidence gaps. Preserve original execution detail rather than reducing verification to a passing scenario count.

The envelope references authoritative approval and baseline records. Writing an envelope does not itself grant approval. A native message format does not authenticate its producer or protect L1 execution.

## Alternatives considered

- Native messages alone omit AAATDD approval, expected-inventory and provenance checks.
- CTRF alone is a reporting projection and cannot replace retained runner relationships and verification authority checks.
- A replacement universal runner-report format would duplicate native information before the first adapter contract is exercised.

## Consequences and limits

Exact envelope/message schemas, compatible versions, artifact-link/digest encoding, completeness validation, provenance storage and CTRF projection rules remain unselected. This composition does not establish producer authenticity, L2 protection or a runnable verifier. No dependency installation, implementation, execution or executable-baseline approval is authorised.

## Approval reference

On 2026-11-03, the user replied **“yes”** to “Should we adopt this evidence composition for the first experiment?” The preceding proposal retained native Cucumber Messages, added a small AAATDD approval/provenance envelope, kept CTRF optional and derived, distinguished native success from approved coverage, and blocked `PASS` for unsupported/incomplete evidence while leaving exact schemas and versions open.

Related: [ADR 0009](0009-distinguish-requirement-failure-from-invalid-verification.md), [ADR 0011](0011-separate-logical-baseline-and-run-identities.md), [runner-evidence research](../research/execution-boundaries.md#existing-capabilities), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
