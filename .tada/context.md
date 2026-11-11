# TADA development context

Current-work handoff. Finished work lives in Git history, merged pull requests, ADRs and validation records. Confirm the branch and next action against Git before relying on this file.

## Current change

Evidence-driven discovery and continuous learning, on branch `feat/evidence-discovery-learning` from `main` at `cf267fa`.

- Decisions: [ADR 0046](../docs/decisions/0046-investigate-before-agreeing-acceptance.md) (risk-proportionate discovery and the change impact brief) and [ADR 0047](../docs/decisions/0047-add-reference-and-augment-knowledge-modes.md) (reference and augment knowledge modes, records, `tada knowledge check`).
- Guide: [discovery and continuous learning](../docs/knowledge.md). Gap analysis: [North Star assessment](../docs/architecture/north-star-assessment.md).
- Agent guidance: [plugin skill](../plugins/tada/skills/tada/SKILL.md) and its [reference](../plugins/tada/skills/tada/references/discovery-and-knowledge.md); generated workflow in `src/Tada.Core/Onboarding.cs`.
- Evaluation: [knowledge workflow evaluation](../docs/validation/knowledge-workflow-evaluation.md), with fixtures from `tests/KnowledgeEvaluation/New-KnowledgeFixture.ps1`.

This repository uses reference mode (no `.tada/knowledge.json`).

## Verification state

Focused and replay tests pass locally, including the knowledge, onboarding and guidance tests. The plugin packaging and setup control passes with the installed verifier reporting the default reference mode. The Orders, multi-service, enforcement and onboarding scripts were not rerun locally for this change; CI runs them.

## Open questions for the requester

- Confirm the knowledge-mode design: settings in `.tada/knowledge.json`, no `writePolicy` setting, and `BLOCKED` for new records in reference mode.
- `AGENTS.md` still names the merged draft PR #1 branch `codex/alpha-release-validation` as the delivery branch. That sentence records the requester's authorisation, so it is left for the requester to update.

## Next action

Open a draft pull request for review. Merge and release remain separate decisions.

## Local environment

SDK 10.0.403 is installed system-wide on this machine. PowerShell 7 was not on `PATH`; a local copy was installed with `dotnet tool install PowerShell --tool-path .artifacts/pwsh` for script controls, and the native fixture DLLs were copied into `.artifacts/native/` from an earlier build of identical sources. Recheck both before reuse. These are environment notes, not project authority.
