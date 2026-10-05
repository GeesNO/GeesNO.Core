using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;

namespace GeesNO.Core.Runtime.Observability;

internal sealed class RuntimeObservability
{
    private readonly TimeProvider? _timeProvider;
    private readonly InProcessEventBus? _eventBus;
    private readonly IExecutionJournalSink? _journal;
    private readonly RuntimeObservabilityDiagnostics? _diagnostics;
    private long _sequence;

    private RuntimeObservability()
    {
    }

    internal RuntimeObservability(
        TimeProvider timeProvider,
        InProcessEventBus eventBus,
        IExecutionJournalSink journal,
        RuntimeObservabilityDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _timeProvider = timeProvider;
        _eventBus = eventBus;
        _journal = journal;
        _diagnostics = diagnostics;
        IsEnabled = true;
    }

    internal static RuntimeObservability Disabled { get; } = new();

    internal bool IsEnabled { get; }

    internal long NextSequence()
    {
        if (!IsEnabled)
        {
            return 0;
        }

        var sequence = Interlocked.Increment(ref _sequence);
        if (sequence <= 0)
        {
            throw new InvalidOperationException("The observability sequence exceeded its positive range.");
        }

        return sequence;
    }

    internal bool TryGetUtcNow(out DateTimeOffset value)
    {
        if (!IsEnabled)
        {
            value = default;
            return false;
        }

        try
        {
            value = _timeProvider!.GetUtcNow();
            if (value.Offset != TimeSpan.Zero)
            {
                throw new InvalidOperationException("The time provider returned a non-UTC timestamp.");
            }

            return true;
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            value = default;
            RecordInfrastructureFailure(RuntimeInfrastructureComponent.Clock, null, exception);
            return false;
        }
    }

    internal bool TryGetTimestamp(out long value)
    {
        if (!IsEnabled)
        {
            value = default;
            return false;
        }

        try
        {
            value = _timeProvider!.GetTimestamp();
            return true;
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            value = default;
            RecordInfrastructureFailure(RuntimeInfrastructureComponent.Clock, null, exception);
            return false;
        }
    }

    internal bool TryGetElapsedTime(long startingTimestamp, out TimeSpan value)
    {
        if (!IsEnabled)
        {
            value = default;
            return false;
        }

        try
        {
            value = _timeProvider!.GetElapsedTime(startingTimestamp);
            if (value < TimeSpan.Zero)
            {
                throw new InvalidOperationException("The time provider returned a negative duration.");
            }

            return true;
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            value = default;
            RecordInfrastructureFailure(RuntimeInfrastructureComponent.Clock, null, exception);
            return false;
        }
    }

    internal Task PublishAsync(RuntimeEvent runtimeEvent)
    {
        ArgumentNullException.ThrowIfNull(runtimeEvent);

        return IsEnabled
            ? _eventBus!.PublishAsync(runtimeEvent)
            : Task.CompletedTask;
    }

    internal void AppendSafely(ExecutionJournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _journal!.Append(entry);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            RecordInfrastructureFailure(
                RuntimeInfrastructureComponent.Journal,
                entry.Sequence,
                exception);
        }
    }

    private void RecordInfrastructureFailure(
        RuntimeInfrastructureComponent component,
        long? relatedSequence,
        Exception exception)
    {
        _diagnostics!.Record(new RuntimeInfrastructureFailure(
            component,
            relatedSequence,
            exception));
    }
}
