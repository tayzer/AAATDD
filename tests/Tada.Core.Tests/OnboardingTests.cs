using System.Text;
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
        Assert.AreEqual(0, Directory.GetFiles(root, "*.feature", SearchOption.AllDirectories).Length);
        Assert.IsFalse(Directory.Exists(Path.Combine(root, ".tada", "approvals")));
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
