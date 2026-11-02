# ADR 0005: Validate the local contract before protected L2 execution

Status: **ACCEPTED**. Decision date: 2026-10-26. Resolves Q20.

## Context

The first complete experiment must exercise the specification, harness, approval/integrity checks and evidence together. Requiring protected build/runtime isolation immediately would test the independent-verification guarantee earlier, but adds substantial execution-infrastructure work before the runner/evidence contract has been exercised.

## Decision

- Start with a complete local vertical slice explicitly classified as **L1/advisory**.
- Run the sample SUT outside the harness process to preserve a practical path to L2. This separation alone does not establish independent protection.
- Follow with a protected **L2** slice that validates the build/runtime trust boundary, oracle/policy protection and evidence publication.
- Make successful validation of those L2 controls a prerequisite for independent-assurance claims.

The local experiment can demonstrate integrity checks, required-execution accounting and relevant sensitivity cases. It cannot prevent a same-user process from replacing its own baseline, policy or evidence, and must not claim that guarantee.

## Alternative considered

Require L2 isolation in the first demonstration. This exposes adversarial-boundary failures sooner, but couples the initial contract experiment to trusted-build, credential, workflow and runtime-isolation design. The staged approach keeps the first experiment smaller while leaving L2 as required work before making independent-assurance claims.

## Consequences and limits

The assurance sequence is agreed. The sample's exact business behaviour, executable baseline, observer exception, package/storage choices, evidence schema and detailed slice acceptance criteria remain open or provisional.

This is approval of design staging only. It does not authorise implementing AAATDD, installing dependencies, creating a CLI or changing approved contracts. The project remains in DESIGN ONLY mode.

## Approval reference

On 2026-10-26, the user replied **“i agree with your recommendation”** to the recommendation that the first complete experiment be explicitly L1/advisory, followed by L2, after being asked whether the first demonstration must already establish L2.

Related: [design reconciliation](../architecture/2026-10-26-design-reconciliation.md), [design status](../design-status.md).
