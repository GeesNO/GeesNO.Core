using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Runtime.Execution;

internal static class MinimumSafetyRules
{
    internal static Error? Evaluate(
        CapabilityDescriptor descriptor,
        CapabilitySupport support)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(support);

        if (!CapabilitySupportRules.IsExecutable(support.SupportLevel))
        {
            return RuntimeExecutionErrors.CapabilityNotExecutable();
        }

        return descriptor.RiskLevel == RiskLevel.ReadOnly
               && !descriptor.HasSideEffects
               && descriptor.ConfirmationPolicy != ConfirmationPolicy.AlwaysRequired
            ? null
            : RuntimeExecutionErrors.ConfirmationRequired();
    }
}
