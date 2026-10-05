using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class RuntimeExecutionRequestTests
{
    [Fact]
    public void Request_preserves_exact_operation_and_optional_provider()
    {
        var operation = ExecutionTestData.Operation();
        var provider = ProviderId.Parse("provider.test");

        var explicitRequest = new RuntimeExecutionRequest(operation, provider);
        var implicitRequest = new RuntimeExecutionRequest(operation);

        Assert.Same(operation, explicitRequest.Operation);
        Assert.Same(provider, explicitRequest.ProviderId);
        Assert.Null(implicitRequest.ProviderId);
        Assert.NotEqual(explicitRequest, new RuntimeExecutionRequest(operation, provider));
    }

    [Fact]
    public void Request_rejects_null_operation_without_reading_operation_members()
    {
        Assert.Throws<ArgumentNullException>(() => new RuntimeExecutionRequest(null!));

        var throwing = new ThrowingOperation();
        var request = new RuntimeExecutionRequest(throwing);
        Assert.Same(throwing, request.Operation);
    }

    private sealed class ThrowingOperation : IOperation
    {
        public OperationId Id => throw new InvalidOperationException("Id must not be read by the request.");

        public CapabilityReference Capability =>
            throw new InvalidOperationException("Capability must not be read by the request.");
    }
}
