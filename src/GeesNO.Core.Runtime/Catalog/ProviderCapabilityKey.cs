using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed record ProviderCapabilityKey
{
    internal ProviderCapabilityKey(
        ProviderId providerId,
        CapabilityReference capability)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(capability);

        ProviderId = providerId;
        Capability = capability;
    }

    internal ProviderId ProviderId { get; }

    internal CapabilityReference Capability { get; }

    public override string ToString()
    {
        return $"{ProviderId}::{Capability}";
    }
}
