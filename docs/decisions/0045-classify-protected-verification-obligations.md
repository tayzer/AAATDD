# ADR 0045: Classify protected verification obligations

Status: **ACCEPTED — IMPLEMENTATION DECISION UNDER [ADR 0041](0041-authorise-autonomous-release-implementation.md)**. Decision date: 2026-11-11.

## Context

Every file in a captured TADA oracle is already approval-bound. Changing any captured byte invalidates the approved baseline. A reviewer still needs to know what a change means: whether an edit alters agreed business behaviour, a service contract, an architectural threshold, the executable interpretation of a scenario, or only runner configuration. Without that classification, a weakened performance budget can look like an ordinary test edit.

Not every test needs human approval. Unit tests and implementation-focused integration tests support engineering work. Requiring approval for each would add ceremony without protecting agreed behaviour.

## Decision

Protection depends on the requirement a check expresses, not on its file extension or test framework. TADA recognises these obligation kinds:

| Kind | Examples | Protection |
| --- | --- | --- |
| `specification` | Gherkin `.feature` files | Approval-bound; authoritative for functional behaviour |
| `executable-interpretation` | Bindings, assertions, fixtures, observers, drivers | Approval-bound; reviewed as the executable meaning of agreed expectations |
| `contract` | Agreed producer/consumer expectations | Approval-bound when declared; optional per project |
| `fitness` | Agreed NFR thresholds and architecture rules | Approval-bound when declared |
| `verification-configuration` | `tada.json`, SDK pins, lock files, project and runner configuration | Approval-bound |
| `enforcement`, `enforcement-policy` | Enforcement status and candidate policy (ADR 0044) | Constrained by gate rules; weakening is reported |
| Supporting engineering tests | Unit and implementation-level tests outside the oracle | Delegated; not individually approved |

Projects declare contract and fitness roots in `tada.json` under `obligations`, for example `[{ "kind": "contract", "roots": ["tests/Acceptance/contracts"] }]`. Declared roots must lie inside the captured oracle, so a declaration cannot point protection at uncaptured files. The declaration is itself part of the captured configuration, so adding, moving or removing it is a reviewed change.

`tada baseline review --against` and `tada gate --base` report changes as `added`, `changed`, `removed` or `reclassified`, by kind. Moving a protected file outside a declared root is reported as a reclassification of its previous kind.

Non-functional requirements and fitness functions are part of requirements discovery, not an afterthought. Where an objective, executable check exists, record and protect its threshold. Where no meaningful automated check exists, record the required human review or other evidence rather than manufacturing automated assurance.

## Consequences

- Reviewers can see when a change alters a contract or fitness threshold, even though the mechanical protection, exact-byte capture and approval, is unchanged.
- In the current supported stack, protected contract and fitness checks run through the approved Reqnroll/MSTest acceptance harness. Running separately captured MSTest suites as protected obligations would build on the verification plans proposed in PR #5 and remains future work.
- Supporting engineering tests outside the oracle can change without approval. They cannot weaken protected obligations, because protected inputs are captured separately.
- Classification uses path conventions and declarations. It does not decide whether an assertion is semantically adequate. Human review of the executable interpretation remains necessary.
