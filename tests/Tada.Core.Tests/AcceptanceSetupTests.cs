using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass, DoNotParallelize]
public sealed class AcceptanceSetupTests
{
    [TestMethod]
    public void NativeNamedServiceMetadataSupportsOneService()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Root, "tada-services.txt"), "inventory|sut/Api.dll\n");
        Assert.AreEqual(fixture.Assembly, fixture.Create().Services["inventory"]);
    }

    [TestMethod]
    public void NativeDuplicateServiceIdsCannotSelectArbitraryAssemblies()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Root, "tada-services.txt"), "inventory|sut/Api.dll\ninventory|sut/Other.dll\n");
        Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Create());
    }

    [TestMethod]
    public void NativeSetupUsesStagedApiAndIsolatesScenarioStorage()
    {
        using var fixture = new Fixture();
        var first = fixture.Create();
        var second = fixture.Create();
        Assert.AreEqual(fixture.Assembly, first.SutAssembly);
        Assert.AreNotEqual(first.WorkingDirectory, second.WorkingDirectory);
        Assert.IsFalse(BuildInputs.Within(first.WorkingDirectory, fixture.Root));
        Assert.IsTrue(Directory.Exists(first.WorkingDirectory));
    }

    [TestMethod]
    public void PartialManagedContextCannotFallBackToNativeOutputs()
    {
        using var fixture = new Fixture();
        Environment.SetEnvironmentVariable("TADA_RUN_ID", "partial-control");
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Create());
        StringAssert.Contains(error.Message, "TADA_RUN_DIRECTORY");
    }

    [TestMethod]
    public void ManagedSetupUsesTheCapturedApiAndRunDirectory()
    {
        using var fixture = new Fixture();
        string run = Path.Combine(fixture.Root, "captured-run");
        Directory.CreateDirectory(run);
        string api = Path.Combine(run, "Api.dll");
        File.WriteAllText(api, "captured API control");
        Environment.SetEnvironmentVariable("TADA_RUN_ID", "managed-control");
        Environment.SetEnvironmentVariable("TADA_RUN_DIRECTORY", run);
        Environment.SetEnvironmentVariable("TADA_SUT_ASSEMBLY", api);
        Environment.SetEnvironmentVariable("TADA_DOTNET_HOST", api);
        Environment.SetEnvironmentVariable("TADA_RUNTIME_DIRECTORY", run);
        var context = fixture.Create();
        Assert.AreEqual(api, context.SutAssembly);
        Assert.IsTrue(BuildInputs.Within(context.WorkingDirectory, run));
        Assert.AreEqual("managed-control", Environment.GetEnvironmentVariable("TADA_RUN_ID"));
    }

    [TestMethod]
    public void MissingNativeApiHasAnActionableBuildDiagnostic()
    {
        using var fixture = new Fixture();
        File.Delete(fixture.Assembly);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Create()).Message, "Rebuild the acceptance project");
    }

    [TestMethod]
    public void NativeMetadataCannotSelectAnAssemblyOutsideStagedOutputs()
    {
        using var fixture = new Fixture();
        File.WriteAllText(Path.Combine(fixture.Root, "tada-sut.txt"), "../unrelated.dll");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Create()).Message, "inside the acceptance output");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly CoreTests.Project project = new();
        private readonly string[] names = ["TADA_RUN_ID", "TADA_RUN_DIRECTORY", "TADA_SUT_ASSEMBLY", "TADA_DOTNET_HOST", "TADA_RUNTIME_DIRECTORY"];
        private readonly Dictionary<string, string?> previous;
        private readonly List<string> directories = [];
        public string Root => project.Root;
        public string Assembly => Path.Combine(Root, "sut", "Api.dll");

        public Fixture()
        {
            previous = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
            foreach (string name in names) Environment.SetEnvironmentVariable(name, null);
            Directory.CreateDirectory(Path.GetDirectoryName(Assembly)!);
            File.WriteAllText(Assembly, "staged API control");
            File.WriteAllText(Path.Combine(Root, "tada-sut.txt"), "sut/Api.dll");
        }

        public TadaAcceptance Create()
        {
            var context = TadaAcceptance.Create(Root);
            directories.Add(context.WorkingDirectory);
            return context;
        }

        public void Dispose()
        {
            foreach (var entry in previous) Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            foreach (string directory in directories) Directory.Delete(directory, recursive: true);
            project.Dispose();
        }
    }
}
