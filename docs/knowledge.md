# Discovery and continuous learning

This guide explains how TADA investigates an existing system before agreeing acceptance, and how it reuses and improves engineering knowledge across changes. [ADR 0046](decisions/0046-investigate-before-agreeing-acceptance.md) and [ADR 0047](decisions/0047-add-reference-and-augment-knowledge-modes.md) record the decisions. The agent-facing rules and templates are in the plugin's [discovery and knowledge reference](../plugins/tada/skills/tada/references/discovery-and-knowledge.md).

## What is mechanical and what is guidance

| Capability | How it is supported |
| --- | --- |
| Risk-proportionate investigation, stopping rule, change impact brief | Agent guidance in the plugin skill and generated `.tada/WORKFLOW.md`. Evaluated with realistic tasks, not enforced. |
| Knowledge authority order and conflict reporting | Agent guidance. |
| Knowledge mode and its default | `.tada/knowledge.json`, validated by `tada knowledge check`. |
| Reference mode forbids new standalone records | Checked by `tada knowledge check`, including against a target branch. |
| Record provenance (status, kind, checked commit, sources) | Checked by `tada knowledge check`. |
| Stale records after their sources change | Checked by `tada knowledge check`. |
| Reviewed records edited without renewed review | Reported by `tada knowledge check`. |
| Broken links in the handoff, index and records | Checked by `tada knowledge check`. |
| Whether a claim is still true, or an old document contradicts the code | Agent and human judgement. The check only flags records whose sources moved. |

## Discovery before agreement

The agent separates what you want from how it might be built. It then retrieves relevant knowledge and investigates to a depth that matches impact and risk:

- **Low.** Documentation, bounded chores and contained refactors. The agent looks at relevant files, conventions and existing checks.
- **Medium.** Business fixes and bounded features. The agent also follows behavioural paths, related scenarios and contracts, persistent state and regression history.
- **High.** Cross-service, consequential data, security-sensitive and distributed changes. The agent also traces service and data ownership, asynchronous consumers, retries and idempotency, consistency, failure recovery, authorisation and NFRs.

It stops when it can responsibly propose the affected acceptance expectations. Before proposing Gherkin, it shares a **change impact brief** that separates what it verified, what it inferred and what is unknown. Unknowns that change acceptance become questions for you. For a small change the brief may be a sentence or two. It is never a required file.

For example, "customers can cancel an order before dispatch" can look local to Orders. Discovery may find that Orders publishes events consumed by Inventory and Payments, and that cancellation can race with fulfilment. The brief then asks what happens if dispatch starts during cancellation, whether reserved stock is released, when payment is refunded, what happens when a downstream service is unavailable, and whether repeated cancellation is idempotent. TADA does not hard-code those answers. You decide them.

## Knowledge modes

```json
{ "schemaVersion": 1, "mode": "reference" }
```

Save this as `.tada/knowledge.json` to make the mode explicit. Without the file, TADA uses reference mode.

| | Reference (default) | Augment (opt-in) |
| --- | --- | --- |
| Search features, contracts, ADRs, docs, records and Git history | Yes | Yes |
| Validate recalled facts against current code and decisions | Yes | Yes |
| Maintain the `.tada/knowledge.md` pointer index and the context handoff | Yes | Yes |
| Propose updates to existing documents and records | Yes | Yes |
| Report documentation gaps and conflicts | Yes | Yes |
| Propose a new focused record in `.tada/knowledge/` | No | Only for a meaningful gap with no suitable home, backed by evidence |

Choose augment mode when your team wants TADA to capture undocumented service responsibilities, root causes, invariants, pitfalls, constraints or trade-offs that have no natural home in existing documentation. Stay in reference mode when you already maintain architecture documentation or ADRs and want TADA to keep using and improving them.

The settings file is outside the captured oracle. Changing the mode does not invalidate approved baselines or change gate results.

## Knowledge authority

| Source | Authority |
| --- | --- |
| Approved Gherkin, contracts and fitness obligations | What must hold |
| Accepted ADRs and established project documentation | Design decisions within their scope |
| Current code, configuration, tests and evidence | What the system does now, not what it should do |
| Reviewed records, issues, pull requests and commits | Historical findings to recheck |
| Proposed records, the context handoff and AI inferences | Leads to confirm |

Knowledge records never approve or redefine requirements. When sources disagree, the agent reports the conflict and its evidence. It may propose an update or supersession. It never rewrites accepted decisions or approved expectations on its own authority.

## Files

- `.tada/context.md` is the current-work handoff: objective and branch, authoritative links, implementation and verification state, open questions, next action and links to durable knowledge. Finished work is replaced rather than appended.
- `.tada/knowledge.md` is an optional pointer index by topic. It links to canonical sources and restates nothing authoritative.
- `.tada/knowledge/<topic>.md` holds focused records, created only in augment mode.

A record looks like this:

```markdown
---
status: proposed
kind: defect
checked: 3f2a1bc
sources: Services/ServiceHost.cs, features/order-journey.feature
---
# Repeated payment outcomes must not release stock twice

Supporting engineering context, not a requirement. Agreed behaviour lives in
[the order journey](../../features/order-journey.feature) `@AC_DUPLICATE_DELIVERY`.

## Verified
- The Orders consumer ignores outcomes for completed or rejected orders — [ServiceHost.cs](../../Services/ServiceHost.cs).

## Inferred
- Duplicates come from the Payments retry loop, which redelivers until it sees success.

## Unknown
- Whether a message broker would add its own redelivery.

## Applies when
- Changing payment outcome handling, inventory release or delivery retries.

## History
- Fixed in PR #123.
```

`kind` is one of `discovery`, `defect`, `invariant`, `pitfall`, `constraint` or `trade-off`. Agents write `status: proposed`. A reviewer may change it to `reviewed` when merging. To replace a record, set `status: superseded` and `supersededBy: <path>` and keep the file as history. `checked` names the commit at which the claims were validated. Update it together with the claims after revalidating.

## `tada knowledge check`

```powershell
dotnet tool run tada knowledge check
dotnet tool run tada knowledge check --base origin/main --json
```

The report shows the mode and where it came from, the index, the context handoff's last change and the number of later commits, each record's state, and findings:

- `invalid-record` (blocking): front matter is missing status, kind, checked commit or repository sources, or contains an unsupported field.
- `reference-mode-record` (blocking): a record was added in reference mode, either uncommitted or since `--base`.
- `source-changed`: a record's sources changed after its `checked` commit. Revalidate before relying on it.
- `reviewed-record-edited`: a `reviewed` record changed since `HEAD` or `--base` without returning to `proposed`. Confirm a human reviewed the edit.
- `source-missing`, `unknown-checkpoint`, `broken-link`, `superseded-target-missing`: references no longer resolve.
- `git-unavailable`: currency could not be checked outside a Git work tree.

`CURRENT` and `REVIEW` exit 0. `BLOCKED` exits 1. Invalid settings exit 3. The result is separate from `PASS`, `FAIL` and `INDETERMINATE`, and from `tada gate`.

To keep reference-mode repositories free of unsolicited records in team CI, add a step after checkout with full history:

```yaml
- run: dotnet tool run tada knowledge check --base origin/${{ github.base_ref }}
```

## Limits

- Discovery depth, retrieval quality and the impact brief depend on the agent. Guidance improves them but does not guarantee them.
- The check detects structural staleness only. It cannot tell whether a claim is true or whether an older document contradicts current code.
- `reviewed` is a declaration by whoever controls the workspace, like other L1 records.
- Retrieval uses conventional search and Markdown links. There is no semantic index.
