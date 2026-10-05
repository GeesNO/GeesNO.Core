using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineObservabilityFailureTests
{
    [Fact]
    public async Task Ordinary_received_observer_failure_preserves_result_and_later_observer()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var failing = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationReceivedEvent
                ? throw new InvalidOperationException("received")
                : Task.CompletedTask);
        var later = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            [failing, later], journal, diagnostics);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal(5, later.Events.Count);
        Assert.Single(journal.GetEntries());
        Assert.Contains(diagnostics.GetFailures(), failure => failure is RuntimeObserverFailure);
    }

    [Fact]
    public async Task Ordinary_terminal_observer_failure_preserves_result()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationSucceededEvent
                ? throw new InvalidOperationException("terminal")
                : Task.CompletedTask);
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Single(journal.GetEntries());
        Assert.IsType<OperationSucceededEvent>(observer.Events[^1]);
        Assert.Contains(diagnostics.GetFailures(), failure => failure is RuntimeObserverFailure);
    }

    [Fact]
    public async Task Null_observer_task_is_diagnosed_without_changing_result()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationReceivedEvent ? null! : Task.CompletedTask);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer,
            diagnostics: diagnostics);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        var failure = Assert.IsType<RuntimeObserverFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    public async Task Observer_cancellation_is_diagnosed_without_changing_result()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationReceivedEvent
                ? Task.FromCanceled(new CancellationToken(true))
                : Task.CompletedTask);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer,
            diagnostics: diagnostics);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        var failure = Assert.IsType<RuntimeObserverFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.IsType<TaskCanceledException>(failure.Exception);
    }

    [Fact]
    public async Task Ordinary_journal_failure_is_diagnosed_and_terminal_is_published()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver();
        var exception = new InvalidOperationException("journal");
        var journal = new DelegatingJournalSink(_ => throw exception);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.IsType<OperationSucceededEvent>(observer.Events[^1]);
        var failure = Assert.IsType<RuntimeInfrastructureFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.Equal(RuntimeInfrastructureComponent.Journal, failure.Component);
        Assert.Same(exception, failure.Exception);
    }

    [Fact]
    public async Task Ordinary_clock_failure_before_received_disables_attempt_only()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var clock = new FaultingTimeProvider(
            utcFailure: call => call == 1 ? new InvalidOperationException("clock") : null);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics, clock);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Empty(observer.Events);
        Assert.Empty(journal.GetEntries());
        AssertClockDiagnostic(diagnostics);
    }

    [Fact]
    public async Task Ordinary_clock_failure_omits_only_intermediate_event()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var clock = new FaultingTimeProvider(
            utcFailure: call => call == 3 ? new InvalidOperationException("resolved") : null);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics, clock);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.DoesNotContain(observer.Events, runtimeEvent => runtimeEvent is OperationResolvedEvent);
        Assert.Contains(observer.Events, runtimeEvent => runtimeEvent is OperationValidatedEvent);
        Assert.IsType<OperationSucceededEvent>(observer.Events[^1]);
        Assert.Single(journal.GetEntries());
        AssertClockDiagnostic(diagnostics);
    }

    [Fact]
    public async Task Ordinary_duration_failure_omits_journal_but_publishes_terminal()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var clock = new FaultingTimeProvider(
            timestampFailure: call => call == 2 ? new InvalidOperationException("duration") : null);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics, clock);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Empty(journal.GetEntries());
        Assert.IsType<OperationSucceededEvent>(observer.Events[^1]);
        AssertClockDiagnostic(diagnostics);
    }

    [Fact]
    public async Task Ordinary_terminal_clock_failure_preserves_result_after_journal()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var clock = new FaultingTimeProvider(
            utcFailure: call => call == 6 ? new InvalidOperationException("terminal") : null);
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics, clock);
        var engine = ExecutionTestData.CreateExecutableEngine(observability: observability);

        var result = await ExecuteAsync(engine);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Single(journal.GetEntries());
        Assert.DoesNotContain(observer.Events, runtimeEvent => runtimeEvent is OperationSucceededEvent);
        AssertClockDiagnostic(diagnostics);
    }

    [Fact]
    public async Task Fatal_clock_before_received_escapes_before_tool()
    {
        var exception = new OutOfMemoryException("fatal clock");
        var clock = new FaultingTimeProvider(utcFailure: call => call == 1 ? exception : null);
        var observability = ToolEngineObservabilityTestData.CreateObservability(timeProvider: clock);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference());
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() => ExecuteAsync(engine));

        Assert.Same(exception, escaped);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Fatal_received_observer_escapes_before_tool()
    {
        var exception = new OutOfMemoryException("fatal observer");
        var observer = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationReceivedEvent ? throw exception : Task.CompletedTask);
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference());
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() => ExecuteAsync(engine));

        Assert.Same(exception, escaped);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Fatal_journal_after_tool_escapes_without_terminal_event()
    {
        var exception = new OutOfMemoryException("fatal journal");
        var observer = new RecordingRuntimeObserver();
        var journal = new DelegatingJournalSink(_ => throw exception);
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var observability = ToolEngineObservabilityTestData.CreateObservability(
            observer, journal, diagnostics);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference());
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() => ExecuteAsync(engine));

        Assert.Same(exception, escaped);
        Assert.Equal(1, tool.InvocationCount);
        Assert.DoesNotContain(observer.Events, runtimeEvent => runtimeEvent is OperationSucceededEvent);
    }

    [Fact]
    public async Task Fatal_terminal_observer_escapes_after_journal_and_tool()
    {
        var exception = new OutOfMemoryException("fatal terminal");
        var observer = new RecordingRuntimeObserver(handler: runtimeEvent =>
            runtimeEvent is OperationSucceededEvent ? throw exception : Task.CompletedTask);
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer, journal);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference());
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() => ExecuteAsync(engine));

        Assert.Same(exception, escaped);
        Assert.Equal(1, tool.InvocationCount);
        Assert.Single(journal.GetEntries());
        Assert.IsType<OperationSucceededEvent>(observer.Events[^1]);
    }

    private static Task<RuntimeExecutionResult> ExecuteAsync(ToolEngine engine)
    {
        return engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);
    }

    private static void AssertClockDiagnostic(RuntimeObservabilityDiagnostics diagnostics)
    {
        Assert.Contains(diagnostics.GetFailures(), failure =>
            failure is RuntimeInfrastructureFailure
            {
                Component: RuntimeInfrastructureComponent.Clock
            });
    }
}
