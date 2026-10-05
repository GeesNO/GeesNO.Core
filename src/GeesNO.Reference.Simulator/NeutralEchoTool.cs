using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Reference.Simulator;

/// <summary>
/// Echoes neutral text without side effects.
/// </summary>
public sealed class NeutralEchoTool : ITool<NeutralEchoOperation, NeutralEchoResult>
{
    private static readonly Error EmptyTextError = new(
        "neutral.echo.empty-text",
        "Neutral echo text cannot be empty.");

    /// <summary>Gets the exact neutral echo capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Echo;

    /// <inheritdoc />
    public Task<Result<NeutralEchoResult>> ExecuteAsync(
        NeutralEchoOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        return string.IsNullOrEmpty(operation.Text)
            ? Task.FromResult(Result<NeutralEchoResult>.Failure(EmptyTextError))
            : Task.FromResult(Result<NeutralEchoResult>.Success(new NeutralEchoResult(operation.Text)));
    }
}
