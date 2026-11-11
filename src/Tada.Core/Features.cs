using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Ast;

namespace Tada;

public static partial class Features
{
    public static ExpectedCase[] Read(Repository repository, IEnumerable<FileInput> files, Config config)
    {
        var cases = new List<ExpectedCase>();
        foreach (var file in files)
        {
            var document = new Parser().Parse(new StringReader(File.ReadAllText(repository.Resolve(file.Path))));
            if (document.Feature is null) throw new TadaException($"No feature in {file.Path}");
            Visit(document.Feature.Children, document.Feature.Tags.Select(t => t.Name).ToArray(), file.Path);
        }
        var allScenarios = cases.Select(c => c.ScenarioId).ToArray();
        if (allScenarios.Distinct(StringComparer.Ordinal).Count() != allScenarios.Length)
            throw new TadaException("Duplicate scenario identity across captured features.");
        if (allScenarios.Intersect(cases.SelectMany(c => c.RequirementIds), StringComparer.Ordinal).Any())
            throw new TadaException("Requirement/scenario identities collide.");
        return cases.OrderBy(c => c.CaseId, StringComparer.Ordinal).ToArray();

        void Visit(IEnumerable<IHasLocation> children, string[] inherited, string path)
        {
            foreach (var child in children)
            {
                if (child is Rule rule)
                    Visit(rule.Children, inherited.Concat(rule.Tags.Select(t => t.Name)).ToArray(), path);
                else if (child is Scenario scenario)
                {
                    if (scenario.Examples.Any() || scenario.Keyword.Contains("Outline", StringComparison.OrdinalIgnoreCase))
                        throw new TadaException("Scenario Outlines are unsupported; no cases may be silently omitted.");
                    var tags = inherited.Concat(scenario.Tags.Select(t => t.Name)).Select(t => t.TrimStart('@')).ToArray();
                    var ids = scenario.Tags.Select(t => t.Name.TrimStart('@')).Where(t => t.StartsWith(config.ScenarioPrefix, StringComparison.Ordinal)).ToArray();
                    var requirements = tags.Where(t => t.StartsWith(config.RequirementPrefix, StringComparison.Ordinal)).Distinct().Order(StringComparer.Ordinal).ToArray();
                    if (ids.Length != 1 || requirements.Length == 0 || !Id().IsMatch(ids[0]) || requirements.Any(t => !Id().IsMatch(t)))
                        throw new TadaException($"Scenario '{scenario.Name}' needs one scenario ID and at least one requirement ID.");
                    if (!scenario.Steps.Any()) throw new TadaException($"Empty scenario: {scenario.Name}");
                    cases.Add(new ExpectedCase(ids[0], ids[0], requirements, path));
                }
            }
        }
    }
    /// <summary>
    /// Identifies each scenario's agreed executable content: inherited tags, background and scenario steps.
    /// Binding, fixture, title and narrative-only changes do not alter a scenario fingerprint, so they cannot
    /// justify returning an active obligation to candidate.
    /// </summary>
    public static Dictionary<string, string> Fingerprints(Repository repository, IEnumerable<FileInput> files, Config config)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files.Where(f => f.Roles.Contains("specification")))
        {
            var document = new Parser().Parse(new StringReader(File.ReadAllText(repository.Resolve(file.Path))));
            if (document.Feature is null) throw new TadaException($"No feature in {file.Path}");
            Visit(document.Feature.Children, document.Feature.Tags.Select(t => t.Name).ToArray(), []);
        }
        return result;

        void Visit(IEnumerable<IHasLocation> children, string[] tags, object[] background)
        {
            var items = children.ToArray();
            var local = background.Concat(items.OfType<Background>().SelectMany(b => b.Steps).Select(Render)).ToArray();
            foreach (var child in items)
            {
                if (child is Rule rule) Visit(rule.Children, tags.Concat(rule.Tags.Select(t => t.Name)).ToArray(), local);
                else if (child is Scenario scenario)
                {
                    var id = scenario.Tags.Select(t => t.Name.TrimStart('@')).SingleOrDefault(t => t.StartsWith(config.ScenarioPrefix, StringComparison.Ordinal));
                    if (id == null) continue;
                    result[id] = CanonicalJson.Identity(new
                    {
                        tags = tags.Concat(scenario.Tags.Select(t => t.Name)).Order(StringComparer.Ordinal).ToArray(),
                        background = local,
                        steps = scenario.Steps.Select(Render).ToArray()
                    });
                }
            }
        }

        static object Render(Step step) => new
        {
            keyword = step.Keyword.Trim(),
            text = step.Text,
            docString = (step.Argument as DocString)?.Content,
            table = (step.Argument as DataTable)?.Rows.Select(row => row.Cells.Select(cell => cell.Value).ToArray()).ToArray()
        };
    }

    [GeneratedRegex(@"\A[A-Z][A-Z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex Id();
}
