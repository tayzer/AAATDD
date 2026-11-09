using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// The CLR startup-hook contract requires this exact unqualified type and method name.
internal static class StartupHook
{
    public static void Initialize() => Tada.RuntimeProbe.Observer.Start();
}

namespace Tada.RuntimeProbe
{
    public sealed record Observation(int SchemaVersion, string RunId, int ProcessId, long ProcessStartedTicks,
        long Sequence, DateTimeOffset Time, string Kind, string Path, string Digest, string Identity);

    /// <summary>Cooperative local observation with native-only loader notifications and initial/final snapshots.</summary>
    public static class Observer
    {
        public const string Policy = "dotnet-loader-observation-v2";
        private static readonly object gate = new();
        private static readonly Dictionary<string, string> observed = new(StringComparer.OrdinalIgnoreCase);
        private static StreamWriter? writer;
        private static Timer? timer;
        private static string runId = "", directory = "";
        private static int pid;
        private static long started, sequence;
        private static bool closed, busy, failed, nativeStarted;
        private static readonly JsonSerializerOptions json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static void Start()
        {
            directory = Environment.GetEnvironmentVariable("TADA_RUNTIME_DIRECTORY") ?? "";
            runId = Environment.GetEnvironmentVariable("TADA_RUN_ID") ?? "";
            if (directory.Length == 0) return;
            if (!Guid.TryParseExact(runId, "N", out _) || !Path.IsPathFullyQualified(directory) || !Directory.Exists(directory))
                throw new InvalidOperationException("TADA runtime observation requires an existing absolute directory and run identity.");
            // Preload the observer's BCL dependencies before subscribing to AssemblyLoad. Logging an
            // event must not recursively load serializer/cryptography dependencies under that callback.
            _ = JsonSerializer.Serialize(new Observation(1, runId, 0, 0, 0, DateTimeOffset.UtcNow, "warmup", "", "", ""), json);
            _ = SHA256.HashData(Array.Empty<byte>());
            using var process = Process.GetCurrentProcess();
            pid = process.Id;
            started = process.StartTime.ToUniversalTime().Ticks;
            writer = new StreamWriter(new FileStream(Path.Combine(directory, $"{pid}-{Guid.NewGuid():N}.ndjson"),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
            Write("start", Assembly.GetEntryAssembly()?.Location ?? "", "", Policy);
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) => RecordAssembly(args.LoadedAssembly);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Finish("exit");
            busy = true;
            try
            {
                string native = NativeLoader.Start();
                nativeStarted = true;
                using var nativeFile = File.OpenRead(native);
                Write("native-loader-start", native, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(nativeFile)), NativeLoader.Protocol.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception error) { Error(error); }
            finally { busy = false; }
            Snapshot();
            timer = new Timer(_ => Tick(), null, 50, 50);
        }

        private static void RecordAssembly(Assembly assembly)
        {
            lock (gate)
            {
                if (closed || busy) return;
                busy = true;
                try
                {
                    if (assembly.IsDynamic)
                        WriteOnce("dynamic-managed", assembly.FullName ?? "unnamed", "", assembly.FullName ?? "");
                    else if (string.IsNullOrEmpty(assembly.Location))
                        WriteOnce("unlocated-managed", assembly.FullName ?? "unnamed", "", assembly.FullName ?? "");
                    else RecordFile("managed", assembly.Location, assembly.FullName ?? "");
                }
                catch (Exception error) { Error(error); }
                finally { busy = false; }
            }
        }

        private static void Snapshot()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) RecordAssembly(assembly);
            lock (gate)
            {
                if (closed || busy) return;
                busy = true;
                try
                {
                    RecordNativeLoads(NativeLoader.ReadPending());
                    using var process = Process.GetCurrentProcess();
                    foreach (ProcessModule module in process.Modules)
                        RecordFile("native", module.FileName, module.ModuleName);
                }
                catch (Exception error) { Error(error); }
                finally { busy = false; }
            }
        }

        private static void Tick()
        {
            try
            {
                Snapshot();
                string request = Path.Combine(directory, $"{pid}.checkpoint-request");
                if (File.Exists(request))
                {
                    string requestedRun;
                    try { requestedRun = File.ReadAllText(request); }
                    // Older checkpoint helpers publish the path before closing the write handle.
                    // Keep observing and read the completed request on the next timer tick.
                    catch (IOException error) when ((error.HResult & 65535) is 32 or 33) { return; }
                    if (requestedRun != runId) throw new InvalidOperationException("Runtime checkpoint request identity mismatch.");
                    Finish("checkpoint");
                }
            }
            catch (Exception error) { lock (gate) Error(error); }
        }

        private static void Finish(string kind)
        {
            lock (gate)
            {
                if (closed) return;
                if (nativeStarted)
                {
                    busy = true;
                    try
                    {
                        var completion = NativeLoader.Complete();
                        RecordNativeLoads(completion.Pending);
                        Write("native-loader-stop", "", "", completion.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch (Exception error) { Error(error); }
                    finally { busy = false; }
                    nativeStarted = false;
                }
                Snapshot();
                Write(kind, "", "", failed ? "incomplete" : "complete");
                closed = true;
                timer?.Dispose();
                writer?.Dispose();
                // Acknowledgement is published only after the final snapshot and stream flush.
                if (kind == "checkpoint")
                {
                    string temporary = Path.Combine(directory, $"{pid}.checkpoint-{Guid.NewGuid():N}.tmp");
                    File.WriteAllText(temporary, runId, new UTF8Encoding(false));
                    File.Move(temporary, Path.Combine(directory, $"{pid}.checkpoint-complete"), overwrite: false);
                }
            }
        }

        private static void RecordNativeLoads(NativeLoad[] loads)
        {
            foreach (var load in loads)
                RecordFile("native-loaded", load.Path, load.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static void RecordFile(string kind, string path, string identity)
        {
            path = Path.GetFullPath(path);
            string key = kind + ":" + path;
            if (kind != "native-loaded" && observed.ContainsKey(key)) return;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            string digest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream));
            if (kind == "native-loaded") Write(kind, path, digest, identity);
            else WriteOnce(kind, path, digest, identity);
        }

        private static void WriteOnce(string kind, string path, string digest, string identity)
        {
            string key = kind + ":" + path;
            if (observed.TryAdd(key, digest)) Write(kind, path, digest, identity);
        }

        private static void Error(Exception error)
        {
            failed = true;
            try { Write("error", "", "", error.GetType().Name + ": " + error.Message); }
            catch (Exception) { /* The missing terminal record also prevents verification. */ }
        }

        private static void Write(string kind, string path, string digest, string identity) => writer!.WriteLine(
            JsonSerializer.Serialize(new Observation(1, runId, pid, started, ++sequence, DateTimeOffset.UtcNow, kind, path, digest, identity), json));
    }
}
