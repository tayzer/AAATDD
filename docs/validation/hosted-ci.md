# Hosted Windows release checkpoint

Status: **HOSTED CONTROLS PASSED; REQUESTER ACCEPTANCE PENDING**. This is validation of a development preview, not release publication. The [machine-readable record](hosted-ci.json) preserves source, run, package and bundle identities.

## Source and CI identity

Public head `13c71f15a84664168b47fd99f4ffc5e33d4eb3cf` passes [push run 42257265484](https://github.com/tayzer/AAATDD/actions/runs/42257265484). [PR run 42257273467](https://github.com/tayzer/AAATDD/actions/runs/42257273467) also passes, testing merge commit `03218b070cbf969f67b197dfff23a33fafe86b3b` against main `473c1f99176575f37b282cac37ced1b539899be9`. [PR #1](https://github.com/tayzer/AAATDD/pull/1) remains draft at this checkpoint. Later commits require their own CI; this evidence does not automatically certify a future publication SHA.

The initial hosted attempt failed an environment-isolation control because starting PowerShell exceeded its ten-second deadline. Commit `3298686` replaces only the probe with `cmd /d /s /c` built-ins, disabling AutoRun. The inherited environment positive control returns 42; the replaced environment returns zero and `clean`. The deadline and production isolation remain unchanged. Both successful runs execute the repaired control.

## Downloaded artifact audit

The push artifact, `windows-validation-13c71f15a84664168b47fd99f4ffc5e33d4eb3cf` (ID `13523224800`), was downloaded and inspected separately from GitHub's green status. Its normal retention expires on 2026-11-20; retain local evidence or rerun validation when needed.

- 111 focused/replay tests passed, zero failures and zero skips.
- All 46 Orders controls completed, including intended business failures, opaque-task contracts, transient native loads and evidence-tampering rejection.
- The installed package exercised separate Echo API RED/GREEN/refactor, offline verification, unchanged-rerun identities and a native-passing rerun that correctly retained an earlier TADA FAIL.
- Task-cache byte drift changed environment identity while preserving implementation identity; the original bytes were restored.
- Bundle installation used isolated caches and paths with spaces, preserved existing instructions and created nothing on repeated `init`.
- The correspondence audit checked 23 retained run inventories and 3,159 recorded file digests. This is inspection of relocated records, not complete local replay of path-bound hosted evidence.

The package nuspec records the exact push head. Package SHA-256 is `0347e159c5669523721da1b1a224b94cc356dcfb46d20ac061f81c2766b3d994`; bundle SHA-256 is `889480ff91f7d84e2a9db1a066e9cb89aaa756c8ffa26381049452efe981675a`. Bundle manifest, complete asset set and checksums match, and its embedded package is identical to the separately retained package. Checksums establish correspondence, not publisher authentication.

The seven redistributed DLLs are Tada.Cli, Tada.Core, Tada.BuildLogger, Tada.RuntimeProbe, Gherkin 35.0.0, Cucumber.Messages 29.0.1 and `tada_native_observer.dll`. Packaged third-party notices match committed source. No SDK MSBuild binary, compiler, native test fixture or Reqnroll/MSTest/SQLite example dependency is redistributed in the tool package. Those example dependencies restore separately under their own terms.

The observer DLL matches local SHA-256 `d2cb0c542bef5b69d49fcdfd14da4e83880e9de8f977b542d292e541badd36ba`. Hosted Git expanded the then-unpinned native C source to CRLF: recorded source hash `4c6cd84fc5541ee33e74dbcf9c655ff91dfc83132abf7186989e433006cf4c89` matches that exact checkout expansion of committed LF bytes (`a695f0f7959efcbea66dc7a8cd6976d7d08079ebf81a08209cff52bf639b41c1`). C source is now pinned to LF for subsequent checkouts. This explains the compiler-source record; no oracle hash is normalised or carried forward across changed bytes.

## Remaining release gates

Use the audited preview for requester-led installation, collaborative discovery and agreed Gherkin, API development, defect recovery, refactoring and a genuinely fresh Codex session. Automated control approvals are explicitly labelled fixtures, never human business acceptance. Keep the [release checklist](../release-readiness.md) and [provenance limits](provenance-audit.md) authoritative about unfinished work. Merge and GitHub/NuGet release publication require their applicable authority; none occurred here.
