using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Tools;

public sealed class ToolContractTests
{
    [Fact]
    public async Task Tool_can_return_a_successful_result()
    {
        var operation = new TestOperation(OperationId.New());
        ITool<TestOperation, TestResult> tool = new SuccessfulTool();

        var result = await tool.ExecuteAsync(operation, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value.Value);
    }

    [Fact]
    public async Task Tool_can_return_a_failed_result()
    {
        var result = await new FailingTool().ExecuteAsync(new TestOperation(OperationId.New()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tool.failed", result.Error?.Code);
    }

    [Fact]
    public async Task Tool_can_propagate_cooperative_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CancellingTool().ExecuteAsync(new TestOperation(OperationId.New()), source.Token));
    }

    [Fact]
    public void ExecuteAsync_returns_task_of_typed_result_and_requires_a_token()
    {
        var method = Assert.Single(typeof(ITool<TestOperation, TestResult>).GetMethods(), method => method.Name == "ExecuteAsync");
        var parameters = method.GetParameters();

        Assert.Equal(typeof(Task<Result<TestResult>>), method.ReturnType);
        Assert.Equal(typeof(TestOperation), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.False(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Tool_generic_parameters_are_invariant()
    {
        var parameters = typeof(ITool<,>).GetGenericArguments();

        Assert.All(parameters, parameter =>
            Assert.Equal(GenericParameterAttributes.None, parameter.GenericParameterAttributes));
    }

    private sealed record TestResult(string Value);

    private sealed record TestOperation(OperationId Id) : IOperation<TestResult>
    {
        public CapabilityReference Capability { get; } = TestCapability;
    }

    private sealed class SuccessfulTool : ITool<TestOperation, TestResult>
    {
        public CapabilityReference Capability => TestCapability;

        public Task<Result<TestResult>> ExecuteAsync(
            TestOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<TestResult>.Success(new TestResult("ok")));
        }
    }

    private sealed class FailingTool : ITool<TestOperation, TestResult>
    {
        public CapabilityReference Capability => TestCapability;

        public Task<Result<TestResult>> ExecuteAsync(
            TestOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<TestResult>.Failure(new Error("tool.failed", "The tool failed.")));
        }
    }

    private sealed class CancellingTool : ITool<TestOperation, TestResult>
    {
        public CapabilityReference Capability => TestCapability;

        public Task<Result<TestResult>> ExecuteAsync(
            TestOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromCanceled<Result<TestResult>>(cancellationToken);
        }
    }

    private static readonly CapabilityReference TestCapability = new(
        CapabilityId.Parse("system.test"),
        new VersionInfo(1, 0, 0));
}
