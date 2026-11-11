# TADA

Test-Assured Development Assistance provides agent-neutral guidance, Gherkin-first acceptance and local advisory verification. Requires a stable .NET 10.0 SDK. The initial supported harness uses Reqnroll 3.3.4, MSTest 4.5.2 and Microsoft.Testing.Platform on Windows x64. New projects permit SDK feature-band and patch roll-forward within 10.0. Existing SDK policies are preserved. Each run records the resolved toolchain identity.

Run `dotnet tool run tada init` to prepare project files. The TADA plugin supplies the agent workflow through the host's enable/disable controls. Describe your project or change normally in a fresh session with the plugin enabled. `baseline prepare`, `baseline review`, `baseline approve`, `run` and `verify` preserve reviewed expectations and retained native evidence. Chores use proportionate existing checks.

This alpha provides local advisory L1 results. A controlling local actor can forge approvals or producer records. Scenario Outlines, partial obligation removal and protected independent assurance are unsupported. See the repository release notes for supported inputs, trust assumptions and installation guidance.
