using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Events;

internal sealed class OperationStartedEvent : OperationRuntimeEvent
{
    internal OperationStartedEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability,
        ProviderId providerId,
        Type declaredResultType)
        : base(sequence, occurredAtUtc, operationId, capability)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(declaredResultType);

        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
    }

    internal ProviderId ProviderId { get; }

    internal Type DeclaredResultType { get; }
}
