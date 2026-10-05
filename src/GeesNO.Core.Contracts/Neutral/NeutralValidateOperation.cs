using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Requests bounded validation of neutral text against an explicit maximum length.
/// </summary>
public sealed record NeutralValidateOperation : IOperation<NeutralValidationResult>
{
    /// <summary>Gets the largest supported input or requested maximum length.</summary>
    public const int MaximumSupportedLength = 4096;

    /// <summary>
    /// Creates a neutral validate operation.
    /// </summary>
    /// <param name="id">The operation identifier.</param>
    /// <param name="input">The bounded neutral input.</param>
    /// <param name="maxLength">The positive requested validation limit.</param>
    public NeutralValidateOperation(OperationId id, string input, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length > MaximumSupportedLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                $"Neutral validation input cannot exceed {MaximumSupportedLength} characters.");
        }

        if (maxLength is <= 0 or > MaximumSupportedLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLength),
                $"The maximum length must be between 1 and {MaximumSupportedLength} characters.");
        }

        Id = id;
        Input = input;
        MaxLength = maxLength;
    }

    /// <summary>Gets the operation identifier.</summary>
    public OperationId Id { get; }

    /// <summary>Gets the exact neutral validate capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Validate;

    /// <summary>Gets the neutral input.</summary>
    public string Input { get; }

    /// <summary>Gets the requested validation limit.</summary>
    public int MaxLength { get; }
}
