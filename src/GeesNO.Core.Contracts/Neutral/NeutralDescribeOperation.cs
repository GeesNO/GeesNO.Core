using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Requests bounded public descriptions for a neutral profile provider.
/// </summary>
public sealed record NeutralDescribeOperation : IOperation<NeutralDescribeResult>
{
    /// <summary>
    /// Creates a neutral describe operation.
    /// </summary>
    /// <param name="id">The operation identifier.</param>
    public NeutralDescribeOperation(OperationId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    /// <summary>Gets the operation identifier.</summary>
    public OperationId Id { get; }

    /// <summary>Gets the exact neutral describe capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Describe;
}
