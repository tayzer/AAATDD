# CI and PR workflows

The existing CLI can run an explicitly selected approved baseline in Windows CI and verify its retained evidence. TADA's own source PR already exercises package installation, Orders and Echo scenarios, failure controls, the candidate/active enforcement lifecycle and evidence verification. Application repositories need their own workflow, baseline selection and approval policy. The trusted approval integration described below is proposed work.

## TADA source repository

The `PR validation` workflow in `.github/workflows/ci.yml` validates pull requests targeting `main`, pushes to `main`, merge-group candidates and manual dispatches. Pull requests use GitHub's proposed merge checkout. There are no path filters or draft exclusions. A newer run for the same ref cancels the older run.

`Windows validation` uses Windows Server 2025, the pinned .NET SDK 10.0.403 and locked dependency restores. It builds and runs focused/replay tests, packages the CLI and plugin, executes Orders business/integrity controls and the enforcement lifecycle controls, installs the package into a fresh cache for the second API, checks task-only environment identity and installs the download bundle. Actions are pinned to commit SHAs, checkout credentials are removed and the workflow token has read-only repository contents permission. Validation does not need repository secrets.

The stable required check is `PR checks`. It runs after Windows validation even when that job fails or is skipped, and succeeds only when Windows validation succeeds. Keep its name in sync with `.github/main-branch-protection.json`. Add future mandatory jobs to its dependencies and result checks before relying on them as merge gates.

Every attempt retains its available packages and evidence for 14 days, including failed attempts. The CI identity artifact and job summary record the repository, event, tested commit, PR head/base commits, run ID and attempt. Download evidence before expiry when longer retention is required.

The main-branch protection configuration requires an up-to-date PR with the GitHub Actions `PR checks` result and resolved conversations. Administrators follow this policy. Force pushes and branch deletion are blocked.

The separate `Main code-owner review` ruleset requires one approving code-owner review for other contributors. New commits dismiss stale approvals, and someone other than the latest reviewable pusher must approve the latest push. Repository administrators may bypass this review ruleset when merging a PR. The separate branch protection continues to enforce the PR, CI and conversation requirements for administrators.

`.github/CODEOWNERS` names `@tayzer` as the repository-wide code owner. At setup, `tayzer` is the sole repository administrator. GitHub prevents authors from approving their own PRs, so owner self-service merging uses the review-only administrator bypass. GitHub's ruleset bypass follows the administrator role. Adding names to CODEOWNERS does not grant that bypass, and adding administrators extends it.

Apply the reviewed configuration using an authenticated repository administrator, then read back the resulting policy.

```powershell
gh api --method PUT repos/tayzer/TADA/branches/main/protection --input .github/main-branch-protection.json
gh api repos/tayzer/TADA/branches/main/protection
# Create the review ruleset once. For updates, use PUT with its returned ID.
gh api --method POST repos/tayzer/TADA/rulesets --input .github/main-review-ruleset.json
gh api repos/tayzer/TADA/rulesets
```

Repository administrators can change this configuration. Review workflow and policy changes as executable infrastructure. The source CI trusts code-owner review or the owner's review bypass for those changes, and its automated fixture approvals remain isolated controls. The independent business-approval adapter for application repositories remains proposed below.

## Pull request execution

Run on the proposed merge of the PR head and current target branch. Pin .NET SDK 10.0.403 and a trusted TADA package version. Obtain the verifier and required workflow from repository-controlled sources so PR changes to a wrapper, tool manifest or workflow cannot substitute a successful result for verification.

Read the baseline digest from the committed `.tada/enforcement.json`. Run `tada run --baseline <digest> --dotnet <host> --json`, retain its run ID and execute `tada gate --run <id> --base origin/<target-branch> --json`. The required check succeeds only when the gate reports PASS and exits 0. FAIL and INDETERMINATE both block the check and retain their distinct reasons. Failed execution must still upload its diagnostics.

The gate separates approval from enforcement under [ADR 0044](decisions/0044-separate-approval-authority-from-merge-enforcement.md). Active obligations must pass. Approved candidates run in the same execution and are reported as non-blocking diagnostics with their age, so a spec-only PR can merge agreed scenarios before their implementation exists. A candidate that passes must be promoted to active in the same PR. Compared with the target branch, an active obligation cannot be demoted unless an approved amendment changed that scenario, and a new baseline must supersede the target's baseline through approved replacements. See the [enforcement guide](enforcement.md) for every rule.

`tada verify --run <id>` still assesses every scenario in the run. While candidates are pending it reports FAIL or INDETERMINATE for them, so use the gate, not `verify`, as the merge check.

```yaml
- name: TADA gate
  shell: pwsh
  run: |
    $ledger = Get-Content .tada/enforcement.json -Raw | ConvertFrom-Json
    $run = dotnet tool run tada run --baseline $ledger.baseline --json | ConvertFrom-Json
    dotnet tool run tada gate --run $run.runId --base "origin/${{ github.base_ref }}" --json | Tee-Object .artifacts/tada-gate.json
    exit $LASTEXITCODE
```

Fetch the target branch before the gate, for example with `fetch-depth: 0` on checkout. Publish the gate JSON with the run evidence. Its `obligations` list shows active and candidate states, and `protectedChanges` lists changed specification, executable-interpretation, contract, fitness, configuration and enforcement items for independent review.

Record the PR head SHA, target SHA, proposed merge SHA, verifier identity, baseline digest, approval identity and run ID in the CI summary and retained artifacts. Every new PR commit needs fresh execution. A target-branch update also requires validation of the new proposed merge.

Implementation-only changes, refactors and behaviour-neutral chores can reuse an approved baseline and ledger while its oracle bytes remain current. Changes to features, assertions, fixture support, captured configuration or captured tool metadata require a new comparative review and human approval. Chores and behaviour-preserving refactors retain proportionate checks and applicable existing acceptance.

## Approval authority

Current approval JSON records a declared approver name, and the verifier checks its relationships and captured hashes. Local advisory L1 trusts those records and the surrounding machine. A contributor can edit files in a PR, including approval JSON. CI therefore needs an independent approval source before it treats a new record as authorised.

Until that integration exists, require code-owner review for `.tada/`, feature files, acceptance harness sources and declared contract and fitness roots, and review the gate's `protectedChanges` list before approving the PR. This makes protected changes visible to an independent reviewer. It is still not a binding of approval to a baseline digest.

The proposed integration uses a trusted approval workflow controlled by the target repository. It prepares the candidate baseline comparison and presents its digest, changed oracle files and acceptance text to an authorised human. Approval binds that exact digest to the repository, relevant PR and prior baseline. The trusted workflow produces the approval record or attestation consumed by execution. A changed oracle digest requires another approval. Implementation changes retaining that digest still require fresh CI execution.

The execution job can reuse approvals already accepted on the protected target branch. Candidate approvals introduced by the PR need validation against the independent approval source. Define who may approve through repository permissions or a protected approval environment. Keep PR code execution in a job with permissions appropriate for untrusted code.

This integration must check conflicting amendments against the latest target branch. TADA's current replacement checks preserve requirement and scenario identities within the supplied history. Repository-level reconciliation of concurrent PR approval chains belongs to the proposed adapter.

## Evidence and merge policy

Keep baselines, snapshots, comparative reviews and accepted approval records as durable project records. Store complete run evidence as CI artifacts, including failed attempts. CI artifact retention determines how long offline verification remains possible, so the repository needs an explicit evidence retention policy.

Within a workspace, TADA preserves earlier failures for the same baseline, implementation and environment. A fresh CI job sees only the history supplied to it. The adapter needs a retained history source if it must preserve that rule across separate CI attempts. A rerun of a flaky failure must not silently become the only published evidence.

Require the TADA result through repository rules and retain ordinary code review and merge authority. Business approval of a baseline and approval to merge the PR are separate decisions. The current CLI's hashing and observation retain their documented local advisory L1 limits in CI.
