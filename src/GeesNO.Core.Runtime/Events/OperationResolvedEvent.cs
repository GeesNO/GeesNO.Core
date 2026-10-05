using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Events;

internal sealed class OperationResolvedEvent : OperationRuntimeEvent
{
    internal OperationResolvedEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability,
        ProviderId providerId)
        : base(sequence, occurredAtUtc, operationId, capability)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ProviderId = providerId;
    }

    internal ProviderId ProviderId { get; }
}
