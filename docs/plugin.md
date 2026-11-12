# TADA plugin

TADA's plugin supplies the agent workflow. The `tada` CLI supplies project preparation, immutable baseline review and evidence verification. The CLI remains usable by other coding agents and local tooling.

The local plugin lives in `plugins/tada`. Codex and Claude Code share its skill, setup helper and matching verifier feed. The root `plugin.json` supplies the portable Codex manifest and `.claude-plugin/plugin.json` supplies Claude Code metadata. Each host's plugin controls govern ordinary-request workflow selection. [Codex plugin documentation](https://developers.openai.com/plugins/build/plugins) and [Claude Code manifest reference](https://code.claude.com/docs/en/plugins-reference).

## Prepare the local plugin

From the source checkout, build the verifier and package the plugin.

```powershell
dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages
$plugin = & ./scripts/New-TadaPlugin.ps1 -Package .artifacts/packages/Tada.Cli.0.1.0-alpha.13.nupkg
pwsh -File scripts/Test-TadaPlugin.ps1 -PluginRoot $plugin.pluginRoot -Archive $plugin.archive
```

The output contains a standalone plugin ZIP and a local marketplace directory. The marketplace directory includes `.agents/plugins/marketplace.json` for Codex, `.claude-plugin/marketplace.json` for Claude Code, and one shared packaged `tada` folder. The plugin ZIP contains one plugin at its root, including both plugin manifests, its own marketplace catalog, installation README, skill, setup helper, verifier feed and licence notices. Validation extracts that exact ZIP and prepares a disposable application from the extracted plugin.

## Install and toggle in Codex

Register the generated marketplace directory with the installed Codex CLI.

```powershell
codex plugin marketplace add $plugin.marketplaceRoot
```

For a downloaded `TADA-plugin-0.1.0-alpha.12.zip`, extract it to a persistent directory. Register that extracted directory directly. The bundled catalog resolves the plugin at its own root.

```powershell
Expand-Archive -LiteralPath ./TADA-plugin-0.1.0-alpha.12.zip -DestinationPath ./TADA-plugin
codex plugin marketplace add (Resolve-Path ./TADA-plugin).Path
codex plugin add tada@tada-development
```

Open the host's Plugins browser, select the `tada-development` marketplace and install TADA. Use its normal plugin controls to enable or disable it. In Codex CLI, `/plugins` opens the browser and Space toggles an installed plugin. Start a fresh session after changing activation so the trial observes the selected skill set. [Official plugin controls](https://learn.chatgpt.com/docs/plugins)

Installation requires no application folder or project setup. Alpha.10 omits the optional `onboardingSkill` hook that launched the development skill during alpha.9 installation. Open the application you want to work on and describe the task normally. The enabled skill discovers requirements and prepares the local verifier in that project's workspace when business verification needs it. It asks for a folder only if the target is then ambiguous. [Official onboarding hook documentation](https://developers.openai.com/plugins/build/plugins#add-an-onboarding-skill)

The host can also apply project-scoped configuration in a trusted application repository.

```toml
[plugins."tada@tada-development"]
enabled = true
```

Set `enabled = false` for the disabled control. Keep project preparation, accepted features and retained evidence available for future work. Host activation governs automatic TADA orchestration in fresh sessions.

## Install and toggle in Claude Code

Register the generated marketplace and install TADA through Claude Code. Use the `marketplaceRoot` returned by the packaging script, or the absolute path to that directory on the receiving machine.

```powershell
claude plugin validate $plugin.pluginRoot
claude plugin validate $plugin.marketplaceRoot
claude plugin marketplace add $plugin.marketplaceRoot
claude plugin install tada@tada-development --scope project
```

Use `--scope user` for a personal installation. For a temporary session with an extracted standalone plugin ZIP, run `claude --plugin-dir 'C:/path/to/extracted/tada'` in the application directory. Retain the complete plugin directory, including its feed. Claude Code caches marketplace plugins with their local files. [Marketplace guide](https://code.claude.com/docs/en/plugin-marketplaces).

The skill's description allows automatic selection for ordinary software work. `/tada:tada` also invokes it explicitly. Use native activation controls and start a fresh session after changing them.

```powershell
claude plugin disable tada@tada-development --scope project
claude plugin enable tada@tada-development --scope project
```

On Windows, the agent runs the packaged PowerShell helper with `pwsh -NoProfile -File` and absolute quoted paths for the helper, application repository and selected .NET host. Installation itself requires no application folder or verifier setup. [Claude Code skill documentation](https://code.claude.com/docs/en/skills).

## Project preparation and migration

While the plugin is enabled, its skill handles discovery and proportionate work from the user's ordinary request. When business verification needs setup, the packaged helper installs the matching repository-local tool and runs `tada init`. It records the SDK host in new project context. The user supplies the business request and acceptance decisions.

Application setup accepts stable .NET 10.0 SDKs and checks selection from the application directory. New application SDK policy permits patch and feature-band roll-forward within 10.0. Existing `global.json` bytes are preserved. TADA's source build retains SDK 10.0.403 for reproducible development validation. The runner records the resolved SDK and observed toolchain identities. Changing an existing captured SDK policy requires a new reviewed and human-approved baseline.

Alpha.9 moves activation into the plugin. `tada init` removes recognised generated guidance from alpha.7 and alpha.8 agent entry points. It preserves every byte outside that generated text, including file encoding and BOM. Edited legacy sections produce a reconciliation error so activation can be checked before further work. Tool-manifest changes can require a fresh baseline review under exact-byte integrity checks.

## Acceptance

Alpha.11's skill uses the generated fixture context and build-only API dependency to support [native MSTest/MTP execution](native-testing.md), including Visual Studio Test Explorer and `dotnet test`. Existing harness files require a reviewable migration and renewed baseline approval when their bytes change.

Enable TADA and make a normal project request in a fresh session. Observe automatic discovery, human-agreed executable acceptance and verified implementation. Then disable TADA through the same host and open another fresh session. Observe that the plugin skill is absent and automatic TADA orchestration has stopped. Re-enable it and recover the project's accepted behaviour and evidence from another ordinary request.

Package, setup and migration checks cover their respective implementation paths. Apply the [ordinary-request trial](validation/default-workflow-trial.md) separately in Codex and Claude Code to assess native installation, enabled/disabled fresh-session behaviour and requester usefulness. Claude Code manifest validation covers package loading structure. Fresh-session workflow adoption remains an experience check. The source package remains a development alpha with local advisory L1 verification.
