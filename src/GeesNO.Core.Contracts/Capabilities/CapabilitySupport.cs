using GeesNO.Core.Contracts.Internal;

namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Describes a stable limitation of a provider's capability support.
/// </summary>
public sealed record CapabilityRestriction
{
    /// <summary>
    /// Creates a capability restriction.
    /// </summary>
    /// <param name="code">The stable restriction code.</param>
    /// <param name="description">The human-readable restriction description.</param>
    public CapabilityRestriction(string code, string description)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (!DottedIdentifierRules.IsValid(code))
        {
            throw new ArgumentException("The restriction code is not canonical.", nameof(code));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Code = code;
        Description = description.Trim();
    }

    /// <summary>Gets the stable restriction code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable restriction description.</summary>
    public string Description { get; }
}

/// <summary>
/// Describes how a provider supports an exact capability.
/// </summary>
public sealed class CapabilitySupport
{
    /// <summary>
    /// Creates a validated capability support declaration.
    /// </summary>
    /// <param name="capability">The exact capability reference.</param>
    /// <param name="supportLevel">The provider support level.</param>
    /// <param name="undoSupportLevel">The provider's undo support level.</param>
    /// <param name="restrictions">The declared support restrictions.</param>
    /// <param name="unavailableReason">The reason an unavailable capability cannot currently execute.</param>
    public CapabilitySupport(
        CapabilityReference capability,
        CapabilitySupportLevel supportLevel,
        UndoSupportLevel undoSupportLevel,
        IEnumerable<CapabilityRestriction> restrictions,
        string? unavailableReason)
    {
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(restrictions);

        if (!Enum.IsDefined(supportLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(supportLevel),
                supportLevel,
                "The capability support level is not defined.");
        }

        if (!Enum.IsDefined(undoSupportLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(undoSupportLevel),
                undoSupportLevel,
                "The undo support level is not defined.");
        }

        var restrictionList = new List<CapabilityRestriction>();
        var restrictionCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var restriction in restrictions)
        {
            if (restriction is null)
            {
                throw new ArgumentException("Restrictions cannot contain null elements.", nameof(restrictions));
            }

            if (!restrictionCodes.Add(restriction.Code))
            {
                throw new ArgumentException("Restriction codes must be unique.", nameof(restrictions));
            }

            restrictionList.Add(restriction);
        }

        ValidateLevelInvariants(supportLevel, undoSupportLevel, restrictionList.Count, unavailableReason);

        Capability = capability;
        SupportLevel = supportLevel;
        UndoSupportLevel = undoSupportLevel;
        Restrictions = Array.AsReadOnly(restrictionList.ToArray());
        UnavailableReason = unavailableReason?.Trim();
    }

    /// <summary>Gets the exact capability reference.</summary>
    public CapabilityReference Capability { get; }

    /// <summary>Gets the provider support level.</summary>
    public CapabilitySupportLevel SupportLevel { get; }

    /// <summary>Gets the provider's undo support level.</summary>
    public UndoSupportLevel UndoSupportLevel { get; }

    /// <summary>Gets the immutable support restrictions.</summary>
    public IReadOnlyList<CapabilityRestriction> Restrictions { get; }

    /// <summary>Gets the reason an unavailable capability cannot currently execute.</summary>
    public string? UnavailableReason { get; }

    private static void ValidateLevelInvariants(
        CapabilitySupportLevel supportLevel,
        UndoSupportLevel undoSupportLevel,
        int restrictionCount,
        string? unavailableReason)
    {
        if (supportLevel is CapabilitySupportLevel.Unsupported or CapabilitySupportLevel.Unavailable
            && undoSupportLevel != UndoSupportLevel.None)
        {
            throw new ArgumentException(
                "Unsupported and unavailable capabilities cannot declare undo support.",
                nameof(undoSupportLevel));
        }

        if (supportLevel is CapabilitySupportLevel.Partial or CapabilitySupportLevel.Emulated)
        {
            if (restrictionCount == 0)
            {
                throw new ArgumentException(
                    "Partial and emulated support require at least one restriction.",
                    nameof(restrictionCount));
            }

            if (unavailableReason is not null)
            {
                throw new ArgumentException(
                    "Only unavailable support can declare an unavailable reason.",
                    nameof(unavailableReason));
            }

            return;
        }

        if (supportLevel == CapabilitySupportLevel.Unavailable)
        {
            if (restrictionCount != 0)
            {
                throw new ArgumentException(
                    "Unavailable support cannot declare restrictions.",
                    nameof(restrictionCount));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(unavailableReason);
            return;
        }

        if (restrictionCount != 0)
        {
            throw new ArgumentException(
                "Native and unsupported support cannot declare restrictions.",
                nameof(restrictionCount));
        }

        if (unavailableReason is not null)
        {
            throw new ArgumentException(
                "Only unavailable support can declare an unavailable reason.",
                nameof(unavailableReason));
        }
    }
}
