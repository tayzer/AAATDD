using System.Text.Json;

namespace Tada;

/// <summary>Conservative supported Reqnroll Messages interpretation, not a general Cucumber adapter.</summary>
public static class MessagesVerifier
{
    public static Verification Verify(Inventory inventory, string baseline, string runId, string file,
        Repository capturedOracle, CommandEvidence? command = null)
    {
        var reasons = new List<Reason>();
        var passed = new List<string>();
        bool behaviouralFailure = false;
        try
        {
            var envelopes = File.ReadLines(file).Select(line =>
            {
                using var document = JsonDocument.Parse(line);
                var value = document.RootElement.Clone();
                CanonicalJson.Encode(value);
                if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 1)
                    throw new TadaException("Each Messages envelope must contain one event.");
                return value;
            }).ToArray();
            JsonElement[] Events(string kind) => envelopes.Where(e => e.TryGetProperty(kind, out _)).Select(e => e.GetProperty(kind)).ToArray();
            var meta = Events("meta");
            if (meta.Length != 1 || S(meta[0], "protocolVersion") != inventory.Policy.MessagesProtocol ||
                S(meta[0].GetProperty("implementation"), "name") != "Reqnroll")
                throw new TadaException("Unsupported or absent Messages producer/protocol.");
            if (Events("testRunStarted").Length != 1 || Events("testRunFinished").Length != 1)
                throw new TadaException("Missing or duplicated run boundaries.");
            var runFinish = Events("testRunFinished")[0];
            if (command != null)
            {
                foreach (var boundary in Events("testRunStarted").Concat(Events("testRunFinished")))
                {
                    var timestamp = boundary.GetProperty("timestamp");
                    var at = DateTimeOffset.FromUnixTimeSeconds(timestamp.GetProperty("seconds").GetInt64());
                    if (at < command.Started.AddSeconds(-2) || at > command.Finished.AddSeconds(2))
                        throw new TadaException("Stale Messages timestamps do not belong to the recorded execution.");
                }
            }
            var sources = Events("source");
            var documents = Events("gherkinDocument");
            var pickles = ById(Events("pickle"));
            var definitions = ById(Events("stepDefinition"));
            var hooks = ById(Events("hook"));
            var testCases = ById(Events("testCase"));
            var starts = ById(Events("testCaseStarted"));
            var finishes = Events("testCaseFinished");
            var stepStarts = Events("testStepStarted");
            var stepFinishes = Events("testStepFinished");
            if (starts.Count != inventory.ExpectedCases.Length || finishes.Length != starts.Count ||
                testCases.Count != inventory.ExpectedCases.Length || pickles.Count != inventory.ExpectedCases.Length)
                reasons.Add(new Reason("coverage", "Native case inventory does not equal the approved expected inventory."));
            var usedStarts = new HashSet<string>();
            var usedPickles = new HashSet<string>();
            var usedSteps = new HashSet<string>();
            foreach (var expected in inventory.ExpectedCases)
            {
                var matching = pickles.Values.Where(p => p.GetProperty("tags").EnumerateArray()
                    .Any(t => S(t, "name") == "@" + expected.ScenarioId)).ToArray();
                if (matching.Length != 1) { Gap("case-mapping", "Expected one native pickle for the logical case."); continue; }
                var pickle = matching[0];
                string pickleId = S(pickle, "id");
                if (!usedPickles.Add(pickleId)) throw new TadaException("Multiple logical cases map to the same native case.");
                string uri = S(pickle, "uri");
                var source = sources.Where(s => S(s, "uri") == uri).ToArray();
                var docs = documents.Where(d => S(d, "uri") == uri).ToArray();
                if (source.Length != 1 || docs.Length != 1 || S(source[0], "data") != File.ReadAllText(capturedOracle.Resolve(expected.SourcePath)))
                    throw new TadaException($"Native source does not match approved feature: {expected.SourcePath}");
                var scenarios = Descendants(docs[0], "scenario").ToArray();
                var scenario = scenarios.SingleOrDefault(s => s.GetProperty("tags").EnumerateArray()
                    .Any(t => S(t, "name") == "@" + expected.ScenarioId));
                if (scenario.ValueKind == JsonValueKind.Undefined ||
                    !pickle.GetProperty("astNodeIds").EnumerateArray().Any(id => id.GetString() == S(scenario, "id")))
                    throw new TadaException("Logical/native AST link is absent or mismatched.");
                var nativeTags = pickle.GetProperty("tags").EnumerateArray().Select(t => S(t, "name")).ToArray();
                if (expected.RequirementIds.Any(id => !nativeTags.Contains("@" + id))) throw new TadaException("Native requirement tags mismatch.");
                var astSteps = Descendants(docs[0], "steps").SelectMany(s => s.EnumerateArray()).ToDictionary(s => S(s, "id"));
                var pickleSteps = ById(pickle.GetProperty("steps").EnumerateArray().ToArray());
                foreach (var ps in pickleSteps.Values)
                {
                    var linked = ps.GetProperty("astNodeIds").EnumerateArray().Select(v => v.GetString()!).ToArray();
                    if (linked.Length != 1 || !astSteps.TryGetValue(linked[0], out var ast) || S(ast, "text") != S(ps, "text"))
                        throw new TadaException("Compiled step does not match its AST source.");
                }
                var test = testCases.Values.Where(t => S(t, "pickleId") == pickleId).ToArray();
                if (test.Length != 1) { Gap("missing-case", "Expected case was not discovered."); continue; }
                var executions = starts.Values.Where(s => S(s, "testCaseId") == S(test[0], "id")).ToArray();
                if (executions.Length != 1) { Gap("attempt-count", "Expected exactly one execution; retries are unsupported."); continue; }
                var start = executions[0];
                string startId = S(start, "id");
                usedStarts.Add(startId);
                if (start.GetProperty("attempt").GetInt32() != 0) { Gap("retry", "Automatic retry is not approved."); continue; }
                var finished = finishes.Where(f => S(f, "testCaseStartedId") == startId).ToArray();
                if (finished.Length != 1 || finished[0].GetProperty("willBeRetried").GetBoolean())
                { Gap("case-incomplete", "Case lacks a unique terminal finish."); continue; }
                var steps = test[0].GetProperty("testSteps").EnumerateArray().ToArray();
                var mapped = steps.Where(s => s.TryGetProperty("pickleStepId", out _)).Select(s => S(s, "pickleStepId")).ToArray();
                if (mapped.Length != pickleSteps.Count || mapped.Distinct().Count() != mapped.Length || mapped.Any(id => !pickleSteps.ContainsKey(id)))
                    throw new TadaException("Discovered steps do not cover every compiled step exactly once.");
                bool casePassed = true;
                foreach (var step in steps)
                {
                    string stepId = S(step, "id");
                    string key = startId + ":" + stepId;
                    if (!usedSteps.Add(key)) throw new TadaException("Duplicate native step identity.");
                    var began = stepStarts.Where(s => S(s, "testCaseStartedId") == startId && S(s, "testStepId") == stepId).ToArray();
                    var ended = stepFinishes.Where(s => S(s, "testCaseStartedId") == startId && S(s, "testStepId") == stepId).ToArray();
                    if (began.Length != 1 || ended.Length != 1) { Gap("step-incomplete", "Missing or duplicate step boundaries."); casePassed = false; continue; }
                    bool isBusiness = false;
                    if (step.TryGetProperty("pickleStepId", out var psid))
                    {
                        var ps = pickleSteps[psid.GetString()!];
                        isBusiness = S(ps, "type") == "Outcome";
                        var matches = step.GetProperty("stepDefinitionIds").EnumerateArray().ToArray();
                        if (matches.Length != 1 || !definitions.ContainsKey(matches[0].GetString()!))
                        { Gap("binding", "Step is unbound or ambiguous."); casePassed = false; continue; }
                    }
                    else if (!step.TryGetProperty("hookId", out var hook) || !hooks.ContainsKey(hook.GetString()!))
                        throw new TadaException("Unknown hook execution.");
                    var result = ended[0].GetProperty("testStepResult");
                    string status = S(result, "status");
                    if (status != "PASSED")
                    {
                        casePassed = false;
                        if (status == "FAILED" && isBusiness && result.TryGetProperty("exception", out var exception) &&
                            S(exception, "type") == "AssertFailedException")
                        {
                            behaviouralFailure = true;
                            reasons.Add(new Reason("requirement-failure", result.TryGetProperty("message", out var msg) ? msg.GetString()! : "Assertion failed.", expected.CaseId));
                        }
                        else Gap("step-" + status.ToLowerInvariant(), "Required step or hook did not pass.");
                    }
                }
                if (casePassed) passed.Add(expected.CaseId);

                void Gap(string code, string detail) => reasons.Add(new Reason(code, detail, expected.CaseId));
            }
            if (usedStarts.Count != starts.Count || stepStarts.Length != usedSteps.Count || stepFinishes.Length != usedSteps.Count)
                reasons.Add(new Reason("unexpected-events", "Execution contains unaccounted-for or incomplete case/step events."));
            bool nativeSuccess = runFinish.GetProperty("success").GetBoolean();
            if (reasons.Count == 0 && (!nativeSuccess || command is { ExitCode: not 0 }))
                reasons.Add(new Reason("runner-status", "Native run or process did not report success."));
            return Result(behaviouralFailure ? "FAIL" : reasons.Count > 0 ? "INDETERMINATE" : "PASS");
        }
        catch (Exception error) when (error is JsonException or IOException or KeyNotFoundException or InvalidOperationException or ArgumentException or TadaException)
        {
            reasons.Add(new Reason("invalid-evidence", error.Message));
            // A globally invalid source/oracle/stream cannot establish a trustworthy business failure.
            passed.Clear();
            return Result("INDETERMINATE");
        }
        Verification Result(string outcome) => new(outcome, "L1-advisory", baseline, runId, passed.ToArray(), reasons.ToArray());
    }

    private static string S(JsonElement element, string name) => element.GetProperty(name).GetString()
        ?? throw new TadaException($"Missing string: {name}");
    private static Dictionary<string, JsonElement> ById(JsonElement[] items) => items.ToDictionary(e => S(e, "id"), StringComparer.Ordinal);
    private static IEnumerable<JsonElement> Descendants(JsonElement root, string property)
    {
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var item in root.EnumerateObject())
            {
                if (item.Name == property) yield return item.Value;
                foreach (var nested in Descendants(item.Value, property)) yield return nested;
            }
        else if (root.ValueKind == JsonValueKind.Array)
            foreach (var item in root.EnumerateArray())
                foreach (var nested in Descendants(item, property)) yield return nested;
    }
}
