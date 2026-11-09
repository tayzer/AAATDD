# ADR 0020: Use scenario-scoped harness components

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q18's initial component model; concrete APIs and reusable provider contracts remain open.

## Context

Fixture, transport, observation and assertion failures have different meanings. The first harness needs these responsibilities to be reviewable without introducing a general extension platform before its requirements have been exercised.

## Decision

Use ordinary scenario-scoped classes or functions in the dedicated acceptance harness:

| Component | Responsibility |
| --- | --- |
| Fixture | Prepare and validate starting state; own cleanup |
| HTTP helper | Send actions and capture transport details |
| Observer | Collect measurements, correlation and evidence availability |
| Gherkin bindings/assertions | Compare observations with approved expectations |

Keep measurement separate from business acceptance. Observers return measured values or evidence unavailability; bindings/assertions apply approved expectations. Verification policy separately accounts for approval, integrity and completeness. Setup checks remain readiness evidence rather than product outcome assertions.

Share reviewed technical plumbing where useful. Defer provider registries and published extension APIs until the experiment demonstrates what needs reuse. The first experiment does not require a universal assertion DSL or separate processes for these internal responsibilities. The separately running SUT required by earlier decisions remains outside the harness.

## Alternatives considered

- A single undifferentiated step implementation obscures which responsibility failed and can mix measurement with inferred success.
- A provider/extension framework introduces contracts before the first experiment establishes a concrete reuse need.
- Separate processes for every component add complexity without independently establishing a trust boundary.

## Consequences and limits

This is responsibility separation, not isolation or proof of independent verification. L1 remains advisory. Concrete classes, interfaces, wire/result types, dependency choices and package versions remain unapproved. This decision does not authorise implementation, execution, general provider APIs or a privileged-observation exception.

## Approval reference

On 2026-11-03, the user replied **“yes”** to “Should we adopt this component model for the first harness?” The preceding proposal assigned fixture, HTTP-helper, observer and binding/assertion responsibilities, permitted shared reviewed plumbing, deferred provider registries/published extension APIs, and distinguished organisation from isolation.

Related: [ADR 0012](0012-bound-the-initial-oracle-to-harness-inputs.md), [ADR 0017](0017-validate-fixture-readiness-before-submission.md), [ADR 0019](0019-require-confirmed-scenario-teardown.md), [responsibility-separation research](../research/execution-boundaries.md#is-fixtureactionobservationassertion-separation-useful), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
