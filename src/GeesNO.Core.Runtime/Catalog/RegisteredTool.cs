using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed class RegisteredTool
{
    internal RegisteredTool(
        ProviderCapabilityKey key,
        CapabilityManifestEntry manifestEntry,
        ITool tool,
        IToolInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(manifestEntry);
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(invoker);

        if (key.Capability != manifestEntry.Descriptor.Reference
            || manifestEntry.Support.Capability != key.Capability
            || tool.Capability is null
            || tool.Capability != key.Capability
            || invoker.Key != key
            || invoker.OperationType is null
            || invoker.ResultType is null)
        {
            throw new ArgumentException("The registered tool association is inconsistent.", nameof(tool));
        }

        Key = key;
        ManifestEntry = manifestEntry;
        Tool = tool;
        Invoker = invoker;
    }

    internal ProviderCapabilityKey Key { get; }

    internal CapabilityManifestEntry ManifestEntry { get; }

    internal ITool Tool { get; }

    internal IToolInvoker Invoker { get; }

    internal Type OperationType => Invoker.OperationType;

    internal Type ResultType => Invoker.ResultType;
}
