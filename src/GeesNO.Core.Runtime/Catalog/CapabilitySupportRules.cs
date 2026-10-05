using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Runtime.Catalog;

internal static class CapabilitySupportRules
{
    internal static bool IsExecutable(CapabilitySupportLevel supportLevel)
    {
        return supportLevel is CapabilitySupportLevel.Native
            or CapabilitySupportLevel.Partial
            or CapabilitySupportLevel.Emulated;
    }

    internal static bool IsConsistent(
        CapabilityDescriptor descriptor,
        CapabilitySupport support)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(support);

        if (descriptor.Reference != support.Capability
            || !Enum.IsDefined(support.SupportLevel)
            || !Enum.IsDefined(support.UndoSupportLevel)
            || (!descriptor.HasSideEffects && support.UndoSupportLevel != UndoSupportLevel.None)
            || (!IsExecutable(support.SupportLevel) && support.UndoSupportLevel != UndoSupportLevel.None))
        {
            return false;
        }

        var hasUnavailableReason = !string.IsNullOrWhiteSpace(support.UnavailableReason);

        return support.SupportLevel switch
        {
            CapabilitySupportLevel.Native =>
                support.Restrictions.Count == 0 && support.UnavailableReason is null,
            CapabilitySupportLevel.Partial or CapabilitySupportLevel.Emulated =>
                support.Restrictions.Count > 0 && support.UnavailableReason is null,
            CapabilitySupportLevel.Unsupported =>
                support.Restrictions.Count == 0 && support.UnavailableReason is null,
            CapabilitySupportLevel.Unavailable =>
                support.Restrictions.Count == 0 && hasUnavailableReason,
            _ => false
        };
    }
}
