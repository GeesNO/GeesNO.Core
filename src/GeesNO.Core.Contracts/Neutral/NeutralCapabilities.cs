using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Exposes the exact references, descriptors, and native manifest entries of the neutral profile.
/// </summary>
public static class NeutralCapabilities
{
    /// <summary>Gets the exact neutral echo capability.</summary>
    public static CapabilityReference Echo { get; } = CreateReference("neutral.echo");

    /// <summary>Gets the exact neutral describe capability.</summary>
    public static CapabilityReference Describe { get; } = CreateReference("neutral.describe");

    /// <summary>Gets the exact neutral validate capability.</summary>
    public static CapabilityReference Validate { get; } = CreateReference("neutral.validate");

    /// <summary>Gets the neutral echo descriptor.</summary>
    public static CapabilityDescriptor EchoDescriptor { get; } = new(
        Echo,
        "Neutral Echo",
        "Returns the supplied neutral text through a typed operation and result.",
        RiskLevel.ReadOnly,
        hasSideEffects: false,
        ConfirmationPolicy.NotRequired);

    /// <summary>Gets the neutral describe descriptor.</summary>
    public static CapabilityDescriptor DescribeDescriptor { get; } = new(
        Describe,
        "Neutral Describe",
        "Returns bounded public descriptions of a provider and its neutral capability profile.",
        RiskLevel.ReadOnly,
        hasSideEffects: false,
        ConfirmationPolicy.NotRequired);

    /// <summary>Gets the neutral validate descriptor.</summary>
    public static CapabilityDescriptor ValidateDescriptor { get; } = new(
        Validate,
        "Neutral Validate",
        "Validates bounded neutral text against an explicitly requested maximum length.",
        RiskLevel.ReadOnly,
        hasSideEffects: false,
        ConfirmationPolicy.NotRequired);

    /// <summary>Gets the native manifest entry for neutral echo.</summary>
    public static CapabilityManifestEntry EchoEntry { get; } = new(
        EchoDescriptor,
        NativeSupport(Echo));

    /// <summary>Gets the native manifest entry for neutral describe.</summary>
    public static CapabilityManifestEntry DescribeEntry { get; } = new(
        DescribeDescriptor,
        NativeSupport(Describe));

    /// <summary>Gets the native manifest entry for neutral validate.</summary>
    public static CapabilityManifestEntry ValidateEntry { get; } = new(
        ValidateDescriptor,
        NativeSupport(Validate));

    /// <summary>
    /// Creates the native, non-undoable support declaration for a neutral profile capability.
    /// </summary>
    /// <param name="capability">One of the exact neutral profile capabilities.</param>
    /// <returns>A native support declaration without restrictions or undo support.</returns>
    public static CapabilitySupport NativeSupport(CapabilityReference capability)
    {
        ArgumentNullException.ThrowIfNull(capability);

        if (capability != Echo && capability != Describe && capability != Validate)
        {
            throw new ArgumentException(
                "The capability is not part of the neutral operations profile.",
                nameof(capability));
        }

        return new CapabilitySupport(
            capability,
            CapabilitySupportLevel.Native,
            UndoSupportLevel.None,
            [],
            unavailableReason: null);
    }

    private static CapabilityReference CreateReference(string id)
    {
        return new CapabilityReference(
            CapabilityId.Parse(id),
            new VersionInfo(1, 0, 0));
    }
}
