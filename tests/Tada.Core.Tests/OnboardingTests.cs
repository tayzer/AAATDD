using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class OnboardingTests
{
    [TestMethod]
    public void NewProjectEnablesDefaultWorkWithoutInventingBusinessAcceptance()
    {
        using var project = new CoreTests.Project();
        string root = Path.Combine(project.Root, "new-api");
        string host = @"C:\portable SDK\dotnet.exe";
        var result = Onboarding.Initialise(root, host);
        CollectionAssert.Contains(result.Created, "AGENTS.md");
        Assert.AreEqual(0, result.Updated.Length);
        string entry = File.ReadAllText(Path.Combine(root, "AGENTS.md"));
        StringAssert.Contains(entry, "Apply the TADA workflow by default to ordinary project requests.");
        StringAssert.Contains(entry, "The user does not need to mention TADA");
        StringAssert.Contains(entry, "Never approve expectations on the human's behalf.");
        StringAssert.Contains(entry, "without inventing business scenarios");
        StringAssert.Contains(File.ReadAllText(Path.Combine(root, ".tada", "context.md")), host);
        Assert.AreEqual(0, Directory.GetFiles(root, "*.feature", SearchOption.AllDirectories).Length);
        Assert.IsFalse(Directory.Exists(Path.Combine(root, ".tada", "approvals")));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ExistingOverrideRetainsItsExactBytesAndGetsDefaultGuidanceOnce(bool utf16)
    {
        using var project = new CoreTests.Project();
        var encoding = utf16 ? (Encoding)Encoding.Unicode : new UTF8Encoding(true);
        string path = Path.Combine(project.Root, "AGENTS.override.md");
        File.WriteAllText(path, "# Existing authority\r\nKeep café and project rules.\r\n", encoding);
        byte[] original = File.ReadAllBytes(path);
        var first = Onboarding.Initialise(project.Root);
        CollectionAssert.Contains(first.Updated, "AGENTS.override.md");
        byte[] enabled = File.ReadAllBytes(path);
        CollectionAssert.AreEqual(original, enabled.Take(original.Length).ToArray());
        StringAssert.Contains(File.ReadAllText(path), Onboarding.AgentInstructions);
        var second = Onboarding.Initialise(project.Root);
        Assert.AreEqual(0, second.Created.Length);
        Assert.AreEqual(0, second.Updated.Length);
        CollectionAssert.AreEqual(enabled, File.ReadAllBytes(path));
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
