# TADA plugin

TADA's plugin supplies the agent workflow. The `tada` CLI supplies project preparation, immutable baseline review and evidence verification. The CLI remains usable by other coding agents and local tooling.

The local plugin lives in `plugins/tada`. Codex, Claude Code and Cursor share its skill, setup helper and matching verifier feed. The root `plugin.json` supplies the portable Codex manifest, `.claude-plugin/plugin.json` supplies Claude Code metadata and `.cursor-plugin/plugin.json` supplies Cursor metadata. Packaging refuses mismatched names or versions. Each host's plugin controls govern ordinary-request workflow selection. See the [Codex plugin documentation](https://developers.openai.com/plugins/build/plugins), [Claude Code manifest reference](https://code.claude.com/docs/en/plugins-reference) and [Cursor plugin reference](https://cursor.com/docs/plugins/building).

## Prepare the local plugin

From the source checkout, build the verifier and package the plugin.

```powershell
dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages
$plugin = & ./scripts/New-TadaPlugin.ps1 -Package .artifacts/packages/Tada.Cli.0.1.0-alpha.13.nupkg
pwsh -File scripts/Test-TadaPlugin.ps1 -PluginRoot $plugin.pluginRoot -Archive $plugin.archive
```

The output contains a standalone plugin ZIP and a local marketplace directory. The marketplace directory includes `.agents/plugins/marketplace.json` for Codex, `.claude-plugin/marketplace.json` for Claude Code, `.cursor-plugin/marketplace.json` for Cursor, and one shared packaged `tada` folder. The plugin ZIP contains one plugin at its root, including all three plugin manifests, Codex and Claude Code marketplace catalogs that resolve the ZIP root, an installation README, the skill, setup helper, verifier feed and licence notices. Validation extracts that exact ZIP and prepares a disposable application from the extracted plugin.

The marketplace directory is also the published content of the [`tayzer/tada-plugins`](https://github.com/tayzer/tada-plugins) Git marketplace. The [release guide](releasing.md) describes how releases update it. From alpha.13 the marketplace is named `tada`, so the plugin ID is `tada@tada`. Alpha.12 used `tada@tada-development`.

## Install and toggle in Codex

Register the published Git marketplace, or a generated or extracted marketplace directory, with the installed Codex CLI.

```powershell
codex plugin marketplace add tayzer/tada-plugins      # or a local directory such as $plugin.marketplaceRoot
codex plugin add tada@tada
```

An extracted standalone plugin ZIP also registers directly, because its bundled catalog resolves the plugin at its own root. `codex plugin marketplace upgrade tada` refreshes a Git marketplace; run `codex plugin add tada@tada` afterwards to install the refreshed version.

Open the host's Plugins browser, select the `tada` marketplace and install TADA. Use its normal plugin controls to enable or disable it. In Codex CLI, `/plugins` opens the browser and Space toggles an installed plugin. Start a fresh session after changing activation so the trial observes the selected skill set. [Official plugin controls](https://learn.chatgpt.com/docs/plugins)

Installation requires no application folder or project setup. Alpha.10 omits the optional `onboardingSkill` hook that launched the development skill during alpha.9 installation. Open the application you want to work on and describe the task normally. The enabled skill discovers requirements and prepares the local verifier in that project's workspace when business verification needs it. It asks for a folder only if the target is then ambiguous. [Official onboarding hook documentation](https://developers.openai.com/plugins/build/plugins#add-an-onboarding-skill)

The host can also apply project-scoped configuration in a trusted application repository.

```toml
[plugins."tada@tada"]
enabled = true
```

Set `enabled = false` for the disabled control. Keep project preparation, accepted features and retained evidence available for future work. Host activation governs automatic TADA orchestration in fresh sessions.

## Install and toggle in Claude Code

Register the published Git marketplace (`claude plugin marketplace add tayzer/tada-plugins`), or a generated marketplace directory, and install TADA through Claude Code. For a local directory, use the `marketplaceRoot` returned by the packaging script, or the absolute path to that directory on the receiving machine.

```powershell
claude plugin validate $plugin.pluginRoot
claude plugin validate $plugin.marketplaceRoot
claude plugin marketplace add $plugin.marketplaceRoot
claude plugin install tada@tada --scope project
```

Use `--scope user` for a personal installation. An extracted standalone plugin ZIP is also a marketplace: run `claude plugin marketplace add 'C:/path/to/extracted/tada'`, then install as above. For a temporary session without installing, run `claude --plugin-dir 'C:/path/to/extracted/tada'` in the application directory. Retain the complete plugin directory, including its feed. Claude Code caches marketplace plugins with their local files. [Marketplace guide](https://code.claude.com/docs/en/plugin-marketplaces).

The skill's description allows automatic selection for ordinary software work. `/tada:tada` also invokes it explicitly. Use native activation controls and start a fresh session after changing them.

```powershell
claude plugin disable tada@tada --scope project
claude plugin enable tada@tada --scope project
```

On Windows, the agent runs the packaged PowerShell helper with `pwsh -NoProfile -File` and absolute quoted paths for the helper, application repository and selected .NET host. Installation itself requires no application folder or verifier setup. [Claude Code skill documentation](https://code.claude.com/docs/en/skills).

## Install and toggle in Cursor

Cursor loads a local plugin from a folder under `~/.cursor/plugins/local`. Copy the complete extracted standalone plugin ZIP there. Keep the `feed` folder, because the setup helper installs the verifier from it.

```powershell
Expand-Archive -LiteralPath ./TADA-plugin-0.1.0-alpha.13.zip -DestinationPath "$HOME/.cursor/plugins/local/tada"
```

Quit and reopen Cursor. A window reload may keep a cached plugin list. Check that TADA appears under **Customize**. If it does not, check that **Include third-party Plugins, Skills, and other configs** is enabled in Cursor settings. Some organisations must also allow local plugin imports in their team settings.

Copy the folder rather than linking it. Cursor has reported problems loading symlinked local plugins on Windows. The plugin root must contain `.cursor-plugin/plugin.json` directly; a nested extraction folder will not load.

Teams and Enterprise workspaces can instead import `https://github.com/tayzer/tada-plugins`, whose root holds `.cursor-plugin/marketplace.json`, through **Dashboard → Plugins & MCPs → Add Marketplace → Import from Repo**. Members then install TADA from **Customize**, choosing project or user scope.

Cursor discovers the shared skill from its `name` and `description`. **Customize** lists installed plugins and their controls; start a new chat after any change. A local plugin applies to every workspace, and Cursor does not document a per-project switch for it. To keep TADA off by default in Cursor, install it through a team marketplace with project scope, or remove the local folder when it is not wanted. [Cursor plugin documentation](https://cursor.com/docs/plugins).

## Project preparation and migration

While the plugin is enabled, its skill handles discovery and proportionate work from the user's ordinary request. When business verification needs setup, the packaged helper installs the matching repository-local tool and runs `tada init`. It records the SDK host in new project context. The user supplies the business request and acceptance decisions.

Application setup accepts stable .NET 10.0 SDKs and checks selection from the application directory. New application SDK policy permits patch and feature-band roll-forward within 10.0. Existing `global.json` bytes are preserved. TADA's source build retains SDK 10.0.403 for reproducible development validation. The runner records the resolved SDK and observed toolchain identities. Changing an existing captured SDK policy requires a new reviewed and human-approved baseline.

Alpha.9 moves activation into the plugin. `tada init` removes recognised generated guidance from alpha.7 and alpha.8 agent entry points. It preserves every byte outside that generated text, including file encoding and BOM. Edited legacy sections produce a reconciliation error so activation can be checked before further work. Tool-manifest changes can require a fresh baseline review under exact-byte integrity checks.

## Acceptance

Alpha.11's skill uses the generated fixture context and build-only API dependency to support [native MSTest/MTP execution](native-testing.md), including Visual Studio Test Explorer and `dotnet test`. Existing harness files require a reviewable migration and renewed baseline approval when their bytes change.

Enable TADA and make a normal project request in a fresh session. Observe automatic discovery, human-agreed executable acceptance and verified implementation. Then disable TADA through the same host and open another fresh session. Observe that the plugin skill is absent and automatic TADA orchestration has stopped. Re-enable it and recover the project's accepted behaviour and evidence from another ordinary request.

Package, setup and migration checks cover their respective implementation paths. Apply the [ordinary-request trial](validation/default-workflow-trial.md) separately in Codex, Claude Code and Cursor to assess native installation, enabled/disabled fresh-session behaviour and requester usefulness. Claude Code manifest validation and the packaged Cursor manifest and marketplace checks cover package loading structure. Fresh-session workflow adoption remains an experience check. The source package remains a development alpha with local advisory L1 verification.
