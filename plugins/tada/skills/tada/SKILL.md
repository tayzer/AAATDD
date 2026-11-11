---
name: tada
description: Apply the TADA workflow by default to software project requests, features, bug fixes, refactors and chores while this plugin is enabled. Discover agreed executable acceptance, verify changes and recover project context without requiring the user to name TADA.
---

# TADA development assistance

The host's plugin controls govern activation. Apply this skill while TADA is enabled. Disabling TADA ends automatic workflow orchestration in fresh sessions. Project files retain agreed behaviour and evidence for future work. Respect explicit user decisions and applicable project instructions.

TADA's principle: you may propose and implement changes, but you must not independently redefine the conditions by which those changes are judged successful. Humans agree what success means; you implement it efficiently; protected obligations and trustworthy evidence keep the work accountable.

Follow **Understand → Agree → Select verification → Establish evidence → Implement and verify → Complete and preserve** in proportion to the change. These are responsibilities, not ceremonies. Combine stages for small changes.

## Understand

Recover context yourself from an ordinary user request. Read `.tada/context.md`, relevant features, `.tada/enforcement.json` and current evidence. Run `tada gate` without `--run` to list active obligations, pending candidates and their ages. Investigate stale or missing evidence before relying on an earlier result.

Identify the requested outcome and current behaviour, the existing agreed scenarios and obligations it touches, affected components and interfaces, architectural constraints, and existing tests. Ask targeted questions only when the answer changes business behaviour, compatibility, security, data integrity, a fitness threshold or the interpretation of an acceptance check. Routine engineering choices are delegated within the request. Record unresolved questions; never silently adopt an assumption as agreed.

## Agree

For new or changed business behaviour, agree observable success and failure examples, edge cases and negative outcomes with the user, and express them in Gherkin before implementing. Gherkin is the authoritative specification. Supporting Markdown records rationale only; do not write a parallel prose specification.

Discover non-functional requirements during the same conversation: performance, resilience, security, compatibility, accessibility, architecture rules and observability. Where an objective check is meaningful, agree its threshold as a fitness obligation. Where it is not, record the required human review rather than claiming automated assurance. For service interfaces, agree contract expectations when the project uses contract verification.

You may propose expectations and amendments. Only an authorised human approves them.

## Select verification

Classify the actual effects as `feat`, `fix`, `refactor`, `chore` or a mixture; branch names are hints. Before implementing, record the objective, affected services, interfaces, storage and journeys, adequate existing coverage, gaps, selected checks and the reasons for omitted levels. Select unit, service, producer/consumer contract and end-to-end journey checks by impact and risk. Each level answers its own questions. Do not require every level for every change, and never waive an obligation through a label.

- **Feature:** new Gherkin plus relevant contract and fitness obligations.
- **Bug fix:** reuse an agreed scenario that exposes the defect, or agree a missing regression scenario.
- **Behaviour-preserving refactor:** reuse active obligations and supporting tests; add characterisation only where coverage is insufficient. No new business scenario.
- **Chore:** proportionate build, dependency, configuration or documentation checks. Say so explicitly when no behavioural check applies; never report that as a behavioural PASS.
- **Mixed or architectural:** determine the real effects; preserve existing obligations and relevant fitness checks.

Supporting unit and implementation tests are yours to select and maintain; keep them outside the captured oracle. Contract and fitness files that express agreed obligations belong inside the oracle and are declared under `obligations` in `tada.json`. A label never downgrades verification.

When coordinated execution is useful, capture `.tada/verification-plan.json` with `tada plan prepare --file .tada/verification-plan.json`, then use `tada run --plan <digest>` and `tada verify --run <id>`. Acceptance checks reference human-approved baselines. Supporting MSTest/MTP checks capture their engineering configuration and unique native discovery names, with TRX reporting enabled. Contract checks exercise both the real producer and consumer against captured contract expectations. Plan preparation never approves acceptance. Revisions identify `previous` and explain `changeReason`. An empty justified selection reports NO_CHECKS and makes no passing-test claim. Read the project workflow for the plan contract.

## Establish evidence

Read `.tada/WORKFLOW.md` for runner setup, supported input contracts, execution and retained-evidence details. Configure a separate application and scenario-scoped acceptance harness, use meaningful public observations and release resources after failures.

Make the harness runnable through Visual Studio Test Explorer and `dotnet test --project tests/Acceptance/Acceptance.csproj`. Use the generated `TadaAcceptance.Create()` context for its working directory, API assembly and .NET host. Add a build-only API `ProjectReference` with `ReferenceOutputAssembly="false"`, `Private="false"` and `OutputItemType="TadaSutOutput"`, and import `Tada.Acceptance.targets`. Match that reference to `execution.sutProject` in `tada.json`. Native builds stage the complete separate API under the test output's `sut` directory; keep the same business bindings for both execution modes. Check native Debug and Release execution as well as recorded TADA execution. Native results are ordinary runner results; TADA assessment additionally requires an approved baseline and retained-evidence verification.

For several owned local .NET services, use schema-2 `execution.services` with a unique ID, project and assembly for each service. Match each native build-only reference with `TadaServiceId`. Fixtures use `TadaAcceptance.Create().Services[id]`, own service readiness and isolated state, and checkpoint every service before termination. Agree business deadlines explicitly and distinguish failed outcomes from unavailable observations.

Prepare and review the immutable baseline with `tada baseline prepare` and `tada baseline review` (with `--against <previous>` for amendments). Present the captured expectations, assertions, fixture setup, classified protected changes and trust declarations to the human. The human declares approval. You may prepare the approval command but must never grant approval on the human's behalf.

Record enforcement status in `.tada/enforcement.json`: existing implemented scenarios stay `active`; newly approved, not-yet-implemented scenarios become `candidates` with today's `since` date and a reason. A spec-only change with candidates is legitimate. Never demote an active scenario to escape a failure.

Demonstrate meaningful behavioural RED for new behaviour and defects: the intended outcome assertion fails. Missing bindings, broken setup or unavailable dependencies are INDETERMINATE, not RED. Already-satisfied behaviour needs no invented RED. For refactors, establish passing preservation evidence first.

## Implement and verify

Implement in small, reviewable increments. Use the selected SDK host and pass it through `--dotnet` to `tada run` when required. Never modify, bypass or weaken protected expectations, bindings, fixtures, observers, configuration or enforcement status to reach GREEN. If implementation exposes a genuine requirement conflict or missing business decision, stop that scope and return it to the human.

Run `tada gate --run <id>` (with `--base <target-branch>` when one exists). When a candidate passes, move it to `active` in the same change. Run relevant supporting and quality checks too.

## Complete and preserve

Report which obligations were satisfied, which checks ran and their results, which protected expectations changed and whether they were approved, which checks were omitted and why, pending candidates, verification gaps, residual risks and the human code-review that remains. Explain PASS, FAIL, INDETERMINATE and NO_CHECKS within their actual scope. Results are local advisory L1: approval records are declarations, and team CI needs independent review of the protected changes the gate lists.

Update concise Git-native context in `.tada/context.md`: current goal, feature links, baseline and run IDs, pending candidates, open questions and the next action. Update or supersede stale entries.

For refactors, establish passing preservation checks before restructuring and add characterisation tests when coverage is inadequate. For chores, no new tests or no test execution can be justified by the actual effects. Record the evaluation and rationale. Use existing tools directly when coordinated execution adds no value. Human approval governs business intent and executable acceptance. Routine supporting engineering-test selection and implementation are delegated.

## Prepare the verifier when needed

Installing or enabling the plugin needs no application folder or verifier setup. For an installation-only request, confirm plugin availability and stop. Do not ask the user to choose a repository or offer a "plugin only" setup choice.

The plugin supplies agent behaviour in Codex and Claude Code. The repository-local `tada` command supplies baseline, enforcement and evidence verification. Initial support is Windows x64, stable .NET 10.0 SDKs, Reqnroll 3.3.4, MSTest 4.5.2 and MTP 2.5.2. New projects use SDK 10.0.100 with `rollForward: latestFeature` and prerelease SDKs disabled. Preserve existing application SDK policies. Each run records the resolved SDK and observed toolchain identities.

Infer the application repository from the current workspace and project context. Prepare its verifier when business verification requires it. Ask for a folder only if the target is genuinely ambiguous at that point. Chores and behaviour-preserving refactors use existing checks without installing a verifier solely for ceremony.

Inspect the existing local tool manifest and `.tada/context.md`. Reuse the recorded SDK host after checking its availability and version. When setup is needed, run [scripts/Initialize-Tada.ps1](scripts/Initialize-Tada.ps1) with the repository path and selected .NET host. The distributed plugin includes its exact local package feed. The helper prepares files and migrates known alpha.8 instruction blocks. It reports edited legacy blocks for review.

On Windows, invoke the setup helper through `pwsh -NoProfile -File` with absolute quoted paths to the helper, `-Repository` and `-Dotnet`. Resolve the helper relative to this skill's installed directory so Claude Code and Codex use the packaged feed.

Keep workflow activation in the plugin. Repository context should index decisions, features, baseline/run IDs, useful findings and the next action. Persistent global or repository instructions that force TADA would bypass the user's plugin toggle.
