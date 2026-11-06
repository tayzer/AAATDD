# SQLite provider for the first sample

Researched: 2026-11-03. Status: **Research findings**; provider approach subsequently **ACCEPTED** in ADR 0027. ADR 0008 selects a disposable SQLite file per scenario; ADR 0027 selects the .NET provider approach without approving concrete versions or an observation exception. Nothing was installed or executed.

## Findings

- Microsoft's [provider overview](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) describes `Microsoft.Data.Sqlite` as a lightweight ADO.NET provider usable independently of Entity Framework Core, with commands, readers and parameterised SQL.
- The [connection-string documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) documents explicit file paths, `Mode=ReadOnly`, and configurable pooling. The default mode permits creating a missing file, so the observer's concrete open configuration must be reviewed rather than inheriting defaults.

These are documented capabilities, not a tested package/native-version combination or an approved observation exception.

## Provider approach subsequently accepted

[ADR 0027](../decisions/0027-use-microsoft-data-sqlite-with-direct-sql.md) subsequently accepts `Microsoft.Data.Sqlite` with direct parameterised SQL for sample SUT persistence, fixture setup/validation and the distinct read-only order observer. Keep the SUT's persistence implementation separate from harness assertions and observation SQL; sharing a low-level provider does not establish independence and must not lead to copying business-rule logic into the oracle.

Direct SQL keeps the small schema and observation queries reviewable. An ORM or a different ADO.NET provider is an alternative, but no sample requirement currently demonstrates a need for another mapping layer. This choice would not restrict future AAATDD providers.

The minimal schema was subsequently accepted in [ADR 0028](../decisions/0028-use-minimal-inventory-and-orders-schema.md). Actual SQL, provider/native package versions, resolved dependencies, transaction/pooling settings and permissions remain unapproved. Fixture write authority does not confer outcome-observation authority. The narrowly scoped privileged exception still needs concrete provider identity, versions, environment and approval before execution.

## Connection profile subsequently accepted

[ADR 0029](../decisions/0029-use-explicit-sqlite-connection-roles.md) subsequently accepts fixture `ReadWriteCreate`, SUT `ReadWrite` and observer `ReadOnly`, each with `Pooling=False`. The first role owns file creation; the other roles must not create a missing scenario store. Disabling pooling simplifies the first sample's handle-release/cleanup exercise without asserting a general performance policy. Concrete settings and resolved behaviour still need review; role-specific connection modes establish no filesystem isolation or privileged-exception approval.
