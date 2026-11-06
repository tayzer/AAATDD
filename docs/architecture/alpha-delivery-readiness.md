# TADA alpha delivery readiness

Status: **RETAINED DESIGN-STAGE AUDIT**. This records the state before the autonomous implementation instruction. For current code, validation and remaining release work, use the [release checklist](../release-readiness.md) and [quickstart](../quickstart.md). Statements below that code is absent are historical, not current blockers or restrictions.

The release boundary in [ADR 0032](../decisions/0032-bound-the-first-publishable-alpha.md) remains authoritative. This document maps obligations to required evidence; it does not approve new architecture, specifications, implementation or publication. Concrete evidence procedures below are proposed until their applicable review.

## Current evidence against the release goal

| Obligation | Evidence needed to establish completion | Current state |
| --- | --- | --- |
| Complete workflow | The installed `tada` tool prepares a draft, presents review content, records exact scoped approval, runs the reviewed harness and verifies retained evidence without rerunning it. Retain command arguments, outputs and linked records. | Incomplete. [ADRs 0035–0036](../decisions/0035-adopt-five-operation-alpha-cli.md) define the CLI and review binding. No TADA runtime or CLI exists. |
| Correct Orders behaviour and business-fault detection | Run the approved [Orders feature](../requirements/order-stock-validation.feature) against correct and selected faulty implementations. The intended business assertion must identify each selected fault; infrastructure failure is not sufficient. | Incomplete. Business intent is agreed. The only project is the synthetic [compatibility probe](../../experiments/compatibility-probe/README.md), which explicitly does not verify Orders. |
| Changed assertions and incomplete evidence block `PASS` | Demonstrate unapproved oracle edits and missing, skipped, stale or truncated required evidence; retain precise integrity/coverage reasons and the assessed identities. Exercise failed setup/observation and required cleanup under the accepted mixed-outcome rules. | Incomplete. [ADR 0014](../decisions/0014-require-targeted-first-harness-validation-controls.md) requires control classes. The complete TADA integrity/evidence path is absent. |
| Approval integrity | Show that exact-byte changes after review prevent approval, and changes after approval block execution. A new draft/review/approval must identify changed content and superseded scope while preserving earlier records. | Incomplete. [ADRs 0036–0038](../decisions/0036-bind-approval-to-reviewed-baseline.md) establish the design. No executable implementation or validation exists. |
| A documented application change | Retain a walkthrough linking agreed intent, approved oracle, starting evidence, an identified implementation change and final verification. A fix may satisfy the existing feature without inventing new business intent. | Incomplete. Orders application code and walkthrough do not exist. Select and review the actual change before implementation. |
| Reuse in a second small project | A separate small project adopts the same versioned TADA package and core, completes the workflow and produces project-specific evidence. Configuration and bindings may differ; the core package must not require project-specific edits. | Incomplete. No second project or adoption evidence exists. Its business intent and executable baseline need their own approval. |
| Clean supported-machine installation | On a fresh Windows x64 environment with documented prerequisites, install/restore the release package as a repository-local .NET tool and follow the quickstart through verification. Retain package identity, environment, steps and results. | Incomplete. No TADA package, quickstart or clean-machine evidence exists. Developer caches and the probe-local SDK do not establish this criterion. |
| Publishable artifacts and support scope | Versioned package/release artifacts, CI for the release source, selected project licence, dependency notices and clear local L1/advisory limitations. Link release evidence to the exact source and package identities. | Incomplete. No product CI or release package exists. Licence and package identity remain undecided. |
| L1 claim discipline | Terminal output, machine-readable results and documentation explicitly state local advisory assurance and the observation boundary. Do not claim protection from an actor controlling the local environment. | Design accepted; product output is absent. Protected L2 remains deferred. |

No row is satisfied solely by the bounded probe. Its [report](../research/compatibility-probe-results.md) establishes one Windows x64 candidate stack, discovery, synthetic pass/failure, Messages relationships and SQLite role/cleanup observations. It does not approve the full Orders oracle or dependency graph.

A subsequent [static licence inspection](../research/compatibility-candidate-licences.md) records declared licences for the 24 candidate graph packages and the separately resolved MSTest SDK, embedded notice-file hashes and distinct .NET SDK distribution terms. It also distinguishes raw archive hashes from NuGet lock identities. This is useful release-input research, not final graph approval or completed attribution/redistribution review; the publication row remains incomplete.

## Proposed sequence toward implementation readiness

1. Finish the concrete input contract: review the [file inventory proposal](alpha-file-inventory-proposal.md), then execution, external-input and policy fields. Resolve logical tag roles and mapping to native evidence.
2. Finish the record and evaluator contract: approval/review/run identities and storage, evidence schema and supported Messages interpretation, outcome reasons, exit codes, interrupted execution and retained-failure resolution. Preserve accepted approval and outcome semantics.
3. Make the Orders baseline reviewable: actual HTTP wire interpretation, SQL/transaction settings, scenario ownership/readiness/cleanup, scoped read-only observer exception and concrete sensitivity/evidence-gap controls. Discuss necessary dependencies and review the full graph. Keep Gherkin as business authority.
4. Review a bounded implementation plan and obtain separate authority to implement and execute it. Distinguish preparing oracle artifacts from implementing the SUT; approval and execution follow the agreed checkpoints. Do not require successful business tests before baseline approval or manufacture a behavioural RED from broken setup.
5. Implement and validate the complete Orders workflow under that authority, preserving failure history. Package the same core for second-project adoption and clean-machine verification.
6. Audit every release obligation against retained evidence and obtain any still-required publication approval. A working demo alone is insufficient.

These are dependencies, not a new requirement to finish every general open question before any implementation. Resolve acceptance-changing questions for the authorised slice and defer capabilities already outside the alpha. Do not silently substitute a probe, schema library or Orders-only demo for the full release goal.

## Next consequential decision

Review the proposed `files` entry schema and conservative repository-path handling in the [file inventory proposal](alpha-file-inventory-proposal.md). That decision fixes how reviewed source bytes are identified without claiming hostile-code isolation. Other delivery decisions remain open.

Related: [design status](../design-status.md), [open questions](../open-questions.md), [baseline proposal](first-experiment-baseline-proposal.md).
