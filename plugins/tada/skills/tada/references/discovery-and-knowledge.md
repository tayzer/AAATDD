# Discovery and knowledge reference

Templates and rules that support the Understand and Complete and preserve stages. Scale everything here to the change. None of it is a required file or approval step.

## Investigation depth

Depth is a guide, not a classification gate. Start from the apparent depth and move up or down as evidence appears.

| Depth | Typical changes | Investigate |
| --- | --- | --- |
| Low | Documentation, bounded chores, contained refactors | Relevant files, conventions, direct dependencies, existing checks |
| Medium | Business fixes, bounded features | Behavioural paths, related scenarios and contracts, persistent state, regression history of touched files |
| High | Cross-service changes, consequential data changes, security-sensitive work, distributed workflows | Service and data ownership, synchronous and asynchronous consumers, retries and idempotency, state transitions and consistency, failure recovery, authorisation, NFRs and operational constraints |

Stop when you can responsibly propose the affected acceptance expectations. Do not explore unrelated subsystems to look thorough. Name sources you could not reach, such as other repositories, issue trackers or production telemetry, as limitations.

## Retrieval by relevance

Search for the names of the components, entities, messages and endpoints the change touches. Look in this order and read only what matches:

1. Features and scenario tags, `.tada/enforcement.json`, declared contract and fitness roots.
2. Accepted decisions (`docs/decisions`, `docs/adr` or the project's convention) and existing architecture, operations or runbook documents.
3. `.tada/knowledge.md` and matching `.tada/knowledge/*.md` records.
4. Git history of the touched paths (`git log --oneline -- <path>`), linked issues and pull requests when accessible.
5. Source code, configuration and tests.

Treat every recalled or recorded fact as a lead. Before relying on one, check it against current code, configuration and accepted decisions. `tada knowledge check` reports records whose sources changed since they were last validated.

## Knowledge authority

| Kind of statement | Source | Authority |
| --- | --- | --- |
| Approved requirement | Approved Gherkin, approved contracts and fitness obligations | Defines what must hold. Changes need human approval. |
| Approved decision | Accepted ADRs and established project documentation, within their scope | Governs design until superseded by a newer decision. |
| Verified observation | Current code, configuration, tests and observed evidence | Shows what the system does now, not what it should do. |
| Historical finding | Reviewed knowledge records, issues, pull requests and commit messages | Useful context. Confirm it still applies. |
| Inference or hypothesis | Proposed records, AI reasoning, the context handoff | A lead to confirm or ask about. Never a requirement. |
| Open question | Missing or contradictory information | Needs a human decision or explicit deferral. |
| Superseded | Records or decisions replaced by newer ones | Retained history. Do not apply. |

When sources conflict, report the conflict and its evidence. Do not quietly choose the convenient source. A newer finding can justify proposing an update or supersession. Never rewrite an accepted decision or approved expectation without human authority.

## Change impact brief

Present this before proposing new acceptance criteria. For small work it can be two sentences. For consequential work, use the structure below in conversation. It is not a specification. Take each reference from the source itself, for example with `grep -n` or `git log`; a wrong line number misleads more than no line number. Persist it only where it is useful anyway: the context handoff, a pull request description, or the `impact` and `omissions` of a verification plan.

```markdown
**Request:** <outcome wanted>. <Implementation approach suggested, if any, kept separate.>
**Depth:** <low | medium | high> because <reason>.
**Existing authority:** <features/scenarios, contracts, fitness obligations, ADRs that apply>.
**Verified:** <finding> — <file:line, test, ADR or commit>.
**Inferred:** <conclusion> — <basis; what would confirm it>.
**Unknown:** <question> — <why it changes acceptance; who decides>.
**History:** <relevant defects, decisions or records, and whether they still apply>.
**Requirements to consider:** <functional and non-functional>.
**Verification implications:** <checks likely needed or reused>.
**Limitations:** <sources not available>.
```

## Knowledge modes

`.tada/knowledge.json` selects the knowledge creation policy. It sits outside the captured oracle, so changing it never invalidates an approved baseline.

```json
{ "schemaVersion": 1, "mode": "reference" }
```

Without the file, the mode is `reference`. Only a human chooses `augment`.

- **Reference** (default): discover, reuse and improve existing knowledge. You may maintain the `.tada/knowledge.md` pointer index and the context handoff, link canonical sources, propose edits to existing documents, flag outdated or conflicting knowledge, and update existing records. Do not create standalone records. When there is no suitable home, keep the references in the index or handoff and report the gap.
- **Augment** (opt-in): everything in reference mode. You may also propose one focused record when all of these hold: a meaningful gap exists, no suitable canonical document exists, the finding is likely to help future engineering decisions, the evidence is sufficient or uncertainty is explicit, and the record will not duplicate or contradict an established source.

In both modes, persistent knowledge changes are proposals governed by normal Git review.

## Significance test

Preserve a finding only when it is likely to influence future decisions, prevent a meaningful recurring defect, explain an important trade-off, reveal a non-obvious constraint or dependency, help future engineers modify the system safely, or reduce repeated investigation. Routine implementation details, successful operations and facts obvious from the code do not qualify.

For a consequential defect, the useful facts are the symptom, confirmed root cause, affected components, resolution and rationale, regression protection and references. If the issue or pull request already holds them, link it rather than copying it. Never restate the Gherkin regression scenario.

## Pointer index

`.tada/knowledge.md` points to canonical sources by topic. It restates nothing authoritative.

```markdown
# Knowledge index

Pointers to canonical sources. Read the source, not this summary.

## Payment outcome delivery
- Agreed behaviour: [order journey](../features/order-journey.feature) `@AC_DUPLICATE_DELIVERY`
- Decision: [ADR 0042](../docs/decisions/0042-example.md)
- Defect history: PR #123; commit `abc1234`
```

## Record template (augment mode only)

```markdown
---
status: proposed
kind: defect
checked: <commit at which you validated the claims>
sources: <repository paths that support the claims, comma separated>
---
# <One-line learning>

Supporting engineering context, not a requirement. Agreed behaviour lives in <link>.

## Verified
- <claim> — [<source>](../../path/to/source)

## Inferred
- <conclusion and what would confirm it>

## Unknown
- <open question>

## Applies when
- <the kinds of change that should consult this record>

## History
- <issue, pull request or commit references>
```

Kinds are `discovery`, `defect`, `invariant`, `pitfall`, `constraint` and `trade-off`. Status is `proposed` when you write it. A human reviewer may set `reviewed`. Replacement uses `status: superseded` and `supersededBy: <path>`; keep the old record. When you revalidate a record, update its claims and `checked` commit together. If you change the claims of a `reviewed` record, set it back to `proposed`; `tada knowledge check` reports reviewed records edited without that step.

## Context handoff

`.tada/context.md` is the current-work handoff, not a history log. Keep:

- current objective and branch, or a statement that no work is in progress;
- authoritative references for that work;
- implementation and verification state, including baseline and run IDs;
- unresolved questions and the next action;
- links to durable knowledge that the next session will need.

At completion, replace finished work instead of appending to it, and move lasting facts to their canonical homes. A fresh session confirms the handoff against Git before trusting its branch names, task status or instructions.
