using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineLifecycleTests
{
    [Fact]
    public void State_contains_exactly_sealed_running_and_stopped()
    {
        Assert.Equal(["Sealed", "Running", "Stopped"], Enum.GetNames<ToolEngineState>());
    }

    [Fact]
    public void Constructor_requires_snapshot_and_starts_sealed()
    {
        Assert.Throws<ArgumentNullException>(() => new ToolEngine(null!));

        var engine = ExecutionTestData.CreateExecutableEngine(start: false);
        Assert.Equal(ToolEngineState.Sealed, engine.State);
    }

    [Fact]
    public void Start_allows_only_sealed_to_running()
    {
        var engine = ExecutionTestData.CreateExecutableEngine(start: false);

        engine.Start();

        Assert.Equal(ToolEngineState.Running, engine.State);
        var exception = Assert.Throws<InvalidOperationException>(engine.Start);
        Assert.Contains(
            RuntimeExecutionErrors.InvalidLifecycleTransitionCode,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Stop_from_sealed_is_terminal_and_idempotent()
    {
        var engine = ExecutionTestData.CreateExecutableEngine(start: false);

        engine.Stop();
        engine.Stop();

        Assert.Equal(ToolEngineState.Stopped, engine.State);
        var exception = Assert.Throws<InvalidOperationException>(engine.Start);
        Assert.Contains(
            RuntimeExecutionErrors.InvalidLifecycleTransitionCode,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Stop_from_running_is_terminal_and_idempotent()
    {
        var engine = ExecutionTestData.CreateExecutableEngine();

        engine.Stop();
        engine.Stop();

        Assert.Equal(ToolEngineState.Stopped, engine.State);
        Assert.Throws<InvalidOperationException>(engine.Start);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execution_outside_running_is_rejected_without_invoking(bool stopFirst)
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = ExecutionTestData.CreateExecutableEngine(tool, start: false);
        if (stopFirst)
        {
            engine.Stop();
        }

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Equal(RuntimeExecutionErrors.NotReadyCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Lifecycle_check_precedes_pre_cancelled_token()
    {
        var engine = ExecutionTestData.CreateExecutableEngine(start: false);
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Equal(RuntimeExecutionErrors.NotReadyCode, result.Error?.Code);
    }

    [Fact]
    public async Task Stop_preserves_snapshot_and_registered_invoker()
    {
        var reference = ExecutionTestData.Reference();
        var provider = ProviderId.Parse("provider.test");
        var key = new ProviderCapabilityKey(provider, reference);
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(ExecutionTestData.Manifest(
            provider,
            ExecutionTestData.Descriptor(reference),
            ExecutionTestData.Support(reference)));
        builder.RegisterTool(provider, new ExecutionTestTool(reference));
        var snapshot = builder.Seal();
        Assert.True(snapshot.TryGetInvoker(key, out var before));
        var engine = new ToolEngine(snapshot);
        engine.Start();

        engine.Stop();

        Assert.True(snapshot.TryGetInvoker(key, out var after));
        Assert.Same(before, after);
        var rejected = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);
        Assert.Equal(RuntimeExecutionStatus.Rejected, rejected.Status);
    }

    [Fact]
    public async Task Started_operation_can_finish_after_stop_without_stop_waiting()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference, async (_, _) =>
        {
            started.SetResult();
            await release.Task;
            return Result<ExecutionTestResult>.Success(new ExecutionTestResult("finished"));
        });
        var engine = ExecutionTestData.CreateExecutableEngine(tool);
        var execution = engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);
        await started.Task;

        engine.Stop();

        Assert.Equal(ToolEngineState.Stopped, engine.State);
        Assert.False(execution.IsCompleted);
        release.SetResult();
        var result = await execution;
        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);

        var later = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);
        Assert.Equal(RuntimeExecutionStatus.Rejected, later.Status);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Execute_rejects_null_request_and_null_operation_identity()
    {
        var engine = ExecutionTestData.CreateExecutableEngine();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            engine.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => engine.ExecuteAsync(
            new RuntimeExecutionRequest(new ExecutionTestOperation(
                null!,
                ExecutionTestData.Reference())),
            CancellationToken.None));
    }

    [Fact]
    public async Task Null_operation_capability_is_rejected_as_invalid_input()
    {
        var engine = ExecutionTestData.CreateExecutableEngine();

        await Assert.ThrowsAsync<ArgumentException>(() => engine.ExecuteAsync(
            new RuntimeExecutionRequest(new ExecutionTestOperation(OperationId.New(), null!)),
            CancellationToken.None));
    }
}
