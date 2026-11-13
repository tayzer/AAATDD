# Knowledge workflow evaluation

Date: 2026-11-11. Change: [ADR 0046](../decisions/0046-investigate-before-agreeing-acceptance.md) and [ADR 0047](../decisions/0047-add-reference-and-augment-knowledge-modes.md) on branch `feat/evidence-discovery-learning`. The protocol, fixtures and rubric are in [`tests/KnowledgeEvaluation`](../../tests/KnowledgeEvaluation/README.md).

## What was evaluated and how

TADA's discovery and learning behaviour is mostly agent guidance. Guidance cannot be proven by a unit test, so each scenario ran as a fresh agent session against a disposable OrderHub repository: three local services that exchange payment outcomes with at-least-once delivery. Each agent received only the repository path, the packaged plugin skill and an ordinary user request. It never saw the rubric. Two-phase scenarios continued the same session with a requester reply that paused the work, which is when learning has to be preserved.

The evaluated agents were Claude Opus 5.5 subagents with shell access, not the Codex host used for the requester trial of [the default workflow](default-workflow-trial.md). Each scenario ran once, or twice when the skill was revised. The evaluator wrote the fixtures, acted as the requester and graded the results. Approvals in E1 were recorded by the evaluator as labelled controls in a disposable repository. They are not product acceptance.

Mechanical behaviour is covered separately by `KnowledgeTests`, `GuidanceTests` and `OnboardingTests` in `tests/Tada.Core.Tests`, and by the packaged plugin control in `scripts/Test-TadaPlugin.ps1`.

## Results

| Scenario | Mode | Result | Evidence |
| --- | --- | --- | --- |
| A. Existing documentation | reference | Met | Retrieved the architecture document and ADR 0001. Found that a refund conflicts with ADR 0001's rule that a late outcome must not change a finished order, and proposed a new ADR narrowing it rather than editing ADR 0001. Found the `PaymentOutcome` v1 contract limit, the finished-order guard and the deployment-order risk from unbounded redelivery. At wrap-up it created only a handoff linking ADR 0001 and the architecture document. No knowledge records. |
| B. Missing documentation (first run) | reference | Partly met | Strong discovery and a correct handoff. No record was created. It did not tell the requester that the delivery invariant it found is undocumented. |
| B. Missing documentation (rerun, revised skill) | reference | Met | The brief had explicit Request, Depth, Existing authority, Verified, Inferred and Limitations sections. At wrap-up it named the undocumented finding and suggested an ADR once finance decides, without writing one. No records. `tada knowledge check`: `CURRENT`, reference (default). Defect found: every line reference was offset by about 194 lines. Each claim was correct. |
| C. Missing documentation (first run) | augment | Not met | Good discovery, but it deferred any record "until the feature is finished", so the most valuable finding stayed only in the handoff. It also installed the verifier before agreement and had to delete duplicate `tada init` scaffolding. |
| C. Missing documentation (rerun, revised skill) | augment | Met | Deferred verifier setup until agreement. At wrap-up it proposed one record, `payment-outcome-after-cancellation.md`, with `status: proposed`, kind `pitfall`, `checked` commit and `sources`. Verified claims carried correct line references, inferred claims said what would confirm them, and the open finance decision was listed as unknown. The record states that it is not a requirement and links the agreed feature. The agent told the requester that the record is an uncommitted proposal for review. `tada knowledge check`: `CURRENT`. |
| D. Distributed investigation | reference | Met | Before proposing Gherkin, traced Payments and Inventory effects with file and line references. Found that charges exist before completion, that release is idempotent, and that the finished-order guard must include `cancelled` or a late approval would complete a cancelled order. Covered worker races, the missing identity and authorisation, and test-time limitations. Asked six policy questions instead of choosing answers. |
| D. Previous-skill baseline | reference | Partly met | Similar business questions, including the money and race questions. No source references, no limitations, no Git history check, and the late-approval dependency on the finished-order guard was not stated. |
| E1. Duplicate-message defect | augment | Met | Reproduced the root cause by running the services: stock went 1 → 2 → 3 on repeated declined outcomes. Agreed a regression scenario, then drafted two more for the requester's answer and asked for them to be read closely. Recorded TADA runs: `FAIL` before the fix at the intended assertions, then `PASS`. `tada gate` passed, candidates were promoted, and supporting tests were added. Preserved the root cause as one proposed `pitfall` record with an index entry, linking scenario tags without restating them. It also explained why `tada knowledge check` reported `REVIEW` until the change is committed. |
| E2. Fresh-session reuse | augment | Met, one gap | Fixture: the E1 work merged, a reviewer marked the record `reviewed` at the merge commit, and a teammate then raised the service timeout from 3 to 10 seconds and the poll interval from 50 to 250 ms. Request: "Support staff need a way to manually re-send a payment result to Orders when an order looks stuck." The agent found the record through the index and ran `tada knowledge check` (`REVIEW`: `source-changed`, and a handoff two commits old). It inspected `b89ccce`, corrected the stale 3-second claim, moved `checked`, and replaced the stale handoff. It re-ran the approved baseline on the latest commit because timing had changed (PASS, gate PASS). The brief used the record's idempotency facts to conclude that automatic redelivery already covers most stuck cases, and asked what support actually sees. All seven line references were correct. Gap: it left `status: reviewed` on the edited record, so the edit looked reviewed. |
| E2. Previous-skill baseline | augment | Not a clean baseline | The behaviour matched the current-skill run, and this agent did reset the edited record to `proposed`. The repository's `.tada/WORKFLOW.md` was generated by the new verifier during E1 and carries the new guidance, and the agent read it. The result shows that the repository-local workflow carries the knowledge behaviour whatever the plugin version. It does not isolate the skill change. |
| F. Stale and contradictory knowledge | reference | Met | Found that the requested retry already exists through Payments redelivery. Reported that the 2025 runbook contradicts the newer accepted ADR 0002 and the code, treated the ADR as authority, and did not edit the runbook or either ADR. Recognised that the handoff named a merged branch and a finished next action, and did not follow it. At wrap-up it replaced the stale handoff, labelled an unobserved inference as such, and recorded the runbook conflict with its owner. |
| G. Routine chore | reference | Met | Read only relevant files. Edited the README, ran both documented commands (2/2 and 2/2), and said this checks the instructions, not behaviour. No brief, record, index, handoff or approval request. |

## Changes made because of the evaluation

1. **Preserve learning when work is paused** (commit `11f4eb8`). The first B and C runs treated Complete and preserve as end-of-feature only. The skill and generated workflow now apply selective preservation at the end of every session. In reference mode, the agent must name a homeless finding and suggest where it belongs, without writing the document. Both scenarios met expectations after the change.
2. **Take references from the source** (commit `c9d8c8e`). The B rerun cited wrong line numbers for correct claims. The brief guidance now asks for references taken directly from the source, for example `grep -n`. E2 later produced correct references, which is one confirming run.
3. **Return edited reviewed records to proposed** (commit `20e5b37`). E2 kept `reviewed` on edited claims. The guidance now requires the reset, and `tada knowledge check` reports `reviewed-record-edited`. On the E2 fixture the check now reports that finding; on the E2 baseline fixture, which reset the status, it reports `CURRENT`. This guidance change has not been re-evaluated with an agent.

## Mechanical checks on realistic data

`tada knowledge check`, run from this branch's build against the fixtures:

- B rerun: `CURRENT`, reference mode from the default, no records.
- C rerun: `CURRENT`, augment mode, the proposed record `current`.
- E2 fixture, after the E1 record was reviewed at its merge commit and a later commit changed its source's timeouts and poll interval: `REVIEW`, with `source-changed` for the record and a note that the handoff predates two later commits.

The focused tests cover the default mode, invalid settings, baseline independence from the mode, reference-mode blocking against uncommitted changes and against `--base`, editing records created earlier, staleness and revalidation, missing sources, unknown checkpoints, broken links, invalid and authority-claiming front matter, supersession, handoff age and the CLI.

## Limits of this evidence

- One run per scenario, plus one rerun after each skill change, with one model family. Results show what these agents did, not what every agent will do. The Codex host used for the earlier requester trial was not re-run.
- The evaluator designed the fixtures and the rubric, played the requester and graded the results. The requester replies avoided coaching, but no independent grader was used.
- Agents were told to stay inside the repository and the plugin. Two agents listed names outside it (the evaluation parent folder and the NuGet package folder) and disclosed this. Neither read the rubric.
- The citation fix was evaluated by E2 only. The final reviewed-record guidance was not evaluated with an agent.
- Baselines with the previous skill are confounded where the repository already contains the new generated workflow (E2).
- Requester acceptance of this change remains a separate decision.
