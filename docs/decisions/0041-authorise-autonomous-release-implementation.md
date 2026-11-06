# ADR 0041: Authorise autonomous release implementation

Status: **ACCEPTED — DIRECT USER INSTRUCTION**. This records the new active goal, not an inferred approval of earlier proposals.

The user explicitly instructed autonomous research, implementation, tests, review and improvement toward a usable first open-source release. This replaces the earlier design-only authority and repeated architectural approval checkpoints. Routine implementation and architecture decisions are now delegated. Human agreement on business acceptance expectations remains necessary; agents must not silently weaken approved behaviour.

The release experience includes agent-independent workflows for features, defects, chores and refactors; real Gherkin/Reqnroll/MSTest/MTP execution; proportionate quality checks; Git-native context; installation and onboarding; and use with Codex to build an API from scratch. An implementation that merely compiles or passes internal tests is insufficient.

Preserve accepted behavioural and integrity semantics, exact-byte oracle hashing and local L1/advisory claims. Implementation details previously left open may now be resolved and tested without individual user approval. The new goal allows pragmatic onboarding commands in addition to the original five operations and proportionate non-Gherkin maintenance paths. Historical ADRs remain unchanged as the decision record.

Public package/repository mutations, unavailable permissions, genuine business acceptance decisions and material blockers still require their applicable authority. Do not report independent L2 assurance or a complete release without evidence for the actual user experience.

Related: [release checklist](../release-readiness.md), [alpha boundary](0032-bound-the-first-publishable-alpha.md).
