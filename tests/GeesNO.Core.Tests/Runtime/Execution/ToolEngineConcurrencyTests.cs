using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineConcurrencyTests
{
    [Fact]
    public async Task Multiple_concurrent_executions_complete_without_global_serialization()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference, async (operation, _) =>
        {
            await Task.Yield();
            return Result<ExecutionTestResult>.Success(
                new ExecutionTestResult(operation.Id.ToString()));
        });
        var engine = ExecutionTestData.CreateExecutableEngine(tool);
        var operations = Enumerable.Range(0, 40)
            .Select(_ => ExecutionTestData.Operation(reference))
            .ToArray();

        var results = await Task.WhenAll(operations.Select(operation => engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None)));

        Assert.All(results, result => Assert.Equal(RuntimeExecutionStatus.Success, result.Status));
        Assert.Equal(40, tool.InvocationCount);
    }

    [Fact]
    public async Task Different_providers_enter_tools_concurrently_and_finish_after_stop()
    {
        var reference = ExecutionTestData.Reference();
        var startedA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var toolA = BlockingTool(reference, startedA, release, "a");
        var toolB = BlockingTool(reference, startedB, release, "b");
        var providerA = ProviderId.Parse("provider.a");
        var providerB = ProviderId.Parse("provider.b");
        var builder = new RuntimeCatalogBuilder();
        Register(builder, providerA, reference, toolA);
        Register(builder, providerB, reference, toolB);
        var engine = new ToolEngine(builder.Seal());
        engine.Start();

        var executionA = engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference), providerA),
            CancellationToken.None);
        var executionB = engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference), providerB),
            CancellationToken.None);
        await Task.WhenAll(startedA.Task, startedB.Task);

        engine.Stop();

        Assert.Equal(ToolEngineState.Stopped, engine.State);
        Assert.False(executionA.IsCompleted);
        Assert.False(executionB.IsCompleted);
        release.SetResult();
        var results = await Task.WhenAll(executionA, executionB);
        Assert.All(results, result => Assert.Equal(RuntimeExecutionStatus.Success, result.Status));

        var rejected = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference), providerA),
            CancellationToken.None);
        Assert.Equal(RuntimeExecutionStatus.Rejected, rejected.Status);
        Assert.Equal(1, toolA.InvocationCount);
        Assert.Equal(1, toolB.InvocationCount);
    }

    private static ExecutionTestTool BlockingTool(
        CapabilityReference reference,
        TaskCompletionSource started,
        TaskCompletionSource release,
        string value)
    {
        return new ExecutionTestTool(reference, async (_, _) =>
        {
            started.SetResult();
            await release.Task;
            return Result<ExecutionTestResult>.Success(new ExecutionTestResult(value));
        });
    }

    private static void Register(
        RuntimeCatalogBuilder builder,
        ProviderId providerId,
        CapabilityReference reference,
        ExecutionTestTool tool)
    {
        builder.RegisterManifest(ExecutionTestData.Manifest(
            providerId,
            ExecutionTestData.Descriptor(reference),
            ExecutionTestData.Support(reference)));
        builder.RegisterTool(providerId, tool);
    }
}
