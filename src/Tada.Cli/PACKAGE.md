# TADA

Test-Assured Development Assistance: agent-neutral guidance, Gherkin-first acceptance and local advisory verification. Requires .NET SDK 10.0.403; initial supported harness: Reqnroll 3.3.4, MSTest 4.5.2 and Microsoft.Testing.Platform on Windows x64.

Run `dotnet tool run tada init` to prepare project files. The TADA plugin supplies the agent workflow through the host's enable/disable controls. Describe your project or change normally in a fresh session with the plugin enabled. `baseline prepare`, `baseline review`, `baseline approve`, `run` and `verify` preserve reviewed expectations and retained native evidence. Chores use proportionate existing checks.

This alpha provides local advisory L1 results. A controlling local actor can forge approvals or producer records. Scenario Outlines, partial obligation removal and protected independent assurance are unsupported. See the repository release notes for supported inputs, trust assumptions and installation guidance.
