using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.System;

/// <summary>
/// Requests the stable system ping capability without host or transport context.
/// </summary>
public sealed record SystemPingOperation : IOperation<SystemPingResult>
{
    /// <summary>
    /// Gets the exact capability reference shared by every system ping operation.
    /// </summary>
    public static CapabilityReference ContractCapability { get; } = new(
        CapabilityId.Parse("system.ping"),
        new VersionInfo(1, 0, 0));

    /// <summary>
    /// Creates a system ping operation.
    /// </summary>
    /// <param name="id">The operation identifier.</param>
    public SystemPingOperation(OperationId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>Gets the operation identifier.</summary>
    public OperationId Id { get; }

    /// <summary>Gets the exact, non-configurable system ping capability.</summary>
    public CapabilityReference Capability => ContractCapability;
}
