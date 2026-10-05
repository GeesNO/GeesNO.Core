using System.Collections.Concurrent;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineObservabilityRouteTests
{
    [Fact]
    public async Task Success_emits_full_order_and_journals_before_terminal()
    {
        var harness = CreateHarness();
        var operation = ExecutionTestData.Operation();
        var engine = ExecutionTestData.CreateExecutableEngine(observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent,
            OperationValidatedEvent, OperationStartedEvent, OperationSucceededEvent>(harness.Observer.Events);
        var entry = Assert.Single(harness.Journal.GetEntries());
        Assert.Equal(RuntimeExecutionStatus.Success, entry.Status);
        Assert.Equal(operation.Id, entry.OperationId);
        Assert.Equal(operation.Capability, entry.Capability);
        Assert.NotNull(entry.ProviderId);
        Assert.Equal(typeof(ExecutionTestResult), entry.DeclaredResultType);
        Assert.Equal(TimeSpan.Zero, entry.StartedAtUtc.Offset);
        Assert.True(entry.Duration >= TimeSpan.Zero);
        Assert.Equal(
            [
                nameof(OperationReceivedEvent),
                nameof(OperationResolvedEvent),
                nameof(OperationValidatedEvent),
                nameof(OperationStartedEvent),
                $"Journal:{RuntimeExecutionStatus.Success}",
                nameof(OperationSucceededEvent)
            ],
            harness.Timeline.ToArray());
        AssertSharedSequences(harness.Observer.Events, harness.Journal.GetEntries());
    }

    [Fact]
    public async Task Missing_explicit_provider_emits_received_failed_only()
    {
        var harness = CreateHarness();
        var engine = ExecutionTestData.CreateExecutableEngine(observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(
                ExecutionTestData.Operation(),
                ProviderId.Parse("provider.missing")),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.ProviderNotFoundCode, result.Error?.Code);
        AssertEarlyRejected(harness, expectedProvider: null);
    }

    [Fact]
    public async Task Missing_capability_emits_received_failed_only()
    {
        var harness = CreateHarness();
        var engine = ExecutionTestData.CreateExecutableEngine(observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(
                ExecutionTestData.Reference("system.missing"))),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.CapabilityNotFoundCode, result.Error?.Code);
        AssertEarlyRejected(harness, expectedProvider: null);
    }

    [Fact]
    public async Task Existing_provider_without_capability_emits_received_failed_only()
    {
        var harness = CreateHarness();
        var provider = ProviderId.Parse("provider.empty");
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(ExecutionTestData.EmptyManifest(provider));
        var engine = ToolEngineObservabilityTestData.Start(builder, harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(), provider),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.CapabilityNotFoundCode, result.Error?.Code);
        AssertEarlyRejected(harness, provider);
    }

    [Fact]
    public async Task Non_executable_capability_emits_received_failed_only()
    {
        var harness = CreateHarness();
        var capability = ExecutionTestData.Reference();
        var provider = ProviderId.Parse("provider.test");
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(ExecutionTestData.Manifest(
            provider,
            ExecutionTestData.Descriptor(capability),
            ExecutionTestData.Support(capability, CapabilitySupportLevel.Unsupported)));
        var engine = ToolEngineObservabilityTestData.Start(builder, harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(capability)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.CapabilityNotExecutableCode, result.Error?.Code);
        AssertEarlyRejected(harness, expectedProvider: null);
    }

    [Fact]
    public async Task Ambiguous_provider_emits_received_failed_only()
    {
        var harness = CreateHarness();
        var capability = ExecutionTestData.Reference();
        var builder = new RuntimeCatalogBuilder();
        ToolEngineObservabilityTestData.RegisterExecutable(
            builder, "provider.a", capability, new ExecutionTestTool(capability));
        ToolEngineObservabilityTestData.RegisterExecutable(
            builder, "provider.b", capability, new ExecutionTestTool(capability));
        var engine = ToolEngineObservabilityTestData.Start(builder, harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(capability)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.AmbiguousProviderCode, result.Error?.Code);
        AssertEarlyRejected(harness, expectedProvider: null);
    }

    [Fact]
    public async Task Safety_rejection_emits_resolved_without_validated()
    {
        var harness = CreateHarness();
        var capability = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(capability);
        var engine = ExecutionTestData.CreateEngine(
            ExecutionTestData.Descriptor(capability, riskLevel: RiskLevel.High),
            ExecutionTestData.Support(capability),
            tool,
            observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(capability)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.ConfirmationRequiredCode, result.Error?.Code);
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent, OperationFailedEvent>(
            harness.Observer.Events);
        Assert.Equal(RuntimeExecutionStatus.Rejected, Assert.Single(harness.Journal.GetEntries()).Status);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Wrong_operation_type_emits_resolved_mismatch_without_validated()
    {
        var harness = CreateHarness();
        var capability = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(capability);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);
        var operation = new AlternateExecutionTestOperation(OperationId.New(), capability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.OperationContractMismatchCode, result.Error?.Code);
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent, OperationFailedEvent>(
            harness.Observer.Events);
        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, Assert.Single(harness.Journal.GetEntries()).Status);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Mutable_tool_capability_emits_resolved_mismatch_without_validated()
    {
        var harness = CreateHarness();
        var capability = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(capability);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);
        tool.ChangeCapability(ExecutionTestData.Reference("system.changed"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(capability)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent, OperationFailedEvent>(
            harness.Observer.Events);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Functional_failure_emits_started_then_failed()
    {
        var harness = CreateHarness();
        var error = new Error("tool.failed", "Expected failure.");
        var tool = new ExecutionTestTool(ExecutionTestData.Reference(), (_, _) => Task.FromResult(
            Result<ExecutionTestResult>.Failure(error)));
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.FunctionalFailure, result.Status);
        Assert.Same(error, result.Error);
        AssertExecutionFailureRoute(harness, RuntimeExecutionStatus.FunctionalFailure);
    }

    [Fact]
    public async Task Unexpected_failure_is_sanitized_in_observability()
    {
        var harness = CreateHarness();
        var exception = new InvalidOperationException("secret C:\\private\\model.dwg");
        var tool = new ExecutionTestTool(ExecutionTestData.Reference(), (_, _) => throw exception);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.UnexpectedFailure, result.Status);
        Assert.Same(exception, result.InternalException);
        AssertExecutionFailureRoute(harness, RuntimeExecutionStatus.UnexpectedFailure);
        var failed = Assert.IsType<OperationFailedEvent>(harness.Observer.Events[^1]);
        var entry = Assert.Single(harness.Journal.GetEntries());
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, failed.Error.Code);
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, entry.Error?.Code);
        Assert.DoesNotContain(exception.Message, failed.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(exception.Message, entry.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pre_cancelled_operation_is_observed_without_resolution()
    {
        var harness = CreateHarness();
        var tool = new ExecutionTestTool(ExecutionTestData.Reference());
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Cancelled, result.Status);
        AssertEventTypes<OperationReceivedEvent, OperationCancelledEvent>(harness.Observer.Events);
        var entry = Assert.Single(harness.Journal.GetEntries());
        Assert.Equal(RuntimeExecutionStatus.Cancelled, entry.Status);
        Assert.Null(entry.ProviderId);
        Assert.Null(entry.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Cooperative_cancellation_is_observed_after_started()
    {
        var harness = CreateHarness();
        using var source = new CancellationTokenSource();
        var tool = new ExecutionTestTool(ExecutionTestData.Reference(), (_, token) =>
        {
            source.Cancel();
            return Task.FromException<Result<ExecutionTestResult>>(new OperationCanceledException(token));
        });
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Cancelled, result.Status);
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent,
            OperationValidatedEvent, OperationStartedEvent, OperationCancelledEvent>(harness.Observer.Events);
        var entry = Assert.Single(harness.Journal.GetEntries());
        Assert.Equal(RuntimeExecutionStatus.Cancelled, entry.Status);
        Assert.NotNull(entry.ProviderId);
        Assert.NotNull(entry.DeclaredResultType);
    }

    [Fact]
    public async Task Not_ready_produces_no_observability()
    {
        var harness = CreateHarness();
        var engine = ExecutionTestData.CreateExecutableEngine(
            start: false,
            observability: harness.Observability);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionErrors.NotReadyCode, result.Error?.Code);
        Assert.Empty(harness.Observer.Events);
        Assert.Empty(harness.Journal.GetEntries());
    }

    [Fact]
    public async Task Invalid_input_produces_no_observability()
    {
        var harness = CreateHarness();
        var engine = ExecutionTestData.CreateExecutableEngine(observability: harness.Observability);
        var capability = ExecutionTestData.Reference();

        await Assert.ThrowsAsync<ArgumentNullException>(() => engine.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => engine.ExecuteAsync(
            new RuntimeExecutionRequest(new InvalidExecutionOperation(null, capability)),
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => engine.ExecuteAsync(
            new RuntimeExecutionRequest(new InvalidExecutionOperation(OperationId.New(), null)),
            CancellationToken.None));
        Assert.Empty(harness.Observer.Events);
        Assert.Empty(harness.Journal.GetEntries());
    }

    private static Harness CreateHarness()
    {
        var timeline = new ConcurrentQueue<string>();
        var observer = new RecordingRuntimeObserver(timeline);
        var journal = new RecordingJournalSink(timeline);
        var observability = ToolEngineObservabilityTestData.CreateObservability(observer, journal);
        return new Harness(observability, observer, journal, timeline);
    }

    private static void AssertEarlyRejected(Harness harness, ProviderId? expectedProvider)
    {
        AssertEventTypes<OperationReceivedEvent, OperationFailedEvent>(harness.Observer.Events);
        var entry = Assert.Single(harness.Journal.GetEntries());
        Assert.Equal(RuntimeExecutionStatus.Rejected, entry.Status);
        Assert.Equal(expectedProvider, entry.ProviderId);
        Assert.Null(entry.DeclaredResultType);
    }

    private static void AssertExecutionFailureRoute(
        Harness harness,
        RuntimeExecutionStatus expectedStatus)
    {
        AssertEventTypes<OperationReceivedEvent, OperationResolvedEvent,
            OperationValidatedEvent, OperationStartedEvent, OperationFailedEvent>(harness.Observer.Events);
        Assert.Equal(expectedStatus, Assert.Single(harness.Journal.GetEntries()).Status);
    }

    private static void AssertSharedSequences(
        IReadOnlyList<RuntimeEvent> events,
        IReadOnlyList<ExecutionJournalEntry> entries)
    {
        var sequences = events.Select(runtimeEvent => runtimeEvent.Sequence)
            .Concat(entries.Select(entry => entry.Sequence))
            .ToArray();

        Assert.Equal(sequences.Length, sequences.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, sequences.Length).Select(value => (long)value),
            sequences.OrderBy(value => value));
    }

    private static void AssertEventTypes<T1, T2>(IReadOnlyList<RuntimeEvent> events)
    {
        Assert.Collection(
            events,
            item => Assert.IsType<T1>(item),
            item => Assert.IsType<T2>(item));
    }

    private static void AssertEventTypes<T1, T2, T3>(IReadOnlyList<RuntimeEvent> events)
    {
        Assert.Collection(
            events,
            item => Assert.IsType<T1>(item),
            item => Assert.IsType<T2>(item),
            item => Assert.IsType<T3>(item));
    }

    private static void AssertEventTypes<T1, T2, T3, T4, T5>(IReadOnlyList<RuntimeEvent> events)
    {
        Assert.Collection(
            events,
            item => Assert.IsType<T1>(item),
            item => Assert.IsType<T2>(item),
            item => Assert.IsType<T3>(item),
            item => Assert.IsType<T4>(item),
            item => Assert.IsType<T5>(item));
    }

    private sealed record Harness(
        RuntimeObservability Observability,
        RecordingRuntimeObserver Observer,
        RecordingJournalSink Journal,
        ConcurrentQueue<string> Timeline);

    private sealed class InvalidExecutionOperation(
        OperationId? operationId,
        CapabilityReference? capability) : IOperation
    {
        public OperationId Id => operationId!;

        public CapabilityReference Capability => capability!;
    }
}
