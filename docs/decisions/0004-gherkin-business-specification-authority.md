# ADR 0004: Make Gherkin authoritative for business behaviour

Status: **ACCEPTED**. Decision date: 2026-10-26. Supersedes no prior ADR.

## Context

The original charter treated established executable DSLs as peers under one verification contract. The user's Architecture & Design Update explicitly makes Gherkin the primary, authoritative business specification. This changes the earlier charter direction without invalidating the common verification contract or the three existing lifecycle ADRs.

## Decision

- Human-approved Gherkin expresses authoritative business expectations for features and business-rule changes.
- Supporting Markdown records rationale, architecture, decisions and context rather than duplicating the functional specification.
- OpenAPI, Pact, fitness functions and unit/integration tests provide complementary verification. They are not competing sources of business-specification authority.
- Use established Gherkin parsing and execution infrastructure. Reqnroll is the currently favoured first integration, not a final package/version selection.
- Keep runner adapters distinct from protocol drivers: adapters handle discovery, execution and results; drivers perform technical interactions with the SUT.
- Specification portability does not imply that runner-specific bindings or driver implementations are portable across languages.

ADR 0003 remains applicable: a chore or refactor need not introduce a new business scenario when another appropriate means can evaluate its success. ADR 0001's intent and executable-baseline approval scopes remain distinct. Gherkin text alone does not approve its executable interpretation.

## Alternatives and rationale

The previous equal-DSL business-specification model offers flexibility, but can create competing business authorities and duplicated requirements. Gherkin as the business authority preserves a single place for agreed behaviour while retaining specialised verification tools for their strengths.

Using lengthy Markdown functional specifications alongside equivalent Gherkin would create another reconciliation burden. Supporting documents should link to the authoritative scenarios instead.

## Consequences and open details

The active charter and lifecycle must reflect this hierarchy. Historical research and ADRs remain unchanged. The provisional hybrid execution model still needs an exercised contract, runner capability checks and stable scenario/Scenario Outline identities.

This decision approves neither a custom runner nor implementation work. It does not claim that syntax, successful parsing or a passing runner establishes correct bindings, complete execution or semantic adequacy.

## Approval reference

The user-supplied **AAATDD — Architecture & Design Update**, received 2026-10-26, section 2 explicitly states that Gherkin is authoritative for business behaviour. Sections 3, 10 and 12 preserve runner separation, adaptive workflows and the common verification direction. The update authorises reconciliation and design documentation, not implementation.

Related: [design reconciliation](../architecture/2026-10-26-design-reconciliation.md), [charter](../vision.md), [ADR 0003](0003-support-change-appropriate-specification-artifacts.md).
