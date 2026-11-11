using System.Xml;
using System.Xml.Linq;

namespace Tada;

/// <summary>Reconciles the pinned native runner's discovery and TRX results for supporting checks.</summary>
public static class TrxVerifier
{
    public static Verification Verify(string[] required, string baseline, string run, string report,
        CommandEvidence discovery, CommandEvidence execution)
    {
        var reasons = new List<Reason>();
        var passed = new List<string>();
        bool failed = false;
        try
        {
            if (discovery.ExitCode != 0 || discovery.TimedOut || discovery.Cancelled || execution.TimedOut || execution.Cancelled ||
                discovery.Finished > execution.Started || execution.Arguments.Length == 0 || !discovery.Arguments.Contains("--list-tests") ||
                !execution.Arguments.Contains("--report-trx"))
                throw new TadaException("Native discovery/execution is missing, interrupted or inconsistent.");
            var discoveryLines = discovery.Stdout.Split('\n');
            var discovered = discoveryLines.TakeWhile(line => !line.StartsWith("Test discovery summary:", StringComparison.Ordinal))
                .Where(line => line.StartsWith("  ", StringComparison.Ordinal))
                .Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
            string? discoverySummary = discoveryLines.SingleOrDefault(line => line.StartsWith("Test discovery summary:", StringComparison.Ordinal));
            if (discovered.Length == 0 || discovered.Distinct(StringComparer.Ordinal).Count() != discovered.Length || required.Except(discovered, StringComparer.Ordinal).Any())
                throw new TadaException("Required native test names are missing or ambiguous in pinned MTP discovery.");
            if (discoverySummary == null || !discoverySummary.StartsWith($"Test discovery summary: found {discovered.Length} test(s) - ", StringComparison.Ordinal))
                throw new TadaException("Native discovery completion does not cover its selected test identities.");
            using var reader = XmlReader.Create(report, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var document = XDocument.Load(reader);
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            if (document.Root?.Name != ns + "TestRun") throw new TadaException("Unsupported native TRX document.");
            var times = document.Root.Element(ns + "Times") ?? throw new TadaException("Native TRX execution times are missing.");
            DateTimeOffset began = (DateTimeOffset)times.Attribute("start")!;
            DateTimeOffset finished = (DateTimeOffset)times.Attribute("finish")!;
            if (began < execution.Started || finished > execution.Finished || finished < began)
                throw new TadaException("Native TRX execution times do not belong to the retained command.");
            var results = document.Root.Element(ns + "Results")?.Elements(ns + "UnitTestResult").ToArray() ?? [];
            var names = results.Select(result => (string?)result.Attribute("testName") ?? "").ToArray();
            var summary = document.Root.Element(ns + "ResultSummary") ?? throw new TadaException("Native TRX completion is missing.");
            var counters = summary.Element(ns + "Counters") ?? throw new TadaException("Native TRX counters are missing.");
            var definitions = document.Root.Element(ns + "TestDefinitions")?.Elements(ns + "UnitTest").ToArray() ?? [];
            var entries = document.Root.Element(ns + "TestEntries")?.Elements(ns + "TestEntry").ToArray() ?? [];
            if (results.Length == 0 || names.Distinct(StringComparer.Ordinal).Count() != names.Length ||
                !discovered.Order(StringComparer.Ordinal).SequenceEqual(names.Order(StringComparer.Ordinal)) ||
                (int?)counters.Attribute("total") != results.Length || (int?)counters.Attribute("executed") != results.Length ||
                definitions.Length != results.Length || entries.Length != results.Length ||
                results.Any(result => string.IsNullOrEmpty((string?)result.Attribute("executionId")) || string.IsNullOrEmpty((string?)result.Attribute("testId"))) ||
                results.Select(result => (string?)result.Attribute("executionId")).Distinct().Count() != results.Length)
                throw new TadaException("Native TRX cases, counters or discovery coverage are incomplete or duplicated.");
            foreach (var result in results)
            {
                string name = (string)result.Attribute("testName")!;
                string testId = (string)result.Attribute("testId")!;
                string executionId = (string)result.Attribute("executionId")!;
                var definition = definitions.Single(item => (string?)item.Attribute("id") == testId);
                var method = definition.Element(ns + "TestMethod") ?? throw new TadaException("Native TRX test method identity is missing.");
                DateTimeOffset caseStart = (DateTimeOffset)result.Attribute("startTime")!;
                DateTimeOffset caseEnd = (DateTimeOffset)result.Attribute("endTime")!;
                if ((string?)definition.Attribute("name") != name || (string?)definition.Element(ns + "Execution")?.Attribute("id") != executionId ||
                    !entries.Any(entry => (string?)entry.Attribute("testId") == testId && (string?)entry.Attribute("executionId") == executionId) ||
                    !string.Equals((string?)method.Attribute("codeBase"), execution.Arguments[0], StringComparison.OrdinalIgnoreCase) ||
                    (string?)method.Attribute("adapterTypeName") != "executor://MSTest.Sdk/4.5.2" ||
                    caseStart < began || caseEnd > finished || caseEnd < caseStart)
                    throw new TadaException("Native TRX test identity, executable or execution interval is inconsistent.");
                string? outcome = (string?)result.Attribute("outcome");
                if (outcome == "Passed") passed.Add(name);
                else if (outcome == "Failed")
                {
                    failed = true;
                    reasons.Add(new Reason("supporting-test-failure", result.Descendants(ns + "Message").FirstOrDefault()?.Value ?? "Native supporting test failed.", name));
                }
                else reasons.Add(new Reason("supporting-test-incomplete", $"Required native test outcome was {outcome ?? "missing"}.", name));
            }
            if ((int?)counters.Attribute("passed") != passed.Count || (int?)counters.Attribute("failed") != results.Count(result => (string?)result.Attribute("outcome") == "Failed"))
                throw new TadaException("Native TRX outcome counters disagree with their cases.");
            foreach (string incomplete in new[] { "error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable", "notExecuted", "disconnected", "inProgress", "pending" })
                if ((int?)counters.Attribute(incomplete) is > 0)
                    reasons.Add(new Reason("supporting-run-incomplete", $"Native report contains {incomplete} results."));
            if (reasons.Count == 0 && (execution.ExitCode != 0 || (string?)summary.Attribute("outcome") != "Completed"))
                reasons.Add(new Reason("supporting-runner-status", "Native runner did not report successful completion."));
        }
        catch (Exception error) when (error is TadaException or IOException or XmlException or InvalidOperationException or ArgumentException or FormatException or OverflowException)
        {
            return new Verification("INDETERMINATE", "L1-advisory", baseline, run, [], [new Reason("supporting-evidence", error.Message)]);
        }
        return new Verification(failed ? "FAIL" : reasons.Count > 0 ? "INDETERMINATE" : "PASS", "L1-advisory", baseline, run, passed.ToArray(), reasons.ToArray());
    }
}
