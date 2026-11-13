# ADR 0048: Publish every validated main build

Status: **ACCEPTED — DIRECT USER INSTRUCTION**. Decision date: 2026-11-13. The workflow design, version scheme and marketplace repository layout are implementation decisions under [ADR 0041](0041-authorise-autonomous-release-implementation.md).

## Context

ADR 0041 kept release and package publication as a gate separate from merge. Alpha.12 was published that way: a human approved a specific candidate, then published it.

On 2026-11-13 the user asked for TADA to ship for Codex, Claude Code and Cursor. They chose a separate marketplace repository named `tada`, with NuGet.org later. They authorised creating `tayzer/tada-plugins` and its deploy key. When asked whether the marketplace push should wait for approval, they said that every CI run from TADA should push a new package out.

## Decision

Every push to `main` that passes the full **PR validation** workflow is published without a further approval step:

- The release workflow builds the validated commit with version `<base>.<validation run number>`. It publishes a GitHub prerelease with checked assets, a release manifest and `SHA256SUMS`. It then pushes the exact marketplace ZIP content to `tayzer/tada-plugins`.
- Pull requests, merge-queue runs and failed validation never publish.
- Merging to `main` is therefore the release decision. Branch protection and required PR checks on `main` are the controls in front of publication.
- The deploy key can write only to the marketplace repository. Its secret is available only to protected-branch runs through the `marketplace` environment.

This supersedes, for package and marketplace publication, the separate publication gate in ADR 0041. It does not change any other authority. Human agreement on business acceptance, human baseline approval and the prohibition on approving for a human all remain unchanged. Public directory submissions (Anthropic, OpenAI and Cursor) remain manual actions by an account holder.

## Consequences

- Users of the Git marketplace receive each merged change through `codex plugin marketplace upgrade` or `claude plugin marketplace update`. The Anthropic directory scans each build when it tracks `main`.
- The plugin's setup helper upgrades a project within the `0.1.0-alpha` line, and refuses downgrades and other lines. A CLI upgrade changes captured tool-manifest bytes, so the next baseline needs renewed human approval. More frequent builds make that review more frequent; it is the intended integrity behaviour, not a regression.
- A defective build reaches users until a fix merges. Recovery is forward-only: merge a fix, or disable the Release workflow to pause publishing.
- Builds are prereleases with local advisory L1 claims. Publication does not imply requester acceptance of a new experience; those trials remain separate records.

Related: [releasing](../releasing.md), [release workflow](../../.github/workflows/release.yml).
