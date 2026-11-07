# ADR 0024: Run initial scenarios serially

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q14's initial scenario-scheduling policy; concrete runner configuration remains open.

## Context

The user selected MSTest with Microsoft.Testing.Platform in ADR 0023. Scheduling remained a separate proposal. The first experiment exercises fixture, observation, evidence and teardown behaviour before adding concurrent scenario execution.

## Decision

Explicitly disable scenario parallelism for the initial Reqnroll/MSTest/MTP experiment. Keep each scenario independently set up with its own disposable store and owned resources. Scenarios must not depend on execution order, share mutable scenario state or rely on a previous scenario's results.

Preserve ADR 0010's no-automatic-retry policy. Serial scheduling does not weaken discovery/completeness checks, replace per-scenario ownership or establish independent assurance.

## Alternatives considered

- Parallel execution could exercise isolation earlier but adds scheduling and evidence-correlation complexity to the first complete lifecycle experiment.
- Serial execution with order-dependent shared setup would hide lifecycle defects and is not the selected model.

## Consequences and limits

This bounds the initial experiment, not the future framework's concurrency capabilities. Revisit parallel execution when its fixture, SUT and evidence behaviour can be validated. Exact MSTest/MTP scheduling configuration, versions and launch commands remain pending executable-baseline review. No installation, implementation or execution is authorised.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should we adopt that scheduling policy?” The preceding recommendation explicitly disabled scenario parallelism for the first MSTest/MTP experiment while retaining independent scenario setup. Framework/platform selection was already accepted separately in ADR 0023.

Related: [ADR 0008](0008-use-disposable-sqlite-per-scenario.md), [ADR 0010](0010-preserve-failures-across-explicit-reruns.md), [ADR 0023](0023-use-mstest-with-microsoft-testing-platform.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
