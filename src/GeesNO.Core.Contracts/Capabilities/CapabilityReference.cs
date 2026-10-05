using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Identifies an exact version of a capability.
/// </summary>
public sealed record CapabilityReference
{
    /// <summary>
    /// Creates a reference to an exact capability version.
    /// </summary>
    /// <param name="id">The capability identifier.</param>
    /// <param name="version">The exact capability version.</param>
    public CapabilityReference(CapabilityId id, VersionInfo version)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(version);

        Id = id;
        Version = version;
    }

    /// <summary>
    /// Gets the capability identifier.
    /// </summary>
    public CapabilityId Id { get; }

    /// <summary>
    /// Gets the exact capability version.
    /// </summary>
    public VersionInfo Version { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Id}@{Version}";
    }
}
