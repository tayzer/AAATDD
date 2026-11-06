# ADR 0034: Rename the tool to TADA

Status: **ACCEPTED**. Decision date: 2026-11-05.

## Context

The project and prospective tool previously used AAATDD (AI-Assisted Acceptance Test-Driven Development). During approval of the alpha CLI boundary, the user explicitly selected a new tool name and command.

## Decision

Name the tool **TADA — Test-Assured Development Assistance**. Use **`tada`** as the CLI command prefix.

Use TADA in active project descriptions and future design documents. Historical accepted ADRs, research snapshots and retained probe evidence keep their original names and content. References to AAATDD in those artifacts denote the same project's earlier name; the rename does not change their authority or scope.

## Alternatives considered

- Retaining AAATDD would conflict with the user's explicit naming choice.
- Rewriting historical approvals or probe evidence would obscure the original record without improving current naming.

## Consequences and limits

This renames the tool and its command, not the approved verification model. NuGet package identity, availability of names on distribution services and any future namespace or file-format identifiers remain separate decisions. Existing repository paths and remote identities are not changed by this decision; repository hosting changes can be handled separately without disrupting the current workspace.

This is a design/documentation change. It grants no implementation, package publication or remote-repository mutation authority.

## Approval reference

On 2026-11-05, the user said: **“Yes but lets use tada as the command, I want to rename the tool to TADA (Test-Assured Development Assistance)”**, in response to the five-operation alpha CLI proposal.

Related: [alpha boundary](0032-bound-the-first-publishable-alpha.md), [verifier packaging](0033-use-dotnet-local-tool-for-alpha-verifier.md), [CLI boundary](0035-adopt-five-operation-alpha-cli.md), [design status](../design-status.md).
