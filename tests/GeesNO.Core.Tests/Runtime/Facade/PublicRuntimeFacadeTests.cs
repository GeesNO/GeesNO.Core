using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime;

namespace GeesNO.Core.Tests.Runtime.Facade;

public sealed class PublicRuntimeFacadeTests
{
    private const string AmbiguousProviderCode = "runtime.ambiguous-provider";
    private const string AmbiguousProviderMessage = "The requested capability has multiple executable providers.";
    private const string CapabilityNotFoundCode = "runtime.capability-not-found";
    private const string CapabilityNotFoundMessage = "The requested capability is not declared.";
    private const string ConfigurationInvalidCode = "runtime.configuration-invalid";
    private const string NotReadyCode = "runtime.not-ready";
    private const string NotReadyMessage = "The Runtime is not ready to execute operations.";
    private const string OperationContractMismatchCode = "runtime.operation-contract-mismatch";
    private const string OperationContractMismatchMessage = "The operation does not match the registered tool contract.";
    private const string ProviderNotFoundCode = "runtime.provider-not-found";
    private const string ProviderNotFoundMessage = "The requested provider is not registered.";
    private const string UnexpectedToolFailureCode = "runtime.unexpected-tool-failure";
    private const string UnexpectedToolFailureMessage = "The tool failed unexpectedly.";

    [Fact]
    public async Task Builder_registers_public_manifest_and_tool_and_builds_executable_runtime()
    {
        var providerId = ProviderId.Parse("provider.facade");
        var reference = Reference();
        var descriptor = Descriptor(reference);
        var support = Support(reference);
        var tool = new FacadeTool(reference);
        using var runtime = new GeesNORuntimeBuilder()
            .RegisterManifest(Manifest(providerId, new CapabilityManifestEntry(descriptor, support)))
            .RegisterTool(providerId, tool)
            .Build();

        var capability = Assert.Single(runtime.Capabilities);
        Assert.Equal(providerId, capability.ProviderId);
        Assert.Same(descriptor, capability.Descriptor);
        Assert.Same(support, capability.Support);

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), reference),
            new ExecutionOptions(providerId));

        Assert.Equal(OperationExecutionStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal(typeof(FacadeResult), result.DeclaredResultType);
        Assert.Equal("ok", result.Value?.Value);
        Assert.Null(result.Error);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public void Capabilities_returns_a_defensive_deterministic_view()
    {
        var alpha = Reference("system.alpha");
        var beta = Reference("system.beta");
        var zeta = Reference("system.zeta");
        using var runtime = BuildRuntime(
            Registration(
                "provider.b",
                [Entry(beta, CapabilitySupportLevel.Unsupported)],
                []),
            Registration(
                "provider.a",
                [Entry(zeta, CapabilitySupportLevel.Unsupported), Entry(alpha, CapabilitySupportLevel.Unsupported)],
                []));

        var ordered = runtime.Capabilities
            .Select(capability => $"{capability.ProviderId.Value}:{capability.Descriptor.Reference.Id.Value}")
            .ToArray();

        Assert.Equal(
            ["provider.a:system.alpha", "provider.a:system.zeta", "provider.b:system.beta"],
            ordered);
        var mutableView = Assert.IsAssignableFrom<ICollection<RuntimeCapability>>(runtime.Capabilities);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(mutableView.Clear);
    }

    [Fact]
    public async Task Explicit_provider_executes_only_the_selected_provider()
    {
        var reference = Reference();
        var toolA = new FacadeTool(reference);
        var toolB = new FacadeTool(reference, (_, _) => Task.FromResult(
            Result<FacadeResult>.Success(new FacadeResult("selected"))));
        using var runtime = BuildRuntime(
            Registration("provider.a", [Entry(reference)], [toolA]),
            Registration("provider.b", [Entry(reference)], [toolB]));

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), reference),
            new ExecutionOptions(ProviderId.Parse("provider.b")));

        Assert.Equal(OperationExecutionStatus.Success, result.Status);
        Assert.Equal("provider.b", result.ProviderId?.Value);
        Assert.Equal("selected", result.Value?.Value);
        Assert.Equal(0, toolA.InvocationCount);
        Assert.Equal(1, toolB.InvocationCount);
    }

    [Fact]
    public async Task Implicit_single_provider_executes_successfully()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.only", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        Assert.Equal(OperationExecutionStatus.Success, result.Status);
        Assert.Equal("provider.only", result.ProviderId?.Value);
        Assert.Equal(typeof(FacadeResult), result.DeclaredResultType);
        Assert.Equal("ok", result.Value?.Value);
        Assert.Null(result.Error);
        Assert.Equal(1, tool.InvocationCount);
    }

    [Fact]
    public async Task Multiple_implicit_executable_providers_are_rejected_as_ambiguous()
    {
        var reference = Reference();
        var toolA = new FacadeTool(reference);
        var toolB = new FacadeTool(reference);
        using var runtime = BuildRuntime(
            Registration("provider.a", [Entry(reference)], [toolA]),
            Registration("provider.b", [Entry(reference)], [toolB]));

        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            AmbiguousProviderCode,
            AmbiguousProviderMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Equal(0, toolA.InvocationCount);
        Assert.Equal(0, toolB.InvocationCount);
    }

    [Fact]
    public async Task Missing_explicit_provider_is_rejected_without_fallback()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.available", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), reference),
            new ExecutionOptions(ProviderId.Parse("provider.missing")));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            ProviderNotFoundCode,
            ProviderNotFoundMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Explicit_provider_without_capability_is_rejected_without_fallback()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(
            Registration("provider.empty", [], []),
            Registration("provider.available", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), reference),
            new ExecutionOptions(ProviderId.Parse("provider.empty")));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            CapabilityNotFoundCode,
            CapabilityNotFoundMessage);
        Assert.Equal("provider.empty", result.ProviderId?.Value);
        Assert.Null(result.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Missing_capability_is_rejected_with_normalized_error()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.available", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), Reference("system.missing")));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            CapabilityNotFoundCode,
            CapabilityNotFoundMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Functional_failure_preserves_public_tool_error()
    {
        var reference = Reference();
        var error = new Error("tool.expected-failure", "Expected failure");
        var tool = new FacadeTool(reference, (_, _) => Task.FromResult(Result<FacadeResult>.Failure(error)));
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.FunctionalFailure,
            error.Code,
            error.Message);
        Assert.Equal("provider.facade", result.ProviderId?.Value);
        Assert.Equal(typeof(FacadeResult), result.DeclaredResultType);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task Contract_mismatch_is_rejected_without_invoking_tool()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(new AlternateFacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.ContractMismatch,
            OperationContractMismatchCode,
            OperationContractMismatchMessage);
        Assert.Equal("provider.facade", result.ProviderId?.Value);
        Assert.Equal(typeof(FacadeResult), result.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Unexpected_failure_returns_sanitized_public_error_without_exception_surface()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference, (_, _) => Task.FromException<Result<FacadeResult>>(
            new InvalidOperationException("secret local path C:\\private\\tool.state")));
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [tool]));

        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.UnexpectedFailure,
            UnexpectedToolFailureCode,
            UnexpectedToolFailureMessage);
        Assert.Equal("provider.facade", result.ProviderId?.Value);
        Assert.Equal(typeof(FacadeResult), result.DeclaredResultType);
        Assert.DoesNotContain("secret", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
        AssertNoPublicExceptionSurface(result);
    }

    [Fact]
    public async Task Cancelled_operation_returns_cancelled_status_without_value_or_error()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [tool]));
        using var source = new CancellationTokenSource();
        source.Cancel();

        var result = await runtime.ExecuteAsync(
            new FacadeOperation(OperationId.New(), reference),
            cancellationToken: source.Token);

        Assert.Equal(OperationExecutionStatus.Cancelled, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Null(result.Value);
        Assert.Null(result.Error);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Stop_is_idempotent_and_execution_after_stop_returns_public_rejected_result()
    {
        var reference = Reference();
        var tool = new FacadeTool(reference);
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [tool]));

        runtime.Stop();
        runtime.Stop();
        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            NotReadyCode,
            NotReadyMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Equal(0, tool.InvocationCount);
    }

    [Fact]
    public async Task Dispose_is_safe_and_equivalent_to_stop()
    {
        var reference = Reference();
        var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [new FacadeTool(reference)]));

        runtime.Dispose();
        runtime.Dispose();
        var result = await runtime.ExecuteAsync(new FacadeOperation(OperationId.New(), reference));

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            NotReadyCode,
            NotReadyMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
    }

    [Fact]
    public async Task Null_operation_is_rejected_with_expected_BCL_exception()
    {
        var reference = Reference();
        using var runtime = BuildRuntime(Registration("provider.facade", [Entry(reference)], [new FacadeTool(reference)]));

        await Assert.ThrowsAsync<ArgumentNullException>(() => runtime.ExecuteAsync<FacadeResult>(null!));
    }

    [Fact]
    public void Null_manifest_tool_and_provider_are_rejected_with_expected_BCL_exceptions()
    {
        var providerId = ProviderId.Parse("provider.facade");
        var tool = new FacadeTool(Reference());

        Assert.Throws<ArgumentNullException>(() => new GeesNORuntimeBuilder().RegisterManifest(null!));
        Assert.Throws<ArgumentNullException>(() => new GeesNORuntimeBuilder().RegisterTool(null!, tool));
        Assert.Throws<ArgumentNullException>(() => new GeesNORuntimeBuilder().RegisterTool(providerId, null!));
    }

    [Fact]
    public void Invalid_build_configuration_uses_public_sanitized_exception()
    {
        var providerId = ProviderId.Parse("provider.facade");
        var reference = Reference();
        var builder = new GeesNORuntimeBuilder()
            .RegisterManifest(Manifest(providerId, Entry(reference)));

        var exception = Assert.Throws<GeesNORuntimeConfigurationException>(builder.Build);

        Assert.Equal("The Runtime configuration is invalid.", exception.Message);
        Assert.NotEmpty(exception.Errors);
        Assert.All(exception.Errors, error =>
        {
            Assert.Equal(ConfigurationInvalidCode, error.Code);
            Assert.NotEmpty(error.Message);
        });
        Assert.DoesNotContain("RuntimeCatalogValidationException", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Builder_cannot_mutate_or_build_again_after_build()
    {
        var builder = new GeesNORuntimeBuilder();
        using var runtime = builder.Build();

        Assert.Empty(runtime.Capabilities);
        Assert.Throws<GeesNORuntimeConfigurationException>(() => builder.RegisterManifest(
            Manifest(ProviderId.Parse("provider.facade"), Entry(Reference()))));
        Assert.Throws<GeesNORuntimeConfigurationException>(builder.Build);
    }

    [Fact]
    public void OperationExecutionResult_public_surface_does_not_expose_exception_stack_trace_or_boxed_value()
    {
        var publicProperties = typeof(OperationExecutionResult<FacadeResult>).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.DoesNotContain(publicProperties, property => property.PropertyType == typeof(object));
        Assert.DoesNotContain(publicProperties, property => typeof(Exception).IsAssignableFrom(property.PropertyType));
        Assert.DoesNotContain(publicProperties, property => property.Name == "SuccessfulValue");
        Assert.DoesNotContain(publicProperties, property => property.Name.Contains("StackTrace", StringComparison.Ordinal));
    }

    private static void AssertPublicFailure<TResult>(
        OperationExecutionResult<TResult> result,
        OperationExecutionStatus expectedStatus,
        string expectedCode,
        string expectedMessage)
        where TResult : notnull
    {
        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Equal(expectedMessage, result.Error?.Message);
        Assert.Null(result.Value);
    }

    private static GeesNORuntime BuildRuntime(params ProviderRegistration[] registrations)
    {
        var builder = new GeesNORuntimeBuilder();
        foreach (var registration in registrations)
        {
            builder.RegisterManifest(new CapabilityManifest(
                registration.ProviderId,
                new VersionInfo(1, 0, 0),
                registration.Entries));

            foreach (var tool in registration.Tools)
            {
                builder.RegisterTool(registration.ProviderId, tool);
            }
        }

        return builder.Build();
    }

    private static ProviderRegistration Registration(
        string providerId,
        CapabilityManifestEntry[] entries,
        ITool[] tools)
    {
        return new ProviderRegistration(ProviderId.Parse(providerId), entries, tools);
    }

    private static CapabilityReference Reference(string id = "system.facade")
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(1, 0, 0));
    }

    private static CapabilityDescriptor Descriptor(CapabilityReference reference)
    {
        return new CapabilityDescriptor(
            reference,
            "Facade Test",
            "Exercises the public Runtime facade.",
            RiskLevel.ReadOnly,
            hasSideEffects: false,
            ConfirmationPolicy.NotRequired);
    }

    private static CapabilitySupport Support(
        CapabilityReference reference,
        CapabilitySupportLevel supportLevel = CapabilitySupportLevel.Native)
    {
        var unavailableReason = supportLevel == CapabilitySupportLevel.Unavailable
            ? "Unavailable for test"
            : null;

        return new CapabilitySupport(
            reference,
            supportLevel,
            UndoSupportLevel.None,
            Array.Empty<CapabilityRestriction>(),
            unavailableReason);
    }

    private static CapabilityManifestEntry Entry(
        CapabilityReference reference,
        CapabilitySupportLevel supportLevel = CapabilitySupportLevel.Native)
    {
        return new CapabilityManifestEntry(Descriptor(reference), Support(reference, supportLevel));
    }

    private static CapabilityManifest Manifest(
        ProviderId providerId,
        params CapabilityManifestEntry[] entries)
    {
        return new CapabilityManifest(providerId, new VersionInfo(1, 0, 0), entries);
    }

    private static void AssertNoPublicExceptionSurface(OperationExecutionResult<FacadeResult> result)
    {
        var publicProperties = result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.DoesNotContain(publicProperties, property => typeof(Exception).IsAssignableFrom(property.PropertyType));
        Assert.DoesNotContain(publicProperties, property => property.Name.Contains("Exception", StringComparison.Ordinal));
        Assert.DoesNotContain(publicProperties, property => property.Name.Contains("StackTrace", StringComparison.Ordinal));
    }

    private sealed record ProviderRegistration(
        ProviderId ProviderId,
        CapabilityManifestEntry[] Entries,
        ITool[] Tools);

    private sealed record FacadeResult(string Value);

    private sealed class FacadeOperation : IOperation<FacadeResult>
    {
        internal FacadeOperation(OperationId id, CapabilityReference capability)
        {
            Id = id;
            Capability = capability;
        }

        public OperationId Id { get; }

        public CapabilityReference Capability { get; }
    }

    private sealed class AlternateFacadeOperation : IOperation<FacadeResult>
    {
        internal AlternateFacadeOperation(OperationId id, CapabilityReference capability)
        {
            Id = id;
            Capability = capability;
        }

        public OperationId Id { get; }

        public CapabilityReference Capability { get; }
    }

    private sealed class FacadeTool : ITool<FacadeOperation, FacadeResult>
    {
        private readonly Func<FacadeOperation, CancellationToken, Task<Result<FacadeResult>>> _handler;
        private int _invocationCount;

        internal FacadeTool(
            CapabilityReference capability,
            Func<FacadeOperation, CancellationToken, Task<Result<FacadeResult>>>? handler = null)
        {
            Capability = capability;
            _handler = handler ?? ((_, _) => Task.FromResult(
                Result<FacadeResult>.Success(new FacadeResult("ok"))));
        }

        public CapabilityReference Capability { get; }

        internal int InvocationCount => Volatile.Read(ref _invocationCount);

        public Task<Result<FacadeResult>> ExecuteAsync(
            FacadeOperation operation,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _invocationCount);
            return _handler(operation, cancellationToken);
        }
    }
}