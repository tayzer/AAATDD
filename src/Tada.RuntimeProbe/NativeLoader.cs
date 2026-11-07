using System.Runtime.InteropServices;

namespace Tada.RuntimeProbe;

public sealed record NativeLoad(int Index, string Path);
public sealed record NativeCompletion(int Count, NativeLoad[] Pending);

/// <summary>Drains a native-only loader callback queue outside the Windows loader lock.</summary>
public static class NativeLoader
{
    public const string FileName = "tada_native_observer.dll";
    public const int Protocol = 1;
    private const int PathCharacters = 2048;
    private static nint module, buffer;
    private static bool running;
    private static int cursor;
    private static Query start = null!, stop = null!, count = null!, status = null!, protocol = null!;
    private static Read read = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Query();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Read(int index, nint destination, int capacity);

    public static string Start()
    {
        if (running) throw new InvalidOperationException("Native loader observer is already running.");
        string path = Path.Combine(Path.GetDirectoryName(typeof(NativeLoader).Assembly.Location)!, FileName);
        if (module == 0)
        {
            nint loaded = NativeLibrary.Load(path);
            try
            {
                start = Export<Query>(loaded, "TadaNativeStart");
                stop = Export<Query>(loaded, "TadaNativeStop");
                count = Export<Query>(loaded, "TadaNativeCount");
                status = Export<Query>(loaded, "TadaNativeStatus");
                protocol = Export<Query>(loaded, "TadaNativeProtocol");
                read = Export<Read>(loaded, "TadaNativeRead");
                module = loaded;
            }
            catch { NativeLibrary.Free(loaded); throw; }
        }
        if (protocol() != Protocol) throw new InvalidOperationException("Unsupported native loader observer protocol.");
        buffer = Marshal.AllocHGlobal(PathCharacters * sizeof(char));
        int result = start();
        if (result < 0)
        {
            Marshal.FreeHGlobal(buffer);
            buffer = 0;
            throw new IOException($"Native loader notification registration failed: {result}.");
        }
        cursor = 0;
        running = true;
        return path;
    }

    public static NativeLoad[] ReadPending()
    {
        if (!running) return [];
        CheckStatus();
        var result = new List<NativeLoad>();
        while (cursor < count())
        {
            int available = read(cursor, buffer, PathCharacters);
            if (available == 0) break; // A callback reserved its slot but has not published it yet.
            if (available != 1) throw new IOException($"Native loader queue read failed: {available}.");
            string path = Marshal.PtrToStringUni(buffer) ?? throw new IOException("Native loader notification has no path.");
            result.Add(new NativeLoad(++cursor, path));
        }
        CheckStatus();
        return result.ToArray();
    }

    public static NativeCompletion Complete()
    {
        if (!running) throw new InvalidOperationException("Native loader observer was not started.");
        try
        {
            int result = stop();
            if (result < 0) throw new IOException($"Native loader notification unregistration failed: {result}.");
            var pending = ReadPending();
            int total = count();
            if (cursor != total) throw new IOException("Native loader notifications are incomplete after unregistration.");
            return new NativeCompletion(total, pending);
        }
        finally
        {
            running = false;
            Marshal.FreeHGlobal(buffer);
            buffer = 0;
            // Keep the module resident. No callback can ever target freed executable memory.
        }
    }

    private static void CheckStatus()
    {
        int failure = status();
        if (failure != 0) throw new IOException($"Native loader observation lost input (queue capacity/path/notification error {failure}).");
    }

    private static T Export<T>(nint library, string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));
}
