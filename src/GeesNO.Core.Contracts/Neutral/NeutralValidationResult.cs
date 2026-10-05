namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Reports whether neutral input is valid and exposes only a bounded public failure reason.
/// </summary>
public sealed record NeutralValidationResult
{
    /// <summary>Gets the maximum accepted failure reason length.</summary>
    public const int MaximumReasonLength = 512;

    /// <summary>
    /// Creates a neutral validation result.
    /// </summary>
    /// <param name="isValid">Whether the input is valid.</param>
    /// <param name="reason">A bounded public reason required only for invalid input.</param>
    public NeutralValidationResult(bool isValid, string? reason)
    {
        if (isValid && reason is not null)
        {
            throw new ArgumentException("A valid result cannot include a reason.", nameof(reason));
        }

        if (!isValid)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            var trimmed = reason.Trim();
            if (trimmed.Length > MaximumReasonLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason),
                    $"A validation reason cannot exceed {MaximumReasonLength} characters.");
            }

            Reason = trimmed;
        }

        IsValid = isValid;
    }

    /// <summary>Gets a value indicating whether the neutral input is valid.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the bounded public failure reason, or null for a valid result.</summary>
    public string? Reason { get; }
}
