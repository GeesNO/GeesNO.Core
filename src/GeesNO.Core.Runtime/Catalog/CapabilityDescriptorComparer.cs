using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed class CapabilityDescriptorComparer : IEqualityComparer<CapabilityDescriptor>
{
    internal static CapabilityDescriptorComparer Instance { get; } = new();

    private CapabilityDescriptorComparer()
    {
    }

    public bool Equals(CapabilityDescriptor? x, CapabilityDescriptor? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Reference == y.Reference
            && string.Equals(x.Name, y.Name, StringComparison.Ordinal)
            && string.Equals(x.Description, y.Description, StringComparison.Ordinal)
            && x.RiskLevel == y.RiskLevel
            && x.HasSideEffects == y.HasSideEffects
            && x.ConfirmationPolicy == y.ConfirmationPolicy;
    }

    public int GetHashCode(CapabilityDescriptor obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var hash = new HashCode();
        hash.Add(obj.Reference);
        hash.Add(obj.Name, StringComparer.Ordinal);
        hash.Add(obj.Description, StringComparer.Ordinal);
        hash.Add(obj.RiskLevel);
        hash.Add(obj.HasSideEffects);
        hash.Add(obj.ConfirmationPolicy);
        return hash.ToHashCode();
    }
}
