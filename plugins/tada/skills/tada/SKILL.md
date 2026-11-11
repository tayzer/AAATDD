---
name: tada
description: Apply the TADA workflow by default to software project requests, features, bug fixes, refactors and chores while this plugin is enabled. Investigate the existing system in proportion to risk, discover agreed executable acceptance, verify changes, and recover and improve project knowledge without requiring the user to name TADA.
---

# TADA development assistance

The host's plugin controls govern activation. Apply this skill while TADA is enabled. Disabling TADA ends automatic workflow orchestration in fresh sessions. Project files retain agreed behaviour and evidence for future work. Respect explicit user decisions and applicable project instructions.

TADA's principle: you may propose and implement changes, but you must not independently redefine the conditions by which those changes are judged successful. Humans agree what success means; you implement it efficiently; protected obligations and trustworthy evidence keep the work accountable.

Follow **Understand → Agree → Select verification → Establish evidence → Implement and verify → Complete and preserve** in proportion to the change. These are responsibilities, not ceremonies. Combine stages for small changes.

## Understand

Recover context yourself from an ordinary user request. Read `.tada/context.md` as a handoff, not as current truth: confirm its objective, branch and next action against Git before relying on it. Read relevant features, `.tada/enforcement.json` and current evidence. Run `tada gate` without `--run` to list active obligations, pending candidates and their ages. Investigate stale or missing evidence before relying on an earlier result.

Separate the requested outcome from any implementation approach the user suggested. Then investigate the existing system in proportion to impact and risk before proposing acceptance. Depth is a guide, not a gate; adjust it as evidence appears:

- **Low** (documentation, bounded chores, contained refactors): relevant files, conventions, direct dependencies and existing checks.
- **Medium** (business fixes, bounded features): behavioural paths, related scenarios and contracts, persistent state and the regression history of touched files.
- **High** (cross-service, consequential data, security-sensitive or distributed work): service and data ownership, synchronous and asynchronous consumers, retries and idempotency, state transitions and consistency, failure recovery, authorisation and NFRs.

Retrieve existing knowledge by relevance rather than reading everything: search for the touched components in features, contract and fitness roots, ADRs and project documentation, `.tada/knowledge.md`, `.tada/knowledge/` and the Git history of touched paths. Recorded or remembered facts are leads, not truth. Check each one you rely on against current code, configuration and accepted decisions; `tada knowledge check` flags records whose sources changed. When sources conflict, report the conflict instead of choosing the convenient one.

Stop expanding when you can responsibly propose the affected acceptance expectations. Do not investigate unrelated subsystems to look thorough. Report unreachable sources, such as other repositories, issue trackers or production telemetry, as limitations rather than guessing.

Before agreeing new acceptance, share a **change impact brief** scaled to the change: a sentence or two for small work, a short structure for consequential work. Separate **Verified** findings (with a code, test, decision or commit reference), **Inferred** conclusions and **Unknowns**, and cover relevant history, functional and non-functional requirements and verification implications. The brief is conversation, not a specification or a required file. Observed code shows what the system does, not what it should do. [The reference](references/discovery-and-knowledge.md) has the depth guide, authority order and brief template.

Ask targeted questions only when the answer changes business behaviour, compatibility, security, data integrity, a fitness threshold or the interpretation of an acceptance check. Routine engineering choices are delegated within the request. Never convert an inference into an agreed requirement: ask, record the open question, or explicitly defer the affected scope.

## Agree

For new or changed business behaviour, agree observable success and failure examples, edge cases and negative outcomes with the user, and express them in Gherkin before implementing. Gherkin is the authoritative specification. Supporting Markdown records rationale only; do not write a parallel prose specification. Use verified findings to ground the examples, and keep current behaviour distinct from desired behaviour. Knowledge records, the handoff and your inferences never define acceptance.

Discover non-functional requirements during the same conversation: performance, resilience, security, compatibility, accessibility, architecture rules and observability. Where an objective check is meaningful, agree its threshold as a fitness obligation. Where it is not, record the required human review rather than claiming automated assurance. For service interfaces, agree contract expectations when the project uses contract verification.

You may propose expectations and amendments. Only an authorised human approves them.

## Select verification

Classify the actual effects as `feat`, `fix`, `refactor`, `chore` or a mixture; branch names are hints. Before implementing, record the objective, affected services, interfaces, storage and journeys, adequate existing coverage, gaps, selected checks and the reasons for omitted levels. Reuse the impact brief rather than rewriting it. Select unit, service, producer/consumer contract and end-to-end journey checks by impact and risk, including the dependencies and historical failures discovery found. Each level answers its own questions. Do not require every level for every change, and never waive an obligation through a label.

- **Feature:** new Gherkin plus relevant contract and fitness obligations.
- **Bug fix:** reuse an agreed scenario that exposes the defect, or agree a missing regression scenario.
- **Behaviour-preserving refactor:** reuse active obligations and supporting tests; add characterisation only where coverage is insufficient. No new business scenario.
- **Chore:** proportionate build, dependency, configuration or documentation checks. Say so explicitly when no behavioural check applies; never report that as a behavioural PASS.
- **Mixed or architectural:** determine the real effects; preserve existing obligations and relevant fitness checks.

Supporting unit and implementation tests are yours to select and maintain; keep them outside the captured oracle. Contract and fitness files that express agreed obligations belong inside the oracle and are declared under `obligations` in `tada.json`. A label never downgrades verification.

When coordinated execution is useful, capture `.tada/verification-plan.json` with `tada plan prepare --file .tada/verification-plan.json`, then use `tada run --plan <digest>` and `tada verify --run <id>`. Acceptance checks reference human-approved baselines. Supporting MSTest/MTP checks capture their engineering configuration and unique native discovery names, with TRX reporting enabled. Contract checks exercise both the real producer and consumer against captured contract expectations. Plan preparation never approves acceptance. Revisions identify `previous` and explain `changeReason`. An empty justified selection reports NO_CHECKS and makes no passing-test claim. Read the project workflow for the plan contract.

## Establish evidence

Read `.tada/WORKFLOW.md` for runner setup, supported input contracts, execution and retained-evidence details. Configure a separate application and scenario-scoped acceptance harness, use meaningful public observations and release resources after failures. Apply known test-infrastructure pitfalls found during discovery.

Make the harness runnable through Visual Studio Test Explorer and `dotnet test --project tests/Acceptance/Acceptance.csproj`. Use the generated `TadaAcceptance.Create()` context for its working directory, API assembly and .NET host. Add a build-only API `ProjectReference` with `ReferenceOutputAssembly="false"`, `Private="false"` and `OutputItemType="TadaSutOutput"`, and import `Tada.Acceptance.targets`. Match that reference to `execution.sutProject` in `tada.json`. Native builds stage the complete separate API under the test output's `sut` directory; keep the same business bindings for both execution modes. Check native Debug and Release execution as well as recorded TADA execution. Native results are ordinary runner results; TADA assessment additionally requires an approved baseline and retained-evidence verification.

For several owned local .NET services, use schema-2 `execution.services` with a unique ID, project and assembly for each service. Match each native build-only reference with `TadaServiceId`. Fixtures use `TadaAcceptance.Create().Services[id]`, own service readiness and isolated state, and checkpoint every service before termination. Agree business deadlines explicitly and distinguish failed outcomes from unavailable observations.

Prepare and review the immutable baseline with `tada baseline prepare` and `tada baseline review` (with `--against <previous>` for amendments). Present the captured expectations, assertions, fixture setup, classified protected changes and trust declarations to the human. The human declares approval. You may prepare the approval command but must never grant approval on the human's behalf.

Record enforcement status in `.tada/enforcement.json`: existing implemented scenarios stay `active`; newly approved, not-yet-implemented scenarios become `candidates` with today's `since` date and a reason. A spec-only change with candidates is legitimate. Never demote an active scenario to escape a failure.

Demonstrate meaningful behavioural RED for new behaviour and defects: the intended outcome assertion fails. Missing bindings, broken setup or unavailable dependencies are INDETERMINATE, not RED. Already-satisfied behaviour needs no invented RED. For refactors, establish passing preservation evidence first.

## Implement and verify

Implement in small, reviewable increments. Use the selected SDK host and pass it through `--dotnet` to `tada run` when required. Never modify, bypass or weaken protected expectations, bindings, fixtures, observers, configuration or enforcement status to reach GREEN. If implementation exposes a genuine requirement conflict or missing business decision, stop that scope and return it to the human.

Run `tada gate --run <id>` (with `--base <target-branch>` when one exists). When a candidate passes, move it to `active` in the same change. Run relevant supporting and quality checks too.

Note provisional discoveries as you go, in your working notes or the handoff. Do not stop implementation for documentation approval; decide what to preserve at completion.

## Complete and preserve

Report which obligations were satisfied, which checks ran and their results, which protected expectations changed and whether they were approved, which checks were omitted and why, pending candidates, verification gaps, residual risks and the human code-review that remains. Explain PASS, FAIL, INDETERMINATE and NO_CHECKS within their actual scope. Results are local advisory L1: approval records are declarations, and team CI needs independent review of the protected changes the gate lists.

Preserve learning selectively. A finding qualifies only if it is likely to influence future decisions, prevent a meaningful recurring defect, explain an important trade-off, reveal a non-obvious constraint or dependency, or save repeated investigation. Routine details, successful operations and facts obvious from code do not qualify. Most changes produce no new knowledge document. Apply this whenever a session ends, including when the human pauses or defers the work, because discovery findings are often the most valuable output of a paused change. For each qualifying finding:

1. Find its canonical home first: features for behaviour, declared contract and fitness roots, the project's ADR convention for material decisions, existing architecture or operations documents, or the issue or pull request. Prefer a link or a proposed edit there. Never restate an agreed Gherkin scenario.
2. Apply the knowledge mode from `.tada/knowledge.json`. Without that file the mode is `reference`.
   - **Reference** (default): never create standalone knowledge records. Point to canonical sources from the concise `.tada/knowledge.md` index or the handoff, propose updates to existing documents and update existing records. When a significant finding has no suitable home, tell the human in your report: name the finding, say that no document covers it, and suggest where it could live or that augment mode would allow a focused record. Do not write that document yourself.
   - **Augment** (opt-in by a human): additionally propose one focused record in `.tada/knowledge/` when a meaningful gap has no suitable canonical home, the evidence is sufficient or uncertainty is explicit, and the record does not duplicate or contradict an established source. Use the reference template: `status: proposed`, a `kind`, the `checked` commit, repository `sources`, and separate Verified, Inferred and Unknown sections.
3. Never present a proposal as an accepted decision, a `reviewed` record or a requirement. Supersede rather than rewrite accepted history. Humans review persistent knowledge through normal Git review.

When the verifier is installed, run `tada knowledge check` (with `--base <target-branch>` for pull requests). It reports the mode, broken links, records whose sources changed since they were checked, and records that reference mode does not allow.

Keep `.tada/context.md` a current-work handoff, not a history log: objective and branch, authoritative links, baseline and run IDs, implementation and verification state, pending candidates, open questions, next action and links to durable knowledge. Replace finished work rather than appending to it, and say so when no work is in progress.

For refactors, establish passing preservation checks before restructuring and add characterisation tests when coverage is inadequate. For chores, no new tests or no test execution can be justified by the actual effects. Record the evaluation and rationale. Chores need no impact brief document, invented learning or extra approval. Use existing tools directly when coordinated execution adds no value. Human approval governs business intent and executable acceptance. Routine supporting engineering-test selection and implementation are delegated.

## Prepare the verifier when needed

Installing or enabling the plugin needs no application folder or verifier setup. For an installation-only request, confirm plugin availability and stop. Do not ask the user to choose a repository or offer a "plugin only" setup choice.

The plugin supplies agent behaviour. The repository-local `tada` command supplies baseline, enforcement and evidence verification. Initial support is Windows x64, .NET SDK 10.0.403, Reqnroll 3.3.4, MSTest 4.5.2 and MTP 2.5.2.

Infer the application repository from the current workspace and project context. Prepare its verifier when business verification requires it. Ask for a folder only if the target is genuinely ambiguous at that point. Chores and behaviour-preserving refactors use existing checks without installing a verifier solely for ceremony.

Inspect the existing local tool manifest and `.tada/context.md`. Reuse the recorded SDK host after checking its availability and version. When setup is needed, run [scripts/Initialize-Tada.ps1](scripts/Initialize-Tada.ps1) with the repository path and selected .NET host. The distributed plugin includes its exact local package feed. The helper prepares files and migrates known alpha.8 instruction blocks. It reports edited legacy blocks for review.

Keep workflow activation in the plugin. Repository context should point to decisions, features, baseline/run IDs, durable knowledge and the next action. Persistent global or repository instructions that force TADA would bypass the user's plugin toggle.
