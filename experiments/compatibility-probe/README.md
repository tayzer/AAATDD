# AAATDD technical compatibility probe

Executed under the explicitly approved [probe plan](../../docs/architecture/compatibility-probe-plan.md). See the [evidence report](../../docs/research/compatibility-probe-results.md). These synthetic scenarios do not implement or verify Orders acceptance.

The harness targets `net10.0`, uses exact SDK `10.0.403`, `MSTest.Sdk/4.5.2`, aligned Reqnroll `3.3.4` and Microsoft.Data.Sqlite `10.0.14`. The frozen [candidate lock](packages.lock.json) records resolved dependencies. `TestingExtensionsProfile=None` avoids adding optional coverage/reporting/retry extensions, and `[assembly: DoNotParallelize]` explicitly disables scenario parallelism.

`Run-ProbeStage.ps1` records exact arguments, process-local environment, timestamps, timeouts, stdout/stderr and executable exit status in a fresh evidence directory per stage. Positive and negative stages differ only by an explicit negative-control environment flag. The negative run is expected to return MTP exit code 2 from the intended assertion. It is not an automatic rerun or an unresolved product defect.

`Summarize-ProbeEvidence.ps1` checks this retained pair of technical controls, native lifecycle/reference links and fixture observations. It rejects extra run folders rather than silently aggregating reruns. It is a probe-specific evidence inspection script, not an AAATDD verifier or a complete Cucumber schema validator.

The approved execution sequence was exploratory restore, locked restore, build, discovery, one positive run and one negative control. The first build could not create a Reqnroll task host inside the sandbox; the unchanged build succeeded outside that restriction. Full commands and all attempts are retained under `evidence/`.

SDK binaries, package caches, generated files and probe scratch state are task-local and ignored by this directory's `.gitignore`; they remain on disk for inspection. Reviewable sources, locks and evidence are retained. All owned scenario databases were deleted, and the remaining probe-owned compiler process was stopped after evidence collection. Do not run further stages without fresh authority beyond the completed bounded probe.
