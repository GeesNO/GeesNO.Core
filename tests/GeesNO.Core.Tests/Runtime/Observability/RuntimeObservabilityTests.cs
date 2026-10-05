using System.Reflection;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Observability;

public sealed class RuntimeObservabilityTests
{
    [Fact]
    public async Task Disabled_is_reusable_and_performs_no_work()
    {
        var disabled = RuntimeObservability.Disabled;
        var runtimeEvent = ObservabilityTestData.Received();
        var entry = ObservabilityTestData.JournalEntry();

        Assert.Same(disabled, RuntimeObservability.Disabled);
        Assert.False(disabled.IsEnabled);
        Assert.Equal(0, disabled.NextSequence());
        Assert.False(disabled.TryGetUtcNow(out var utc));
        Assert.False(disabled.TryGetTimestamp(out var timestamp));
        Assert.False(disabled.TryGetElapsedTime(1, out var elapsed));
        await disabled.PublishAsync(runtimeEvent);
        disabled.AppendSafely(entry);
        Assert.Equal(default, utc);
        Assert.Equal(0, timestamp);
        Assert.Equal(TimeSpan.Zero, elapsed);
    }

    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var bus = new InProcessEventBus([], diagnostics);
        var journal = new InMemoryExecutionJournal();

        Assert.Throws<ArgumentNullException>(() => new RuntimeObservability(
            null!, bus, journal, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new RuntimeObservability(
            TimeProvider.System, null!, journal, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new RuntimeObservability(
            TimeProvider.System, bus, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new RuntimeObservability(
            TimeProvider.System, bus, journal, null!));
    }

    [Fact]
    public void Active_sequence_starts_at_one()
    {
        var observability = CreateActive();

        Assert.True(observability.IsEnabled);
        Assert.Equal(1, observability.NextSequence());
        Assert.Equal(2, observability.NextSequence());
    }

    [Fact]
    public void Active_sequence_is_unique_under_concurrency()
    {
        var observability = CreateActive();
        var sequences = new long[512];

        Parallel.For(0, sequences.Length, index =>
            sequences[index] = observability.NextSequence());

        Assert.Equal(Enumerable.Range(1, sequences.Length).Select(value => (long)value),
            sequences.OrderBy(value => value));
    }

    [Fact]
    public void Sequence_overflow_is_rejected()
    {
        var observability = CreateActive();
        var sequenceField = typeof(RuntimeObservability).GetField(
            "_sequence",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sequenceField);
        sequenceField.SetValue(observability, long.MaxValue);

        Assert.Throws<InvalidOperationException>(() => observability.NextSequence());
    }

    [Fact]
    public void Clock_returns_valid_utc_timestamp_and_elapsed_time()
    {
        var timeProvider = new ControlledTimeProvider(
            ObservabilityTestData.UtcNow,
            timestamp: 20,
            timestampFrequency: 10);
        var observability = CreateActive(timeProvider);

        Assert.True(observability.TryGetUtcNow(out var utc));
        Assert.True(observability.TryGetTimestamp(out var timestamp));
        Assert.True(observability.TryGetElapsedTime(10, out var elapsed));
        Assert.Equal(ObservabilityTestData.UtcNow, utc);
        Assert.Equal(20, timestamp);
        Assert.Equal(TimeSpan.FromSeconds(1), elapsed);
    }

    [Fact]
    public void Ordinary_clock_exception_returns_false_and_creates_diagnostic()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var exception = new InvalidOperationException("clock");
        var observability = CreateActive(
            new ThrowingTimeProvider(exception, throwFromUtc: true),
            diagnostics: diagnostics);

        var succeeded = observability.TryGetUtcNow(out var value);

        Assert.False(succeeded);
        Assert.Equal(default, value);
        var failure = Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.Equal(RuntimeInfrastructureComponent.Clock, failure.Component);
        Assert.Null(failure.RelatedSequence);
        Assert.Same(exception, failure.Exception);
    }

    [Fact]
    public void Ordinary_timestamp_exception_returns_false_and_creates_diagnostic()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observability = CreateActive(
            new ThrowingTimeProvider(new InvalidOperationException("timestamp"), throwFromUtc: false),
            diagnostics: diagnostics);

        Assert.False(observability.TryGetTimestamp(out _));
        Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
    }

    [Fact]
    public void Non_utc_clock_value_is_rejected_as_ordinary_failure()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var nonUtc = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.FromHours(2));
        var observability = CreateActive(
            new ControlledTimeProvider(nonUtc, 1, 1),
            diagnostics: diagnostics);

        Assert.False(observability.TryGetUtcNow(out _));
        var failure = Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    public void Negative_elapsed_time_is_rejected_as_ordinary_failure()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observability = CreateActive(
            new ControlledTimeProvider(ObservabilityTestData.UtcNow, 5, 1),
            diagnostics: diagnostics);

        Assert.False(observability.TryGetElapsedTime(10, out _));
        var failure = Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    public void Fatal_clock_exception_escapes()
    {
        var exception = new OutOfMemoryException("fatal");
        var observability = CreateActive(
            new ThrowingTimeProvider(exception, throwFromUtc: true));

        Assert.Same(exception, Assert.Throws<OutOfMemoryException>(() =>
            observability.TryGetUtcNow(out _)));
    }

    [Fact]
    public async Task Publication_is_delegated_to_bus()
    {
        RuntimeEvent? received = null;
        var observer = new DelegateObserver(runtimeEvent =>
        {
            received = runtimeEvent;
            return Task.CompletedTask;
        });
        var observability = CreateActive(observer: observer);
        var runtimeEvent = ObservabilityTestData.Received();

        await observability.PublishAsync(runtimeEvent);

        Assert.Same(runtimeEvent, received);
    }

    [Fact]
    public async Task Publication_rejects_null_even_when_disabled()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RuntimeObservability.Disabled.PublishAsync(null!));
    }

    [Fact]
    public void Journal_append_is_delegated()
    {
        var sink = new RecordingJournalSink();
        var observability = CreateActive(journal: sink);
        var entry = ObservabilityTestData.JournalEntry();

        observability.AppendSafely(entry);

        Assert.Same(entry, Assert.Single(sink.Entries));
    }

    [Fact]
    public void Ordinary_journal_failure_is_isolated_and_diagnosed()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var exception = new InvalidOperationException("journal");
        var observability = CreateActive(
            journal: new ThrowingJournalSink(exception),
            diagnostics: diagnostics);
        var entry = ObservabilityTestData.JournalEntry(17);

        observability.AppendSafely(entry);

        var failure = Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.Equal(RuntimeInfrastructureComponent.Journal, failure.Component);
        Assert.Equal(17, failure.RelatedSequence);
        Assert.Same(exception, failure.Exception);
    }

    [Fact]
    public void Fatal_journal_failure_escapes()
    {
        var exception = new OutOfMemoryException("fatal");
        var observability = CreateActive(journal: new ThrowingJournalSink(exception));

        Assert.Same(exception, Assert.Throws<OutOfMemoryException>(() =>
            observability.AppendSafely(ObservabilityTestData.JournalEntry())));
    }

    [Fact]
    public void Append_rejects_null_even_when_disabled()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RuntimeObservability.Disabled.AppendSafely(null!));
    }

    private static RuntimeObservability CreateActive(
        TimeProvider? timeProvider = null,
        IRuntimeEventObserver? observer = null,
        IExecutionJournalSink? journal = null,
        RuntimeObservabilityDiagnostics? diagnostics = null)
    {
        diagnostics ??= new RuntimeObservabilityDiagnostics();
        var bus = new InProcessEventBus(
            observer is null ? [] : [observer],
            diagnostics);

        return new RuntimeObservability(
            timeProvider ?? TimeProvider.System,
            bus,
            journal ?? new InMemoryExecutionJournal(),
            diagnostics);
    }

    private sealed class ControlledTimeProvider(
        DateTimeOffset utcNow,
        long timestamp,
        long timestampFrequency) : TimeProvider
    {
        public override long TimestampFrequency => timestampFrequency;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }

        public override long GetTimestamp()
        {
            return timestamp;
        }
    }

    private sealed class ThrowingTimeProvider(
        Exception exception,
        bool throwFromUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return throwFromUtc ? throw exception : ObservabilityTestData.UtcNow;
        }

        public override long GetTimestamp()
        {
            return !throwFromUtc ? throw exception : 1;
        }
    }

    private sealed class DelegateObserver(Func<RuntimeEvent, Task> handler) : IRuntimeEventObserver
    {
        public Task ObserveAsync(RuntimeEvent runtimeEvent)
        {
            return handler(runtimeEvent);
        }
    }

    private sealed class RecordingJournalSink : IExecutionJournalSink
    {
        internal List<ExecutionJournalEntry> Entries { get; } = [];

        public void Append(ExecutionJournalEntry entry)
        {
            Entries.Add(entry);
        }
    }

    private sealed class ThrowingJournalSink(Exception exception) : IExecutionJournalSink
    {
        public void Append(ExecutionJournalEntry entry)
        {
            throw exception;
        }
    }
}
