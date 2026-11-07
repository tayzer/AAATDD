# ADR 0033: Use C# and a repository-local .NET tool for the alpha verifier

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves Q14's alpha verifier language, target and packaging approach; package identity and detailed distribution policy remain open.

## Context

ADR 0032 bounds the first publishable alpha to a local L1/advisory workflow with Reqnroll, MSTest and MTP on Windows x64/.NET 10. The verifier language and packaging need a separate decision; choosing the sample stack did not settle them.

## Decision

Implement the alpha verifier in C# targeting .NET 10 (`net10.0`). Distribute its CLI as a NuGet-packaged .NET tool, used through a repository-local tool manifest that pins the selected verifier version. Developers and CI restore the version selected by that repository.

Keep verification logic separate from the CLI and runner integration. This separation supports later reuse without committing to a public extension API now.

Support Windows x64 initially, consistent with ADR 0032. Other platforms require separate validation.

## Alternatives considered

- Python or TypeScript would introduce another runtime and package ecosystem alongside the selected .NET harness and sample API.
- A standalone executable could simplify installation later, but the initial supported runner workflow already requires .NET. Defer a separate executable distribution.
- A global tool installation would not by itself express the verifier version selected by each repository. Prefer a repository-local manifest.

## Consequences and limits

The harness, example API and verifier share one development toolchain. A repository-local manifest records the selected tool version for repeatable developer and CI setup; version pinning alone does not establish trusted execution or protect against a locally controlling actor.

Package identity, CLI commands, dependencies and project licence remain separate decisions. The package feed, restore/source controls, exact verifier toolchain/runtime pins and clean-machine installation procedure still need design and review. This decision does not select additional dependencies or approve the probe's complete dependency graph.

The alpha remains L1/advisory. This is a design decision, not authority to scaffold projects, install packages, run another probe, implement the verifier, or publish a package. Existing accepted ADRs and the Orders feature remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Shall we adopt C#/.NET 10 with repository-local .NET tool packaging for the alpha?” The preceding recommendation specified NuGet distribution, a repository-local manifest, separation of verification logic from the CLI and runner integration, and initial Windows x64 support. It reserved package identity, commands, dependencies and licence for separate decisions.

Related: [alpha boundary](0032-bound-the-first-publishable-alpha.md), [pinning controls](0030-pin-sdks-and-use-locked-restore.md), [design status](../design-status.md), [open questions](../open-questions.md).
