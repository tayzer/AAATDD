using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tada;

public sealed class TadaException(string message) : Exception(message);
public sealed record FileInput(string Path, string[] Roles, string Digest);
public sealed record Scope(string[] RequirementIds, string[] ScenarioIds);
public sealed record ExpectedCase(string CaseId, string ScenarioId, string[] RequirementIds, string SourcePath);
public sealed record Execution(string Project, string Assembly, string MessagesFile, int TimeoutSeconds = 120,
    string? SutProject = null, string? SutAssembly = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Service[]? Services = null)
{
    public Service[] Applications() => Services ?? (SutProject == null ? [] : [new Service("sut", SutProject, SutAssembly!)]);
    public string Output(Service service) => Services == null ? "sut" : "sut/" + service.Id;
}
public sealed record Service(string Id, string Project, string Assembly);
public sealed record Engineering(Config Configuration, string[] RequiredTests);
public sealed record ExternalInput(string Id, string Digest);
public sealed record Policy(string Assurance = "L1-advisory", bool AutomaticRetries = false,
    string MessagesProtocol = "30.1.0",
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BuildInputs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RuntimeInputs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BuildTasks = null);
public sealed record Inventory(int SchemaVersion, Scope Scope, FileInput[] Files, ExpectedCase[] ExpectedCases,
    Execution Execution, ExternalInput[] ExternalInputs, Policy Policy,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Engineering? Engineering = null);
public sealed record Config(int SchemaVersion, string[] OracleRoots, Execution Execution,
    string RequirementPrefix = "REQ_", string ScenarioPrefix = "AC_", string[]? ImplementationRoots = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BuildTaskContract[]? BuildTaskContracts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ObligationRoot[]? Obligations = null);
/// <summary>Declares captured oracle roots that express agreed contract or fitness obligations.</summary>
public sealed record ObligationRoot(string Kind, string[] Roots);
public sealed record BuildTaskContract(string Origin, string Target, string Task, string[] InputRoots);
public sealed record Review(string Id, string Baseline, DateTimeOffset Created, string? ComparedTo);
public sealed record Approval(string Id, string Review, string Baseline, string By, DateTimeOffset Created,
    string? Replaces);
public sealed record Reason(string Code, string Detail, string? CaseId = null);
public sealed record Verification(string Outcome, string Assurance, string Baseline, string RunId,
    string[] PassedCases, Reason[] Reasons)
{
    public int ExitCode => Outcome switch { "PASS" => 0, "FAIL" => 1, _ => 2 };
}
public sealed record CommandEvidence(string FileName, string[] Arguments, int ExitCode, bool TimedOut,
    DateTimeOffset Started, DateTimeOffset Finished, string Stdout, string Stderr,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Cancelled = false);
public sealed record RunRecord(string Id, string Baseline, string Approval, DateTimeOffset Started,
    DateTimeOffset Finished, string Implementation, string Environment, string MessagesDigest,
    Dictionary<string, string> Artifacts, CommandEvidence[] Commands, Reason[] IntegrityGaps,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BuildInputsDigest = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RuntimeInputsDigest = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExecutionEnvironmentDigest = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TaskInputsDigest = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SupportingPlan = null);
public sealed record RunIntent(string Id, string Baseline, DateTimeOffset Started);

public static class Data
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static T Read<T>(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using var document = JsonDocument.Parse(bytes);
        CanonicalJson.Encode(document.RootElement); // Reject duplicate keys before typed deserialisation.
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new TadaException($"Empty document: {path}");
    }

    public static T Parse<T>(byte[] bytes, string source)
    {
        using var document = JsonDocument.Parse(bytes);
        CanonicalJson.Encode(document.RootElement); // Reject duplicate keys before typed deserialisation.
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new TadaException($"Empty document: {source}");
    }

    public static void WriteNew<T>(string path, T value)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value, Json);
    }
}
