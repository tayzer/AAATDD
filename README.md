# TADA

**Test-Assured Development Assistance.** Formerly AAATDD.

## What is TADA?

**TADA is an AI-first software development workflow built around collaborative executable specifications, incremental implementation and protected verification obligations. It shifts human attention toward agreeing behaviour, architectural constraints and consequential risks, while using trustworthy evidence to reduce the burden of reviewing AI-generated implementation code.**

Its central principle is:

> **AI may propose and implement changes, but it must not independently redefine the conditions by which those changes are judged successful.**

Humans agree what success means. AI helps implement it efficiently. Protected executable obligations and trustworthy evidence keep the implementation accountable to that agreement.

TADA is a development methodology supported by tooling. It is not just a test runner or an evidence verifier. Its tooling is an agent plugin, which guides ordinary development requests, and a repository-local `tada` CLI, which protects agreed expectations and assesses evidence. The current alpha supports C#/.NET with Gherkin, Reqnroll and MSTest on Windows x64. Its results are local and advisory (L1).

## The problem with conventional AI-assisted SDD

Specification-driven development with AI often follows a familiar shape. Humans and AI write a prose specification. AI plans and generates a large implementation. Tests are written alongside or after the code. Humans then review a large pull request.

This shape has gaps, although not every SDD tool has all of them:

- **Prose is not executable.** AI can misread a requirement or fill a gap with an assumption nobody approved.
- **Retrospective tests can confirm the implementation instead of the requirement.** Tests written against generated code may encode the same assumption.
- **Review capacity does not scale with generation.** AI can produce code faster than people can understand it or confirm it matches the original intent.

## How TADA approaches software development

TADA moves agreement to the front and keeps it protected.

- **Evidence before agreement.** Before proposing acceptance criteria, the agent investigates the existing system in proportion to risk and reports what it verified, what it inferred and what is still unknown.
- **Executable acceptance first.** For business behaviour, humans and AI agree concrete Gherkin examples before production code is written. Gherkin is the authoritative specification. Supporting Markdown explains rationale and context. It does not restate the specification.
- **Human authority over success criteria.** AI may propose expectations, contracts, thresholds and amendments. Only authorised humans approve acceptance-changing decisions. Routine engineering choices stay delegated.
- **Non-functional requirements from the start.** Discovery considers performance, resilience, security, compatibility, accessibility, architecture rules and observability alongside behaviour. Objective checks become protected fitness obligations. Other concerns are recorded as required human review rather than manufactured automated assurance.
- **Protected obligations.** Approved Gherkin, its executable interpretation, declared contracts and fitness thresholds, and verification configuration are captured byte for byte. Changing them requires renewed approval, and the change is classified so reviewers can see what kind of obligation moved.
- **Small increments.** Implementation proceeds in small, reviewable steps against agreed expectations, starting from meaningful behavioural RED when there is something new to demonstrate.
- **Trustworthy evidence.** A passing runner exit code is not a TADA PASS. TADA distinguishes `PASS`, `FAIL` and `INDETERMINATE`, retains failure history, and reports missing, skipped or invalid evidence explicitly.
- **Learning that compounds.** Each change can leave the project easier to change next time. TADA reuses and improves the engineering knowledge a team already has, and creates new knowledge only when a human has enabled it and a real gap exists.

## The TADA development lifecycle

The lifecycle is **Understand → Agree → Select verification → Establish evidence → Implement and verify → Complete and preserve**. These are responsibilities and decision points, not six commands or six approval meetings. Small changes combine steps. Chores and refactors take proportionate paths.

```mermaid
flowchart TD
    request["Change request<br/>(feature, defect, refactor, chore)"] --> discover["Evidence-driven discovery<br/>retrieve knowledge, investigate by risk,<br/>verified / inferred / unknown"]
    discover --> kind{"Does agreed<br/>behaviour change?"}
    kind -- "No: chore or refactor" --> reuse["Select proportionate checks<br/>reuse existing obligations,<br/>add characterisation if coverage is thin"]
    kind -- "Yes: feature or defect" --> agree["Agree Gherkin examples<br/>plus contract and fitness expectations"]
    agree --> approveSpec["Human approves expectations<br/>(may merge as approved candidates)"]
    approveSpec --> baseline["Select verification and build<br/>executable baseline: bindings, fixtures, observers"]
    baseline --> reviewOracle["Human reviews the<br/>executable interpretation"]
    reviewOracle --> red["Establish starting evidence<br/>meaningful behavioural RED"]
    reuse --> preserve["Establish preservation evidence<br/>existing obligations PASS"]
    red --> increment["Small implementation increment"]
    preserve --> increment
    increment --> verify{"Verify<br/>PASS / FAIL / INDETERMINATE"}
    verify -- "FAIL or INDETERMINATE" --> increment
    verify -- "Acceptance-changing discovery" --> agree
    verify -- "PASS" --> promote["Promote satisfied candidates<br/>to active obligations"]
    promote --> gate["Protected-change review and CI gate<br/>active obligations must pass,<br/>protected changes listed for human review"]
    gate -- "Blocked" --> increment
    gate -- "Passed and code reviewed" --> merge["Merge and preserve learning<br/>decisions, approvals, evidence,<br/>knowledge by mode, current handoff"]
    merge -. "next change retrieves it" .-> discover
```

The [lifecycle guide](docs/architecture/change-lifecycle.md) describes each stage and the change-type paths in detail.

## Evidence-driven discovery

A change that looks local can reach other services, API consumers, shared data, asynchronous messages, retries, state transitions, security and operations. AI agents tend to start implementing after reading a few files. TADA asks the agent to understand the affected system first, so the examples you agree describe the system you actually have.

Investigation is **risk-proportionate**. A documentation chore needs the relevant files and existing checks. A business fix follows behavioural paths, related scenarios, persistent state and regression history. A cross-service or security-sensitive change also traces data ownership, message consumers, idempotency, consistency, failure recovery and non-functional requirements. The agent stops when it can responsibly propose the affected expectations, and it names sources it could not reach instead of guessing.

Before proposing Gherkin, the agent shares a short **change impact brief**:

- **Verified** findings, each with a reference to code, tests, configuration, decisions or history.
- **Inferred** conclusions that are plausible but not yet established.
- **Unknowns** that would change acceptance, framed as questions for you.

For a small change the brief is a sentence or two. It is conversation, not a document you must approve. Code shows what the system does today, not what it should do, so the agent never turns an inference into an agreed requirement.

## Continuous learning

TADA retrieves knowledge from earlier changes, checks that it is still true, and preserves significant new learning where future work will find it: **Retrieve → Validate → Learn → Propose → Preserve**. This runs alongside the lifecycle rather than as an extra stage, and most changes create no new documents.

```mermaid
flowchart LR
    retrieve["Retrieve<br/>relevant decisions, docs,<br/>records and history"] --> validate["Validate<br/>against current code"]
    validate --> learn["Learn<br/>during the change"]
    learn --> propose["Propose<br/>links, updates or,<br/>in augment mode, a record"]
    propose --> preserve["Preserve<br/>through normal Git review"]
    preserve -.-> retrieve
```

The knowledge mode controls what TADA may create. It never limits what TADA reads.

| Mode | What TADA does |
| --- | --- |
| **Reference** (default) | Finds and reuses your existing documentation, decisions, issues, pull requests and history. Keeps a concise pointer index and a current-work handoff. Proposes updates to existing documents and flags outdated or conflicting knowledge. Never creates standalone knowledge documents just because something is undocumented. |
| **Augment** (opt-in) | Everything in reference mode. When a significant finding, such as a root cause, invariant, pitfall or undocumented service responsibility, has no suitable home, TADA may propose one focused, evidence-backed record for review. |

Select augment mode with `.tada/knowledge.json`: `{ "schemaVersion": 1, "mode": "augment" }`. The setting sits outside the captured oracle, so changing it never invalidates approved expectations. `tada knowledge check` shows the mode and flags records whose sources have changed since they were last validated.

## Knowledge authority

TADA must not create a competing source of truth. Approved Gherkin, contracts and fitness obligations define what must hold. Your accepted decisions and project documentation keep their authority. Current code shows what the system does now. Earlier findings and AI inferences are leads to check, not requirements. When sources disagree, TADA reports the conflict and proposes an update or supersession; it never quietly picks the convenient answer or rewrites an accepted decision. The [discovery and knowledge guide](docs/knowledge.md) explains modes, records and checks.

## Requirement and verification types

| Type | Purpose | Approval |
| --- | --- | --- |
| **Functional behaviour** | Gherkin scenarios executed through Reqnroll, MSTest and the Microsoft Testing Platform | Human-approved. The authoritative business specification. |
| **Executable interpretation** | Bindings, assertions, fixtures, observers and drivers that decide whether a scenario passes | Human-reviewed with the baseline. Changes need renewed approval. |
| **Service contracts** (optional) | Agreed producer/consumer expectations, declared per project | Protected like acceptance criteria when declared. |
| **Fitness functions** | Agreed NFR thresholds and architecture rules, such as latency budgets or dependency boundaries | Protected when declared. Non-automatable concerns are recorded as required human review. |
| **Supporting engineering tests** | Unit tests and implementation-focused integration tests | Delegated to the developer and agent. Not individually approved. Can be coordinated with acceptance through a captured [verification plan](docs/multi-level-verification.md). |

Protection follows the requirement a check expresses, not its file type or test framework. See [ADR 0045](docs/decisions/0045-classify-protected-verification-obligations.md).

## Agreeing a specification before it is implemented

An approved scenario is not automatically mandatory for merge. TADA tracks two separate questions:

- **Is this expectation approved?** Recorded by human review and approval of an immutable baseline.
- **Is it currently enforced?** Recorded in `.tada/enforcement.json`. Each approved scenario is either **active**, which means it must pass for merge, or a **candidate**, which means it is approved and pending implementation.

A spec-only change can therefore merge approved candidates without breaking mainline CI. `tada gate` requires every active obligation to pass, reports candidates with their age as non-blocking diagnostics, and requires a candidate to be promoted to active once it passes. Against the target branch, an active obligation cannot be demoted unless an approved amendment changed that scenario. See [ADR 0044](docs/decisions/0044-separate-approval-authority-from-merge-enforcement.md) and the [enforcement guide](docs/enforcement.md).

## What human approval means

| AI may | Humans decide |
| --- | --- |
| Investigate the system in proportion to risk and report verified, inferred and unknown findings | Business behaviour, compatibility, security and data-integrity questions |
| Propose Gherkin, contracts, thresholds and amendments | Approval of acceptance-changing expectations and amendments |
| Write bindings, fixtures, supporting tests and implementation | Review of the executable interpretation of protected expectations |
| Select supporting checks and routine technical approaches | Whether a contract or fitness obligation is protected, and any weakening |
| Run checks and report results, omissions and residual risks | Code review proportionate to risk, and merge |
| Retrieve and validate knowledge; propose updates, and records in augment mode | The knowledge mode, and review of persistent knowledge changes |

Approval of an expectation is distinct from permission to implement or merge. An agent may prepare an approval command, but it must never approve on a human's behalf.

**Assurance limits.** The current release is local advisory **L1**. Approval records are declarations by whoever controls the workspace. Exact hashes show that evidence corresponds to approved inputs, not that an assertion is adequate. In CI, approvals introduced by a pull request need independent authority, such as required code-owner review of the protected changes the gate lists. TADA does not claim protected L2 assurance. Passing obligations do not prove that arbitrary implementation changes are secure, maintainable or free of unintended behaviour, so proportionate code review still applies.

## Quickstart

1. Download the [first public alpha](https://github.com/tayzer/TADA/releases/tag/v0.1.0-alpha.12), or build from source using the [quickstart](docs/quickstart.md). Requirements: Windows x64 and .NET SDK 10.0.403.
2. Install and enable the [TADA plugin](docs/plugin.md) through your agent host's plugin controls.
3. Start a fresh session in your application repository and describe your task normally, for example:

   > Build me a simple API for tracking my reading list.

   > Orders above five units should be rejected.

You do not need to name TADA, Gherkin or any workflow command. The enabled agent workflow inspects the repository, asks the questions that change acceptance, proposes examples for you to agree, prepares verification when it is needed, and implements in small increments. It records a concise current-work handoff in `.tada/context.md` so the next session can continue, and it reuses what earlier changes learned.

The CLI operations used by the workflow are `tada init`, `tada baseline prepare | review | approve`, `tada plan prepare`, `tada run`, `tada verify`, `tada gate` and `tada knowledge check`. The [CI guide](docs/ci-pr-workflows.md) shows how to run the gate on pull requests.

## Current capabilities and roadmap

**Released in `0.1.0-alpha.12`:** plugin-guided workflow for ordinary requests; `tada init`; exact-byte baseline capture with review and approval; real Reqnroll/MSTest/MTP execution of a separate ASP.NET Core API; retained-evidence verification with `PASS`, `FAIL` and `INDETERMINATE`; preserved failure history; build-input and runtime observation with documented L1 limits. See the [release notes](docs/releases/0.1.0-alpha.12.md).

**In source for `0.1.0-alpha.13`, not yet released:** [change-aware multi-level verification](docs/multi-level-verification.md) with captured verification plans, supporting MSTest/MTP suites, several owned local .NET services and explicit `NO_CHECKS` results for justified no-test changes ([ADR 0042](docs/decisions/0042-add-change-aware-multi-level-verification.md)); the North Star workflow guidance; separate enforcement status with approved candidates and active obligations; `tada gate` with base-branch comparison; classification of protected contract, fitness, specification, interpretation and configuration changes; and the end-to-end lifecycle controls in `scripts/Test-EnforcementWorkflow.ps1`. Evidence-driven discovery and continuous learning with reference and augment knowledge modes ([ADR 0046](docs/decisions/0046-investigate-before-agreeing-acceptance.md), [ADR 0047](docs/decisions/0047-add-reference-and-augment-knowledge-modes.md)) and `tada knowledge check`.

**Planned:** an independent approval adapter for team CI (L2), plan checks that run protected contract and fitness suites outside the acceptance harness, and further runners, languages and platforms. The [North Star assessment](docs/architecture/north-star-assessment.md) records the gap analysis and prioritised roadmap.

## Further reading

- [Project charter](docs/vision.md) and [decision records](docs/decisions/)
- [Development lifecycle](docs/architecture/change-lifecycle.md), [enforcement guide](docs/enforcement.md) and [multi-level verification](docs/multi-level-verification.md)
- [Discovery and continuous learning](docs/knowledge.md)
- [CI and PR workflows](docs/ci-pr-workflows.md)
- [Quickstart](docs/quickstart.md), [plugin](docs/plugin.md) and [native testing](docs/native-testing.md)
- [Build-task contracts](docs/build-task-contracts.md) and [provenance audit](docs/validation/provenance-audit.md) for verifier trust boundaries
- [Release checklist](docs/release-readiness.md) and [design status](docs/design-status.md)

Source licence: [MIT](LICENSE). Historical ADRs and probe evidence retain the earlier AAATDD name.
