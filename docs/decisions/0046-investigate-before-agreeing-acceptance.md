# ADR 0046: Investigate the existing system before agreeing acceptance

Status: **ACCEPTED — DIRECT USER INSTRUCTION**. The concrete guidance is an implementation decision under [ADR 0041](0041-authorise-autonomous-release-implementation.md). Decision date: 2026-11-11.

## Context

The Understand stage of the [North Star lifecycle](0043-adopt-the-tada-north-star.md) asked the agent to inspect the request, current behaviour, existing obligations, affected components and risks. It gave no guidance on how deep to go, when to stop, or how to report what was found. An agent could read a few files and then propose Gherkin for a change that looked local but affected other services, asynchronous consumers, retries, consistency or security.

The opposite failure is also real. Exhaustive investigation of every task wastes time, buries the consequential findings and adds ceremony to chores.

Code observation has a further trap. It shows what the system does now, not what it is supposed to do. An inference about why the code behaves a certain way is not an agreed requirement.

## Decision

The Understand stage performs **risk-proportionate, evidence-backed investigation** before new acceptance criteria are proposed.

1. Separate the requested outcome from any suggested implementation approach.
2. Retrieve relevant existing knowledge by relevance, not wholesale: features, contract and fitness roots, accepted decisions, project documents, the knowledge index and records from [ADR 0047](0047-add-reference-and-augment-knowledge-modes.md), and the Git history of touched paths.
3. Investigate to a depth set by impact and risk. Low, medium and high depths are guides with example scopes, not classification gates. The agent adjusts depth as evidence appears.
4. Stop expanding when the affected acceptance expectations can be proposed responsibly. Unrelated subsystems are not investigated for completeness. Unreachable sources are reported as limitations.
5. Present a **change impact brief** before agreeing new acceptance. It separates **Verified** findings with references, **Inferred** conclusions and **Unknowns**, and covers relevant history, functional and non-functional requirements and verification implications.
6. Ask targeted questions where the answer changes acceptance. Never convert an inference into an agreed requirement.

The brief is conversational and scaled to the change. A small change may need two sentences. It is not a new artifact type, approval step or specification. When it is worth keeping, it goes where the information is useful anyway: the context handoff, the pull request description, or the `impact` and `omissions` fields of an existing verification plan ([ADR 0042](0042-add-change-aware-multi-level-verification.md)).

Knowledge sources have an explicit authority order. Approved Gherkin, contracts and fitness obligations come first, then accepted decisions and established project documentation. Current code, configuration, tests and evidence show actual behaviour. Reviewed records, issues, pull requests and commits are historical findings. Proposed records, the context handoff and inferences are leads. Conflicts are reported, not silently resolved.

### Alternatives considered

| Alternative | Why not chosen |
| --- | --- |
| A mandatory discovery document for every change | Adds ceremony to chores and small fixes. It would also become a second specification competing with Gherkin. |
| Fixed complexity classes that gate the workflow | Real risk is often only visible after some investigation. Fixed gates encourage misclassification to avoid work. |
| A CLI discovery command that scans the repository | Discovery is judgement about relevance. A scanner would produce comprehensive-looking reports that bury what matters, and it could not reach other repositories or external systems. Agent guidance plus existing tools is enough for now. |

## Consequences

- The plugin skill, its [discovery and knowledge reference](../../plugins/tada/skills/tada/references/discovery-and-knowledge.md) and the generated `.tada/WORKFLOW.md` carry the depth guide, stopping rule, authority order and brief template.
- Discovery quality remains agent behaviour. It is not mechanically enforced. The [knowledge workflow evaluation](../validation/knowledge-workflow-evaluation.md) records how agents behaved on realistic tasks. Its results do not guarantee the behaviour of other agents or models.
- Discovery findings feed verification selection, so dependencies and earlier defects inform which checks are reused or added.
- Humans still decide acceptance-changing questions. Discovery improves the questions; it does not answer them.
