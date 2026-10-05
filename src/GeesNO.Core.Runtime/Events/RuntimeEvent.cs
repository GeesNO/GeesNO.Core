namespace GeesNO.Core.Runtime.Events;

internal abstract class RuntimeEvent
{
    private protected RuntimeEvent(long sequence, DateTimeOffset occurredAtUtc)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "The event sequence must be positive.");
        }

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The event timestamp must use the UTC offset.",
                nameof(occurredAtUtc));
        }

        Sequence = sequence;
        OccurredAtUtc = occurredAtUtc;
    }

    internal long Sequence { get; }

    internal DateTimeOffset OccurredAtUtc { get; }
}
