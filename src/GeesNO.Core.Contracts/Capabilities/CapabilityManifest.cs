using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Joins a capability descriptor with a provider support declaration.
/// </summary>
public sealed class CapabilityManifestEntry
{
    /// <summary>
    /// Creates a coherent manifest entry.
    /// </summary>
    /// <param name="descriptor">The capability descriptor.</param>
    /// <param name="support">The provider support declaration.</param>
    public CapabilityManifestEntry(CapabilityDescriptor descriptor, CapabilitySupport support)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(support);

        if (descriptor.Reference != support.Capability)
        {
            throw new ArgumentException(
                "The descriptor and support must reference the same capability.",
                nameof(support));
        }

        if (!descriptor.HasSideEffects && support.UndoSupportLevel != UndoSupportLevel.None)
        {
            throw new ArgumentException(
                "A capability without side effects cannot declare provider undo support.",
                nameof(support));
        }

        Descriptor = descriptor;
        Support = support;
    }

    /// <summary>Gets the capability descriptor.</summary>
    public CapabilityDescriptor Descriptor { get; }

    /// <summary>Gets the provider support declaration.</summary>
    public CapabilitySupport Support { get; }
}

/// <summary>
/// Declares the exact capabilities supported by a provider version.
/// </summary>
public sealed class CapabilityManifest
{
    /// <summary>
    /// Creates a validated, deterministically ordered capability manifest.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="providerVersion">The exact provider version.</param>
    /// <param name="capabilities">The provider capability entries.</param>
    public CapabilityManifest(
        ProviderId providerId,
        VersionInfo providerVersion,
        IEnumerable<CapabilityManifestEntry> capabilities)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(providerVersion);
        ArgumentNullException.ThrowIfNull(capabilities);

        var entries = new List<CapabilityManifestEntry>();
        var references = new HashSet<CapabilityReference>();

        foreach (var capability in capabilities)
        {
            if (capability is null)
            {
                throw new ArgumentException("Capabilities cannot contain null elements.", nameof(capabilities));
            }

            if (!references.Add(capability.Descriptor.Reference))
            {
                throw new ArgumentException("Capability references must be unique.", nameof(capabilities));
            }

            entries.Add(capability);
        }

        var orderedEntries = entries
            .OrderBy(entry => entry.Descriptor.Reference.Id.Value, StringComparer.Ordinal)
            .ThenBy(entry => entry.Descriptor.Reference.Version)
            .ToArray();

        ProviderId = providerId;
        ProviderVersion = providerVersion;
        Capabilities = Array.AsReadOnly(orderedEntries);
    }

    /// <summary>Gets the provider identifier.</summary>
    public ProviderId ProviderId { get; }

    /// <summary>Gets the exact provider version.</summary>
    public VersionInfo ProviderVersion { get; }

    /// <summary>Gets the immutable, deterministically ordered capability entries.</summary>
    public IReadOnlyList<CapabilityManifestEntry> Capabilities { get; }
}
