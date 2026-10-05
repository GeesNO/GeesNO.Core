using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Reference.Simulator;

/// <summary>
/// Validates neutral text against the explicit maximum length declared by the operation.
/// </summary>
public sealed class NeutralValidateTool : ITool<NeutralValidateOperation, NeutralValidationResult>
{
    private const string TooLongReason = "Input exceeds the requested maximum length.";

    /// <summary>Gets the exact neutral validate capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Validate;

    /// <inheritdoc />
    public Task<Result<NeutralValidationResult>> ExecuteAsync(
        NeutralValidateOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        var result = operation.Input.Length <= operation.MaxLength
            ? new NeutralValidationResult(isValid: true, reason: null)
            : new NeutralValidationResult(isValid: false, TooLongReason);

        return Task.FromResult(Result<NeutralValidationResult>.Success(result));
    }
}
