using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed class RuntimeCatalogBuilder
{
    private const string RegistrySealedCode = "runtime.registry-sealed";

    private readonly Dictionary<ProviderId, CapabilityManifest> _manifests = [];
    private readonly Dictionary<ProviderCapabilityKey, RegisteredTool> _registeredTools = [];
    private RuntimeCatalogSnapshot? _snapshot;

    internal RuntimeCatalogBuildState State { get; private set; } = RuntimeCatalogBuildState.Building;

    internal void RegisterManifest(CapabilityManifest manifest)
    {
        EnsureBuilding();
        ArgumentNullException.ThrowIfNull(manifest);

        if (!_manifests.TryAdd(manifest.ProviderId, manifest))
        {
            throw new ArgumentException(
                $"A manifest is already registered for provider '{manifest.ProviderId}'.",
                nameof(manifest));
        }
    }

    internal void RegisterTool(ProviderId providerId, ITool tool)
    {
        EnsureBuilding();
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(tool);

        if (!_manifests.TryGetValue(providerId, out var manifest))
        {
            throw new InvalidOperationException($"Provider '{providerId}' is not registered.");
        }

        var capability = tool.Capability;
        if (capability is null)
        {
            throw new ArgumentException("The tool capability cannot be null.", nameof(tool));
        }

        var entry = manifest.Capabilities.SingleOrDefault(candidate =>
            candidate.Descriptor.Reference == capability);
        if (entry is null)
        {
            throw new ArgumentException(
                $"Capability '{capability}' is not declared by provider '{providerId}'.",
                nameof(tool));
        }

        if (!CapabilitySupportRules.IsExecutable(entry.Support.SupportLevel))
        {
            throw new ArgumentException(
                $"Capability '{capability}' is not executable for provider '{providerId}'.",
                nameof(tool));
        }

        var key = new ProviderCapabilityKey(providerId, capability);
        if (_registeredTools.ContainsKey(key))
        {
            throw new ArgumentException($"A tool is already registered for '{key}'.", nameof(tool));
        }

        var invoker = ToolInvokerFactory.Create(key, tool);
        var registeredTool = new RegisteredTool(key, entry, tool, invoker);

        _registeredTools.Add(key, registeredTool);
    }

    internal RuntimeCatalogSnapshot Seal()
    {
        if (State == RuntimeCatalogBuildState.Sealed)
        {
            return _snapshot
                ?? throw new InvalidOperationException("The sealed runtime catalog has no snapshot.");
        }

        var issues = ValidateCatalog();
        if (issues.Count > 0)
        {
            throw new RuntimeCatalogValidationException(issues);
        }

        var snapshot = new RuntimeCatalogSnapshot(
            _manifests.Values.ToArray(),
            new Dictionary<ProviderCapabilityKey, RegisteredTool>(_registeredTools));

        _snapshot = snapshot;
        State = RuntimeCatalogBuildState.Sealed;
        return snapshot;
    }

    private List<string> ValidateCatalog()
    {
        var issues = new List<string>();
        var entries = _manifests.Values
            .OrderBy(manifest => manifest.ProviderId.Value, StringComparer.Ordinal)
            .SelectMany(manifest => manifest.Capabilities.Select(entry => new
            {
                Manifest = manifest,
                Entry = entry,
                Key = new ProviderCapabilityKey(manifest.ProviderId, entry.Descriptor.Reference)
            }))
            .ToArray();

        foreach (var item in entries)
        {
            if (!CapabilitySupportRules.IsConsistent(item.Entry.Descriptor, item.Entry.Support))
            {
                issues.Add($"runtime.manifest-entry-incoherent:{item.Key}");
            }

            if (CapabilitySupportRules.IsExecutable(item.Entry.Support.SupportLevel)
                && !_registeredTools.ContainsKey(item.Key))
            {
                issues.Add($"runtime.tool-not-found:{item.Key}");
            }
        }

        foreach (var group in entries.GroupBy(item => item.Entry.Descriptor.Reference))
        {
            var orderedGroup = group
                .OrderBy(item => item.Manifest.ProviderId.Value, StringComparer.Ordinal)
                .ToArray();
            var canonical = orderedGroup[0].Entry.Descriptor;

            foreach (var item in orderedGroup.Skip(1))
            {
                if (!CapabilityDescriptorComparer.Instance.Equals(canonical, item.Entry.Descriptor))
                {
                    issues.Add($"runtime.descriptor-incompatible:{item.Key}");
                }
            }
        }

        foreach (var pair in _registeredTools
                     .OrderBy(item => item.Key.ProviderId.Value, StringComparer.Ordinal)
                     .ThenBy(item => item.Key.Capability.Id.Value, StringComparer.Ordinal)
                     .ThenBy(item => item.Key.Capability.Version))
        {
            var key = pair.Key;
            var registeredTool = pair.Value;

            if (!_manifests.TryGetValue(key.ProviderId, out var manifest))
            {
                issues.Add($"runtime.tool-registration-incoherent:{key}");
                continue;
            }

            var entry = manifest.Capabilities.SingleOrDefault(candidate =>
                candidate.Descriptor.Reference == key.Capability);
            if (entry is null
                || !ReferenceEquals(entry, registeredTool.ManifestEntry)
                || registeredTool.Key != key
                || registeredTool.Invoker is null
                || registeredTool.Invoker.Key != key
                || registeredTool.OperationType is null
                || registeredTool.ResultType is null
                || registeredTool.ManifestEntry.Descriptor.Reference != key.Capability
                || registeredTool.ManifestEntry.Support.Capability != key.Capability)
            {
                issues.Add($"runtime.tool-registration-incoherent:{key}");
            }

            if (!CapabilitySupportRules.IsExecutable(registeredTool.ManifestEntry.Support.SupportLevel))
            {
                issues.Add($"runtime.capability-not-executable:{key}");
            }

            if (registeredTool.Tool.Capability is null
                || registeredTool.Tool.Capability != key.Capability)
            {
                issues.Add($"runtime.tool-capability-changed:{key}");
            }
        }

        return issues;
    }

    private void EnsureBuilding()
    {
        if (State != RuntimeCatalogBuildState.Building)
        {
            throw new InvalidOperationException(
                $"{RegistrySealedCode}: The runtime catalog is already sealed.");
        }
    }
}
