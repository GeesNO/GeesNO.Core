namespace GeesNO.Core.Runtime.Observability;

internal sealed class RuntimeObserverFailure : RuntimeObservabilityFailure
{
    internal RuntimeObserverFailure(
        long eventSequence,
        Type eventType,
        Type observerType,
        Exception exception)
        : base(exception)
    {
        if (eventSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventSequence),
                eventSequence,
                "The related event sequence must be positive.");
        }

        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(observerType);

        EventSequence = eventSequence;
        EventType = eventType;
        ObserverType = observerType;
    }

    internal long EventSequence { get; }

    internal Type EventType { get; }

    internal Type ObserverType { get; }
}
