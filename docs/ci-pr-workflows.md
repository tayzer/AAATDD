# CI and PR workflows

The existing CLI can run an explicitly selected approved baseline in Windows CI and verify its retained evidence. TADA's own source PR already exercises package installation, Orders and Echo scenarios, failure controls and evidence verification. Application repositories need their own workflow, baseline selection and approval policy. The trusted approval integration described below is proposed work.

## Pull request execution

Run on the proposed merge of the PR head and current target branch. Pin .NET SDK 10.0.403 and a trusted TADA package version. Obtain the verifier and required workflow from repository-controlled sources so PR changes to a wrapper, tool manifest or workflow cannot substitute a successful result for verification.

Select an explicit baseline digest whose approval is valid under the repository's CI policy. Run `tada run --baseline <digest> --dotnet <host> --json`, retain its run ID and execute `tada verify --run <id> --json`. A required check succeeds only after both commands report PASS and exit 0. FAIL and INDETERMINATE both block the check and retain their distinct reasons. Failed execution must still upload its diagnostics.

Record the PR head SHA, target SHA, proposed merge SHA, verifier identity, baseline digest, approval identity and run ID in the CI summary and retained artifacts. Every new PR commit needs fresh execution. A target-branch update also requires validation of the new proposed merge.

Implementation-only changes can reuse an approved baseline while its oracle bytes remain current. Changes to features, assertions, fixture support, captured configuration or captured tool metadata require a new comparative review and human approval. Chores and behaviour-preserving refactors retain proportionate checks and applicable existing acceptance.

## Approval authority

Current approval JSON records a declared approver name, and the verifier checks its relationships and captured hashes. Local advisory L1 trusts those records and the surrounding machine. A contributor can edit files in a PR, including approval JSON. CI therefore needs an independent approval source before it treats a new record as authorised.

The proposed integration uses a trusted approval workflow controlled by the target repository. It prepares the candidate baseline comparison and presents its digest, changed oracle files and acceptance text to an authorised human. Approval binds that exact digest to the repository, relevant PR and prior baseline. The trusted workflow produces the approval record or attestation consumed by execution. A changed oracle digest requires another approval. Implementation changes retaining that digest still require fresh CI execution.

The execution job can reuse approvals already accepted on the protected target branch. Candidate approvals introduced by the PR need validation against the independent approval source. Define who may approve through repository permissions or a protected approval environment. Keep PR code execution in a job with permissions appropriate for untrusted code.

This integration must check conflicting amendments against the latest target branch. TADA's current replacement checks preserve requirement and scenario identities within the supplied history. Repository-level reconciliation of concurrent PR approval chains belongs to the proposed adapter.

## Evidence and merge policy

Keep baselines, snapshots, comparative reviews and accepted approval records as durable project records. Store complete run evidence as CI artifacts, including failed attempts. CI artifact retention determines how long offline verification remains possible, so the repository needs an explicit evidence retention policy.

Within a workspace, TADA preserves earlier failures for the same baseline, implementation and environment. A fresh CI job sees only the history supplied to it. The adapter needs a retained history source if it must preserve that rule across separate CI attempts. A rerun of a flaky failure must not silently become the only published evidence.

Require the TADA result through repository rules and retain ordinary code review and merge authority. Business approval of a baseline and approval to merge the PR are separate decisions. The current CLI's hashing and observation retain their documented local advisory L1 limits in CI.
