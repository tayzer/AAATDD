# TADA plugin

TADA's plugin supplies the agent workflow. The `tada` CLI supplies project preparation, immutable baseline review and evidence verification. The CLI remains usable by other coding agents and local tooling.

The local plugin lives in `plugins/tada`. It contains one implicitly selectable skill and a setup helper. Distribution adds the matching verifier package as a local feed. The host's plugin controls govern whether the skill is available for ordinary software requests. Codex supports enabling and disabling local plugins through its plugin browser and project configuration. [Official plugin documentation](https://developers.openai.com/plugins/build/plugins)

## Prepare the local plugin

From the source checkout, build the verifier and package the plugin.

```powershell
dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages
$plugin = & ./scripts/New-TadaPlugin.ps1 -Package .artifacts/packages/Tada.Cli.0.1.0-alpha.9.nupkg
pwsh -File scripts/Test-TadaPlugin.ps1 -PluginRoot $plugin.pluginRoot
```

The output contains a standalone plugin ZIP and a local marketplace directory. The marketplace directory includes `.agents/plugins/marketplace.json` and the packaged `tada` folder. The plugin ZIP contains one plugin at its root, including the skill, setup helper, verifier feed and licence notices.

## Install and toggle

Register the generated marketplace directory with the installed Codex CLI.

```powershell
codex plugin marketplace add $plugin.marketplaceRoot
```

Open the host's Plugins browser, select the `tada-development` marketplace and install TADA. Use its normal plugin controls to enable or disable it. In Codex CLI, `/plugins` opens the browser and Space toggles an installed plugin. Start a fresh session after changing activation so the trial observes the selected skill set. [Official plugin controls](https://learn.chatgpt.com/docs/plugins)

The host can also apply project-scoped configuration in a trusted application repository.

```toml
[plugins."tada@tada-development"]
enabled = true
```

Set `enabled = false` for the disabled control. Keep project preparation, accepted features and retained evidence available for future work. Host activation governs automatic TADA orchestration in fresh sessions.

## Project preparation and migration

While the plugin is enabled, its skill handles discovery and proportionate work from the user's ordinary request. When business verification needs setup, the packaged helper installs the matching repository-local tool and runs `tada init`. It records the SDK host in new project context. The user supplies the business request and acceptance decisions.

Alpha.9 moves activation into the plugin. `tada init` removes recognised generated guidance from alpha.7 and alpha.8 agent entry points. It preserves every byte outside that generated text, including file encoding and BOM. Edited legacy sections produce a reconciliation error so activation can be checked before further work. Tool-manifest changes can require a fresh baseline review under exact-byte integrity checks.

## Acceptance

Enable TADA and make a normal project request in a fresh session. Observe automatic discovery, human-agreed executable acceptance and verified implementation. Then disable TADA through the same host and open another fresh session. Observe that the plugin skill is absent and automatic TADA orchestration has stopped. Re-enable it and recover the project's accepted behaviour and evidence from another ordinary request.

Package, setup and migration checks cover their respective implementation paths. Native host installation, enabled/disabled fresh-session behaviour and requester usefulness require the [ordinary-request trial](validation/default-workflow-trial.md). The source package remains a development alpha with local advisory L1 verification.
