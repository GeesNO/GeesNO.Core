using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineInvocationTests
{
    [Fact]
    public async Task Success_preserves_operation_provider_type_value_and_token()
    {
        var reference = ExecutionTestData.Reference();
        var operation = ExecutionTestData.Operation(reference);
        var value = new ExecutionTestResult("completed");
        var tool = new ExecutionTestTool(reference, (_, _) => Task.FromResult(
            Result<ExecutionTestResult>.Success(value)));
        var engine = ExecutionTestData.CreateExecutableEngine(tool);
        using var source = new CancellationTokenSource();

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal(operation.Id, result.OperationId);
        Assert.Equal(reference, result.Capability);
        Assert.Equal("provider.test", result.ProviderId?.Value);
        Assert.Equal(typeof(ExecutionTestResult), result.DeclaredResultType);
        Assert.Same(value, result.SuccessfulValue);
        Assert.Null(result.Error);
        Assert.Null(result.InternalException);
        Assert.Equal(source.Token, tool.LastToken);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Functional_failure_preserves_exact_error()
    {
        var reference = ExecutionTestData.Reference();
        var error = new Error("tool.failed", "Expected failure");
        var tool = new ExecutionTestTool(reference, (_, _) => Task.FromResult(
            Result<ExecutionTestResult>.Failure(error)));
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.FunctionalFailure, result.Status);
        Assert.Same(error, result.Error);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }

    [Fact]
    public async Task Pre_cancelled_token_returns_early_cancelled_without_resolution_or_invocation()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = ExecutionTestData.CreateExecutableEngine(tool);
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Cancelled, result.Status);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Null(result.Error);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Cooperative_cancellation_is_cancelled_with_resolved_metadata()
    {
        var reference = ExecutionTestData.Reference();
        using var source = new CancellationTokenSource();
        var tool = new ExecutionTestTool(reference, (_, token) =>
        {
            source.Cancel();
            return Task.FromException<Result<ExecutionTestResult>>(
                new OperationCanceledException(token));
        });
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            source.Token);

        Assert.Equal(RuntimeExecutionStatus.Cancelled, result.Status);
        Assert.Equal("provider.test", result.ProviderId?.Value);
        Assert.Equal(typeof(ExecutionTestResult), result.DeclaredResultType);
        Assert.IsType<OperationCanceledException>(result.InternalException);
    }

    [Fact]
    public async Task Foreign_cancellation_is_unexpected_failure()
    {
        var reference = ExecutionTestData.Reference();
        var exception = new OperationCanceledException("foreign");
        var tool = new ExecutionTestTool(reference, (_, _) =>
            Task.FromException<Result<ExecutionTestResult>>(exception));
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertUnexpected(result, exception);
    }

    [Fact]
    public async Task Synchronous_ordinary_exception_is_sanitized_and_retained_internally()
    {
        var reference = ExecutionTestData.Reference();
        var exception = new InvalidOperationException("secret C:\\private\\model.dwg");
        var tool = new ExecutionTestTool(reference, (_, _) => throw exception);
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertUnexpected(result, exception);
        Assert.Equal("The tool failed unexpectedly.", result.Error?.Message);
        Assert.DoesNotContain(exception.Message, result.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asynchronous_ordinary_exception_is_sanitized_and_retained_internally()
    {
        var reference = ExecutionTestData.Reference();
        var exception = new ApplicationException("private async detail");
        var tool = new ExecutionTestTool(reference, (_, _) =>
            Task.FromException<Result<ExecutionTestResult>>(exception));
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertUnexpected(result, exception);
    }

    [Fact]
    public async Task Null_task_is_tool_contract_mismatch()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference, (_, _) => null!);

        var result = await ExecutionTestData.CreateExecutableEngine(tool).ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertToolMismatch(result);
    }

    [Fact]
    public async Task Null_result_is_tool_contract_mismatch()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference, (_, _) =>
            Task.FromResult<Result<ExecutionTestResult>>(null!));

        var result = await ExecutionTestData.CreateExecutableEngine(tool).ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertToolMismatch(result);
    }

    [Fact]
    public async Task Fatal_exception_escapes_engine_without_normalization()
    {
        var reference = ExecutionTestData.Reference();
        var exception = new OutOfMemoryException("fatal");
        var tool = new ExecutionTestTool(reference, (_, _) => throw exception);
        var engine = ExecutionTestData.CreateExecutableEngine(tool);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() => engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None));

        Assert.Same(exception, escaped);
    }

    [Fact]
    public async Task Tool_capability_changed_after_seal_is_contract_mismatch()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = ExecutionTestData.CreateExecutableEngine(tool);
        tool.ChangeCapability(ExecutionTestData.Reference("system.changed"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertToolMismatch(result);
        Assert.Equal(0, tool.InvocationCount);
    }

    private static void AssertUnexpected(
        RuntimeExecutionResult result,
        Exception expectedException)
    {
        Assert.Equal(RuntimeExecutionStatus.UnexpectedFailure, result.Status);
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, result.Error?.Code);
        Assert.Same(expectedException, result.InternalException);
        Assert.Null(result.SuccessfulValue);
    }

    private static void AssertToolMismatch(RuntimeExecutionResult result)
    {
        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }
}
