# Knowledge workflow evaluation

These trials check how an agent applies TADA's discovery and continuous-learning guidance to ordinary requests. They complement the focused `KnowledgeTests`, which check the mechanical parts. Results are recorded in [the knowledge workflow evaluation](../../docs/validation/knowledge-workflow-evaluation.md).

A Markdown instruction does not guarantee agent behaviour. These trials show what agents did on specific tasks with a specific model. Repeat them when the skill changes or when a different agent host is adopted.

## Fixtures

`New-KnowledgeFixture.ps1 -Scenario <id> -Destination <path>` builds a disposable Git repository, OrderHub, from `examples/MultiService`. Orders, Payments and Inventory exchange messages through polling workers with at-least-once delivery. Build fixtures outside this repository so the evaluated agent cannot read this rubric.

| Fixture | Documentation | Knowledge mode | Code and history |
| --- | --- | --- | --- |
| A | Architecture document and ADR 0001 | reference (default) | Current sample |
| B | None | reference (default) | Current sample |
| C | None | augment | Current sample |
| D | Architecture document and ADR 0001 | reference (default) | Current sample |
| E1 | None | augment | Duplicate-message defect: Orders processes outcomes for finished orders and Inventory release is not idempotent |
| F | Architecture, ADR 0001, newer ADR 0002, an older runbook that contradicts ADR 0002, and a stale context handoff | reference (default) | Release fixed in a later commit |
| G | Architecture document and ADR 0001 | reference (default) | Current sample |

E2 is built from the E1 repository after the first session ends. The 2026-11-11 run used three commits: `fix: release stock once for repeated payment outcomes (#14)` with all of the session's work; `docs: review payment outcome delivery learning`, which sets the record's `checked` to that commit and its status to `reviewed`; and `perf: give slow services more time and poll less often`, which changes the `HttpClient` timeout from 3 to 10 seconds and the worker delay from 50 to 250 ms in `Services/ServiceHost.cs`. The record's source therefore changes after it was reviewed, and one of its claims becomes wrong.

## Protocol

1. Package the plugin with `scripts/New-TadaPlugin.ps1`. Give the agent the packaged skill path and the repository path, nothing else. For a baseline comparison, replace `SKILL.md` with an earlier version and remove `references/`.
2. Start a fresh agent session per scenario. Say that the TADA plugin is enabled and its skill is loaded from the given path. Limit reads to the repository and the plugin. The agent cannot ask questions mid-task; it ends its turn with the message it would send the user.
3. Send the request exactly as listed. Do not mention TADA, Gherkin, knowledge, records, briefs or modes.
4. For two-phase scenarios, reply as the requester with the listed follow-up. Answer questions plainly; never coach the workflow.
5. Grade the final messages against the rubric, and inspect `git status`, created files and `tada knowledge check` output in the fixture.

Requester approvals given during a trial are labelled evaluation controls in a disposable repository. They are not product acceptance.

## Requests and rubric

**A — existing documentation, reference mode.** Request: "Payments needs to be able to report a refund for an order that has already completed. When that happens, Orders should show the order as refunded and put the item back in stock." Follow-up: park the change and wrap up.
Expected: retrieves the architecture document and ADR 0001; cites them; identifies that the finished-order guard, the `PaymentOutcome` v1 contract and inventory release are affected; proposes updates to the existing documents instead of new ones; creates no `.tada/knowledge/` records.

**B — missing documentation, reference mode.** Request: "Some payments take ages to come back. Orders that are still waiting for payment after 15 minutes should be cancelled automatically and their stock released." Follow-up: park the change and wrap up.
Expected: discovers the undocumented at-least-once delivery and finished-order guard and the late-approval charge risk; keeps references in the handoff or index; creates no standalone record; makes the documentation gap visible.

**C — missing documentation, augment mode.** Same request and follow-up as B.
Expected: recognises the gap and proposes one focused record with `status: proposed`, provenance (`checked`, `sources`) and separated Verified, Inferred and Unknown claims; states that it is not a requirement; leaves it for review; `tada knowledge check` reports no blocking findings.

**D — distributed investigation.** Request: "Customers should be able to cancel an order before it completes." Single phase. Also run with the previous skill as a baseline.
Expected: investigates Payments and Inventory effects before proposing Gherkin; finds that charges exist before completion, that late or repeated outcomes interact with the finished-order guard, and that release must be idempotent; considers failure handling and NFRs; separates verified findings from assumptions; asks rather than invents policy.

**E1 — bug fix with learning, augment mode.** Request: "After declined payments we sometimes end up with more stock available than we actually have. Please fix it." The requester agrees the proposed regression scenario and fix, as a labelled evaluation control.
Expected: reproduces the duplicate-delivery root cause; agrees a regression scenario before fixing; fixes and verifies; preserves the root cause selectively in augment mode without restating the Gherkin.

**E2 — fresh session reuse.** Request, in a fresh session: "Support staff need a way to manually re-send a payment result to Orders when an order looks stuck."
Expected: finds the earlier learning; checks its currency (for example with `tada knowledge check` or Git history) before relying on it; corrects stale claims and returns an edited reviewed record to `proposed`; uses it in the impact brief; does not treat it as current truth or as a requirement.

**F — stale or contradictory knowledge.** Request: "When Orders can't reach Inventory to release stock after a declined payment, the order just stays stuck. Please add an automatic retry for the release so stock isn't left reserved." Follow-up: park, and say the platform team owns the runbook.
Expected: identifies the runbook's conflict with ADR 0002 and the code; does not silently choose a source; does not edit the runbook or the accepted ADRs; does not follow the stale handoff's branch or next action; replaces the stale handoff at wrap-up.

**G — routine chore.** Request: "The README doesn't say how to run the supporting tests. Please add that." Single phase.
Expected: minimal relevant discovery; edits the README; runs or checks the commands; no impact brief document, records, invented learning or approval request.

## Requester follow-ups

- A, B and C: "Thanks, this is really useful. I need to take the open questions to the team before we go any further, so let's stop here for today. Wrap up as you normally would." The wording names the question when one is clearly central.
- F: "It was (b): the orders only stayed stuck while Inventory was down, and they cleared up afterwards. I need to take questions 2, 4 and 5 to the team before we change anything, so let's stop here for today. The platform team owns the runbook, so please don't edit it yourself. Wrap up as you normally would."
