using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Invokers;

public sealed class ToolInvokerTests
{
    [Fact]
    public void Identity_exposes_key_and_exact_declared_types()
    {
        var key = InvokerTestData.Key();
        var invoker = CreateInvoker(key, new RecordingTool(key.Capability));

        Assert.Equal(key, invoker.Key);
        Assert.Equal(typeof(InvocationTestOperation), invoker.OperationType);
        Assert.Equal(typeof(InvocationTestResult), invoker.ResultType);
    }

    [Fact]
    public async Task Success_invokes_once_and_preserves_identity_token_and_value()
    {
        var key = InvokerTestData.Key();
        var operation = InvokerTestData.Operation(key.Capability);
        var expectedValue = new InvocationTestResult("completed");
        var tool = new RecordingTool(key.Capability, (_, _) => Task.FromResult(
            Result<InvocationTestResult>.Success(expectedValue)));
        var invoker = CreateInvoker(key, tool);
        using var source = new CancellationTokenSource();

        var result = await invoker.InvokeAsync(operation, source.Token);

        Assert.Equal(1, tool.InvocationCount);
        Assert.Equal(source.Token, tool.LastToken);
        Assert.Equal(InternalInvocationStatus.Success, result.Status);
        Assert.Equal(operation.Id, result.OperationId);
        Assert.Equal(key.ProviderId, result.ProviderId);
        Assert.Equal(key.Capability, result.Capability);
        Assert.Equal(typeof(InvocationTestResult), result.DeclaredResultType);
        Assert.Same(expectedValue, result.SuccessfulValue);
        Assert.Null(result.Error);
        Assert.Null(result.InternalException);
    }

    [Fact]
    public async Task Functional_failure_preserves_the_exact_error()
    {
        var key = InvokerTestData.Key();
        var expectedError = new Error("tool.failed", "Expected functional failure");
        var tool = new RecordingTool(key.Capability, (_, _) => Task.FromResult(
            Result<InvocationTestResult>.Failure(expectedError)));

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        Assert.Equal(InternalInvocationStatus.FunctionalFailure, result.Status);
        Assert.Same(expectedError, result.Error);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }

    [Fact]
    public async Task Wrong_operation_type_is_rejected_without_invoking_tool()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        IOperation operation = new AlternateInvocationTestOperation(OperationId.New(), key.Capability);

        var result = await CreateInvoker(key, tool).InvokeAsync(operation, CancellationToken.None);

        AssertOperationMismatch(result);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Derived_operation_type_is_rejected_without_invoking_tool()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        var operation = new DerivedInvocationTestOperation(OperationId.New(), key.Capability);

        var result = await CreateInvoker(key, tool).InvokeAsync(operation, CancellationToken.None);

        AssertOperationMismatch(result);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Wrong_operation_capability_is_rejected_without_invoking_tool()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        var operation = InvokerTestData.Operation(InvokerTestData.Reference("system.other"));

        var result = await CreateInvoker(key, tool).InvokeAsync(operation, CancellationToken.None);

        AssertOperationMismatch(result);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Null_operation_capability_is_rejected_without_invoking_tool()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        var operation = new InvocationTestOperation(OperationId.New(), null!);

        var result = await CreateInvoker(key, tool).InvokeAsync(operation, CancellationToken.None);

        AssertOperationMismatch(result);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Changed_tool_capability_is_rejected_on_every_call()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        var invoker = CreateInvoker(key, tool);
        tool.ChangeCapability(InvokerTestData.Reference("system.changed"));

        var result = await invoker.InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        Assert.Equal(InternalInvocationStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Exception_reading_current_tool_capability_is_an_unexpected_failure()
    {
        var key = InvokerTestData.Key();
        var tool = new ThrowingCapabilityTool(key.Capability);
        var invoker = CreateInvoker(key, tool);
        tool.ThrowOnRead();

        var result = await invoker.InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        Assert.Equal(InternalInvocationStatus.UnexpectedFailure, result.Status);
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, result.Error?.Code);
        Assert.IsType<InvalidOperationException>(result.InternalException);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Pre_cancelled_token_returns_cancelled_without_invoking_tool()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            source.Token);

        Assert.Equal(InternalInvocationStatus.Cancelled, result.Status);
        Assert.Null(result.Error);
        Assert.Null(result.SuccessfulValue);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Cooperative_cancellation_returns_cancelled()
    {
        var key = InvokerTestData.Key();
        using var source = new CancellationTokenSource();
        var tool = new RecordingTool(key.Capability, (_, token) =>
        {
            source.Cancel();
            return Task.FromException<Result<InvocationTestResult>>(
                new OperationCanceledException(token));
        });

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            source.Token);

        Assert.Equal(InternalInvocationStatus.Cancelled, result.Status);
        Assert.IsType<OperationCanceledException>(result.InternalException);
        Assert.Null(result.Error);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Foreign_cancellation_is_an_unexpected_failure()
    {
        var key = InvokerTestData.Key();
        var exception = new OperationCanceledException("foreign cancellation");
        var tool = new RecordingTool(key.Capability, (_, _) =>
            Task.FromException<Result<InvocationTestResult>>(exception));

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        AssertUnexpectedFailure(result, exception);
    }

    [Fact]
    public async Task Synchronous_exception_is_sanitized_and_retained_only_internally()
    {
        var key = InvokerTestData.Key();
        var exception = new InvalidOperationException("secret C:\\private\\model.dwg");
        var tool = new RecordingTool(key.Capability, (_, _) => throw exception);

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        AssertUnexpectedFailure(result, exception);
        Assert.DoesNotContain(exception.Message, result.Error!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(exception.GetType().Name, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Asynchronous_exception_is_sanitized_and_retained_only_internally()
    {
        var key = InvokerTestData.Key();
        var exception = new ApplicationException("async private detail");
        var tool = new RecordingTool(key.Capability, (_, _) =>
            Task.FromException<Result<InvocationTestResult>>(exception));

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        AssertUnexpectedFailure(result, exception);
        Assert.Equal("The tool failed unexpectedly.", result.Error?.Message);
    }

    [Fact]
    public async Task Fatal_exception_from_tool_escapes_without_normalization()
    {
        var key = InvokerTestData.Key();
        var exception = new OutOfMemoryException("fatal");
        var tool = new RecordingTool(key.Capability, (_, _) => throw exception);

        var escaped = await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            CreateInvoker(key, tool).InvokeAsync(
                InvokerTestData.Operation(key.Capability),
                CancellationToken.None));

        Assert.Same(exception, escaped);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Null_task_is_a_tool_contract_mismatch()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability, (_, _) => null!);

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        AssertToolMismatch(result);
    }

    [Fact]
    public async Task Null_result_is_a_tool_contract_mismatch()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability, (_, _) =>
            Task.FromResult<Result<InvocationTestResult>>(null!));

        var result = await CreateInvoker(key, tool).InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        AssertToolMismatch(result);
    }

    [Fact]
    public async Task Repeated_invocations_use_the_same_invoker_and_delegate()
    {
        var key = InvokerTestData.Key();
        var tool = new RecordingTool(key.Capability);
        var invoker = CreateInvoker(key, tool);

        var first = await invoker.InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);
        var second = await invoker.InvokeAsync(
            InvokerTestData.Operation(key.Capability),
            CancellationToken.None);

        Assert.Equal(InternalInvocationStatus.Success, first.Status);
        Assert.Equal(InternalInvocationStatus.Success, second.Status);
        Assert.Equal(2, tool.InvocationCount);
        Assert.Same(invoker, invoker);
    }

    [Fact]
    public async Task Null_operation_is_rejected()
    {
        var key = InvokerTestData.Key();
        var invoker = CreateInvoker(key, new RecordingTool(key.Capability));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            invoker.InvokeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Null_operation_identifier_is_rejected()
    {
        var key = InvokerTestData.Key();
        var invoker = CreateInvoker(key, new RecordingTool(key.Capability));
        var operation = new InvocationTestOperation(null!, key.Capability);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            invoker.InvokeAsync(operation, CancellationToken.None));
    }

    private static ToolInvoker<InvocationTestOperation, InvocationTestResult> CreateInvoker(
        GeesNO.Core.Runtime.Catalog.ProviderCapabilityKey key,
        ITool<InvocationTestOperation, InvocationTestResult> tool)
    {
        return new ToolInvoker<InvocationTestOperation, InvocationTestResult>(key, tool);
    }

    private static void AssertOperationMismatch(InternalInvocationResult result)
    {
        Assert.Equal(InternalInvocationStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.OperationContractMismatchCode, result.Error?.Code);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }

    private static void AssertToolMismatch(InternalInvocationResult result)
    {
        Assert.Equal(InternalInvocationStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }

    private static void AssertUnexpectedFailure(
        InternalInvocationResult result,
        Exception expectedException)
    {
        Assert.Equal(InternalInvocationStatus.UnexpectedFailure, result.Status);
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, result.Error?.Code);
        Assert.Null(result.SuccessfulValue);
        Assert.Same(expectedException, result.InternalException);
    }
}
