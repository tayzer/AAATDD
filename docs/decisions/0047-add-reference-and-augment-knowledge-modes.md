# ADR 0047: Add reference and augment knowledge modes

Status: **ACCEPTED — DIRECT USER INSTRUCTION**. The configuration location, record format and `tada knowledge check` are implementation decisions under [ADR 0041](0041-authorise-autonomous-release-implementation.md). On 2026-11-11 the requester confirmed this design as built: settings in `.tada/knowledge.json` outside the captured oracle, no `writePolicy` setting, and `BLOCKED` only for invalid records and records added in reference mode. Decision date: 2026-11-11.

## Context

The [charter](../vision.md) already states that Git is the authority for project knowledge and that existing artifacts should be reused and indexed by relevance. [Open question Q11](../open-questions.md) left the mechanics open. In practice TADA had one knowledge mechanism, `.tada/context.md`. It worked as a session handoff, but nothing distinguished current work from history, nothing checked whether it was still true, and lasting findings had no defined home. This repository's own handoff showed the problem: after PR #5 merged, it still named a merged branch and a completed next action.

The user asked for continuous learning that improves future work without creating competing sources of truth, with two modes: reference by default and augment by opt-in.

## Decision

### Learning is part of the normal workflow

Continuous learning is a cross-cutting capability: **Retrieve → Validate → Learn → Propose → Preserve**. It strengthens Understand and Complete and preserve. It is not a seventh lifecycle stage, and it does not change verification outcomes.

### Two modes control creation, not retrieval

`.tada/knowledge.json` selects the knowledge creation policy:

```json
{ "schemaVersion": 1, "mode": "reference" }
```

- **Reference** (default, and the mode when the file is absent). Discover, retrieve, validate and improve existing knowledge. Maintain the optional `.tada/knowledge.md` pointer index and the context handoff, link canonical sources, propose updates to existing documents and existing records, and report gaps. Do not create standalone knowledge records.
- **Augment** (opt-in, chosen by a human). Everything in reference mode. The agent may also propose a focused record in `.tada/knowledge/` when a meaningful gap has no suitable canonical home, the finding would help future engineering decisions, the evidence is sufficient or uncertainty is explicit, and the record would not duplicate or contradict an established source.

Retrieval and validation happen in both modes.

**Location.** The settings live in `.tada/`, not in `tada.json`. `tada.json` is captured verification configuration, so changing it invalidates approved baselines. `.tada/` is excluded from oracle capture. Knowledge policy does not change verification semantics, so it must not invalidate an approved functional baseline. A focused test proves the baseline digest is unchanged when the mode changes.

**No `writePolicy` setting.** The conceptual sketch included `writePolicy: "propose"`. Every persistent knowledge change is already a proposal governed by normal Git review, and an agent must never treat its own record as reviewed. A second policy value would only add a way to weaken that. Unknown fields are rejected, so a future setting would need a deliberate schema change.

### Authority hierarchy

1. Approved Gherkin defines functional behaviour. Approved contracts and fitness obligations define their protected expectations.
2. Accepted decisions and established project documentation keep their authority within their scope.
3. Current code, configuration, tests and evidence show what the system does now. They do not establish what it should do.
4. Reviewed knowledge records, issues, pull requests and commit messages are historical findings. Their currency must be checked.
5. Proposed records, the context handoff and AI inferences are leads.

Superseded material is retained as history and is not applied. Knowledge never approves or redefines requirements. Agents propose updates or supersession and never rewrite accepted decisions without human authority.

### Records are small, evidence-bound and checkable

A record is a Markdown file in `.tada/knowledge/` with a minimal front matter:

```markdown
---
status: proposed            # proposed | reviewed | superseded
kind: defect                # discovery | defect | invariant | pitfall | constraint | trade-off
checked: 3f2a1bc            # commit at which the claims were last validated
sources: Services/ServiceHost.cs, features/order-journey.feature
supersededBy: ...           # required when superseded
---
```

The body separates Verified, Inferred and Unknown claims and links its evidence. Agents write `status: proposed`. A human reviewer may set `reviewed`. The format needs no YAML library and no TADA-specific tooling to read.

### The context handoff stays current

`.tada/context.md` holds current work only: objective and branch, authoritative links, implementation and verification state, open questions, the next action and links to durable knowledge. Finished work is replaced, not appended. Lasting facts move to their canonical homes.

### One mechanical check

`tada knowledge check [--base <revision>]` turns the parts that can be checked deterministically into a report:

| Finding | Severity | Meaning |
| --- | --- | --- |
| `invalid-record` | blocking | A record lacks valid status, kind, checked commit or repository sources, or claims an unsupported field |
| `reference-mode-record` | blocking | A record was added in reference mode, uncommitted or since `--base` |
| `source-changed` | review | A record's sources changed after its `checked` commit |
| `reviewed-record-edited` | review | A `reviewed` record changed since `HEAD` or `--base` without returning to `proposed` |
| `source-missing`, `unknown-checkpoint`, `broken-link`, `superseded-target-missing` | review | References no longer resolve |
| `git-unavailable` | review | Currency could not be checked |

`CURRENT` and `REVIEW` exit 0. `BLOCKED` exits 1. Invalid settings are command errors (3). The report also shows the mode and its source, and how many commits have landed since the context handoff last changed. It never contributes to `PASS`, `FAIL` or `INDETERMINATE`.

### Alternatives considered

| Alternative | Why not chosen |
| --- | --- |
| Settings in `tada.json` | Would invalidate approved baselines whenever the mode changed. |
| Knowledge records created automatically after every change | Produces a parallel wiki of low-value summaries that goes stale and competes with canonical sources. |
| A vector index or semantic search service | No demonstrated need. Conventional search and Markdown links work across models and need no new infrastructure. |
| No CLI support, guidance only | Agents do not reliably compare each record's sources with later history. A deterministic check catches stale records and reference-mode violations cheaply, including in CI. |
| Making knowledge findings part of `tada gate` | Would mix supporting context into verification outcomes and let documentation hygiene block or excuse obligations. |

## Consequences

- Existing repositories need no TADA knowledge directory. Without settings, records or an index, the check reports `CURRENT` in reference mode, and `tada init` creates none of them.
- Changing the mode does not invalidate baselines or change gate results.
- The check catches structural staleness: changed sources, missing files and broken links. It cannot tell whether a claim is semantically true or whether an old architecture document contradicts the code. Agents still validate content and report conflicts.
- `reviewed` status is an L1 declaration, like baseline approval. The check cannot tell who set it.
- The skill, its reference, the generated workflow and the [knowledge guide](../knowledge.md) describe the behaviour. The [knowledge workflow evaluation](../validation/knowledge-workflow-evaluation.md) records realistic agent trials in both modes.
- Q11's retention and redaction details remain open.
