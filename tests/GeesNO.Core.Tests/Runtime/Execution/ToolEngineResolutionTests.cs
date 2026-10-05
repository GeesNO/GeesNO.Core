using System.Collections.Frozen;
using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineResolutionTests
{
    [Fact]
    public async Task Explicit_provider_executes_only_the_requested_provider()
    {
        var reference = ExecutionTestData.Reference();
        var toolA = new ExecutionTestTool(reference);
        var toolB = new ExecutionTestTool(reference);
        var engine = BuildEngine(
            Registration.Executable("provider.a", reference, toolA),
            Registration.Executable("provider.b", reference, toolB));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(
                ExecutionTestData.Operation(reference),
                ProviderId.Parse("provider.b")),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal("provider.b", result.ProviderId?.Value);
        Assert.Equal(0, toolA.InvocationCount);
        Assert.Equal(1, toolB.InvocationCount);
    }

    [Fact]
    public async Task Missing_explicit_provider_is_rejected_without_fallback()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(Registration.Executable("provider.available", reference, tool));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(
                ExecutionTestData.Operation(reference),
                ProviderId.Parse("provider.missing")),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.ProviderNotFoundCode);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Explicit_provider_without_capability_is_rejected_without_fallback()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(ExecutionTestData.EmptyManifest(ProviderId.Parse("provider.empty")));
        Register(builder, Registration.Executable("provider.available", reference, tool));
        var engine = Start(builder.Seal());

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(
                ExecutionTestData.Operation(reference),
                ProviderId.Parse("provider.empty")),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.CapabilityNotFoundCode);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Unsupported)]
    [InlineData(CapabilitySupportLevel.Unavailable)]
    public async Task Explicit_non_executable_support_is_rejected(
        CapabilitySupportLevel supportLevel)
    {
        var reference = ExecutionTestData.Reference();
        var engine = BuildEngine(Registration.NonExecutable("provider.test", reference, supportLevel));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(
                ExecutionTestData.Operation(reference),
                ProviderId.Parse("provider.test")),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.CapabilityNotExecutableCode);
    }

    [Fact]
    public async Task Implicit_single_executable_provider_is_selected()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(Registration.Executable("provider.only", reference, tool));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal("provider.only", result.ProviderId?.Value);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Implicit_missing_capability_is_rejected()
    {
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(ExecutionTestData.EmptyManifest(ProviderId.Parse("provider.empty")));
        var engine = Start(builder.Seal());

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation()),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.CapabilityNotFoundCode);
    }

    [Fact]
    public async Task Implicit_only_non_executable_declarations_are_rejected()
    {
        var reference = ExecutionTestData.Reference();
        var engine = BuildEngine(
            Registration.NonExecutable("provider.a", reference, CapabilitySupportLevel.Unsupported),
            Registration.NonExecutable("provider.b", reference, CapabilitySupportLevel.Unavailable));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.CapabilityNotExecutableCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Multiple_implicit_executables_are_ambiguous_regardless_of_registration_order(
        bool reverse)
    {
        var reference = ExecutionTestData.Reference();
        var toolA = new ExecutionTestTool(reference);
        var toolB = new ExecutionTestTool(reference);
        var registrations = new[]
        {
            Registration.Executable("provider.a", reference, toolA),
            Registration.Executable("provider.b", reference, toolB)
        };
        if (reverse)
        {
            Array.Reverse(registrations);
        }

        var result = await BuildEngine(registrations).ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.AmbiguousProviderCode);
        Assert.Equal(0, toolA.InvocationCount);
        Assert.Equal(0, toolB.InvocationCount);
    }

    [Fact]
    public async Task One_executable_and_multiple_non_executable_declarations_selects_executable()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(
            Registration.NonExecutable("provider.a", reference, CapabilitySupportLevel.Unsupported),
            Registration.Executable("provider.b", reference, tool),
            Registration.NonExecutable("provider.c", reference, CapabilitySupportLevel.Unavailable));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal("provider.b", result.ProviderId?.Value);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Missing_tool_and_invoker_in_defensive_snapshot_are_rejected()
    {
        var reference = ExecutionTestData.Reference();
        var provider = ProviderId.Parse("provider.test");
        var manifest = ExecutionTestData.Manifest(
            provider,
            ExecutionTestData.Descriptor(reference),
            ExecutionTestData.Support(reference));
        var snapshot = new RuntimeCatalogSnapshot(
            [manifest],
            new Dictionary<ProviderCapabilityKey, RegisteredTool>());
        var engine = Start(snapshot);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.ToolNotFoundCode);
    }

    [Fact]
    public async Task Missing_invoker_in_defensive_snapshot_is_rejected_without_reconstruction()
    {
        var reference = ExecutionTestData.Reference();
        var provider = ProviderId.Parse("provider.test");
        var tool = new ExecutionTestTool(reference);
        var snapshot = BuildSnapshot(Registration.Executable(provider.Value, reference, tool));
        ReplaceFrozenIndex<IToolInvoker>(
            snapshot,
            "_invokersByKey",
            new Dictionary<ProviderCapabilityKey, IToolInvoker>());
        var engine = Start(snapshot);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.ToolNotFoundCode);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Incoherent_entry_is_contract_mismatch_without_invocation()
    {
        var reference = ExecutionTestData.Reference();
        var provider = ProviderId.Parse("provider.test");
        var key = new ProviderCapabilityKey(provider, reference);
        var tool = new ExecutionTestTool(reference);
        var snapshot = BuildSnapshot(Registration.Executable(provider.Value, reference, tool));
        var otherReference = ExecutionTestData.Reference("system.other");
        var incoherent = new CapabilityManifestEntry(
            ExecutionTestData.Descriptor(otherReference),
            ExecutionTestData.Support(otherReference));
        ReplaceFrozenIndex(snapshot, "_entriesByKey", new Dictionary<ProviderCapabilityKey, CapabilityManifestEntry>
        {
            [key] = incoherent
        });
        var engine = Start(snapshot);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Mutated_tool_capability_is_contract_mismatch_without_invocation()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(Registration.Executable("provider.test", reference, tool));
        tool.ChangeCapability(ExecutionTestData.Reference("system.changed"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.ToolContractMismatchCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Wrong_operation_type_is_contract_mismatch_without_invocation()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(Registration.Executable("provider.test", reference, tool));
        var operation = new AlternateExecutionTestOperation(OperationId.New(), reference);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(RuntimeInvocationErrors.OperationContractMismatchCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Different_operation_capability_never_partially_matches()
    {
        var reference = ExecutionTestData.Reference();
        var tool = new ExecutionTestTool(reference);
        var engine = BuildEngine(Registration.Executable("provider.test", reference, tool));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(
                ExecutionTestData.Reference("system.execute-test.other"))),
            CancellationToken.None);

        AssertRejected(result, RuntimeExecutionErrors.CapabilityNotFoundCode);
        Assert.Equal(0, tool.InvocationCount);
    }

    private static ToolEngine BuildEngine(params Registration[] registrations)
    {
        return Start(BuildSnapshot(registrations));
    }

    private static RuntimeCatalogSnapshot BuildSnapshot(params Registration[] registrations)
    {
        var builder = new RuntimeCatalogBuilder();
        foreach (var registration in registrations)
        {
            Register(builder, registration);
        }

        return builder.Seal();
    }

    private static void Register(RuntimeCatalogBuilder builder, Registration registration)
    {
        builder.RegisterManifest(ExecutionTestData.Manifest(
            registration.ProviderId,
            registration.Descriptor,
            registration.Support));
        if (registration.Tool is not null)
        {
            builder.RegisterTool(registration.ProviderId, registration.Tool);
        }
    }

    private static ToolEngine Start(RuntimeCatalogSnapshot snapshot)
    {
        var engine = new ToolEngine(snapshot);
        engine.Start();
        return engine;
    }

    private static void ReplaceFrozenIndex<TValue>(
        RuntimeCatalogSnapshot snapshot,
        string fieldName,
        IReadOnlyDictionary<ProviderCapabilityKey, TValue> values)
    {
        var field = typeof(RuntimeCatalogSnapshot).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found.");
        field.SetValue(snapshot, values.ToFrozenDictionary());
    }

    private static void AssertRejected(RuntimeExecutionResult result, string code)
    {
        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Equal(code, result.Error?.Code);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
    }

    private sealed record Registration(
        ProviderId ProviderId,
        CapabilityDescriptor Descriptor,
        CapabilitySupport Support,
        ITool? Tool)
    {
        internal static Registration Executable(
            string providerId,
            CapabilityReference reference,
            ExecutionTestTool tool)
        {
            return new Registration(
                ProviderId.Parse(providerId),
                ExecutionTestData.Descriptor(reference),
                ExecutionTestData.Support(reference),
                tool);
        }

        internal static Registration NonExecutable(
            string providerId,
            CapabilityReference reference,
            CapabilitySupportLevel supportLevel)
        {
            return new Registration(
                ProviderId.Parse(providerId),
                ExecutionTestData.Descriptor(reference),
                ExecutionTestData.Support(reference, supportLevel),
                Tool: null);
        }
    }
}
