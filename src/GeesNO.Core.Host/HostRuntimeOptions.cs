using GeesNO.Core.Primitives;

namespace GeesNO.Core.Host;

internal sealed class HostRuntimeOptions
{
    internal const string DefaultRuntimeName = "GeesNO.Core.Runtime";
    internal const string DefaultRuntimeVersion = "0.1.0";
    internal const bool DefaultRunPingOnStartup = true;

    public string RuntimeName { get; init; } = DefaultRuntimeName;

    public string RuntimeVersion { get; init; } = DefaultRuntimeVersion;

    public bool RunPingOnStartup { get; init; } = DefaultRunPingOnStartup;

    internal HostRuntimeOptions Normalize()
    {
        return new HostRuntimeOptions
        {
            RuntimeName = string.IsNullOrWhiteSpace(RuntimeName)
                ? DefaultRuntimeName
                : RuntimeName.Trim(),
            RuntimeVersion = string.IsNullOrWhiteSpace(RuntimeVersion)
                ? DefaultRuntimeVersion
                : RuntimeVersion.Trim(),
            RunPingOnStartup = RunPingOnStartup
        };
    }

    internal VersionInfo ParseRuntimeVersion()
    {
        var normalized = Normalize();
        var parts = normalized.RuntimeVersion.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || !int.TryParse(parts[2], out var patch))
        {
            throw new FormatException("The runtime version must use the major.minor.patch format.");
        }

        return new VersionInfo(major, minor, patch);
    }
}
