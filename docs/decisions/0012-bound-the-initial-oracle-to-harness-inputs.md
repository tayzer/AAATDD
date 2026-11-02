# ADR 0012: Bound the initial oracle to harness execution inputs

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q4's initial baseline boundary; concrete inventory and amendment rules remain open.

## Context

The baseline must cover the effective test oracle without making every application fix an oracle amendment. A harness directory alone is insufficient because shared code, inherited build configuration and resolved dependencies can affect execution from outside it.

## Decision

For the initial experiment, use one dedicated acceptance harness plus its identified external execution inputs as the approved-oracle boundary. Track the SUT separately under implementation identity.

The boundary includes:

- Gherkin, bindings, assertions, hooks, transformations, fixtures/schema/seed inputs, observers and technical helpers.
- Runner, build, launch and verification-policy configuration.
- Shared code/resources, inherited build inputs, resolved dependencies and relevant toolchain identities supplied through an identified trusted-input policy.
- Identities associating executed harness binaries, adapters and dependencies with the reviewed inputs.

A helper capable of changing the oracle remains inside this boundary wherever its source is located. A detected unmatched material input cannot inherit approval from a directory name. This establishes no claim of complete automatic dependency discovery.

The first acceptance harness drives the separately running SUT through its interface without referencing the SUT implementation assembly. Ordinary SUT changes can be assessed against the same approved oracle; changes affecting oracle inputs require reassessment.

Generated databases, logs, reports and build outputs are execution artifacts. Their creation does not itself amend the source-input baseline, but executable producers and their provenance still require checking. The verifier and approval-policy identity remain trusted control inputs rather than replacements accepted from candidate code.

## Alternatives considered

- A whole-repository oracle baseline couples routine SUT fixes to oracle revision.
- Fine-grained per-binding dependency capture can reduce unnecessary reapproval, but adds dependency-analysis complexity before the first contract is exercised.

The dedicated-harness boundary with explicitly identified external inputs is the initial compromise.

## Consequences and limits

Concrete paths, dependency/toolchain versions, import/restore settings, hashing rules, built-artifact validation, non-semantic-edit policy and the privileged-observation exception remain open. No actual baseline, project layout, dependency installation or implementation is approved here. L1 checks remain advisory; this boundary alone does not establish L2 isolation.

## Approval reference

On 2026-10-27, the user replied **“yes”** to: “Should the initial approved baseline cover this dedicated harness and its external inputs, with the SUT tracked separately?” The preceding proposal included inherited build inputs, resolved dependencies and executed-binary identities.

Related: [ADR 0011](0011-separate-logical-baseline-and-run-identities.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md), [execution-input research](../research/execution-boundaries.md).
