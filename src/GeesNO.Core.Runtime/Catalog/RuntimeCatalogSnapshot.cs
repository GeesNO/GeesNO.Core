using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed class RuntimeCatalogSnapshot
{
    private readonly FrozenDictionary<ProviderId, CapabilityManifest> _manifestsByProvider;
    private readonly FrozenDictionary<ProviderCapabilityKey, CapabilityManifestEntry> _entriesByKey;
    private readonly FrozenDictionary<ProviderCapabilityKey, ITool> _toolsByKey;
    private readonly FrozenDictionary<ProviderCapabilityKey, IToolInvoker> _invokersByKey;
    private readonly FrozenDictionary<CapabilityReference, CapabilityDescriptor> _canonicalDescriptors;
    private readonly FrozenDictionary<CapabilityReference, IReadOnlyList<ProviderCapabilityKey>> _implementations;
    private readonly FrozenDictionary<CapabilityReference, IReadOnlyList<ProviderCapabilityKey>> _executableImplementations;

    internal RuntimeCatalogSnapshot(
        IEnumerable<CapabilityManifest> manifests,
        IReadOnlyDictionary<ProviderCapabilityKey, RegisteredTool> registeredTools)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(registeredTools);

        var orderedManifests = manifests
            .OrderBy(manifest => manifest.ProviderId.Value, StringComparer.Ordinal)
            .ToArray();
        var entries = orderedManifests
            .SelectMany(manifest => manifest.Capabilities.Select(entry => new CatalogEntry(
                new ProviderCapabilityKey(manifest.ProviderId, entry.Descriptor.Reference),
                entry)))
            .ToArray();

        Manifests = Array.AsReadOnly(orderedManifests);
        _manifestsByProvider = orderedManifests.ToFrozenDictionary(manifest => manifest.ProviderId);
        _entriesByKey = entries.ToFrozenDictionary(item => item.Key, item => item.Entry);
        _toolsByKey = registeredTools.ToFrozenDictionary(item => item.Key, item => item.Value.Tool);
        _invokersByKey = registeredTools.ToFrozenDictionary(item => item.Key, item => item.Value.Invoker);
        _canonicalDescriptors = BuildCanonicalDescriptors(entries);
        _implementations = BuildImplementations(entries, executableOnly: false);
        _executableImplementations = BuildImplementations(entries, executableOnly: true);
    }

    internal IReadOnlyList<CapabilityManifest> Manifests { get; }

    internal bool TryGetManifest(
        ProviderId providerId,
        [NotNullWhen(true)] out CapabilityManifest? manifest)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        return _manifestsByProvider.TryGetValue(providerId, out manifest);
    }

    internal bool TryGetEntry(
        ProviderCapabilityKey key,
        [NotNullWhen(true)] out CapabilityManifestEntry? entry)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _entriesByKey.TryGetValue(key, out entry);
    }

    internal bool TryGetTool(
        ProviderCapabilityKey key,
        [NotNullWhen(true)] out ITool? tool)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _toolsByKey.TryGetValue(key, out tool);
    }

    internal bool TryGetInvoker(
        ProviderCapabilityKey key,
        [NotNullWhen(true)] out IToolInvoker? invoker)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _invokersByKey.TryGetValue(key, out invoker);
    }

    internal bool TryGetCanonicalDescriptor(
        CapabilityReference capability,
        [NotNullWhen(true)] out CapabilityDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(capability);
        return _canonicalDescriptors.TryGetValue(capability, out descriptor);
    }

    internal IReadOnlyList<ProviderCapabilityKey> GetImplementations(CapabilityReference capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        return _implementations.GetValueOrDefault(capability, EmptyImplementations);
    }

    internal IReadOnlyList<ProviderCapabilityKey> GetExecutableImplementations(CapabilityReference capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        return _executableImplementations.GetValueOrDefault(capability, EmptyImplementations);
    }

    private static ReadOnlyCollection<ProviderCapabilityKey> EmptyImplementations { get; }
        = Array.AsReadOnly(Array.Empty<ProviderCapabilityKey>());

    private static FrozenDictionary<CapabilityReference, CapabilityDescriptor> BuildCanonicalDescriptors(
        IEnumerable<CatalogEntry> entries)
    {
        return entries
            .GroupBy(item => item.Key.Capability)
            .ToFrozenDictionary(group => group.Key, group => group.First().Entry.Descriptor);
    }

    private static FrozenDictionary<CapabilityReference, IReadOnlyList<ProviderCapabilityKey>> BuildImplementations(
        IEnumerable<CatalogEntry> entries,
        bool executableOnly)
    {
        return entries
            .Where(item => !executableOnly || CapabilitySupportRules.IsExecutable(item.Entry.Support.SupportLevel))
            .GroupBy(item => item.Key.Capability)
            .ToFrozenDictionary(
                group => group.Key,
                group => (IReadOnlyList<ProviderCapabilityKey>)Array.AsReadOnly(group
                    .Select(item => item.Key)
                    .OrderBy(key => key.ProviderId.Value, StringComparer.Ordinal)
                    .ThenBy(key => key.Capability.Id.Value, StringComparer.Ordinal)
                    .ThenBy(key => key.Capability.Version)
                    .ToArray()));
    }

    private sealed record CatalogEntry(
        ProviderCapabilityKey Key,
        CapabilityManifestEntry Entry);
}
