# Alpha file inventory and path handling

Status: **PROPOSAL — NOT ACCEPTED**. This is the next bounded decision after [ADR 0040](../decisions/0040-define-alpha-scope-and-expected-cases.md). It adds no dependencies or runtime code and grants no execution authority.

## Established constraints

[ADR 0039](../decisions/0039-define-alpha-inventory-structure-and-ordering.md) requires captured repository-relative file paths, roles and digests; keyed-set ordering; duplicate/collision rejection; and explicit external inputs. [ADR 0037](../decisions/0037-hash-oracle-input-files-as-exact-bytes.md) requires exact-byte hashing. [ADR 0038](../decisions/0038-use-sha256-and-rfc8785-for-baseline-inventories.md) selects SHA-256 and RFC 8785 inventory encoding. The proposals below make the `files` field concrete without revising those decisions.

## Proposed entry shape

Use a `files` array keyed by `path`. Each entry has exactly three required fields:

| Field | Type and rule |
| --- | --- |
| `path` | Nonempty repository-relative string using `/` separators, subject to the path rules below. |
| `roles` | Nonempty set of supported role strings, sorted using case-sensitive ordinal comparison with no duplicates. |
| `digest` | SHA-256 of the captured file's exact bytes, represented as `sha256:` plus 64 lowercase hexadecimal characters. |

An illustrative fragment using the current, unchanged Orders feature is:

```json
{
  "files": [
    {
      "path": "docs/requirements/order-stock-validation.feature",
      "roles": ["specification"],
      "digest": "sha256:24c3c0b4b54f5bdd191e51b9577fbfc22f0b2522f95779e5acb903e29f3a2c5d"
    }
  ]
}
```

This is not a complete inventory or an executable-baseline approval. Its digest identifies the current feature bytes only.

Use the alpha roles `specification`, `binding`, `fixture`, `observer`, `driver`, `build`, `configuration`, `policy` and `support`. A file may have multiple roles; all entries receive the same exact-byte integrity checks. Roles explain review responsibilities, not security permissions or discovery exemptions. `support` covers captured helpers/resources that affect execution. Unknown fields or role values are validation errors in the initial supported schema rather than silently ignored extensions. The full inventory schema version remains a separate decision.

Sort `files` by `path` using case-sensitive ordinal comparison. Reject duplicate paths, including entries that merely assign different roles to the same path; merge roles into one entry instead. Reject paths that collide under the supported filesystem's name resolution even when their ordinal strings differ.

## Proposed conservative path policy

Establish one explicit repository root for the operation. Inventory paths identify files relative to that root, not the caller's changing working directory.

- Require canonical relative spelling: reject leading/trailing separators, repeated separators, backslashes and `.` or `..` segments. Do not silently rewrite an inventory path before validation or hashing.
- Reject rooted, drive-qualified, UNC and device paths. Reject Windows alternate data stream syntax and ambiguous Windows names, including reserved device names or segments ending in a dot or space.
- Resolve only existing regular files. Reject directories and missing or unreadable files.
- For this alpha, reject symlinks and reparse points in the selected root or file path rather than following their targets. This is a proposed compatibility restriction, not a claim of isolation; hard links and concurrent local modification remain relevant L1 limits.
- Verify the resolved file stays within the selected repository root. Represent any intended external input under `externalInputs` with its own explicit identity; do not admit it through path traversal.
- Preserve path and content spelling. Do not case-fold or Unicode-normalise paths for hashing; reject ambiguities instead. Collision detection is distinct from the ordinal ordering used to build the inventory.

Source capture and the run preflight must validate the selected material file set as well as listed bytes. A file added to a compile glob cannot be ignored merely because it was absent from an earlier list. Concrete MSBuild/import discovery and the complete selection algorithm remain separate execution/input-contract work. The `files` array alone is not proof that every oracle input was captured.

## Proposed validation examples

These are acceptance examples for the proposed structural rules, not executed tests or duplicated Orders business specifications:

| Input or change | Required handling |
| --- | --- |
| Existing in-root regular feature with its actual exact-byte digest and role `specification` | Valid file entry, subject to the complete baseline checks. |
| Same captured file with CRLF changed to LF, a changed string literal, or a different byte-order mark | Different digest; no approval carry-forward. |
| A duplicate path, unsorted/duplicate roles or unsupported role/field | Reject the invalid entry/collection. |
| An unsorted valid file set during preparation | Generate ordinal path order before canonical inventory encoding. Validate externally supplied inventory order rather than silently repairing approved data. |
| `../outside.cs`, `C:/outside.cs`, `//server/share/file`, `a/../b.cs`, `a//b.cs` or backslash-separated spelling | Reject before accessing the purported input. |
| Two paths resolving to the same Windows name, an alternate stream or reserved device spelling | Reject ambiguity; do not select one silently. |
| A file path through a symlink/junction, or a missing/unreadable file | Reject for this alpha with a specific path/input reason. |
| A new material build input absent from the selected approved inventory | Block approval/run until the material input is captured or explicitly covered by reviewed external-input policy. |

On preparation or approval, structural errors prevent creating a valid approved baseline. Under [ADR 0036](../decisions/0036-bind-approval-to-reviewed-baseline.md), changed or missing approved oracle inputs block execution with `INDETERMINATE`; retain earlier trustworthy findings under ADRs 0009–0010. The exact command error/result representation is still open.

## Alternatives and review point

A single role per file is simpler but misrepresents a source containing both fixture and observer responsibilities. Free-form roles avoid a fixed list but make typos and unsupported meaning easier to overlook. The proposed small role set and role arrays retain clarity without a provider/plugin API.

Following links after checking containment could support more workspaces, but adds resolution and mutation cases before the initial Windows-only workflow is proven. The proposed rejection policy trades that compatibility for a bounded initial contract. It does not prevent a locally controlling actor from changing paths or files during execution; source-to-build/run correspondence still needs concrete checks and validation.

The decision requested is the three-field entry shape, role set and conservative path policy. No implementation, material-input discovery algorithm, general filesystem sandbox or full-baseline approval is implied.
