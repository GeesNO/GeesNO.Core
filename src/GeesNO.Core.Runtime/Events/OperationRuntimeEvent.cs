using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Events;

internal abstract class OperationRuntimeEvent : RuntimeEvent
{
    private protected OperationRuntimeEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability)
        : base(sequence, occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(capability);

        OperationId = operationId;
        Capability = capability;
    }

    internal OperationId OperationId { get; }

    internal CapabilityReference Capability { get; }
}
