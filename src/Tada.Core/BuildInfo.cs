using System.Reflection;

namespace Tada;

public static class BuildInfo
{
    public static string Version => typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion.Split('+')[0];
}
