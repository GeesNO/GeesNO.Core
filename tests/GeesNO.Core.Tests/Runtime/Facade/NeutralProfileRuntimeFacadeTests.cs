using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime;

namespace GeesNO.Core.Tests.Runtime.Facade;

public sealed class NeutralProfileRuntimeFacadeTests
{
    [Fact]
    public async Task Neutral_echo_profile_can_execute_through_the_public_runtime_facade()
    {
        var providerId = ProviderId.Parse("provider.neutral");
        var tool = new FakeNeutralEchoTool();
        using var runtime = new GeesNORuntimeBuilder()
            .RegisterManifest(new CapabilityManifest(
                providerId,
                new VersionInfo(1, 0, 0),
                [NeutralCapabilities.EchoEntry]))
            .RegisterTool(providerId, tool)
            .Build();

        var operation = new NeutralEchoOperation(OperationId.New(), "neutral text");

        var result = await runtime.ExecuteAsync(operation, new ExecutionOptions(providerId), TestContext.Current.CancellationToken);

        Assert.Equal(OperationExecutionStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(operation.Id, result.OperationId);
        Assert.Equal(NeutralCapabilities.Echo, result.Capability);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal(typeof(NeutralEchoResult), result.DeclaredResultType);
        Assert.Equal("neutral text", result.Value?.Text);
        Assert.Null(result.Error);
        Assert.Equal(1, tool.InvocationCount);
    }

    private sealed class FakeNeutralEchoTool : ITool<NeutralEchoOperation, NeutralEchoResult>
    {
        private int _invocationCount;

        public CapabilityReference Capability => NeutralCapabilities.Echo;

        internal int InvocationCount => Volatile.Read(ref _invocationCount);

        public Task<Result<NeutralEchoResult>> ExecuteAsync(
            NeutralEchoOperation operation,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _invocationCount);

            return Task.FromResult(Result<NeutralEchoResult>.Success(new NeutralEchoResult(operation.Text)));
        }
    }
}
