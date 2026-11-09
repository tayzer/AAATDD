# ADR 0039: Define the alpha inventory structure and ordering

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha top-level inventory fields and collection-ordering policy within Q4 and Q6. Detailed field schemas remain open.

## Context

The alpha uses exact-byte input-file digests and hashes a generated baseline inventory encoded with RFC 8785. RFC 8785 preserves array order, so the inventory needs explicit content and ordering rules beyond JSON object canonicalisation.

## Decision

Use seven top-level inventory fields:

| Field | Content |
| --- | --- |
| `schemaVersion` | Inventory format version. |
| `scope` | Explicit requirement/scenario obligations covered by this baseline. |
| `files` | Captured oracle-input paths, roles and exact-byte SHA-256 digests. |
| `expectedCases` | Complete logical case inventory established before execution filters. |
| `execution` | Reviewed harness entry point, arguments and relevant configuration. |
| `externalInputs` | Identified dependencies, toolchain inputs and explicit trusted-environment policy references. |
| `policy` | Versioned verification rules and required observation-exception references. |

References to policies or exceptions must identify their exact content or revision. A mutable filename or display name alone cannot carry approval.

Keep implementation-under-test identity, actual environment observations, generated binaries, run results, reviews and approvals in their separate records. The baseline captures approved execution requirements; run evidence records what actually executed.

### Collection ordering

- Sort collections representing sets by a documented unique key, such as file path, logical case ID or external-input ID.
- Use case-sensitive ordinal string ordering.
- Reject duplicate keys. Also reject file paths that collide on the supported filesystem.
- Preserve order where it affects execution, such as command arguments.
- Define every array as either an ordered sequence or a keyed set. Never guess during canonicalisation.

### Repository-file paths

For captured repository files, use `/`-separated relative paths. Reject absolute paths and traversal outside the repository; represent external inputs explicitly.

These fields describe the inventory contract, not a claim of automatic discovery of every relevant input. An unresolved required input or unapproved observation exception must block baseline approval.

## Alternatives considered

- Hashing an unstructured file list alone would omit expected cases, scope, execution requirements and verification policy.
- Sorting every array would change meaningful execution sequences such as argument order.
- Leaving set ordering unspecified would allow equivalent inventories to acquire different baseline digests.
- Using mutable policy names alone would let referenced expectations change without identifying a newly reviewed revision.

## Consequences and limits

Detailed field schemas, exact unique-key definitions, schema-version values, path collision and resolution mechanics, logical-ID grammar, reference representation and acceptance checks still need design. This decision does not select a parser, canonicalisation dependency, complete input-discovery mechanism or implementation layout.

The records retain their L1/advisory limits. This decision grants no implementation, additional runtime execution or publication authority. Existing accepted ADRs and the Orders feature remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“yes”** to “Shall we adopt these seven fields and ordering rules as the alpha inventory structure?” The preceding proposal included the seven fields, pinned references, separate execution evidence, ordering and path rules, and approval blocking for unresolved required inputs or unapproved observation exceptions recorded here. Detailed field schemas were explicitly left for later design.

Related: [hashing and canonical encoding](0038-use-sha256-and-rfc8785-for-baseline-inventories.md), [exact-byte inputs](0037-hash-oracle-input-files-as-exact-bytes.md), [review binding](0036-bind-approval-to-reviewed-baseline.md), [oracle boundary](0012-bound-the-initial-oracle-to-harness-inputs.md), [design status](../design-status.md), [open questions](../open-questions.md).
