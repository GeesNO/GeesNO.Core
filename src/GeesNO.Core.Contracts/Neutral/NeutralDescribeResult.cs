namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Returns bounded public descriptions of a neutral provider and capability profile.
/// </summary>
public sealed record NeutralDescribeResult
{
    /// <summary>Gets the maximum accepted length of each description.</summary>
    public const int MaximumDescriptionLength = 512;

    /// <summary>
    /// Creates a neutral describe result.
    /// </summary>
    /// <param name="providerDescription">The public provider description.</param>
    /// <param name="capabilityDescription">The public capability profile description.</param>
    public NeutralDescribeResult(string providerDescription, string capabilityDescription)
    {
        ProviderDescription = ValidateDescription(providerDescription, nameof(providerDescription));
        CapabilityDescription = ValidateDescription(capabilityDescription, nameof(capabilityDescription));
    }

    /// <summary>Gets the public provider description.</summary>
    public string ProviderDescription { get; }

    /// <summary>Gets the public capability profile description.</summary>
    public string CapabilityDescription { get; }

    private static string ValidateDescription(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > MaximumDescriptionLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Neutral descriptions cannot exceed {MaximumDescriptionLength} characters.");
        }

        return trimmed;
    }
}
