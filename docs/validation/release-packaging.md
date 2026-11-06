# Local source and distribution checkpoint

Status: **LOCAL PREVIEW VALIDATED; RELEASE INCOMPLETE**. The [machine-readable record](release-packaging.json) links the source snapshot, package, download bundle and control reports. No public source upload, GitHub release or NuGet publication is established by this checkpoint.

## Distribution

The [bundle scripts](../release-bundles.md) create a ZIP containing the tool package, installation/agent-start guidance, MIT licence, packaged dependency notices, a complete file manifest and checksums. The redistributed DLL set is explicitly checked; no SDK MSBuild binary, compiler or native test fixture belongs in the package. Bundle preparation and installation are also part of the CI workflow.

Installation from the extracted feed succeeds with isolated package/resolver caches and paths containing spaces. Existing instructions remain unchanged; workflow/context scaffolding is present; repeated `init` creates nothing. The control suppresses SDK global-tool PATH updates and checks the user PATH remains unchanged. A corrupted package is rejected by the bundle checksum before any SDK command starts. Checksums establish correspondence, not publisher authentication.

## Source rebuild

An exact working-tree source snapshot initially contained no build outputs, package cache or native compiler cache. Locked restoration, native compiler bootstrap and release compilation succeeded with new NuGet/CLI/compiler caches. The installed .NET SDK remained the documented prerequisite on this host; this is not a clean-machine claim.

The first deep-path Git round-trip failed because Git's default Windows checkout path limit was exceeded. Test commands now supply `core.longpaths=true`; the quickstart and bundle guidance document the repository-local setting. No global Git setting was changed. All 111 focused tests then passed with zero skips.

The rebuilt package passes all 46 Orders controls and fresh installed-package Echo RED/GREEN/refactor, unchanged-rerun identity, real failure-history, maintenance and Git-handoff controls. A task-only cache-byte change creates a distinct environment and the original bytes are restored. The cold native bootstrap produces the same observer SHA-256 as the earlier validated build.

## Source delivery and remaining gates

Prepare the review branch in the owned source snapshot, preserving the original checkout. Git's checked-in attributes normalise active source/documentation line endings; accepted ADRs and the original Orders feature remain byte-exact. Recheck the resulting committed tree before requesting its first public upload. Package metadata from a dirty working tree is not final source provenance; hosted CI must build and validate the final committed source.

The public repository's main branch still contained only the original design commit when checked. The public NuGet index returned no `Tada.Cli` package at the recorded observation time; this neither reserves that identity nor grants publication permission.

Remaining work includes final-source hosted CI, clean supported-environment validation, the [open provenance boundary](provenance-audit.md), genuine fresh-agent continuation and requester/Codex API acceptance. Automated fixture approvals remain separate from human business acceptance.
