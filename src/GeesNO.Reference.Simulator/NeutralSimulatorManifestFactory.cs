using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Primitives;

namespace GeesNO.Reference.Simulator;

/// <summary>
/// Creates the neutral capability manifest implemented by the reference simulator.
/// </summary>
public static class NeutralSimulatorManifestFactory
{
    /// <summary>
    /// Creates a neutral simulator manifest for the default or supplied provider.
    /// </summary>
    /// <param name="providerId">The optional provider identifier to use in the manifest.</param>
    /// <returns>A manifest declaring the three neutral capabilities.</returns>
    public static CapabilityManifest Create(ProviderId? providerId = null)
    {
        return new CapabilityManifest(
            providerId ?? NeutralSimulatorProvider.Id,
            new VersionInfo(1, 0, 0),
            [
                NeutralCapabilities.EchoEntry,
                NeutralCapabilities.DescribeEntry,
                NeutralCapabilities.ValidateEntry
            ]);
    }
}
