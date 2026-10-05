using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime.System;

internal static class SystemPingManifestFactory
{
    internal static CapabilityManifest Create(
        ProviderId providerId,
        VersionInfo providerVersion)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(providerVersion);

        var reference = SystemPingOperation.ContractCapability;
        var descriptor = new CapabilityDescriptor(
            reference,
            "System Ping",
            "Verifies local typed Runtime execution without testing a network or external service.",
            RiskLevel.ReadOnly,
            hasSideEffects: false,
            ConfirmationPolicy.NotRequired);
        var support = new CapabilitySupport(
            reference,
            CapabilitySupportLevel.Native,
            UndoSupportLevel.None,
            restrictions: [],
            unavailableReason: null);

        return new CapabilityManifest(
            providerId,
            providerVersion,
            [new CapabilityManifestEntry(descriptor, support)]);
    }
}
