using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

/// <summary>Guards the distributed agent guidance against silently dropping discovery and knowledge rules.</summary>
[TestClass]
public sealed class GuidanceTests
{
    [TestMethod]
    public void PluginSkillCarriesDiscoveryAndKnowledgeGuidanceWithItsReference()
    {
        DirectoryInfo? source = new(AppContext.BaseDirectory);
        while (source != null && !Directory.Exists(Path.Combine(source.FullName, "plugins", "tada"))) source = source.Parent;
        if (source == null) throw new InvalidOperationException("Cannot locate the plugin source.");
        string skillDirectory = Path.Combine(source.FullName, "plugins", "tada", "skills", "tada");
        string skill = File.ReadAllText(Path.Combine(skillDirectory, "SKILL.md"));
        string reference = File.ReadAllText(Path.Combine(skillDirectory, "references", "discovery-and-knowledge.md"));
        StringAssert.Contains(skill, "](references/discovery-and-knowledge.md)");
        foreach (string guidance in new[] { "**Low**", "**Medium**", "**High**", "Stop expanding", "change impact brief", "**Verified**", "**Inferred**", "**Unknowns**",
                     "never create standalone knowledge records", "**Augment** (opt-in by a human)", "Never convert an inference into an agreed requirement",
                     "tada knowledge check", "not a history log", "Chores need no impact brief document" })
            StringAssert.Contains(skill, guidance);
        foreach (string guidance in new[] { "## Knowledge authority", "## Change impact brief", "## Significance test", "## Record template (augment mode only)",
                     "status: proposed", "supersededBy", "## Context handoff" })
            StringAssert.Contains(reference, guidance);
    }
}
