using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reqnroll;

[assembly: DoNotParallelize]

namespace CompatibilityProbe;

[Binding]
public sealed class ProbeBindings(ScenarioContext scenario)
{
    private static int activeScenarios;
    private static readonly object evidenceLock = new();
    private readonly Guid contextId = Guid.NewGuid();
    private readonly HashSet<string> ownedFiles = [];
    private string? value;
    private bool providerExercised;

    private static string EvidenceDirectory =>
        Environment.GetEnvironmentVariable("AAATDD_PROBE_EVIDENCE_DIR")
        ?? throw new InvalidOperationException("Explicit probe evidence directory is required.");

    private void Record(string kind, object data)
    {
        var row = JsonSerializer.Serialize(new
        {
            utc = DateTimeOffset.UtcNow,
            kind,
            contextId,
            scenario = scenario.ScenarioInfo.Title,
            thread = Environment.CurrentManagedThreadId,
            data
        });
        lock (evidenceLock)
            File.AppendAllText(Path.Combine(EvidenceDirectory, "probe-events.jsonl"), row + Environment.NewLine);
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        Directory.CreateDirectory(EvidenceDirectory);
        var active = Interlocked.Increment(ref activeScenarios);
        Record("before", new { active });
        Assert.AreEqual(1, active, "Technical scenarios must not overlap.");
    }

    [Given("an independent probe context")]
    public void GivenContext()
    {
        Assert.IsNull(value, "Scenario state must start empty.");
        Record("context", new { contextId });
    }

    [When("a technical value is recorded")]
    public void RecordValue() => value = contextId.ToString("D");

    [Then("the technical value is observed")]
    public void ObserveValue()
    {
        Assert.AreEqual(contextId.ToString("D"), value);
        if (Environment.GetEnvironmentVariable("AAATDD_PROBE_NEGATIVE") == "1")
            Assert.Fail("PROBE_EXPECTED_ASSERTION_FAILURE: deliberate sensitivity control.");
    }

    private static string ConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString();

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [When("SQLite connection roles are exercised")]
    public void ExerciseProvider()
    {
        var path = Path.Combine(EvidenceDirectory, "probe-" + contextId.ToString("N") + ".sqlite");
        var missingPath = Path.Combine(EvidenceDirectory, "missing-" + contextId.ToString("N") + ".sqlite");
        ownedFiles.Add(path);
        ownedFiles.Add(missingPath);
        foreach (var mode in new[] { SqliteOpenMode.ReadWrite, SqliteOpenMode.ReadOnly })
        {
            var rejected = false;
            try
            {
                using var missing = new SqliteConnection(ConnectionString(missingPath, mode));
                missing.Open();
            }
            catch (SqliteException exception)
            {
                Assert.AreEqual(14, exception.SqliteErrorCode);
                rejected = true;
                Record("missing-file-rejected", new { mode = mode.ToString(), exception.SqliteErrorCode });
            }
            Assert.IsTrue(rejected, "A missing file must not be opened successfully.");
            Assert.IsFalse(File.Exists(missingPath), "Only fixture creation may create a file.");
        }

        using (var fixture = new SqliteConnection(ConnectionString(path, SqliteOpenMode.ReadWriteCreate)))
        {
            fixture.Open();
            Execute(fixture, "CREATE TABLE probe_values (id TEXT PRIMARY KEY NOT NULL, value INTEGER NOT NULL);");
            using var seed = fixture.CreateCommand();
            seed.CommandText = "INSERT INTO probe_values (id, value) VALUES ($id, $value);";
            seed.Parameters.AddWithValue("$id", contextId.ToString("D"));
            seed.Parameters.AddWithValue("$value", 1);
            Assert.AreEqual(1, seed.ExecuteNonQuery());
        }
        using (var writer = new SqliteConnection(ConnectionString(path, SqliteOpenMode.ReadWrite)))
        {
            writer.Open();
            using var transaction = writer.BeginTransaction();
            using var command = writer.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE probe_values SET value = $value WHERE id = $id;";
            command.Parameters.AddWithValue("$value", 2);
            command.Parameters.AddWithValue("$id", contextId.ToString("D"));
            Assert.AreEqual(1, command.ExecuteNonQuery());
            transaction.Commit();
        }
        using (var observer = new SqliteConnection(ConnectionString(path, SqliteOpenMode.ReadOnly)))
        {
            observer.Open();
            using var observation = observer.CreateCommand();
            observation.CommandText = "SELECT id, value FROM probe_values;";
            using (var reader = observation.ExecuteReader())
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual(contextId.ToString("D"), reader.GetString(0));
                Assert.AreEqual(2L, reader.GetInt64(1));
                Assert.IsFalse(reader.Read());
            }
            var rejected = false;
            try { Execute(observer, "UPDATE probe_values SET value = 99;"); }
            catch (SqliteException exception)
            {
                Assert.AreEqual(8, exception.SqliteErrorCode);
                rejected = true;
                Record("read-only-write-rejected", new { exception.SqliteErrorCode });
            }
            Assert.IsTrue(rejected);
            using var version = observer.CreateCommand();
            version.CommandText = "SELECT sqlite_version();";
            Record("sqlite-version", new { version = version.ExecuteScalar() });
        }
        Record("runtime", new
        {
            runtime = RuntimeInformation.FrameworkDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processPath = Environment.ProcessPath,
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => new { name = a.FullName, path = a.Location, sha256 = Hash(a.Location) }).ToArray(),
            native = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                .Where(m => m.ModuleName.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
                .Select(m => new { name = m.ModuleName, path = m.FileName, sha256 = Hash(m.FileName) }).ToArray()
        });
        File.Delete(path);
        Assert.IsFalse(File.Exists(path), "Disposed non-pooled connections must permit deletion.");
        providerExercised = true;
        Record("provider-complete", new { pooling = false, committedValue = 2, databaseDeleted = true });
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Then("provider capabilities and cleanup are recorded")]
    public void ProviderComplete() => Assert.IsTrue(providerExercised);

    [AfterScenario]
    public void AfterScenario()
    {
        var cleanupErrors = new List<string>();
        foreach (var path in ownedFiles)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception exception) { cleanupErrors.Add(exception.Message); }
        }
        var active = Interlocked.Decrement(ref activeScenarios);
        Record("after", new { active, cleanupErrors, error = scenario.TestError?.Message });
        Assert.AreEqual(0, cleanupErrors.Count, "Required probe file cleanup failed.");
    }
}
