using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;

namespace Tada.BuildLogger;

public sealed record TaskValue(string Value, string? Path, string? Digest);
public sealed record TaskEvent(int SchemaVersion, long Sequence, DateTimeOffset Time, string Kind, string Key,
    string Project, string Origin, string Target, string Name, string Assembly, string AssemblyDigest,
    string Parameter, TaskValue[] Values, bool Succeeded);
public static class Marker { }

public static class TaskRules
{
    private static readonly Dictionary<string, string[]> reads = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ReadLinesFromFile"] = ["File"], ["Copy"] = ["SourceFiles"], ["GetFileHash"] = ["Files"],
        ["GetAssemblyIdentity"] = ["AssemblyFiles"], ["XmlPeek"] = ["XmlInputPath"],
        ["ResolveKeySource"] = ["KeyFile", "CertificateFile"], ["Unzip"] = ["SourceFiles"],
        ["Csc"] = ["Sources", "References", "Analyzers", "AdditionalFiles", "AnalyzerConfigFiles", "Resources", "LinkResources", "Win32Resource", "Win32Icon", "Win32Manifest", "KeyFile", "ResponseFiles"]
    };
    private static readonly HashSet<string> pure = new(StringComparer.OrdinalIgnoreCase)
    {
        "Message", "Warning", "Error", "CreateItem", "CreateProperty", "CallTarget", "MSBuild", "MakeDir",
        "Delete", "RemoveDir", "Touch", "WriteLinesToFile", "WriteCodeFragment", "AssignTargetPath", "AssignLinkMetadata",
        "FindUnderPath", "ConvertToAbsolutePath", "CombinePath", "GetDuplicateItems", "RemoveDuplicates", "Hash"
    };
    public static string ShortName(string name) => name.Split('.').Last();
    public static bool Reads(string name, string parameter) => reads.TryGetValue(ShortName(name), out var parameters) && parameters.Contains(parameter, StringComparer.OrdinalIgnoreCase);
    public static bool Standard(string name) => reads.ContainsKey(ShortName(name)) || pure.Contains(ShortName(name));
    public static string[] RequiredInputs(string name) => ShortName(name).ToUpperInvariant() switch
    {
        // MSBuild omits null/empty item arrays from structured input events. Copy and Csc
        // legitimately execute without source items (for example empty static web assets).
        // A successful ReadLinesFromFile, however, must receive its scalar File input.
        "READLINESFROMFILE" => ["File"], _ => []
    };
}

/// <summary>Loaded by the pinned SDK. It shares no MSBuild binaries with the packaged CLI.</summary>
public sealed class AuditLogger : ILogger
{
    private readonly object gate = new();
    private readonly Dictionary<string, TaskEvent> tasks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> targets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (long Length, long Modified, string Digest)> hashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private StreamWriter? writer;
    private long sequence;
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Diagnostic;
    public string? Parameters { get; set; }

    public void Initialize(IEventSource source)
    {
        if (string.IsNullOrWhiteSpace(Parameters) || !Path.IsPathFullyQualified(Parameters)) throw new LoggerException("TADA task logger requires an absolute report filename.");
        writer = new StreamWriter(new FileStream(Parameters, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
        if (source is not IEventSource3 detailed) throw new LoggerException("MSBuild does not support structured task input events.");
        detailed.IncludeTaskInputs();
        Write(new TaskEvent(1, 0, DateTimeOffset.UtcNow, "logger-start", "", "", "", "", "", "", "", "", [], true));
        source.TargetStarted += (_, item) => { lock (gate) targets[TargetKey(item.BuildEventContext)] = item.TargetName; };
        source.TaskStarted += (_, item) =>
        {
            lock (gate)
            {
                string assembly = item.TaskAssemblyLocation ?? "";
                string digest = File.Exists(assembly) ? Hash(assembly) : "";
                string key = TaskKey(item.BuildEventContext);
                var entry = new TaskEvent(1, 0, new DateTimeOffset(item.Timestamp.ToUniversalTime()), "task-start", key,
                    item.ProjectFile ?? "", item.TaskFile ?? "", targets.GetValueOrDefault(TargetKey(item.BuildEventContext), ""),
                    item.TaskName ?? "", assembly, digest, "", [], true);
                tasks.Add(key, entry);
                Write(entry);
            }
        };
        source.AnyEventRaised += (_, item) =>
        {
            if (item is not TaskParameterEventArgs parameter || parameter.Kind != TaskParameterMessageKind.TaskInput) return;
            lock (gate)
            {
                string key = TaskKey(parameter.BuildEventContext);
                if (!tasks.TryGetValue(key, out var task)) throw new LoggerException("Task parameters have no matching task-start identity.");
                string name = parameter.ParameterName ?? parameter.ItemType ?? "";
                var values = new List<TaskValue>();
                foreach (object value in parameter.Items ?? Array.Empty<object>())
                {
                    string text = value is ITaskItem itemValue ? itemValue.ItemSpec : value.ToString() ?? "";
                    string? path = null, digest = null;
                    if (TaskRules.Reads(task.Name, name))
                    {
                        try
                        {
                            path = Path.GetFullPath(text, Path.GetDirectoryName(task.Project)!);
                            if (File.Exists(path)) digest = Hash(path);
                        }
                        catch (Exception error) when (error is ArgumentException or NotSupportedException) { path = null; }
                    }
                    values.Add(new TaskValue(text, path, digest));
                }
                Write(task with { Kind = "task-input", Parameter = name, Values = values.ToArray(), Time = new DateTimeOffset(parameter.Timestamp.ToUniversalTime()) });
            }
        };
        source.TaskFinished += (_, item) =>
        {
            lock (gate)
            {
                string key = TaskKey(item.BuildEventContext);
                if (!tasks.Remove(key, out var task)) throw new LoggerException("Task completion has no matching startup identity.");
                Write(task with { Kind = "task-finish", Succeeded = item.Succeeded, Time = new DateTimeOffset(item.Timestamp.ToUniversalTime()) });
            }
        };
        source.BuildFinished += (_, item) =>
        {
            lock (gate)
                Write(new TaskEvent(1, 0, new DateTimeOffset(item.Timestamp.ToUniversalTime()), "logger-finish", "", "", "", "", "", "", "", "", [], item.Succeeded && tasks.Count == 0));
        };
        source.ErrorRaised += (_, item) =>
        {
            lock (gate) Write(new TaskEvent(1, 0, DateTimeOffset.UtcNow, "error", "", item.ProjectFile ?? "", item.File ?? "", "", item.Code ?? "", "", "", "", [new TaskValue(item.Message ?? "Build error", null, null)], false));
        };
    }

    public void Shutdown() { lock (gate) writer?.Dispose(); }
    private void Write(TaskEvent item) => writer!.WriteLine(JsonSerializer.Serialize(item with { Sequence = ++sequence }, json));
    private string Hash(string path)
    {
        var file = new FileInfo(path);
        if (hashes.TryGetValue(path, out var previous) && previous.Length == file.Length && previous.Modified == file.LastWriteTimeUtc.Ticks) return previous.Digest;
        using var stream = File.OpenRead(path);
        string digest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream));
        hashes[path] = (file.Length, file.LastWriteTimeUtc.Ticks, digest);
        return digest;
    }
    private static string TargetKey(BuildEventContext? item) => item == null ? throw new LoggerException("Task event context is missing.") : $"{item.NodeId}:{item.ProjectContextId}:{item.TargetId}";
    private static string TaskKey(BuildEventContext? item) => TargetKey(item) + ":" + item!.TaskId;
}
