using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class EnvironmentTests
{
    [TestMethod]
    public void TaskOnlyToolCodeChangeProducesADifferentEnvironmentInput()
    {
        using var project = new CoreTests.Project();
        string path = Path.Combine(project.Root, "SdkTask.dll");
        File.WriteAllText(path, "first tool code");
        var before = new BuildInput(path, "sdk", CanonicalJson.FileDigest(path), ["task-assembly"]);
        File.WriteAllText(path, "changed tool code");
        var after = before with { Digest = CanonicalJson.FileDigest(path) };
        Assert.AreNotEqual(CanonicalJson.Identity(ExecutionEnvironment.Tools([before])),
            CanonicalJson.Identity(ExecutionEnvironment.Tools([after])));
    }

    [TestMethod]
    public void LockedTaskDataIsAlsoPartOfTheToolEnvironment()
    {
        using var project = new CoreTests.Project();
        var input = new BuildInput(Path.Combine(project.Root, "package-input.txt"), "locked-package", "sha256:" + new string('a', 64), ["task-input:ReadLinesFromFile:File"]);
        Assert.AreEqual(input.Digest, ExecutionEnvironment.Tools([input]).Single().Digest);
    }

    [TestMethod]
    public void FreshRunPathsAndImplementationEditsDoNotManufactureNewToolEnvironments()
    {
        using var project = new CoreTests.Project();
        var tool = new BuildInput(Path.Combine(project.Root, "SdkTask.dll"), "sdk", "sha256:" + new string('a', 64), ["task-assembly"]);
        BuildInput Generated(string run) => new(Path.Combine(project.Root, run, "temp.txt"), "generated", "sha256:" + new string('b', 64), ["task-input"]);
        var implementation = new BuildInput(Path.Combine(project.Root, "Program.cs"), "implementation", "sha256:" + new string('c', 64), ["Sources"]);
        Assert.AreEqual(CanonicalJson.Identity(ExecutionEnvironment.Tools([tool, Generated("first"), implementation])),
            CanonicalJson.Identity(ExecutionEnvironment.Tools([Generated("second"), tool, implementation with { Digest = "sha256:" + new string('d', 64) }])));
    }

    [TestMethod]
    public void DuplicateUsesDoNotChangeToolIdentityButConflictingHashesAreRejected()
    {
        using var project = new CoreTests.Project();
        var input = new BuildInput(Path.Combine(project.Root, "SdkTask.dll"), "sdk", "sha256:" + new string('a', 64), ["task-assembly"]);
        Assert.AreEqual(CanonicalJson.Identity(ExecutionEnvironment.Tools([input])),
            CanonicalJson.Identity(ExecutionEnvironment.Tools([input with { Uses = ["ReferencePath"] }, input])));
        Assert.ThrowsExactly<TadaException>(() => ExecutionEnvironment.Tools([input, input with { Digest = "sha256:" + new string('b', 64) }]));
    }
}
