# TADA plugin

The packaged plugin includes its matching verifier package and a local marketplace catalog. Requires Codex with local-plugin support or Claude Code, Windows x64, a stable .NET 10.0 SDK and Git for Windows. Results are local advisory L1.

Extract the plugin ZIP to a persistent directory. From that extracted directory, register it with Codex.

```powershell
codex plugin marketplace add (Get-Location).Path
codex plugin add tada@tada-development
```

The second command installs TADA. The Codex Plugins browser also offers installation from `tada-development`. Enable the plugin and start a fresh chat in the application repository. Describe the project or change normally. The skill prepares the verifier when business verification needs it. Human agreement and approval remain required for business acceptance.

Use the host's plugin controls to disable or re-enable TADA and start a fresh chat after each change. In Codex CLI, `/plugins` opens the browser and Space toggles the installed plugin.

In Claude Code, run `claude --plugin-dir "<extracted directory>"` from the application repository for a session with TADA loaded. The [plugin guide](https://github.com/tayzer/AAATDD/blob/main/docs/plugin.md) covers marketplace installation and the enable and disable commands.

Keep the extracted directory available while it remains a configured local marketplace. Source checkouts acquire the package feed and generated catalog through `scripts/New-TadaPlugin.ps1` before installation.

Read the quickstart, supported trust policy and release notes in the [repository](https://github.com/tayzer/AAATDD).
