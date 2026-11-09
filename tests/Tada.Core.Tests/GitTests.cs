using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class GitTests
{
    [TestMethod]
    public async Task ApprovedSnapshotAndCurrentOracleSurviveGitRoundTripWithAutoCrlf()
    {
        using var project = new CoreTests.Project();
        Onboarding.Initialise(project.Root);
        string assertion = Path.Combine(project.Root, "acceptance", "Assertion.cs");
        File.WriteAllText(assertion, "// preserve CRLF literal and source bytes\r\nclass Example { string Value = \"hello world\"; }\r\n");
        string baseline = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(baseline).Id, "synthetic Git round-trip control");
        string hooks = Path.Combine(project.Root, ".test-hooks");
        Directory.CreateDirectory(hooks);
        string emptyConfig = Path.Combine(project.Root, ".test-gitconfig");
        File.WriteAllText(emptyConfig, "");
        var environment = new Dictionary<string, string> { ["GIT_CONFIG_NOSYSTEM"] = "1", ["GIT_CONFIG_GLOBAL"] = emptyConfig };
        async Task Git(params string[] args)
        {
            var command = await Processes.Execute("git", ["-c", "core.longpaths=true", "-c", "core.autocrlf=true", "-c", "commit.gpgsign=false", "-c", "core.hooksPath=" + hooks,
                "-c", "user.name=TADA integration control", "-c", "user.email=tada-control@example.invalid", .. args], project.Root, 30, environment);
            Assert.AreEqual(0, command.ExitCode, command.Stdout + command.Stderr);
            Assert.IsFalse(command.TimedOut);
        }
        await Git("init", "--initial-branch=main", "--template=" + hooks);
        await Git("add", "--all");
        await Git("commit", "-m", "test: retain approved bytes");
        string clone = Path.Combine(project.Root, "checkout-control");
        await Git("clone", "--no-hardlinks", "--template=" + hooks, "--config", "core.autocrlf=true", project.Root, clone);
        var clonedStore = new BaselineStore(new Repository(clone));
        Assert.AreEqual(baseline, CanonicalJson.Identity(clonedStore.Read(baseline)));
        clonedStore.CheckCurrent(baseline);
        Assert.AreEqual(baseline, clonedStore.Approved(baseline).Baseline);
        Assert.AreEqual(CanonicalJson.FileDigest(assertion), CanonicalJson.FileDigest(Path.Combine(clone, "acceptance", "Assertion.cs")));
    }

    [TestMethod]
    public void PreparationIsIdempotentAndPublishesNoScratchBytes()
    {
        using var project = new CoreTests.Project();
        string first = project.Store.Prepare();
        Assert.AreEqual(first, project.Store.Prepare());
        string scratch = Path.Combine(project.Root, ".tada", "scratch");
        Assert.AreEqual(0, Directory.GetFileSystemEntries(scratch).Length);
    }
}
