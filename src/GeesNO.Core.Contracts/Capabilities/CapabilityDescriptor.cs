namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Describes the stable public semantics and safety characteristics of a capability.
/// </summary>
public sealed class CapabilityDescriptor
{
    /// <summary>
    /// Creates a validated capability descriptor.
    /// </summary>
    /// <param name="reference">The exact capability reference.</param>
    /// <param name="name">The human-readable capability name.</param>
    /// <param name="description">The human-readable capability description.</param>
    /// <param name="riskLevel">The capability risk level.</param>
    /// <param name="hasSideEffects">Whether execution may change state.</param>
    /// <param name="confirmationPolicy">The minimum confirmation policy.</param>
    public CapabilityDescriptor(
        CapabilityReference reference,
        string name,
        string description,
        RiskLevel riskLevel,
        bool hasSideEffects,
        ConfirmationPolicy confirmationPolicy)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!Enum.IsDefined(riskLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(riskLevel), riskLevel, "The risk level is not defined.");
        }

        if (!Enum.IsDefined(confirmationPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmationPolicy),
                confirmationPolicy,
                "The confirmation policy is not defined.");
        }

        if (riskLevel == RiskLevel.ReadOnly && hasSideEffects)
        {
            throw new ArgumentException("A read-only capability cannot have side effects.", nameof(hasSideEffects));
        }

        if (riskLevel == RiskLevel.Critical && confirmationPolicy == ConfirmationPolicy.NotRequired)
        {
            throw new ArgumentException(
                "A critical capability must require confirmation or risk-based confirmation.",
                nameof(confirmationPolicy));
        }

        Reference = reference;
        Name = name.Trim();
        Description = description.Trim();
        RiskLevel = riskLevel;
        HasSideEffects = hasSideEffects;
        ConfirmationPolicy = confirmationPolicy;
    }

    /// <summary>Gets the exact capability reference.</summary>
    public CapabilityReference Reference { get; }

    /// <summary>Gets the human-readable capability name.</summary>
    public string Name { get; }

    /// <summary>Gets the human-readable capability description.</summary>
    public string Description { get; }

    /// <summary>Gets the capability risk level.</summary>
    public RiskLevel RiskLevel { get; }

    /// <summary>Gets a value indicating whether execution may change state.</summary>
    public bool HasSideEffects { get; }

    /// <summary>Gets the minimum confirmation policy.</summary>
    public ConfirmationPolicy ConfirmationPolicy { get; }

}
