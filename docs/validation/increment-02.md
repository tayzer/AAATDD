# Alpha.2 integrity, Git and install-to-API checkpoint

Status: **VERIFIED DEVELOPMENT PROGRESS; RELEASE INCOMPLETE**. The [machine-readable record](increment-02.json) pins package/source hashes, test results, retained reports, run IDs and the local control context commit. Earlier increments remain historical snapshots.

## Verified changes

- 57 tests pass with zero skips, covering canonicalisation, paths, review/approval, real native-message replay, process deadlines, Git round trips, failure history, static resources and CLI behaviour.
- Passing reruns re-evaluate earlier native evidence instead of trusting cached summaries. Missing/edited summaries cannot clear a native failure. Interrupted or damaged history blocks PASS; a trustworthy earlier failure remains FAIL alongside other gaps. A changed identified implementation or resolved setup gap can produce valid new satisfaction.
- Snapshot publication is staged and atomic. Repeated preparation is idempotent. Approved snapshot bytes and the current oracle survive commit/clone with `core.autocrlf=true`; generated attributes protect state independently of existing root attributes.
- Review prints captured Gherkin, changed/removed inputs and removed cases. Default results explain blockers; `--json` is explicit. Approval-option parsing cannot consume a flag as an approver name. Terminal control sequences in diagnostics are escaped without changing stored evidence.
- Static artifact integrity covers non-code resources, not just binaries. Changed or added CSV expectations cannot pass. Native Messages and MTP diagnostics have explicit reserved output locations. Scenario-generated state belongs outside immutable build outputs.
- Sixteen Orders controls pass, including two independent business defects: oversubscription and incorrect persisted quantity. Correct behaviour, offline verification, wrong seed, cleanup gap, oracle edits, stale review and corrupted/incomplete evidence produce the expected outcomes.

## Installed-package development experience

The unchanged alpha.2 core/package was installed into a new project with a fresh NuGet cache. The script established assertion RED against a working HTTP host returning 501, implemented the Echo endpoint and established GREEN under the same approved oracle. It then extracted the response function as a behaviour-preserving refactor and reran the unchanged oracle successfully. A warn-as-error build passed without adding another business scenario.

The three implementation identities are distinct while the baseline remains the same. The handoff records RED, GREEN and refactor run IDs and was committed/read back in a local control Git repository. These are labelled automated demonstration approvals, not requester business acceptance. This is a stronger automated proxy for the intended agent workflow, not proof that the requester has used Codex with it.

The first handoff readback checker incorrectly applied PowerShell `-join` to a command invocation without parentheses. The API stages and context commit had succeeded; inspecting the committed content proved the checker defect. Parenthesising the invocation fixed it, and the complete script then passed. All earlier test attempts remain local evidence.

## Provenance finding and remaining gates

The installed MSBuild supports post-target `-getItem` output redirected with `-getResultOutputFile`. A controlled query collected 13 compile items, 8 analyzers and 169 reference paths. Preprocessing exposed 114 distinct existing file-path markers while `MSBuildAllProjects` contained only 2 entries. That property alone cannot establish complete import coverage. These are feasibility observations, not a shipped input-audit implementation or exhaustive discovery proof.

Complete execution/import/loaded-runtime provenance, final-source CI, clean-machine validation and the actual requester/Codex experience remain open. The public NuGet index returned 404 for `Tada.Cli`; this does not establish ownership or publication rights. Nothing was publicly released. See the [live release checklist](../release-readiness.md) and [quickstart](../quickstart.md).
