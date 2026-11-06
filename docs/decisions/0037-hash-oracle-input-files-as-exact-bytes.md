# ADR 0037: Hash oracle input files as exact bytes

Status: **ACCEPTED**. Decision date: 2026-11-05. Resolves the alpha file-content normalisation policy within Q4 and Q6; digest algorithm, identifier syntax and inventory encoding remain open.

## Context

Baseline approval must identify the oracle content actually reviewed and used. During discussion of hashing, the user proposed removing whitespace and line endings before hashing, reasoning that a test run would detect broken Gherkin or unmatched bindings.

Whitespace removal can also hide valid changes to expectations. For example, steps expecting `"out of stock"` and `"outofstock"` can both match the same `{string}` binding while specifying different outcomes. Removing whitespace before hashing makes these changed inputs indistinguishable. A successful run against the changed expectation cannot establish satisfaction of the original approved expectation. Assertion source and other oracle inputs have the same risk.

## Decision

For the alpha, hash captured oracle input files as their exact bytes. Do not remove whitespace, normalise line endings, strip comments, change encodings or otherwise transform file contents before computing their approval-integrity digests.

Whitespace, line-ending, comment and encoding changes therefore change the file's content identity, even when they appear to preserve behaviour. Changed captured input bytes require a new draft and review under ADR 0036; they do not automatically inherit approval.

Test execution complements approval-integrity checks. Matching a binding, compiling or passing tests does not establish that the approved oracle content remained unchanged.

## Alternatives considered

- Removing all whitespace would collapse distinct valid expectations and permit changed assertion content to appear unchanged.
- Normalising only line endings would still change literal content in some inputs and would require an explicitly reviewed equivalence policy.
- Language-aware formatting comparison could simplify future reviews, but does not replace exact-byte approval identity in this alpha. No such comparison feature or automatic approval carry-forward is selected here.

## Consequences and limits

Formatting-only edits can require reapproval. This is the accepted alpha trade-off for a predictable content-identity policy without semantic-equivalence rules. Git checkout or editor conversions must not silently redefine the bytes receiving approval; concrete capture and execution controls still need design.

This decision concerns captured file contents. It does not choose the baseline inventory schema or its deterministic encoding, digest algorithm, identifier syntax, UUID policy, path rules or exclusions. Those parts of the earlier identity proposal remain open. A canonical encoding for a generated inventory would not permit normalising captured source-file contents.

Hashes establish content correspondence within the stated L1 limits, not assertion adequacy, complete input discovery or authentic human approval. This decision authorises documentation and continued design, not implementation or execution.

## Approval reference

On 2026-11-05, after discussing whitespace removal and valid changed expectations that still match bindings and pass tests, the user said **“Ok lets go with byte exact”**. This accepts exact-byte file hashing; it does not approve the remaining identifier and inventory-encoding proposals by implication.

Related: [review-to-approval binding](0036-bind-approval-to-reviewed-baseline.md), [identity roles](0011-separate-logical-baseline-and-run-identities.md), [oracle boundary](0012-bound-the-initial-oracle-to-harness-inputs.md), [design status](../design-status.md), [open questions](../open-questions.md).
