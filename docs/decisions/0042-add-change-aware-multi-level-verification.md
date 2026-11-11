# ADR 0042 — Add change-aware multi-level verification

## Status

Accepted implementation direction from the requester's 2026-11-10 planning choices and instruction to implement the resulting plan. Requester acceptance of the implementation remains a separate gate.

## Decision

Classify actual changes during initial discovery and select evidence according to impact and risk. Support mixed feature, fix, refactor and chore work. Reuse adequate checks and justify omitted levels or no test execution. Human approval governs business expectations and executable acceptance. Supporting engineering checks are delegated.

Add optional content-addressed verification plans combining supporting MSTest/MTP projects and existing approved Reqnroll acceptance. Capture supporting inputs without business approval. Preserve lineage, coverage, raw evidence and failure history. Keep lightweight maintenance workflows available through existing tools.

Support several named owned local .NET services in one repository. Preserve normal native test execution, scenario-owned resources and the acceptance/implementation boundary. Capture each service build and require complete cooperative runtime observations. Version new inventories independently of historical schema-6 evidence.

The first delivery uses the existing Windows x64 and pinned .NET stack. Containers, remote deployments, cross-repository coordination and other runner adapters are later increments. Reporting remains local advisory L1. New coordination grants no stronger trust or independent-assurance claim.

## Consequences

Plan preparation cannot approve business expectations. Supporting test changes remain visible in captured engineering identities. Shared acceptance inputs retain their approval requirements. Verified supporting failures block completion while retaining their actual scope. Justified empty selections report NO_CHECKS.

The original accepted ADRs and Orders feature remain historical records. The implementation, user guide and isolated package controls describe the new supported behaviour.
