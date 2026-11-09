namespace Tada;

public sealed record ToolIdentity(string Path, string Digest);

/// <summary>Supported local execution inherits OS locations, not arbitrary MSBuild/CLR properties.</summary>
public static class ExecutionEnvironment
{
    public static ToolIdentity[] Tools(IEnumerable<BuildInput> inputs) => inputs
        .Where(input => input.Category is "sdk" or "locked-package")
        .GroupBy(input => input.Path, StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            if (group.Select(input => input.Digest).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new TadaException($"Tool input had inconsistent observed identities: {group.Key}");
            return new ToolIdentity(group.Select(input => Path.GetFullPath(input.Path)).Order(StringComparer.Ordinal).First(), group.First().Digest);
        })
        .OrderBy(input => input.Path, StringComparer.Ordinal).ToArray();

    public static Dictionary<string, string> Capture()
    {
        string[] allowed = ["PATH", "SystemRoot", "WINDIR", "COMSPEC", "PATHEXT", "TEMP", "TMP", "USERPROFILE",
            "HOMEDRIVE", "HOMEPATH", "LOCALAPPDATA", "APPDATA", "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432",
            "PROCESSOR_ARCHITECTURE", "NUMBER_OF_PROCESSORS", "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_CLI_HOME", "NUGET_PACKAGES"];
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in allowed)
            if (Environment.GetEnvironmentVariable(name) is { } value) result.Add(name, value);
        result["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        result["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false";
        result["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        return result;
    }
}
