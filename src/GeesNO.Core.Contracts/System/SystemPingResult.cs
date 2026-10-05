using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.System;

/// <summary>
/// Reports the logical runtime identity returned by the system ping capability.
/// </summary>
public sealed record SystemPingResult
{
    /// <summary>
    /// Creates a system ping result.
    /// </summary>
    /// <param name="runtimeName">The logical runtime name.</param>
    /// <param name="runtimeVersion">The runtime version.</param>
    public SystemPingResult(string runtimeName, VersionInfo runtimeVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeName);
        ArgumentNullException.ThrowIfNull(runtimeVersion);

        RuntimeName = runtimeName.Trim();
        RuntimeVersion = runtimeVersion;
    }

    /// <summary>Gets the logical runtime name.</summary>
    public string RuntimeName { get; }

    /// <summary>Gets the runtime version.</summary>
    public VersionInfo RuntimeVersion { get; }
}
