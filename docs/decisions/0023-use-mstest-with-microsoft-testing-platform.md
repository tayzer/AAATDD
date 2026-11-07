# ADR 0023: Use MSTest with Microsoft.Testing.Platform

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q14's first-harness test framework and execution-platform choice; exact versions, project configuration and scenario scheduling remain open.

## Context

ADR 0022 selects C#/.NET and Reqnroll for the first harness. NUnit with serial scenario execution was proposed but never accepted. The user explicitly selected MSTest and Microsoft.Testing.Platform (MTP).

## Decision

Use MSTest as the first Reqnroll harness's test execution framework and Microsoft.Testing.Platform as its execution platform. Configure and validate an MTP execution path; do not silently substitute VSTest or retain the unaccepted NUnit recommendation.

Retain the existing native Cucumber Messages and AAATDD envelope composition. Platform-level reporting or process success does not replace approved-inventory reconciliation, original scenario evidence or verification-policy checks.

This choice does not accept the preceding serial-execution proposal. Scenario parallelism remains undecided; independent per-scenario setup and the no-automatic-retry policy still apply.

## Alternatives considered

- NUnit was the initial recommendation when no framework preference was known; it was not selected.
- MSTest on VSTest would not satisfy the user's explicit MTP choice.
- Other supported Reqnroll test frameworks remain outside the first-harness selection.

## Consequences and limits

Exact .NET, Reqnroll, MSTest and MTP versions, SDK/package arrangement, runner selection, command-line arguments and configuration require compatibility review. The [integration research](../research/2026-11-03-mstest-mtp.md) distinguishes documented capabilities from untested integration. Discovery, hooks, native-message completeness and teardown under the actual MTP configuration must be validated before runnable-baseline acceptance. This decision grants no installation, restore, scaffolding, implementation or execution authority, and does not select the AAATDD framework/verifier language.

## Approval reference

On 2026-11-03, the user stated **“I want mstest and MTP”** in response to the proposed NUnit execution profile. This explicitly selects the test framework and platform, not scenario scheduling or exact versions. No earlier accepted ADR is superseded; ADR 0022 deliberately left these choices open.

Related: [ADR 0010](0010-preserve-failures-across-explicit-reruns.md), [ADR 0021](0021-retain-native-messages-with-aaatdd-envelope.md), [ADR 0022](0022-select-dotnet-reqnroll-for-first-experiment.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
