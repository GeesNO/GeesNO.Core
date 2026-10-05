namespace GeesNO.Core.Runtime.Observability;

internal sealed class RuntimeObservabilityDiagnostics
{
    internal const int DefaultCapacity = 128;

    private readonly int _capacity;
    private readonly Queue<RuntimeObservabilityFailure> _failures = new();
    private readonly object _sync = new();

    internal RuntimeObservabilityDiagnostics(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "The diagnostic capacity must be positive.");
        }

        _capacity = capacity;
    }

    internal void Record(RuntimeObservabilityFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        lock (_sync)
        {
            if (_failures.Count == _capacity)
            {
                _failures.Dequeue();
            }

            _failures.Enqueue(failure);
        }
    }

    internal IReadOnlyList<RuntimeObservabilityFailure> GetFailures()
    {
        lock (_sync)
        {
            return Array.AsReadOnly(_failures.ToArray());
        }
    }
}
