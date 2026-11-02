# ADR 0011: Separate logical, baseline and execution identities

Status: **ACCEPTED**. Decision date: 2026-10-27. Resolves Q6's identity model; exact encodings and generated-case rules remain open.

## Context

Preserved verification history must distinguish another execution from a changed implementation, environment or approved oracle. File paths, line numbers, display names and runner-generated IDs cannot by themselves supply durable requirement identity and approval revision.

## Decision

| Identity | Responsibility |
| --- | --- |
| Requirement/scenario ID | Explicit, project-unique logical identity, represented through Gherkin tags and retained across file moves or display-name changes |
| Baseline revision | Content-based identity of the reviewed specification/harness/policy artifact set, with Git provenance; approval refers to this revision |
| Implementation/environment identity | Records the identified SUT artifact/source and relevant conditions separately from the approved oracle |
| Run ID | Fresh identity for each individual verification run, linked to the assessed identities and relevant earlier findings/resolutions |
| Native runner IDs | Preserve for within-run traceability and map to logical cases; do not use them as substitutes for durable IDs |

Retaining a logical ID never extends approval to changed authoritative content. Added/deleted cases change the expected inventory, which must be established before execution filtering.

## Alternative considered

Derive identity from paths, source lines or scenario text. This reduces explicit tagging but makes refactoring and historical reconciliation harder. Reusing runner-generated identifiers alone also cannot establish continuity across runs or execution environments.

## Consequences and limits

The identity roles are agreed. Exact tag grammar, cross-project namespaces, digest/canonicalisation rules, manifest encoding and approval carry-forward for non-semantic edits remain open. The current sample tags remain candidates until the concrete schema/mapping is reviewed.

Scenario Outline examples need durable case keys before outline support is claimed. The first sample has no outlines; this decision does not invent their encoding or ignore generated cases.

No baseline has been computed or approved by this decision. The existing Gherkin remains unchanged, and no runtime, dependency installation or implementation is authorised.

## Approval reference

On 2026-10-27, the user replied **“yrs”**, interpreted as an affirmative response in context, to: “Should we use stable explicit requirement/scenario IDs, separate content-based baseline revisions, and fresh IDs for each verification run?” The preceding proposal also separated implementation/environment identity from the oracle.

Related: [ADR 0010](0010-preserve-failures-across-explicit-reruns.md), [baseline proposal](../architecture/first-experiment-baseline-proposal.md).
