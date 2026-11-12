using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class OnboardingTests
{
    [TestMethod]
    public void NewProjectPreparationLeavesWorkflowActivationToThePlugin()
    {
        using var project = new CoreTests.Project();
        string root = Path.Combine(project.Root, "new-api");
        string host = @"C:\portable SDK\dotnet.exe";
        var result = Onboarding.Initialise(root, host);
        CollectionAssert.Contains(result.Created, ".tada/WORKFLOW.md");
        Assert.AreEqual(0, result.Updated.Length);
        Assert.IsFalse(File.Exists(Path.Combine(root, "AGENTS.md")));
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, ".tada", "context.md")), host);
        using var global = JsonDocument.Parse(File.ReadAllText(Path.Join(root, "global.json")));
        var sdk = global.RootElement.GetProperty("sdk");
        Assert.AreEqual("10.0.100", sdk.GetProperty("version").GetString());
        Assert.AreEqual("latestFeature", sdk.GetProperty("rollForward").GetString());
        Assert.IsFalse(sdk.GetProperty("allowPrerelease").GetBoolean());
        Assert.AreEqual(0, Directory.GetFiles(root, "*.feature", SearchOption.AllDirectories).Length);
        Assert.IsFalse(Directory.Exists(Path.Combine(root, ".tada", "approvals")));
    }

    [TestMethod]
    public void NewProjectsStartInReferenceModeWithoutKnowledgeCeremony()
    {
        using var project = new CoreTests.Project();
        string root = Path.Combine(project.Root, "new-api");
        Onboarding.Initialise(root);
        // Reference mode is the default. Initialisation creates no settings file, index or knowledge records.
        Assert.IsFalse(File.Exists(Path.Combine(root, ".tada", "knowledge.json")));
        Assert.IsFalse(File.Exists(Path.Combine(root, ".tada", "knowledge.md")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, ".tada", "knowledge")));
        Assert.AreEqual("reference", Knowledge.ReadSettings(new Repository(root)).Settings.Mode);
        string workflow = File.ReadAllText(Path.Combine(root, ".tada", "WORKFLOW.md"));
        foreach (string guidance in new[] { "## Understand before agreeing", "change impact brief", "Verified findings", "Stop when you can responsibly propose",
                     "never creates standalone records", "\"augment\"", "tada knowledge check", "outside the captured oracle", "including paused work" })
            StringAssert.Contains(workflow, guidance);
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, ".tada", "context.md")), "Replace finished work instead of appending history");
    }

    [TestMethod]
    public void ExistingSdkPolicySurvivesRepeatedPreparationByteExact()
    {
        using var project = new CoreTests.Project();
        string path = Path.Join(project.Root, "global.json");
        File.WriteAllText(path, "{\r\n  \"sdk\": { \"version\": \"10.0.103\", \"rollForward\": \"disable\" }\r\n}\r\n", new UTF8Encoding(true));
        byte[] before = File.ReadAllBytes(path);
        Onboarding.Initialise(project.Root);
        Onboarding.Initialise(project.Root);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void LegacyMigrationSignatureKeepsTheExactAlpha8Bytes()
    {
        // The migration only removes unmodified alpha.8 blocks, so the signature must not track current guidance.
        string hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(Onboarding.AgentInstructions)));
        Assert.AreEqual("7faa7f5a0858eb38adf8a4a4f1f4692061fe8e513df5bbd6422e60c9fb353b5c", hash);
    }

    [TestMethod]
    [DataRow(false, "AGENTS.override.md")]
    [DataRow(true, "AGENTS.override.md")]
    [DataRow(false, "AGENTS.md")]
    [DataRow(true, "AGENTS.md")]
    public void LegacyInstructionsAreRemovedWithoutChangingOtherBytes(bool utf16, string name)
    {
        using var project = new CoreTests.Project();
        var encoding = utf16 ? (Encoding)Encoding.Unicode : new UTF8Encoding(true);
        string path = Path.Combine(project.Root, name);
        File.WriteAllText(path, "# Existing authority\r\nKeep café and project rules.\r\n", encoding);
        byte[] original = File.ReadAllBytes(path);
        File.AppendAllText(path, "\n\n" + Onboarding.AgentInstructions + "\n", encoding);
        var first = Onboarding.Initialise(project.Root);
        CollectionAssert.Contains(first.Updated, name);
        byte[] enabled = File.ReadAllBytes(path);
        CollectionAssert.AreEqual(original, enabled);
        var second = Onboarding.Initialise(project.Root);
        Assert.AreEqual(0, second.Created.Length);
        Assert.AreEqual(0, second.Updated.Length);
        CollectionAssert.AreEqual(enabled, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void Alpha7GeneratedEntryPointAlsoStopsForcingTheWorkflow()
    {
        using var project = new CoreTests.Project();
        string path = Path.Combine(project.Root, "AGENTS.md");
        File.WriteAllText(path, "# Agent entry point\n\nRead `.tada/WORKFLOW.md` and `.tada/context.md` before changing this repository. Gherkin defines business acceptance. Ask the human to agree acceptance-changing decisions; routine implementation choices are delegated.\n\n");
        var result = Onboarding.Initialise(project.Root);
        CollectionAssert.Contains(result.Updated, "AGENTS.md");
        Assert.AreEqual("# Agent entry point\n\n\n", File.ReadAllText(path));
    }

    [TestMethod]
    public void DamagedActivationSectionIsNotSilentlyReplacedOrReportedEnabled()
    {
        using var project = new CoreTests.Project();
        string path = Path.Combine(project.Root, "AGENTS.md");
        File.WriteAllText(path, "# User rules\n<!-- TADA:BEGIN -->\ncustom unfinished section");
        byte[] before = File.ReadAllBytes(path);
        Assert.ThrowsExactly<TadaException>(() => Onboarding.Initialise(project.Root));
        CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void UnsupportedInstructionEncodingIsRejectedWithoutChangingBytes()
    {
        using var project = new CoreTests.Project();
        string path = Path.Combine(project.Root, "AGENTS.md");
        byte[] original = [0xff, 0xff, 0x41];
        File.WriteAllBytes(path, original);
        Assert.ThrowsExactly<TadaException>(() => Onboarding.Initialise(project.Root));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
    }
}
