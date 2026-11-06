# ADR 0022: Select .NET and Reqnroll for the first experiment

Status: **ACCEPTED**. Decision date: 2026-11-03. Resolves Q14's initial sample-stack choice; framework/verifier language, exact versions and test execution framework remain open.

## Context

Reqnroll/.NET was the researched preference for the first adapter. The sample now has an agreed HTTP/SQLite direction, native Cucumber Messages evidence composition and a dedicated harness boundary. Selecting the experiment stack allows concrete compatibility and binding-design work without choosing the future framework's language.

## Decision

Use C#/.NET with Reqnroll for the dedicated acceptance harness, and a small separately running ASP.NET Core HTTP API as the sample SUT. Apply the agreed HTTP/SQLite sample design and native Cucumber Messages evidence composition.

Keep the SUT implementation separate from the harness under ADR 0012. Choosing a common ecosystem establishes no isolation or independent-assurance guarantee. Cucumber.js remains a later portability candidate rather than the first experiment's runner.

## Alternatives considered

- Cucumber.js is a credible alternative Gherkin runner, but choosing it first would change the researched preference and still require reviewed bindings, fixtures and evidence integration.
- Selecting an AAATDD-wide framework language now would extend beyond the concrete sample decision.

## Consequences and limits

This selects only the initial experiment stack. Exact SDK, Reqnroll, test-framework, adapter, SQLite-provider/native and message-schema versions still require compatibility review. The test execution framework is not selected here. AAATDD's framework/verifier implementation language, packaging and public APIs remain open. No scaffolding, installation, restore, implementation, execution or executable-baseline approval is authorised.

## Approval reference

On 2026-11-03, the user replied **“yes”** to “Should we adopt this stack for the first experiment?” The preceding recommendation selected C#/.NET with a Reqnroll harness and a separate ASP.NET Core API, retained the HTTP/SQLite and native-messages design, left exact versions/test framework open, and distinguished the sample from AAATDD's core language.

Related: [ADR 0012](0012-bound-the-initial-oracle-to-harness-inputs.md), [ADR 0021](0021-retain-native-messages-with-aaatdd-envelope.md), [reuse research](../research/execution-boundaries.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
