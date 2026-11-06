using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tada.RuntimeProbe;

namespace Tada.Tests;

[TestClass, DoNotParallelize]
public sealed class NativeLoaderTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Value();

    [TestMethod]
    public void NativeLoadNotificationSurvivesImmediateUnloadBeforePolling()
    {
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tada_fixture.dll");
        NativeLoader.Start();
        bool completed = false;
        try
        {
            nint loaded = NativeLibrary.Load(fixture);
            try { Assert.AreEqual(42, Marshal.GetDelegateForFunctionPointer<Value>(NativeLibrary.GetExport(loaded, "TadaFixtureValue"))()); }
            finally { NativeLibrary.Free(loaded); }
            using var process = Process.GetCurrentProcess();
            Assert.IsFalse(process.Modules.Cast<ProcessModule>().Any(module => string.Equals(module.FileName, fixture, StringComparison.OrdinalIgnoreCase)), "Fixture must already be absent from a module snapshot.");
            var events = NativeLoader.ReadPending();
            Assert.IsTrue(events.Any(item => string.Equals(item.Path, fixture, StringComparison.OrdinalIgnoreCase)), "Short-lived native load was lost.");
            completed = true;
            var terminal = NativeLoader.Complete();
            var all = events.Concat(terminal.Pending).ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, terminal.Count).ToArray(), all.Select(item => item.Index).ToArray());
        }
        finally { if (!completed) NativeLoader.Complete(); }
    }

    [TestMethod]
    public void QueueOverflowCannotProduceSuccessfulCompletion()
    {
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tada_fixture.dll");
        NativeLoader.Start();
        try
        {
            for (int i = 0; i < 4100; i++)
            {
                nint loaded = NativeLibrary.Load(fixture);
                NativeLibrary.Free(loaded);
            }
            StringAssert.Contains(Assert.ThrowsExactly<IOException>(() => NativeLoader.ReadPending()).Message, "lost input");
        }
        finally { Assert.ThrowsExactly<IOException>(() => NativeLoader.Complete()); }
    }

    [TestMethod]
    public void NativeObserverImportsOnlyKernel32AndNoRuntimeLibrary()
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, NativeLoader.FileName));
        using var image = new PEReader(file);
        Assert.AreEqual(System.Reflection.PortableExecutable.Machine.Amd64, image.PEHeaders.CoffHeader.Machine);
        int rva = image.PEHeaders.PEHeader!.ImportTableDirectory.RelativeVirtualAddress;
        Assert.AreNotEqual(0, rva);
        var reader = image.GetSectionData(rva).GetReader();
        var imports = new List<string>();
        while (true)
        {
            int lookup = reader.ReadInt32(), stamp = reader.ReadInt32(), chain = reader.ReadInt32(), name = reader.ReadInt32(), addresses = reader.ReadInt32();
            if ((lookup | stamp | chain | name | addresses) == 0) break;
            var text = image.GetSectionData(name).GetReader();
            var bytes = new List<byte>();
            byte next;
            while ((next = text.ReadByte()) != 0) bytes.Add(next);
            imports.Add(Encoding.ASCII.GetString(bytes.ToArray()).ToUpperInvariant());
        }
        CollectionAssert.AreEqual(new[] { "KERNEL32.DLL" }, imports.ToArray());
    }
}
