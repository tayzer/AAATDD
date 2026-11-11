# Approved candidates and active obligations

This guide describes how TADA lets a team agree a specification before implementing it, without breaking mainline CI and without letting an agent switch verification off. The decision is recorded in [ADR 0044](decisions/0044-separate-approval-authority-from-merge-enforcement.md). Obligation kinds are defined in [ADR 0045](decisions/0045-classify-protected-verification-obligations.md).

## Three separate questions

| Question | Where it is answered |
| --- | --- |
| Has a human approved this revision of the expectations? | Baseline review and approval records under `.tada/` |
| Must this approved scenario pass before merge? | `.tada/enforcement.json` |
| Did the implementation satisfy it? | Retained run evidence, assessed by `tada verify` and `tada gate` |

Approval never changes a verification result. A verification result never grants approval. Enforcement status decides only which approved scenarios block a merge.

## The enforcement ledger

`.tada/enforcement.json` is committed with the project. It names one approved baseline and classifies every scenario in it.

```json
{
  "schemaVersion": 1,
  "baseline": "sha256:<approved baseline digest>",
  "active": ["AC_DEMO_STOCK_001_01", "AC_DEMO_STOCK_001_02"],
  "candidates": [
    { "scenario": "AC_DEMO_LIMIT_001_01", "since": "2026-11-11", "reason": "Agreed; bindings pending" }
  ],
  "policy": { "candidateMaxDays": 30 }
}
```

- **Active** scenarios are mandatory merge obligations.
- **Candidates** are approved but not yet implemented. They run with every gate execution and are reported, but do not block.
- `since` records when the candidate was agreed. It cannot be reset later.
- `candidateMaxDays` is optional. When set, an overdue candidate blocks the gate, so pending scope cannot be left behind indefinitely.

Unknown properties, duplicate entries, and a scenario listed as both active and candidate are rejected.

## The gate

```powershell
dotnet tool run tada run --baseline sha256:<digest> --dotnet <host>
dotnet tool run tada gate --run <run-id> --base origin/main
```

`--base` reads the target branch's committed ledger through Git. The pull request cannot alter that reference. A Git failure other than a missing ledger fails closed. Without `--base`, the gate reports `baseBaseline: not-compared` and applies only the local rules. That mode is for local status and agent self-checks. It cannot detect demotion or policy weakening, so a merge check must always pass `--base`. Without `--run`, the gate reports status without executing anything: active obligations are `not-executed` and the result is `INDETERMINATE`.

| Outcome | Exit | Meaning |
| --- | --- | --- |
| `PASS` | 0 | Every active obligation has a verified pass, and every enforcement rule holds. Candidates may still be pending. |
| `NO_CHECKS` | 0 | No active obligation exists and no run was supplied. This is not a behavioural PASS. |
| `FAIL` | 1 | An active obligation failed its intended assertion, now or in an unresolved earlier run of the same implementation and environment. |
| `INDETERMINATE` | 2 | Evidence is missing or invalid, or an enforcement rule is broken. |
| Command error | 3 | The ledger or a referenced record could not be read. |

The gate reports each obligation's state: `satisfied`, `failed` or `unverified` for active scenarios, and `behavioural-red`, `incomplete`, `satisfied-unpromoted` or `not-executed` for candidates. `incomplete` means the candidate lacks valid evidence, for example because its bindings do not exist yet. It is not behavioural RED.

### Rules that block

| Reason | Cause |
| --- | --- |
| `unapproved-baseline`, `oracle-changed` | The ledger baseline is unapproved, superseded, or no longer matches the captured oracle bytes. |
| `unclassified-obligation`, `unknown-obligation` | An approved scenario has no status, or an entry names no approved scenario. |
| `candidate-satisfied-unpromoted` | A candidate passes. Move it to `active` in the same change. |
| `active-obligation-demoted` | An obligation active on the base branch became a candidate or disappeared, and an approved amendment did not change its Gherkin steps, background or tags. Title, narrative and binding edits do not count. |
| `unapproved-lineage` | The baseline does not supersede the base branch's baseline through approved, compared replacements. |
| `candidate-history-changed` | A candidate's `since` date was reset or set in the future. |
| `candidate-overdue` | A candidate exceeded `candidateMaxDays`. |
| `not-executed`, `run-baseline-mismatch` | Active obligations were not executed, or the run assessed a different baseline. |

Run-level evidence problems from `tada verify`, such as invalid Messages, coverage gaps, integrity drift or history gaps, also block.

### Protected changes

With `--base`, the gate lists `protectedChanges` between the base baseline and the current one, by kind: `specification`, `executable-interpretation`, `contract`, `fitness` and `verification-configuration`. It also lists enforcement changes, such as an amended scenario returned to candidate, a withdrawn candidate or a weakened candidate policy. `tada baseline review --against <previous>` shows the same classification during review.

A listed protected change does not by itself block the gate. Mechanical protection comes from approval: the changed inputs already needed a new human-approved baseline. The list exists so that a reviewer with independent authority can confirm that approval. Local approval records are L1 declarations.

## Declaring contract and fitness obligations

Declare protected contract and fitness roots in `tada.json`. Each root must lie inside the captured oracle roots.

```json
"obligations": [
  { "kind": "contract", "roots": ["tests/Acceptance/contracts"] },
  { "kind": "fitness", "roots": ["tests/Acceptance/fitness"] }
]
```

In the current supported stack, these files are used by the approved acceptance harness. For example, a binding can compare a response with an agreed contract file, or assert a latency budget read from a threshold file. Supporting unit tests outside the oracle remain delegated and are not reported.

## Change-type paths

| Change | Typical enforcement path |
| --- | --- |
| Feature | Approve new scenarios as candidates, optionally in a spec-only change. Review bindings and demonstrate behavioural RED. Implement in small increments. Promote each candidate when it passes. |
| Defect | Reuse an active scenario that already exposes the defect, or approve a regression scenario as a candidate. Show RED, fix it, then promote it. |
| Refactor | Keep the baseline and ledger unchanged. Run the gate to show that active obligations still pass. Add a characterisation scenario only when coverage is insufficient. |
| Chore | Keep the baseline and ledger unchanged. Run the gate when the chore can affect behaviour. A gate without a run never reports PASS while active obligations exist. |
| Amendment of active behaviour | Approve a replacement of the baseline. The amended scenario can return to candidate until its implementation passes, and the gate lists that change. Unchanged active scenarios cannot be demoted. |

## Limits

- Removing an approved scenario is not supported. Replacements must preserve existing scenario and requirement identities.
- If a harness also contains non-Gherkin tests that fail, the native runner reports failure without attributing it to a scenario. A run where every active scenario passes but the native runner fails is `INDETERMINATE`. When a candidate also fails, that unattributed failure can be masked. Keep the acceptance harness limited to Reqnroll scenarios.
- The gate enforces rules over committed records. It does not authenticate who approved a baseline. Team CI needs independent authority, such as required code-owner review of `.tada/`, features and declared obligation roots, until a trusted approval adapter exists.
- Failure history comes from local run records. Edited or deleted earlier run records can hide an earlier failure, which is the same local-tampering trust class as approval records.
- A weakened `candidateMaxDays` takes effect only after it merges. The base branch's limit governs the change that weakens it.
- Results remain local advisory L1, with the observation limits described in the [provenance audit](validation/provenance-audit.md).
