# Initial landscape research

Researched: 2026-10-23. Status: findings plus explicitly labelled recommendations.

Scope: current primary project documentation and licence files. No tools were installed or executed, and no security audit or adapter feasibility test was performed. Website documentation and default branches can change. Capability claims below describe the reviewed surfaces, not every extension or possible configuration.

## Existing AI development workflows

| Project | Verified capabilities | Implication for AAATDD |
| --- | --- | --- |
| [OpenSpec](https://github.com/Fission-AI/OpenSpec) | Markdown requirements and scenarios; change proposals, designs, tasks, exploration, application and archiving; multiple AI-tool integrations and custom workflows. Its [CLI](https://openspec.dev/docs/cli) describes `validate` as structural checking. | Reuse its change/delta and exploration patterns. Structural validation does not itself demonstrate application behaviour or approval of the effective oracle. A workflow integration is plausible; no integration selected. |
| [Spec Kit](https://github.com/github/spec-kit) | Specification, clarification, planning, tasks, implementation and convergence; separate bug-fix and idea-assessment paths. [Workflows](https://github.github.io/spec-kit/reference/workflows.html) support shell execution, gates, state, resume, extensions and overlays. [Convergence](https://github.github.io/spec-kit/reference/agentic-sdd.html) is an agent assessment that can append remaining tasks. | A credible workflow host, not merely a collection of Markdown templates. Gate inputs and shell permissions still need a trust model: workflow documentation explicitly disclaims a capability sandbox. |
| [BMAD Method](https://github.com/bmad-code-org/BMAD-METHOD) | Adaptive planning, specialist perspectives, persistent context, brownfield support and delivery workflows. The reviewed main-branch README distinguishes stable v6.12 from a changing v7 preview; do not conflate them. | Neither specialist roles nor adaptive ceremony is a distinctive AAATDD invention. Reuse the practices without requiring its entire method. |
| [BMAD TEA](https://github.com/bmad-code-org/bmad-method-test-architecture-enterprise) | Acceptance automation, test review, NFR assessment, traceability and release gates. [`tea-trace`](https://bmad-code-org.github.io/bmad-method-test-architecture-enterprise/reference/tea-trace-cli/) freezes criteria and priorities, validates oracle and evidence artifacts, checks revision identity and retains attempts. Its [architecture](https://bmad-code-org.github.io/bmad-method-test-architecture-enterprise/explanation/verification-architecture/) separates stack-neutral decisions from execution targets. | Strong overlap with AAATDD. Its documentation says there is currently no execution-target plugin API and backend review coverage is partial. Evaluate extension or component reuse before proposing a separate engine. These findings do not establish that TEA lacks every protection AAATDD seeks. |

The reviewed [OpenSpec licence](https://github.com/Fission-AI/OpenSpec/blob/main/LICENSE), [Spec Kit licence](https://github.com/github/spec-kit/blob/main/LICENSE), [BMAD licence](https://github.com/bmad-code-org/BMAD-METHOD/blob/main/LICENSE), and [TEA licence](https://github.com/bmad-code-org/bmad-method-test-architecture-enterprise/blob/main/LICENSE) are MIT. Reuse must preserve applicable notices. BMAD also reserves trademark rights. Dependency and asset licences require separate review if components are selected. AAATDD's own licence is undecided.

## What is, and is not, distinctive

**Finding:** collaborative examples, executable acceptance tests, incremental development, risk-based gates, traceability, and living knowledge already exist. AAATDD should not claim to invent them or describe all SDD as non-executable.

**Candidate distinction, still to validate:** one portable verification contract combining human-authorised oracle revisions, explicit accounting for all required checks, independently enforced execution, and implementation/harness/environment evidence across established DSLs. The emphasis is preventing an implementation agent from redefining success.

This is a proposed combination and guarantee, not demonstrated novelty. TEA substantially narrows the gap. A separate agent persona is not evidence of a separate security boundary.

**Recommendation, unapproved:** keep the verification specification portable; consider thin integrations with existing workflow tools. Compare extending TEA, reusing selected components, and building a small independent core at checkpoint 2. Avoid a fork or mandatory method dependency until a documented capability gap justifies it.

## Practices to reuse

| Practice and primary source | Useful lesson | Limit |
| --- | --- | --- |
| [TDD](https://www.martinfowler.com/bliki/TestDrivenDevelopment.html) | Drive small changes with failing tests, then refactor. | An arbitrary failing process is not evidence that the intended assertion detects a defect. |
| [ATDD](https://agilealliance.org/glossary/atdd/) | Customer, developer and tester perspectives jointly define acceptance before implementation. | Tooling must support the conversation; automation cannot establish that the requirement itself is right. |
| [BDD example mapping](https://cucumber.io/docs/bdd/example-mapping/) | Separate rules, concrete examples, questions and deferred stories. | Structured wording still needs trustworthy executable bindings. |
| [Pact Broker verification matrix](https://docs.pact.io/pact_broker/can_i_deploy) | Bind compatibility results to actual consumer/provider versions and deployment context. | Interface compatibility does not establish all business behaviour. |
| [Fitness-function-driven development](https://www.thoughtworks.com/insights/articles/fitness-function-driven-development) | Express architectural qualities through meaningful measurements and appropriate pipeline checks. | Metrics can be incomplete proxies; statistical checks need workload and environment policies. |
| [SLSA artifact verification](https://slsa.dev/spec/v1.2/verifying-artifacts) | Check artifact digests, trusted producer identity and expected inputs; provenance must be verified. | Attested production is not proof of behavioural correctness. AAATDD L1/L2/L3 must not imply SLSA certification. |

## Initial adapter screen

These are feasibility leads, not an approved order or verified implementation estimates.

| Candidate | Capability, licence and reporting evidence | Cost or limitation to investigate |
| --- | --- | --- |
| Gherkin / [Reqnroll](https://github.com/reqnroll/Reqnroll) | Established .NET executable scenarios using mainstream .NET test runners. [BSD-3-Clause](https://github.com/reqnroll/Reqnroll/blob/main/LICENSE). [TRX reporting](https://docs.reqnroll.net/latest/reporting/vstest-loggers.html) uses .NET loggers. | Promising fit for the .NET example. Need stable scenario/outline identities, complete discovery, binding inspection, and runner-specific status mapping. [Missing/pending step outcome is configurable](https://docs.reqnroll.net/latest/installation/configuration.html), so runner configuration belongs inside the oracle boundary. |
| OpenAPI / [Specmatic](https://github.com/specmatic/specmatic) | Executable API contract tooling. Repository [licence](https://github.com/specmatic/specmatic/blob/main/License.md) is MIT. [Reporting documentation](https://docs.specmatic.io/references/configuration/reports) labels native CTRF output commercial. | Can verify a .NET HTTP service through its interface, but runtime/distribution and edition choices add integration work. Do not assume free native CTRF output. Check an OSS structured-result path, supported OpenAPI features, and generated-case identities before selection. Shape checks alone may miss business-rule defects. |
| Generic test/fitness evidence | [CTRF](https://ctrf.io/docs/specification/status) normalises passed, failed, skipped, pending and other outcomes. The [schema repository](https://github.com/ctrf-io/ctrf) is [MIT](https://github.com/ctrf-io/ctrf/blob/main/LICENSE). | A report importer may be cheap, but it cannot establish execution completeness or trusted provenance alone. An execution adapter needs more capabilities. CTRF retries report the final status; retain enough detail to apply an approved flake policy. |

A comparative maturity ranking and implementation-cost estimate remain open. Robot Framework, Pact/PactNet, Schemathesis, Karate, and AsyncAPI tools remain later candidates; this screen does not eliminate them. Check identities, reporting fidelity, licence boundaries, maintenance, and conformance cases before deciding order.

## Research anchors

Default-branch heads observed during this research are listed below for follow-up source inspection. Website pages are dated retrievals, not claimed to be generated from these exact commits.

| Repository | Observed commit |
| --- | --- |
| OpenSpec | `56ef0ac1fd2270e88d276b40da9f23ec0f9c6e96` |
| Spec Kit | `27d14e20c187c1f2916540ea201ecbdc9f8ed588` |
| BMAD Method | `61e01f82644c4bb533750cfa0d6d61ab61d2cc9b` |
| BMAD TEA | `2d09752353e66119fe5046a01268aa1c492140ce` |
| Reqnroll | `488afb3735dcfb55f8ec60e929536e4f74b66a3c` |
| Specmatic | `0288581590b28a0da13dd26460b4f71c7a7bec31` |
| CTRF | `e325eb33c7662a208bfa4d093a41cc55439d6e6a` |
