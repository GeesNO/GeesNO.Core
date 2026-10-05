using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Runtime.Invokers;

internal static class InvokerTestData
{
    internal static CapabilityReference Reference(string id = "system.test")
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(1, 0, 0));
    }

    internal static ProviderCapabilityKey Key(CapabilityReference? capability = null)
    {
        return new ProviderCapabilityKey(
            ProviderId.Parse("provider.test"),
            capability ?? Reference());
    }

    internal static InvocationTestOperation Operation(CapabilityReference? capability = null)
    {
        return new InvocationTestOperation(OperationId.New(), capability ?? Reference());
    }
}

internal sealed record InvocationTestResult(string Value);

internal class InvocationTestOperation : IOperation<InvocationTestResult>
{
    internal InvocationTestOperation(OperationId id, CapabilityReference capability)
    {
        Id = id;
        Capability = capability;
    }

    public OperationId Id { get; }

    public CapabilityReference Capability { get; }
}

internal sealed class DerivedInvocationTestOperation : InvocationTestOperation
{
    internal DerivedInvocationTestOperation(OperationId id, CapabilityReference capability)
        : base(id, capability)
    {
    }
}

internal sealed class AlternateInvocationTestOperation : IOperation<InvocationTestResult>
{
    internal AlternateInvocationTestOperation(OperationId id, CapabilityReference capability)
    {
        Id = id;
        Capability = capability;
    }

    public OperationId Id { get; }

    public CapabilityReference Capability { get; }
}

internal class RecordingTool : ITool<InvocationTestOperation, InvocationTestResult>
{
    private readonly Func<
        InvocationTestOperation,
        CancellationToken,
        Task<Result<InvocationTestResult>>> _handler;

    internal RecordingTool(
        CapabilityReference capability,
        Func<
            InvocationTestOperation,
            CancellationToken,
            Task<Result<InvocationTestResult>>>? handler = null)
    {
        Capability = capability;
        _handler = handler ?? ((_, _) => Task.FromResult(
            Result<InvocationTestResult>.Success(new InvocationTestResult("ok"))));
    }

    public virtual CapabilityReference Capability { get; private set; }

    internal int InvocationCount { get; private set; }

    internal CancellationToken LastToken { get; private set; }

    public Task<Result<InvocationTestResult>> ExecuteAsync(
        InvocationTestOperation operation,
        CancellationToken cancellationToken)
    {
        InvocationCount++;
        LastToken = cancellationToken;
        return _handler(operation, cancellationToken);
    }

    internal void ChangeCapability(CapabilityReference capability)
    {
        Capability = capability;
    }
}

internal sealed class ThrowingCapabilityTool : RecordingTool
{
    private bool _throwOnRead;

    internal ThrowingCapabilityTool(CapabilityReference capability)
        : base(capability)
    {
    }

    public override CapabilityReference Capability => _throwOnRead
        ? throw new InvalidOperationException("private capability detail")
        : base.Capability;

    internal void ThrowOnRead()
    {
        _throwOnRead = true;
    }
}
