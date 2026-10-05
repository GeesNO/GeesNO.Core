using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Execution;

internal static class ExecutionTestData
{
    internal static CapabilityReference Reference(string id = "system.execute-test")
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(1, 0, 0));
    }

    internal static CapabilityDescriptor Descriptor(
        CapabilityReference reference,
        RiskLevel riskLevel = RiskLevel.ReadOnly,
        bool hasSideEffects = false,
        ConfirmationPolicy confirmationPolicy = ConfirmationPolicy.NotRequired)
    {
        return new CapabilityDescriptor(
            reference,
            "Execution Test",
            "Exercises the internal execution pipeline.",
            riskLevel,
            hasSideEffects,
            confirmationPolicy);
    }

    internal static CapabilitySupport Support(
        CapabilityReference reference,
        CapabilitySupportLevel supportLevel = CapabilitySupportLevel.Native,
        UndoSupportLevel undoSupportLevel = UndoSupportLevel.None)
    {
        var restrictions = supportLevel is CapabilitySupportLevel.Partial or CapabilitySupportLevel.Emulated
            ? [new CapabilityRestriction("limit.test", "Test restriction")]
            : Array.Empty<CapabilityRestriction>();
        var unavailableReason = supportLevel == CapabilitySupportLevel.Unavailable
            ? "Unavailable for test"
            : null;

        return new CapabilitySupport(
            reference,
            supportLevel,
            undoSupportLevel,
            restrictions,
            unavailableReason);
    }

    internal static CapabilityManifest Manifest(
        ProviderId providerId,
        CapabilityDescriptor descriptor,
        CapabilitySupport support)
    {
        return new CapabilityManifest(
            providerId,
            new VersionInfo(1, 0, 0),
            [new CapabilityManifestEntry(descriptor, support)]);
    }

    internal static CapabilityManifest EmptyManifest(ProviderId providerId)
    {
        return new CapabilityManifest(providerId, new VersionInfo(1, 0, 0), []);
    }

    internal static ExecutionTestOperation Operation(CapabilityReference? capability = null)
    {
        return new ExecutionTestOperation(OperationId.New(), capability ?? Reference());
    }

    internal static ToolEngine CreateEngine(
        CapabilityDescriptor descriptor,
        CapabilitySupport support,
        ExecutionTestTool? tool = null,
        string providerId = "provider.test",
        bool start = true,
        RuntimeObservability? observability = null)
    {
        var provider = ProviderId.Parse(providerId);
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(Manifest(provider, descriptor, support));

        if (tool is not null)
        {
            builder.RegisterTool(provider, tool);
        }

        var engine = new ToolEngine(builder.Seal(), observability);
        if (start)
        {
            engine.Start();
        }

        return engine;
    }

    internal static ToolEngine CreateExecutableEngine(
        ExecutionTestTool? tool = null,
        string providerId = "provider.test",
        bool start = true,
        RuntimeObservability? observability = null)
    {
        var reference = tool?.Capability ?? Reference();
        return CreateEngine(
            Descriptor(reference),
            Support(reference),
            tool ?? new ExecutionTestTool(reference),
            providerId,
            start,
            observability);
    }
}

internal sealed record ExecutionTestResult(string Value);

internal class ExecutionTestOperation : IOperation<ExecutionTestResult>
{
    internal ExecutionTestOperation(OperationId id, CapabilityReference capability)
    {
        Id = id;
        Capability = capability;
    }

    public OperationId Id { get; }

    public CapabilityReference Capability { get; }
}

internal sealed class AlternateExecutionTestOperation : IOperation<ExecutionTestResult>
{
    internal AlternateExecutionTestOperation(OperationId id, CapabilityReference capability)
    {
        Id = id;
        Capability = capability;
    }

    public OperationId Id { get; }

    public CapabilityReference Capability { get; }
}

internal class ExecutionTestTool : ITool<ExecutionTestOperation, ExecutionTestResult>
{
    private readonly Func<
        ExecutionTestOperation,
        CancellationToken,
        Task<Result<ExecutionTestResult>>> _handler;
    private int _invocationCount;

    internal ExecutionTestTool(
        CapabilityReference capability,
        Func<ExecutionTestOperation, CancellationToken, Task<Result<ExecutionTestResult>>>? handler = null)
    {
        Capability = capability;
        _handler = handler ?? ((_, _) => Task.FromResult(
            Result<ExecutionTestResult>.Success(new ExecutionTestResult("ok"))));
    }

    public CapabilityReference Capability { get; private set; }

    internal int InvocationCount => Volatile.Read(ref _invocationCount);

    internal CancellationToken LastToken { get; private set; }

    public Task<Result<ExecutionTestResult>> ExecuteAsync(
        ExecutionTestOperation operation,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocationCount);
        LastToken = cancellationToken;
        return _handler(operation, cancellationToken);
    }

    internal void ChangeCapability(CapabilityReference capability)
    {
        Capability = capability;
    }
}
