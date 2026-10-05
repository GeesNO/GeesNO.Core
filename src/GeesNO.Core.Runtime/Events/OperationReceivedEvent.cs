using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Events;

internal sealed class OperationReceivedEvent : OperationRuntimeEvent
{
    internal OperationReceivedEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability)
        : base(sequence, occurredAtUtc, operationId, capability)
    {
    }
}
