using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Reference.Simulator;

/// <summary>
/// Returns public metadata for the neutral reference simulator.
/// </summary>
public sealed class NeutralDescribeTool : ITool<NeutralDescribeOperation, NeutralDescribeResult>
{
    /// <summary>Gets the exact neutral describe capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Describe;

    /// <inheritdoc />
    public Task<Result<NeutralDescribeResult>> ExecuteAsync(
        NeutralDescribeOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var result = new NeutralDescribeResult(
            "GeesNO neutral reference simulator",
            "Reference implementation of neutral echo, describe and validate capabilities.");

        return Task.FromResult(Result<NeutralDescribeResult>.Success(result));
    }
}
