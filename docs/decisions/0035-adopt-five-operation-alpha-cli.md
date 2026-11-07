# ADR 0035: Adopt the five-operation alpha CLI

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha CLI operation boundary within Q14; exact flags, configuration, exit-code values and approval-record formats remain open.

## Context

The alpha release boundary and repository-local .NET tool packaging are agreed. The CLI needs a small, complete approval and verification workflow. The user approved five operations and their shared rules, replacing the proposed command prefix with `tada` and renaming the tool in ADR 0034.

## Decision

Provide these five operations:

| Command | Behaviour |
| --- | --- |
| `tada baseline prepare` | Create a draft inventory of cases, oracle inputs and policy. Never overwrite an approved revision. |
| `tada baseline review` | Show draft content, changes from the previous baseline and unresolved approval prerequisites. |
| `tada baseline approve` | Record explicit human approval of the reviewed revision and scope. Preserve earlier revisions and replacement history. |
| `tada run` | Check the selected approved baseline, execute the harness, retain fresh evidence and evaluate it. |
| `tada verify` | Evaluate retained evidence without rerunning tests, using the same verification rules as `run`. |

Every verification result identifies the baseline, implementation, environment and run. Show outcome, reasons, missing obligations and unresolved earlier failures. Unavailable identities are evidence gaps, not invented values; command errors are not successful verification results.

Apply these shared rules:

- Require explicit baseline selection; never silently select the newest draft.
- Reject approval if content changed after review.
- Keep approval separate from execution. Do not automatically approve during `run`.
- Preserve evidence from failed and interrupted runs. Do not automatically retry scenarios.
- Produce readable terminal output and a machine-readable result.
- Give verification outcomes distinct exit codes. Only `PASS` returns verification success; command errors also return nonzero. Successful draft/review/approval operations do not assert a verification `PASS`.
- Treat `approve` as recording human authority within cooperative L1 operation. It cannot authenticate a human against a controlling local agent.

Defer a separate `init` operation, reporting command and interactive wizard. The quickstart can supply initial configuration; `run` and `verify` provide reports.

## Alternatives considered

- An offline-only verifier would omit the agreed execution workflow; a run-only command would prevent reassessing retained evidence without another execution.
- Combining approval into execution would blur human authority and verification satisfaction.
- Additional setup, reporting or wizard operations would enlarge the initial interface before the minimum workflow is validated.

## Consequences and limits

Exact flags, configuration schema, numeric exit codes and approval-record format remain separate design work. The mechanism linking review to unchanged approval content, baseline selection syntax, replacement scope, interrupted-run handling and report formats still need concrete contracts. No additional dependencies, runtime execution, implementation or publication are authorised by this decision.

## Approval reference

On 2026-11-05, the user replied **“Yes but lets use tada as the command, I want to rename the tool to TADA (Test-Assured Development Assistance)”** to “Shall we adopt these five operations and shared rules as the alpha CLI boundary?” This accepts the proposed operations and rules with the explicit naming amendment recorded in ADR 0034.

Related: [tool name](0034-rename-tool-to-tada.md), [alpha boundary](0032-bound-the-first-publishable-alpha.md), [verifier packaging](0033-use-dotnet-local-tool-for-alpha-verifier.md), [design status](../design-status.md), [open questions](../open-questions.md).
