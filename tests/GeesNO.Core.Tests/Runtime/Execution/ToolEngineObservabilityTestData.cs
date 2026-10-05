using System.Collections.Concurrent;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Execution;

internal static class ToolEngineObservabilityTestData
{
    internal static RuntimeObservability CreateObservability(
        RecordingRuntimeObserver? observer = null,
        IExecutionJournalSink? journal = null,
        RuntimeObservabilityDiagnostics? diagnostics = null,
        TimeProvider? timeProvider = null)
    {
        diagnostics ??= new RuntimeObservabilityDiagnostics();
        var bus = new InProcessEventBus(
            observer is null ? [] : [observer],
            diagnostics);

        return new RuntimeObservability(
            timeProvider ?? new DeterministicTimeProvider(),
            bus,
            journal ?? new InMemoryExecutionJournal(),
            diagnostics);
    }

    internal static RuntimeObservability CreateObservability(
        IEnumerable<IRuntimeEventObserver> observers,
        IExecutionJournalSink journal,
        RuntimeObservabilityDiagnostics diagnostics,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(observers);
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new RuntimeObservability(
            timeProvider ?? new DeterministicTimeProvider(),
            new InProcessEventBus(observers, diagnostics),
            journal,
            diagnostics);
    }

    internal static ToolEngine Start(
        RuntimeCatalogBuilder builder,
        RuntimeObservability observability)
    {
        return Start(builder.Seal(), observability);
    }

    internal static ToolEngine Start(
        RuntimeCatalogSnapshot snapshot,
        RuntimeObservability observability)
    {
        var engine = new ToolEngine(snapshot, observability);
        engine.Start();
        return engine;
    }

    internal static void RegisterExecutable(
        RuntimeCatalogBuilder builder,
        string providerId,
        CapabilityReference capability,
        ExecutionTestTool tool)
    {
        var provider = ProviderId.Parse(providerId);
        builder.RegisterManifest(ExecutionTestData.Manifest(
            provider,
            ExecutionTestData.Descriptor(capability),
            ExecutionTestData.Support(capability)));
        builder.RegisterTool(provider, tool);
    }
}

internal sealed class RecordingRuntimeObserver(
    ConcurrentQueue<string>? timeline = null,
    Func<RuntimeEvent, Task>? handler = null) : IRuntimeEventObserver
{
    private readonly ConcurrentQueue<RuntimeEvent> _events = new();

    internal IReadOnlyList<RuntimeEvent> Events => _events.ToArray();

    public Task ObserveAsync(RuntimeEvent runtimeEvent)
    {
        _events.Enqueue(runtimeEvent);
        timeline?.Enqueue(runtimeEvent.GetType().Name);
        return handler is null ? Task.CompletedTask : handler(runtimeEvent);
    }
}

internal sealed class RecordingJournalSink(ConcurrentQueue<string>? timeline = null)
    : IExecutionJournalSink
{
    private readonly InMemoryExecutionJournal _journal = new();

    public void Append(ExecutionJournalEntry entry)
    {
        _journal.Append(entry);
        timeline?.Enqueue($"Journal:{entry.Status}");
    }

    internal IReadOnlyList<ExecutionJournalEntry> GetEntries()
    {
        return _journal.GetEntries();
    }
}

internal sealed class DelegatingJournalSink(Action<ExecutionJournalEntry> append)
    : IExecutionJournalSink
{
    public void Append(ExecutionJournalEntry entry)
    {
        append(entry);
    }
}

internal class DeterministicTimeProvider : TimeProvider
{
    private long _timestamp;
    private long _utcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow()
    {
        var ticks = Interlocked.Increment(ref _utcTicks);
        return new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero).AddTicks(ticks);
    }

    public override long GetTimestamp()
    {
        return Interlocked.Increment(ref _timestamp);
    }
}

internal sealed class FaultingTimeProvider(
    Func<int, Exception?>? utcFailure = null,
    Func<int, Exception?>? timestampFailure = null) : DeterministicTimeProvider
{
    private int _utcCalls;
    private int _timestampCalls;

    public override DateTimeOffset GetUtcNow()
    {
        var call = Interlocked.Increment(ref _utcCalls);
        var exception = utcFailure?.Invoke(call);
        return exception is null ? base.GetUtcNow() : throw exception;
    }

    public override long GetTimestamp()
    {
        var call = Interlocked.Increment(ref _timestampCalls);
        var exception = timestampFailure?.Invoke(call);
        return exception is null ? base.GetTimestamp() : throw exception;
    }
}
