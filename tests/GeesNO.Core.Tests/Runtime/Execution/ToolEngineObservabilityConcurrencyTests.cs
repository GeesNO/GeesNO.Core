using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.System;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineObservabilityConcurrencyTests
{
    [Fact]
    public async Task Concurrent_pings_preserve_per_operation_order_and_shared_unique_sequences()
    {
        const int operationCount = 24;
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer, journal);
        var provider = ProviderId.Parse("provider.system");
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(SystemPingManifestFactory.Create(provider, new VersionInfo(1, 0, 0)));
        builder.RegisterTool(provider, new SystemPingTool("Observed Runtime", new VersionInfo(1, 0, 0)));
        var engine = ToolEngineObservabilityTestData.Start(builder, observability);
        var operations = Enumerable.Range(0, operationCount)
            .Select(_ => new SystemPingOperation(OperationId.New()))
            .ToArray();

        var results = await Task.WhenAll(operations.Select(operation => engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation, provider),
            CancellationToken.None)));

        Assert.All(results, result => Assert.Equal(RuntimeExecutionStatus.Success, result.Status));
        Assert.Equal(operationCount, journal.GetEntries().Count);
        Assert.Equal(operationCount * 5, observer.Events.Count);
        AssertPerOperationSuccessOrder(observer.Events, operations.Select(operation => operation.Id));
        AssertUniqueSharedSequences(observer.Events, journal.GetEntries());
        Assert.Equal(
            journal.GetEntries().Select(entry => entry.Sequence).OrderBy(sequence => sequence),
            journal.GetEntries().Select(entry => entry.Sequence));
    }

    [Fact]
    public async Task Concurrent_operations_can_interleave_without_losing_relative_order()
    {
        var entered = 0;
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference(), async (_, _) =>
        {
            if (Interlocked.Increment(ref entered) == 2)
            {
                bothEntered.SetResult();
            }

            await release.Task;
            return Result<ExecutionTestResult>.Success(new ExecutionTestResult("ok"));
        });
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer, journal);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);
        var operations = new[] { ExecutionTestData.Operation(), ExecutionTestData.Operation() };

        var executions = operations.Select(operation => engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None)).ToArray();
        await bothEntered.Task;

        Assert.Equal(2, observer.Events.Count(runtimeEvent => runtimeEvent is OperationStartedEvent));
        Assert.DoesNotContain(observer.Events, IsTerminal);
        release.SetResult();
        var results = await Task.WhenAll(executions);

        Assert.All(results, result => Assert.Equal(RuntimeExecutionStatus.Success, result.Status));
        AssertPerOperationSuccessOrder(observer.Events, operations.Select(operation => operation.Id));
        AssertUniqueSharedSequences(observer.Events, journal.GetEntries());
        var firstTerminalSequence = observer.Events.Where(IsTerminal).Min(runtimeEvent => runtimeEvent.Sequence);
        Assert.Contains(observer.Events, runtimeEvent =>
            runtimeEvent is OperationReceivedEvent && runtimeEvent.Sequence < firstTerminalSequence);
    }

    [Fact]
    public async Task Stop_does_not_cancel_started_operation_and_new_work_is_not_observed()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tool = new ExecutionTestTool(ExecutionTestData.Reference(), async (_, _) =>
        {
            started.SetResult();
            await release.Task;
            return Result<ExecutionTestResult>.Success(new ExecutionTestResult("finished"));
        });
        var observer = new RecordingRuntimeObserver();
        var journal = new RecordingJournalSink();
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer, journal);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);
        var operation = ExecutionTestData.Operation();

        var execution = engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None);
        await started.Task;
        engine.Stop();
        release.SetResult();
        var completed = await execution;

        Assert.Equal(RuntimeExecutionStatus.Success, completed.Status);
        AssertPerOperationSuccessOrder(observer.Events, [operation.Id]);
        Assert.Single(journal.GetEntries());
        var eventCount = observer.Events.Count;
        var journalCount = journal.GetEntries().Count;

        var rejected = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Rejected, rejected.Status);
        Assert.Equal(RuntimeExecutionErrors.NotReadyCode, rejected.Error?.Code);
        Assert.Equal(eventCount, observer.Events.Count);
        Assert.Equal(journalCount, journal.GetEntries().Count);
    }

    private static void AssertPerOperationSuccessOrder(
        IReadOnlyList<RuntimeEvent> events,
        IEnumerable<OperationId> operationIds)
    {
        foreach (var operationId in operationIds)
        {
            var types = events
                .OfType<OperationRuntimeEvent>()
                .Where(runtimeEvent => runtimeEvent.OperationId == operationId)
                .Select(runtimeEvent => runtimeEvent.GetType())
                .ToArray();

            Assert.Equal(
                [
                    typeof(OperationReceivedEvent),
                    typeof(OperationResolvedEvent),
                    typeof(OperationValidatedEvent),
                    typeof(OperationStartedEvent),
                    typeof(OperationSucceededEvent)
                ],
                types);
        }
    }

    private static void AssertUniqueSharedSequences(
        IReadOnlyList<RuntimeEvent> events,
        IReadOnlyList<ExecutionJournalEntry> entries)
    {
        var sequences = events.Select(runtimeEvent => runtimeEvent.Sequence)
            .Concat(entries.Select(entry => entry.Sequence))
            .ToArray();

        Assert.Equal(sequences.Length, sequences.Distinct().Count());
    }

    private static bool IsTerminal(RuntimeEvent runtimeEvent)
    {
        return runtimeEvent is OperationSucceededEvent
            or OperationFailedEvent
            or OperationCancelledEvent;
    }
}
