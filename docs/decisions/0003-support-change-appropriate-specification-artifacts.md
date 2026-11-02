# ADR 0003: Support change-appropriate specification artifacts

Status: **ACCEPTED**. Decision date: 2026-10-23.

## Context

The intent-review discussion offered a compact behaviour brief or executable-DSL scenarios as a uniform requirement. The user clarified that the appropriate artifact depends on the change: a bug fix can call for an executable regression scenario, while a chore may not need a behavioural DSL scenario.

## Decision

AAATDD supports both intent-review paths:

- Changes whose acceptance is expressed through executable-DSL scenarios can include those scenarios in the intent review.
- Changes such as chores can proceed through the methodology without a mandatory behavioural DSL scenario when that artifact is not appropriate.

The framework must not impose one specification format on every change. This decision supports conditional use of DSL scenarios; it does not make a compact prose brief the universal substitute for them.

## Alternatives considered

1. Require executable-DSL scenarios for every change. This creates unnecessary artifacts for work that does not benefit from behavioural scenarios.
2. Standardise the first checkpoint on a prose brief and defer all DSL scenarios to the second. This unnecessarily constrains changes where a concrete regression scenario is already the useful starting artifact.

## Rationale and consequences

The methodology can accommodate bug fixes and maintenance work without treating all changes identically. It preserves the common verification direction while allowing suitable specification artifacts.

Not requiring a DSL scenario does not itself establish that a change needs no verification. The rules for selecting evidence, using existing checks, and determining whether a scenario is appropriate remain open. No blanket exemption for work labelled “chore” is accepted here.

ADR 0001's two approval scopes remain in force, with combined review available for clear small changes. A scenario presented during intent review does not automatically approve its assertions, bindings or harness. This decision does not create an exemption from baseline approval or authorise AAATDD implementation.

## Approval reference

In the design conversation on 2026-10-23, the user stated: **“Well it depends, you would imagine a bug fix would need a executable DSL scenario, but chores probably wont, so we should support it”**.

The accepted scope is support for change-appropriate artifacts. Exact classification rules, mandatory review contents, and timing of executable readiness remain undecided.

Related: [ADR 0001](0001-separate-intent-and-baseline-approval.md), [lifecycle discussion](../architecture/change-lifecycle.md).
