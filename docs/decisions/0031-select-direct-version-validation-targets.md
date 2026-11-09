# ADR 0031: Select direct version validation targets

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves Q14's first direct-version targets; full dependency resolution and executable-baseline approval remain pending.

## Context

The first sample stack and reproducible-input controls are agreed. Primary release/package listings identify concrete versions for compatibility validation, but those listings do not establish a complete or working dependency graph.

## Decision

Use these direct targets for the first compatibility review:

| Input | Target |
| --- | --- |
| Stable .NET SDK | `10.0.403` |
| Harness project SDK | `MSTest.Sdk` `4.5.2` |
| Reqnroll integration/core/generation | Aligned `3.3.4`, including `Reqnroll.MSTest` |
| SQLite provider | `Microsoft.Data.Sqlite` `10.0.14` |

Validate the complete combination under the selected MSTest/MTP path. Keep transitive dependency constraints distinct from actual resolved versions. Inspect shipped project-SDK imports and actual runtime/native assets rather than inferring them from direct version numbers or conventional NuGet dependency listings.

## Alternatives considered

- Unversioned targets would not exercise the agreed exact-input policy.
- Declaring the graph compatible from package availability alone would overstate the research evidence.
- Substituting another framework/platform or silently changing these targets after incompatibility would bypass the agreed choices.

## Consequences and limits

This selects validation targets, not a resolved lock graph or approved executable baseline. Exact transitive/native versions, Cucumber Messages schema compatibility, shipped SDK inputs, runtime identity, actual commands, observer exception and execution evidence remain pending. Unexpected incompatibility requires an explicit amendment. No dependency download/installation, restore, project scaffolding, implementation or execution is authorised by this decision.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Should we adopt these direct version targets, with full dependency-graph and executable-baseline approval still pending?” The preceding table specified SDK `10.0.403`, MSTest SDK `4.5.2`, aligned Reqnroll `3.3.4` and Microsoft.Data.Sqlite `10.0.14`, explicitly distinguishing availability from tested compatibility.

Related: [ADR 0023](0023-use-mstest-with-microsoft-testing-platform.md), [ADR 0030](0030-pin-sdks-and-use-locked-restore.md), [version review](../research/2026-11-05-version-candidates.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
