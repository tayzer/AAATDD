# ADR 0038: Use SHA-256 and RFC 8785 for baseline inventories

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha digest algorithm, digest representation and inventory canonicalisation within Q4 and Q6. Detailed inventory schema and array ordering remain open.

## Context

ADR 0037 requires exact-byte hashing of captured oracle input files. A deterministic baseline identity also needs a selected digest algorithm and an unambiguous encoding of the generated inventory. The inventory and captured files have different encoding responsibilities.

## Decision

Use SHA-256 for both hashing steps:

1. Compute each captured input-file digest from that file's exact bytes, as required by ADR 0037.
2. Compute the baseline digest from the inventory's canonical JSON bytes. The inventory records file digests, expected cases, scope, policy and external-input identities.

Represent digests as `sha256:` followed by exactly 64 lowercase hexadecimal characters:

```text
sha256:<64 lowercase hexadecimal characters>
```

For the generated baseline inventory:

- Include a schema version in the hashed content.
- Use the JSON Canonicalization Scheme specified by RFC 8785 to produce deterministic UTF-8 JSON bytes.
- Reject duplicate object keys and unsupported schema versions.
- Define ordering explicitly for inventory arrays. RFC 8785 sorts object properties but preserves array order; it does not supply the application's array-ordering rules.
- Keep the baseline's own digest outside its hashed payload to avoid a circular definition.
- Keep review records, approvals, run records and their timestamps separate from baseline content.

Canonicalisation applies only to the generated inventory. Captured Gherkin, source and configuration files retain exact-byte digests without content normalisation.

## Alternatives considered

- Ordinary JSON serialisation does not by itself establish the agreed canonical encoding across implementations.
- A bespoke deterministic encoding would introduce a new format to specify and validate where an existing specification is available.
- Normalising source-file contents along with the inventory would contradict ADR 0037 and could hide changes to valid expectations.

## Consequences and limits

The implementation must demonstrate RFC 8785 conformance; selecting a JSON serializer is not evidence of conformance. Exact inventory fields, array-ordering rules and their acceptance checks remain the next design work. This decision does not select a canonicalisation dependency, logical-ID grammar, path policy or UUID format.

Digests establish content correspondence under the stated L1 limits, not complete input discovery, assertion adequacy or authenticated approval. This decision grants no implementation, runtime execution or publication authority. Earlier accepted ADRs remain unchanged.

## Approval reference

On 2026-11-05, the user replied **“Yes”** to “Shall we adopt SHA-256, the digest format above and RFC 8785 canonical JSON for baseline inventories?” The preceding proposal specified the two hashing steps, inventory rules and separation from exact-byte source hashing recorded here, while leaving the detailed inventory schema and array-ordering rules open.

## Reference

- [RFC 8785: JSON Canonicalization Scheme](https://www.rfc-editor.org/rfc/rfc8785), especially sections 3.1 and 3.2. Retrieved during the preceding design discussion on 2026-11-05.

Related: [exact-byte file hashing](0037-hash-oracle-input-files-as-exact-bytes.md), [review-to-approval binding](0036-bind-approval-to-reviewed-baseline.md), [identity roles](0011-separate-logical-baseline-and-run-identities.md), [design status](../design-status.md), [open questions](../open-questions.md).
