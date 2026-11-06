# ADR 0025: Use MSTest SDK for the acceptance harness

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q14's initial harness SDK arrangement; exact versions and integration validation remain open.

## Context

ADRs 0022–0024 select C#/.NET, Reqnroll, MSTest/MTP and serial scenario execution. The acceptance harness still needs a project arrangement that aligns its MSTest and MTP dependencies while integrating Reqnroll generation and native evidence production.

## Decision

Use `MSTest.Sdk` for the first acceptance harness with the Reqnroll MSTest integration, `Reqnroll.MSTest`. Keep the separately running sample SUT on its ASP.NET Core project SDK.

Review the SDK/package graph, Reqnroll generated-test imports, discovery, MTP invocation and native Cucumber Messages production as one coherent pinned configuration. Choosing the SDK arrangement does not establish that the eventual combination works.

## Alternative considered

Manual `Microsoft.NET.Sdk` configuration with explicit MSTest/MTP properties and package references remains a possible amendment if evidence shows the selected arrangement cannot meet the approved baseline. It is not the selected starting arrangement. Preserve the user's MTP platform choice; do not silently substitute VSTest.

## Consequences and limits

SDK-managed dependency alignment reduces manual configuration, but exact SDK, framework, Reqnroll and platform versions remain unselected. Target framework, project files, generated-test integration, lock graph and launch commands require review. This decision approves no concrete executable baseline, package installation, restore, scaffolding, implementation or execution, and does not select AAATDD's framework/verifier language.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should we adopt this SDK arrangement for the first harness?” The preceding recommendation selected `MSTest.Sdk` with `Reqnroll.MSTest`, subject to exact-version, generated-test, discovery and native-message compatibility validation.

Related: [ADR 0022](0022-select-dotnet-reqnroll-for-first-experiment.md), [ADR 0023](0023-use-mstest-with-microsoft-testing-platform.md), [ADR 0024](0024-run-initial-scenarios-serially.md), [integration research](../research/2026-11-03-mstest-mtp.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
