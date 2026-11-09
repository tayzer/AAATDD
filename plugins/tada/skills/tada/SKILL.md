---
name: tada
description: Apply the TADA workflow by default to software project requests, features, bug fixes, refactors and chores while this plugin is enabled. Discover agreed executable acceptance, verify changes and recover project context without requiring the user to name TADA.
---

# TADA development assistance

The host's plugin controls govern activation. Apply this skill while TADA is enabled. Disabling TADA ends automatic workflow orchestration in fresh sessions. Project files retain agreed behaviour and evidence for future work. Respect explicit user decisions and applicable project instructions.

Read relevant `.tada/context.md`, authoritative features and current verification evidence when continuing an existing project. Recover the goal and next action yourself from an ordinary user request. Investigate stale or missing evidence before relying on an earlier result.

## Choose proportionate work

For a new project or feature, discover intended behaviour with the user. Agree observable success and failure examples and express them in Gherkin before implementing business behaviour. Resolve consequential ambiguities with the user. Routine engineering choices are delegated within the request.

For a bug fix, reuse adequate agreed scenarios or agree the missing expectation. Demonstrate the intended failing assertion, repair the implementation and verify existing obligations.

For a chore or behaviour-preserving refactor, use proportionate existing regression, build, format or fitness checks. New business scenarios belong to changes in agreed behaviour.

## Prepare the verifier when needed

Installing or enabling the plugin needs no application folder or verifier setup. For an installation-only request, confirm plugin availability and stop. Do not ask the user to choose a repository or offer a "plugin only" setup choice.

The plugin supplies agent behaviour. The repository-local `tada` command supplies baseline and evidence verification. Initial support is Windows x64, .NET SDK 10.0.403, Reqnroll 3.3.4, MSTest 4.5.2 and MTP 2.5.2.

For ordinary project work, discover the intended behaviour first. Infer the application repository from the current workspace and project context. Prepare its verifier when business verification requires it, using that repository. Ask for a folder only if the target is genuinely ambiguous at that point. Chores and behaviour-preserving refactors use existing checks without installing a verifier solely for ceremony.

For business verification, inspect the existing local tool manifest and `.tada/context.md`. Reuse the recorded SDK host after checking its availability and version. When setup is needed, run [scripts/Initialize-Tada.ps1](scripts/Initialize-Tada.ps1) with the repository path and selected .NET host. The distributed plugin includes its exact local package feed. The helper prepares files and migrates known alpha.8 instruction blocks. It reports edited legacy blocks for review.

Keep workflow activation in the plugin. Repository context should index decisions, features, baseline/run IDs, useful findings and the next action. Persistent global or repository instructions that force TADA would bypass the user's plugin toggle.

## Specify, verify and proceed

Read `.tada/WORKFLOW.md` for runner setup, supported input contracts, execution and retained-evidence details when preparing business verification. Configure a separate application and scenario-scoped acceptance harness, use meaningful public observations and release resources after failures.

Prepare and review the immutable baseline with `tada baseline prepare` and `tada baseline review`. Present the captured expectations, assertions, fixture setup and relevant trust declarations to the human. The human declares approval. An agent may prepare the approval command but must never grant business approval on the human's behalf.

Use the selected SDK host for tool/build commands and pass it through `--dotnet` to `tada run` when required. Observe meaningful behavioural RED for missing behaviour and GREEN after small implementation changes. Already-satisfied behaviour can pass directly. Broken setup or missing evidence is INDETERMINATE. Preserve approved oracle bytes and failure history throughout implementation.

Run relevant quality checks and inspect retained evidence with `tada verify`. Explain PASS, FAIL and INDETERMINATE within their actual scope. Results are local advisory L1, with declared-trust and observation limits described by the project workflow. Update concise Git-native context before handing work over.
