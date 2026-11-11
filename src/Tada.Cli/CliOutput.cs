using System.Text;
using System.Text.Json;

namespace Tada;

public static class CliOutput
{
    public static string Render(JsonElement value)
    {
        var text = new StringBuilder();
        if (value.TryGetProperty("plan", out var plan))
        {
            text.AppendLine($"Plan: {plan.GetString()}");
            if (value.TryGetProperty("outcome", out var assessment))
            {
                text.AppendLine($"{assessment.GetString()} — local advisory L1");
                text.AppendLine($"Run: {value.GetProperty("runId").GetString()}");
                foreach (var check in value.GetProperty("checks").EnumerateArray())
                {
                    var result = check.GetProperty("result");
                    text.AppendLine($"  {check.GetProperty("id").GetString()} ({check.GetProperty("level").GetString()}, {check.GetProperty("purpose").GetString()}): {result.GetProperty("outcome").GetString()}");
                    text.AppendLine($"    Evidence run: {result.GetProperty("runId").GetString()}");
                    foreach (var reason in result.GetProperty("reasons").EnumerateArray())
                        text.AppendLine($"    {reason.GetProperty("caseId").GetString()}: {reason.GetProperty("detail").GetString()}");
                }
                foreach (var reason in value.GetProperty("reasons").EnumerateArray()) text.AppendLine("  " + reason.GetProperty("detail").GetString());
            }
            else
            {
                var definition = value.GetProperty("inventory").GetProperty("definition");
                text.AppendLine(definition.GetProperty("objective").GetString());
                text.AppendLine("Change categories: " + string.Join(", ", definition.GetProperty("categories").EnumerateArray().Select(category => category.GetString())));
                foreach (var impact in definition.GetProperty("impact").EnumerateArray()) text.AppendLine("  Impact: " + impact.GetString());
                foreach (var check in definition.GetProperty("checks").EnumerateArray())
                {
                    text.AppendLine($"  {check.GetProperty("id").GetString()} ({check.GetProperty("level").GetString()}, {check.GetProperty("purpose").GetString()}): {check.GetProperty("rationale").GetString()}");
                    foreach (var obligation in check.GetProperty("obligations").EnumerateArray()) text.AppendLine("    Obligation: " + obligation.GetString());
                }
                if (definition.GetProperty("previous").ValueKind == JsonValueKind.String)
                    text.AppendLine($"Previous plan: {definition.GetProperty("previous").GetString()}. {definition.GetProperty("changeReason").GetString()}");
                foreach (var omission in definition.GetProperty("omissions").EnumerateArray()) text.AppendLine("  Omission rationale: " + omission.GetString());
                foreach (var check in value.GetProperty("changedChecks").EnumerateArray()) text.AppendLine("  New/changed check: " + check.GetString());
                foreach (var check in value.GetProperty("removedChecks").EnumerateArray()) text.AppendLine("  Removed check: " + check.GetString());
                if (definition.GetProperty("noChecksReason").ValueKind == JsonValueKind.String) text.AppendLine(definition.GetProperty("noChecksReason").GetString());
                text.AppendLine("Plan captured. Human business approval remains separate.");
                text.AppendLine($"Next: tada run --plan {plan.GetString()}");
            }
        }
        else if (value.TryGetProperty("outcome", out var outcome))
        {
            text.AppendLine($"{outcome.GetString()} — local advisory L1");
            text.AppendLine($"Baseline: {value.GetProperty("baseline").GetString()}");
            text.AppendLine($"Run: {value.GetProperty("runId").GetString()}");
            foreach (var item in value.GetProperty("passedCases").EnumerateArray()) text.AppendLine($"  Passed: {item.GetString()}");
            foreach (var reason in value.GetProperty("reasons").EnumerateArray())
            {
                string? caseId = reason.GetProperty("caseId").GetString();
                text.AppendLine($"  {reason.GetProperty("code").GetString()}{(caseId == null ? "" : " [" + caseId + "]")}: {reason.GetProperty("detail").GetString()}");
            }
            if (outcome.GetString() != "PASS") text.AppendLine("Completion is blocked. Inspect the reasons and retained run evidence.");
            text.AppendLine("This checks the selected expectations; it is not protected independent assurance.");
        }
        else if (value.TryGetProperty("review", out var review) && review.ValueKind == JsonValueKind.Object)
        {
            text.AppendLine($"Review: {review.GetProperty("id").GetString()}");
            text.AppendLine($"Baseline: {review.GetProperty("baseline").GetString()}");
            var policy = value.GetProperty("inventory").GetProperty("policy");
            text.AppendLine(policy.TryGetProperty("buildInputs", out var buildPolicy)
                ? $"Build-input policy: {buildPolicy.GetString()}. Trusts the installed SDK and explicitly pinned package cache; captures observed input hashes."
                : "Historical baseline: no evaluated-build-input policy was recorded.");
            text.AppendLine(policy.TryGetProperty("runtimeInputs", out var runtimePolicy)
                ? $"Runtime policy: {runtimePolicy.GetString()}. Managed/native load observation and cooperative final snapshots; trusts captured .NET platform and Windows system modules."
                : "Historical baseline: no runtime-observation policy was recorded.");
            if (policy.TryGetProperty("buildTasks", out var taskPolicy))
            {
                text.AppendLine($"Task policy: {taskPolicy.GetString()}. SDK/pinned package tasks remain trusted tools; opaque custom code must honour its declared read roots.");
                foreach (var contract in value.GetProperty("buildTaskContracts").EnumerateArray())
                {
                    text.AppendLine($"  Trusted custom task: {contract.GetProperty("origin").GetString()} / {contract.GetProperty("target").GetString()} / {contract.GetProperty("task").GetString()}");
                    foreach (var root in contract.GetProperty("inputRoots").EnumerateArray()) text.AppendLine("    Read root: " + root.GetString());
                }
            }
            text.AppendLine("Business acceptance — captured source:");
            foreach (var specification in value.GetProperty("specifications").EnumerateArray())
            {
                text.AppendLine($"\n{specification.GetProperty("path").GetString()}");
                text.AppendLine(specification.GetProperty("content").GetString());
            }
            text.AppendLine("Changed/new oracle files:");
            foreach (var path in value.GetProperty("changedFiles").EnumerateArray()) text.AppendLine("  " + path.GetString());
            foreach (var path in value.GetProperty("removedFiles").EnumerateArray()) text.AppendLine("  Removed: " + path.GetString());
            foreach (var id in value.GetProperty("removedCaseIds").EnumerateArray()) text.AppendLine("  Removed acceptance case: " + id.GetString());
            text.AppendLine("Review assertions, setup, observers and configuration in the captured snapshot too.");
            text.AppendLine("This is a review record, not approval. Only the human declares acceptance approval.");
            text.AppendLine($"Next: tada baseline approve --review {review.GetProperty("id").GetString()} --by \"Your name\"");
        }
        else if (value.TryGetProperty("by", out var by))
        {
            text.AppendLine($"Approval recorded by: {by.GetString()}");
            text.AppendLine($"Baseline: {value.GetProperty("baseline").GetString()}");
            text.AppendLine($"Approval: {value.GetProperty("id").GetString()}");
            text.AppendLine("Approval establishes the expected behaviour, not a passing implementation.");
            text.AppendLine($"Next: tada run --baseline {value.GetProperty("baseline").GetString()}");
        }
        else if (value.TryGetProperty("baseline", out var baseline))
        {
            text.AppendLine($"Draft baseline: {baseline.GetString()}");
            text.AppendLine("No approval has been granted.");
            text.AppendLine($"Next: tada baseline review --baseline {baseline.GetString()}");
        }
        else if (value.TryGetProperty("created", out var created))
        {
            text.AppendLine("TADA project prepared. Use the host plugin controls to enable or disable automatic workflow selection.");
            foreach (var path in created.EnumerateArray()) text.AppendLine("  Created: " + path.GetString());
            if (value.TryGetProperty("updated", out var updated))
                foreach (var path in updated.EnumerateArray()) text.AppendLine("  Migrated legacy guidance in: " + path.GetString());
            text.AppendLine(value.GetProperty("next").GetString());
            text.AppendLine("If .gitattributes already existed, review line-ending rules for exact-byte oracle inputs.");
        }
        else text.AppendLine(JsonSerializer.Serialize(value, Data.Json));
        return SafeText(text.ToString().TrimEnd());
    }

    public static string SafeText(string value)
    {
        var output = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n') { output.Append('\n'); i++; }
            else if ((char.IsControl(c) && c is not '\n' and not '\t') || c is >= '\u202a' and <= '\u202e' or >= '\u2066' and <= '\u2069')
                output.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
            else output.Append(c);
        }
        return output.ToString();
    }
}
