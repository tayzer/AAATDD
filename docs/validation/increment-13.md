# Alpha.13 application SDK selection and Claude Code packaging

Date 2026-11-10. Local development validation for the requested workplace adoption changes.

This work was validated as uncommitted changes on `673d6f7` and committed on 2026-11-11 after rebasing onto `main` at `cf267fa`, which had since added multi-level verification, enforcement and a packaged plugin README. The controls below describe the original working tree. The pull request that introduced this record lists what was rerun on the rebased source.

## Changes

New application projects select SDK 10.0.100 with `rollForward: latestFeature` and `allowPrerelease: false`. This admits installed stable .NET 10.0 patches and feature bands. Existing `global.json` files remain byte exact through repeated initialisation. The plugin helper checks SDK selection from the application directory.

The runner delegates the reviewed SDK policy to the .NET host and identifies the actual resolved SDK installation. Each run retains the selected version, installed SDK inventory, platform and observed toolchain identities. Unsupported SDK versions and unavailable policies produce INDETERMINATE. Existing exact pins continue to use the host's exact-selection rules. Editing a captured policy requires a new baseline review and human approval. TADA's source-build SDK remains 10.0.403.

The plugin includes native Claude Code metadata and a generated marketplace alongside Codex metadata. Both hosts use the same skill, setup helper and verifier feed. The CLI and plugin version is `0.1.0-alpha.13`. Installation and toggle commands are in the [plugin guide](../plugin.md).

## Completed local controls

- Locked restore and Release build passed. The full focused/replay suite passed all 142 tests, with zero skipped tests. Before the implementation change, the new onboarding assertion exposed the exact 10.0.403 pin. Four unsupported-SDK controls also failed before their guard was added. These are engineering regression controls.
- Fresh-cache packaged plugin setup and repeated initialisation passed on SDK 10.0.403 and SDK 10.0.103. The latter retained an existing exact SDK policy byte for byte. Reports are under `.artifacts/plugin-controls/1e13a38d30d048609f648836658a9777/` and `.artifacts/plugin-controls/da841a2b1e374cb8b2d776fcef17ddc0/`.
- Claude Code 2.1.322 passed strict native plugin-manifest and marketplace validation. Its plugin-directory advisory reported a missing optional directory listing icon. Logs are under `.artifacts/claude-validation/`.
- Native Claude Code marketplace registration, project-scoped installation, disable and re-enable passed with isolated `CLAUDE_CONFIG_DIR` settings. The cached manifest, skill, helper and verifier package hashes matched the distribution. State observations are under `.artifacts/claude-controls/4f02ca59699f4be9b46c227b341ff46d/`.
- Download-bundle checksum, isolated package installation and repeated project preparation passed. Report is under `.artifacts/bundle-install/bundle install a3129947767c48768ae76d83ed513510/validation.json`.
- Complete installed-package API onboarding passed on both SDK 10.0.403 with the generated roll-forward policy and SDK 10.0.103 with an existing exact policy. Each workflow exercised meaningful HTTP assertion RED, verified GREEN, native Debug/Release tests, refactoring, stable unchanged-rerun identities, retained earlier failure history and Git context handoff. The restored run evidence captured the actual selected SDK. Reports are under `.artifacts/onboarding/69c319e6abce4b2ba1ca71d9a904c19e/validation.json` and `.artifacts/onboarding/09fe2fef94a3468a81cf4d2880df79d2/validation.json`.
- The complete real HTTP/SQLite Orders workflow and integrity controls passed. Report is under `.artifacts/orders-workflow/96d43a68b30e47cbb23dc82eea925546/validation.json`.

The initial restricted restore/build attempts stopped before compiler diagnostics. Unchanged sources built with the required Windows permissions. Infrastructure restrictions were retained in the chat evidence.

## Remaining validation

CI now installs both SDKs and exercises both application policies. Hosted execution of this modified workflow remains pending. The optional native Claude Code CLI used locally is confined to ignored `.artifacts/` and is excluded from distribution.

The local package and plugin artifacts were built from an already dirty working tree. Their parent Git commit identifies historical committed inputs. `.artifacts/alpha13-source-inputs.json` records digests of the changed runtime, setup and CI sources. `.artifacts/alpha13-plugin.json` and `.artifacts/alpha13-bundle.json` record artifact hashes. A publication candidate still requires its selected final committed source and hosted validation.

Ordinary-request discovery, human agreement and useful development in a genuinely fresh Claude Code session remain an experience check. Native plugin state controls establish installation and activation state. Verification retains local advisory L1 scope and its existing observation limits.

## Cursor packaging and standalone Claude Code marketplace

Date 2026-11-13. Source `main` at `42f1429` plus the change that introduced this section.

The shared plugin adds `.cursor-plugin/plugin.json`. Packaging refuses Cursor metadata whose name or version differs from the Codex manifest. The marketplace directory adds `.cursor-plugin/marketplace.json`, and Cursor resolves its `tada` entry through `tada/.cursor-plugin/plugin.json`. The standalone plugin ZIP adds a Claude Code catalog that resolves its own root, so a downloaded ZIP registers as a Claude Code marketplace as well as a Codex marketplace. A Cursor user copies the extracted ZIP to `~/.cursor/plugins/local/tada`.

Completed local controls:

- `dotnet pack` with PowerShell 7.6.1 and the packaged plugin control (`Test-TadaPlugin.ps1 -Archive`) passed on SDK 10.0.403. The control checks all three manifests, the Codex, Claude Code and Cursor catalogs and the shared skill frontmatter, then prepares a disposable application from the extracted ZIP.
- Claude Code 2.1.323 `claude plugin validate` passed for the packaged plugin, its standalone marketplace and the generated marketplace directory. The optional listing-icon advisory remains.
- With an isolated `CLAUDE_CONFIG_DIR`, the extracted standalone ZIP registered as a marketplace, and TADA installed, disabled and re-enabled at user scope.
- With an isolated `CODEX_HOME`, the extracted ZIP registered and installed. `codex debug prompt-input` showed the `tada:tada` skill when enabled for the user, hid it when disabled for the user, and showed it again inside a trusted project whose `.codex/config.toml` enabled it. This confirms the documented off-by-default, on-per-project setup.

Remaining: Cursor plugin loading, its Customize controls and an ordinary-request trial in a fresh Cursor chat have not been exercised; Cursor was not available in this environment. Fresh-session workflow adoption in Claude Code and Cursor remains an experience check. Hosted CI runs the updated plugin control.
