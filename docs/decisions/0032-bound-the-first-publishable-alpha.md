# ADR 0032: Bound the first publishable alpha

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the initial release boundary within Q14; detailed contracts and implementation remain pending.

## Context

The completed compatibility probe establishes bounded tooling feasibility, not a usable AAATDD product. A first release needs a complete, reusable workflow and explicit release evidence without requiring every longer-term capability.

## Decision

Target `0.1.0-alpha` as a local, advisory L1 tool for approving acceptance baselines and checking evidence against them. Support one developer, one Git repository and one runner stack: Reqnroll with MSTest and Microsoft.Testing.Platform on Windows x64/.NET 10.

The minimum workflow must:

1. Prepare a baseline identifying Gherkin cases, harness inputs, dependencies and verification policy, and show what requires approval.
2. Record human approval of exact reviewed content and scope. Changed content creates a draft; passing tests never grant approval.
3. Execute the reviewed harness against an identified, separately running application.
4. Validate expected cases, baseline integrity, implementation/environment identities and native Cucumber Messages evidence.
5. Explain `PASS`, `FAIL` or `INDETERMINATE` with reasons and retained evidence. Only `PASS` permits completion; explicit reruns retain earlier unresolved trustworthy failures.
6. Show baseline amendments and record explicit replacement approval while preserving history.

Command names, storage formats and the verifier implementation language remain separate design decisions. The supported .NET runner stack does not select the verifier language.

## Release criteria

Publication requires evidence that:

- The Orders example passes with correct behaviour and fails for selected business defects.
- Changed assertions, missing or skipped cases, stale or truncated evidence and failed setup/observation cannot produce `PASS`.
- A documented change to Orders completes the approval, implementation and verification workflow.
- Another small project uses the same tool without changes to its core.
- A clean supported machine can install the release and follow its quickstart.
- Versioned release artifacts, CI, a project licence, dependency notices and documented limitations exist.

These are accepted release obligations, not completed checks. Concrete scenarios, fault controls, installation/package details and measurable acceptance procedures still need design and review.

## Deferred scope

Defer protected L2 execution and independent-assurance claims; other runners and platforms; GUI and hosted services; MCP integrations; agent orchestration; and general provider APIs.

Automated workflows for chores and refactors are also deferred from this alpha. Their agreed support under ADR 0003 remains future scope; this release boundary does not revoke it.

## Alternatives considered

- Publishing the compatibility probe alone would not provide the complete reusable approval and verification workflow.
- Requiring protected L2, multiple runners or broader integrations before any release would enlarge the first delivery beyond the bounded advisory workflow.
- Shipping only the Orders demonstration would not establish reuse outside the example; a second small project is therefore a release criterion.

## Consequences and limits

L1 integrity checks detect mismatches within a cooperative local environment. They cannot protect approval records or evidence from an actor controlling that environment. Report the alpha as advisory; neither a separate SUT process nor passing checks establishes independent assurance.

This decision authorises continued design within the agreed release boundary. It does not approve the Orders executable baseline, full dependency graph, privileged-observer exception, further runtime preparation, implementation, or publication itself. Those require their applicable separate approvals. Existing accepted ADRs and the Orders feature remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Do you agree that this is the release boundary for `0.1.0-alpha`?” The preceding proposal specified the workflow, supported stack, release criteria, deferrals and L1 limits recorded above, and explicitly reserved separate implementation authorisation.

Related: [design status](../design-status.md), [open questions](../open-questions.md), [assurance staging](0005-validate-l1-before-protected-l2.md), [change-appropriate artifacts](0003-support-change-appropriate-specification-artifacts.md), [probe results](../research/compatibility-probe-results.md).
