# ADR 0040: Define alpha scope and expected cases

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha `scope` and `expectedCases` shape, logical-ID grammar and ordinary-scenario restriction within Q6. Other inventory field schemas remain open.

## Context

ADR 0039 identifies the seven inventory fields and their ordering policy. Scope and expected cases need concrete identities and traceability rules so missing execution cannot be mistaken for complete verification.

## Decision

Use the following shape for `scope` and `expectedCases`, illustrated with the agreed Orders feature. This fragment is not a complete baseline inventory:

```json
{
  "scope": {
    "requirementIds": ["REQ_DEMO_STOCK_001"],
    "scenarioIds": [
      "AC_DEMO_STOCK_001_01",
      "AC_DEMO_STOCK_001_02"
    ]
  },
  "expectedCases": [
    {
      "caseId": "AC_DEMO_STOCK_001_01",
      "scenarioId": "AC_DEMO_STOCK_001_01",
      "requirementIds": ["REQ_DEMO_STOCK_001"],
      "sourcePath": "docs/requirements/order-stock-validation.feature"
    },
    {
      "caseId": "AC_DEMO_STOCK_001_02",
      "scenarioId": "AC_DEMO_STOCK_001_02",
      "requirementIds": ["REQ_DEMO_STOCK_001"],
      "sourcePath": "docs/requirements/order-stock-validation.feature"
    }
  ]
}
```

Apply these rules:

- Read logical IDs from explicit Gherkin tags; omit the leading `@` in inventory values.
- Use case-sensitive, repository-unique logical IDs matching the entire pattern `[A-Z][A-Z0-9_]*`. Requirement and scenario IDs must not collide. References to the same requirement across cases retain that one logical identity.
- Sort ID arrays ordinally and `expectedCases` by `caseId`. Reject duplicates within these keyed collections.
- For an ordinary scenario, `caseId` equals `scenarioId`.
- Require every scoped scenario to have an expected case. Every scoped requirement must link to at least one expected case.
- Require each `sourcePath` to reference a captured feature file in `files`. Validate the recorded IDs against that file.
- Preserve runner-generated IDs separately for within-run evidence mapping. Names and line numbers never substitute for logical IDs.
- Establish the inventory before filtering. An unexecuted required case prevents `PASS`.

Support ordinary scenarios only in the alpha. Reject Scenario Outlines explicitly until durable example-row identities and expansion rules are designed; never silently omit them.

## Alternatives considered

- Runner-generated IDs, scenario names or line numbers would not provide the agreed stable logical identity across runs and source moves.
- Deriving the required inventory after filtering would hide cases that were required but not selected.
- Supporting Scenario Outlines before defining durable row identities would leave generated-case continuity and completeness ambiguous.

## Consequences and limits

These rules validate traceability and coverage accounting. They cannot establish that scenarios adequately express a requirement.

The sample tags now have an accepted representation under this decision. The retained feature's historical comment calling them proposed remains unchanged as part of preserving the agreed source; this ADR records the later decision. The fragment does not approve an executable Orders baseline or its actual discovery/evidence mapping.

Detailed parsing and tag-role classification, formal schema validation constraints, native-to-logical case mapping, filter configuration and the remaining inventory fields still need design. The grammar does not reserve `REQ_` or `AC_` prefixes for all projects merely because the example uses them.

This decision grants no implementation, runtime execution or publication authority. Existing accepted ADRs and the Orders feature remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Shall we adopt this scope/case schema, ID grammar and ordinary-scenario restriction for the alpha?” The preceding proposal supplied the JSON fragment and rules recorded here, including the traceability limitation.

Related: [inventory structure](0039-define-alpha-inventory-structure-and-ordering.md), [identity roles](0011-separate-logical-baseline-and-run-identities.md), [Orders feature](../requirements/order-stock-validation.feature), [design status](../design-status.md), [open questions](../open-questions.md).
