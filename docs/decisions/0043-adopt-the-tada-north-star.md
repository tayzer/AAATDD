# ADR 0043: Adopt the TADA North Star as the product decision framework

Status: **ACCEPTED — DIRECT USER INSTRUCTION**. Decision date: 2026-11-11. ADR number 0042 records the change-aware multi-level verification work merged from PR #5.

## Context

Earlier documents introduced TADA mainly through its verifier: exact-byte baselines, provenance capture and retained evidence. Those mechanisms are valuable, but they describe how TADA protects an agreement rather than why a team would adopt it. The user has clarified the product's purpose and asked for it to guide subsequent architecture decisions.

## Decision

TADA's North Star is:

> TADA is an AI-first software development workflow built around collaborative executable specifications, incremental implementation and protected verification obligations. It shifts human attention toward agreeing behaviour, architectural constraints and consequential risks, while using trustworthy evidence to reduce the burden of reviewing AI-generated implementation code.

Its central principle is that **AI may propose and implement changes, but it must not independently redefine the conditions by which those changes are judged successful.**

TADA is a development methodology supported by tooling. Verification machinery serves the methodology. The conceptual lifecycle is **Understand → Agree → Select verification → Establish evidence → Implement and verify → Complete and preserve**. These are responsibilities and decision boundaries, not six commands, documents or approval meetings. Each change follows a path proportional to its actual effects.

Human and AI responsibilities are:

| Responsibility | AI may | Requires authorised human decision |
| --- | --- | --- |
| Understanding | Inspect context, existing obligations, constraints and risks; ask targeted questions | Answers that change business behaviour, compatibility, security, data integrity or other acceptance-critical outcomes |
| Agreement | Propose Gherkin examples, contracts, fitness thresholds and amendments | Approval of acceptance-changing expectations and amendments |
| Verification selection | Select supporting engineering checks; propose protected obligations | Agreement that a contract or fitness obligation is protected, and any weakening of one |
| Evidence | Write bindings, fixtures, observers and drivers; demonstrate RED | Review of the executable interpretation of protected expectations |
| Implementation | Implement in small increments; choose routine technical details | Resolution of genuine requirement conflicts discovered during implementation |
| Completion | Run checks, report results, omissions, gaps and residual risks; record context | Code review proportionate to risk; merge |

Gherkin remains the authoritative executable specification for functional business behaviour. Supporting Markdown records rationale, decisions and context without becoming a second business specification.

## Consequences

- The README and charter lead with the workflow, its problem statement and its human/AI boundary. Verifier internals move to linked technical documentation.
- Architecture changes are judged by whether they help humans agree success criteria, let AI implement efficiently, and keep implementation accountable to protected obligations. Additional commands, manifests or approval ceremony need a concrete benefit.
- Earlier accepted ADRs remain historical records. ADR 0044 separates approval authority from merge enforcement. ADR 0045 defines protected obligation kinds.
- Passing obligations do not prove arbitrary implementation code is secure, maintainable or free of unintended behaviour. Proportionate human code review remains part of completion.
- Local approval records remain L1 declarations. TADA does not claim protected L2 assurance until an independent approval authority exists.
