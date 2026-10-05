using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Reference.Simulator;

/// <summary>
/// Exposes the stable provider identifier used by the non-normative neutral reference simulator.
/// </summary>
public static class NeutralSimulatorProvider
{
    /// <summary>Gets the exact reference simulator provider identifier.</summary>
    public static ProviderId Id { get; } = ProviderId.Parse("geesno.reference-simulator");
}
