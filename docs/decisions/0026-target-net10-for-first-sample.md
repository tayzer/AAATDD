# ADR 0026: Target .NET 10 LTS for the first sample

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q14's sample target framework; exact SDK/runtime patches, packages and integration validation remain open.

## Context

The first experiment has an agreed C#/.NET stack, a Reqnroll/MSTest/MTP harness using MSTest SDK, and a separate ASP.NET Core API. Both projects need a supported target before a coherent SDK/package graph can be proposed.

## Decision

Target the first acceptance harness and sample API at .NET 10 LTS using `net10.0` and a stable .NET 10 SDK. Review exact SDK/runtime patches and package versions separately as one compatibility set.

This is a common target for the two sample projects, not an AAATDD-wide framework/verifier target or a multi-targeting requirement. It does not change the separate SUT/harness boundary or MSTest/MTP choices.

## Alternatives considered

- .NET 8 or 9 would begin the experiment near their documented end of support on 2026-11-10.
- A preview target adds unnecessary release-state uncertainty to the first experiment.
- Multi-targeting would expand the compatibility surface before the first contract is exercised.

## Consequences and limits

Microsoft's support policy lists .NET 10 as active LTS through 2028-11-14, and Reqnroll documentation lists .NET 10 compatibility; see the [research follow-up](../research/2026-11-03-mstest-mtp.md#target-framework-follow-up). These support findings do not prove the eventual pinned integration. Exact SDK/runtime patches, native dependencies, package graph, platform settings and execution evidence remain pending review. No installation, project creation, restore, implementation, execution or runnable-baseline approval is authorised.

## Approval reference

On 2026-11-03, the user replied **“Yes”** to “Should both sample projects target .NET 10 LTS?” The preceding recommendation specified `net10.0` for the harness and API, with exact SDK patches, packages and integration validation separate.

Related: [ADR 0022](0022-select-dotnet-reqnroll-for-first-experiment.md), [ADR 0025](0025-use-mstest-sdk-for-acceptance-harness.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
