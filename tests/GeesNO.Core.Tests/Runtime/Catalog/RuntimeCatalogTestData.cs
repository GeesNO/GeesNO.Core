using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Runtime.Catalog;

internal static class RuntimeCatalogTestData
{
    internal static CapabilityReference Reference(string id = "system.ping", int major = 1)
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(major, 0, 0));
    }

    internal static CapabilityDescriptor Descriptor(
        CapabilityReference reference,
        string name = "Capability",
        string description = "Capability description",
        RiskLevel? riskLevel = null,
        bool hasSideEffects = false,
        ConfirmationPolicy? confirmationPolicy = null)
    {
        return new CapabilityDescriptor(
            reference,
            name,
            description,
            riskLevel ?? (hasSideEffects ? RiskLevel.Low : RiskLevel.ReadOnly),
            hasSideEffects,
            confirmationPolicy ?? (hasSideEffects
                ? ConfirmationPolicy.RiskBased
                : ConfirmationPolicy.NotRequired));
    }

    internal static CapabilitySupport Support(
        CapabilityReference reference,
        CapabilitySupportLevel supportLevel = CapabilitySupportLevel.Native,
        UndoSupportLevel undoSupportLevel = UndoSupportLevel.None)
    {
        var restrictions = supportLevel is CapabilitySupportLevel.Partial or CapabilitySupportLevel.Emulated
            ? [new CapabilityRestriction("limit.size", "Size is limited")]
            : Array.Empty<CapabilityRestriction>();
        var unavailableReason = supportLevel == CapabilitySupportLevel.Unavailable
            ? "Dependency unavailable"
            : null;

        return new CapabilitySupport(
            reference,
            supportLevel,
            undoSupportLevel,
            restrictions,
            unavailableReason);
    }

    internal static CapabilityManifestEntry Entry(
        CapabilityReference reference,
        CapabilitySupportLevel supportLevel = CapabilitySupportLevel.Native,
        UndoSupportLevel undoSupportLevel = UndoSupportLevel.None,
        CapabilityDescriptor? descriptor = null)
    {
        return new CapabilityManifestEntry(
            descriptor ?? Descriptor(reference, hasSideEffects: undoSupportLevel != UndoSupportLevel.None),
            Support(reference, supportLevel, undoSupportLevel));
    }

    internal static CapabilityManifest Manifest(
        string providerId,
        IEnumerable<CapabilityManifestEntry> entries,
        int versionMajor = 1)
    {
        return new CapabilityManifest(
            ProviderId.Parse(providerId),
            new VersionInfo(versionMajor, 0, 0),
            entries);
    }
}

internal sealed record CatalogTestResult(string Value);

internal sealed record CatalogTestOperation(
    OperationId Id,
    CapabilityReference Capability) : IOperation<CatalogTestResult>;

internal class CatalogTestTool : ITool<CatalogTestOperation, CatalogTestResult>
{
    internal CatalogTestTool(CapabilityReference capability)
    {
        Capability = capability;
    }

    public CapabilityReference Capability { get; protected set; }

    public Task<Result<CatalogTestResult>> ExecuteAsync(
        CatalogTestOperation operation,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<CatalogTestResult>.Success(new CatalogTestResult("ok")));
    }
}

internal sealed class MutableCatalogTestTool : CatalogTestTool
{
    internal MutableCatalogTestTool(CapabilityReference capability)
        : base(capability)
    {
    }

    internal void ChangeCapability(CapabilityReference capability)
    {
        Capability = capability;
    }
}
