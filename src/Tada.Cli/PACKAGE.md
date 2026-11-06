# TADA

Test-Assured Development Assistance: agent-neutral guidance, Gherkin-first acceptance and local advisory verification. Requires .NET SDK 10.0.403; initial supported harness: Reqnroll 3.3.4, MSTest 4.5.2 and Microsoft.Testing.Platform on Windows x64.

Run `dotnet tool run tada init`, then read `.tada/WORKFLOW.md`. Discover business examples with the human before implementing them. `baseline prepare`, `baseline review`, `baseline approve`, `run` and `verify` preserve reviewed expectations and retained native evidence. Chores use proportionate checks without invented scenarios.

This is an unreleased development alpha. Results are L1/advisory, not protected independent assurance. A controlling local actor can forge approvals or producer records. Scenario Outlines and partial obligation removal are unsupported.
