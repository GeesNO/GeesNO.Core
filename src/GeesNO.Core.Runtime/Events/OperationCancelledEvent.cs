using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Events;

internal sealed class OperationCancelledEvent : OperationRuntimeEvent
{
    internal OperationCancelledEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability,
        ProviderId? providerId = null,
        Type? declaredResultType = null)
        : base(sequence, occurredAtUtc, operationId, capability)
    {
        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
    }

    internal ProviderId? ProviderId { get; }

    internal Type? DeclaredResultType { get; }
}
