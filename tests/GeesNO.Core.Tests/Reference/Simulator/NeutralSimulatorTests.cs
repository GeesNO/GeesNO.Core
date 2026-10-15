using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime;
using GeesNO.Reference.Simulator;

namespace GeesNO.Core.Tests.Reference.Simulator;

public sealed class NeutralSimulatorTests
{
    private const string AmbiguousProviderCode = "runtime.ambiguous-provider";
    private const string AmbiguousProviderMessage = "The requested capability has multiple executable providers.";
    private const string CapabilityNotExecutableCode = "runtime.capability-not-executable";
    private const string CapabilityNotExecutableMessage = "The requested capability is not executable.";
    private const string ProviderNotFoundCode = "runtime.provider-not-found";
    private const string ProviderNotFoundMessage = "The requested provider is not registered.";

    [Fact]
    public void Provider_id_is_strong_and_canonical()
    {
        Assert.Equal("geesno.reference-simulator", NeutralSimulatorProvider.Id.Value);
        Assert.Same(NeutralSimulatorProvider.Id, NeutralSimulatorManifestFactory.Create().ProviderId);
    }

    [Fact]
    public void Manifest_declares_the_three_neutral_capabilities_with_native_read_only_support()
    {
        var providerId = ProviderId.Parse("provider.custom-simulator");

        var manifest = NeutralSimulatorManifestFactory.Create(providerId);

        Assert.Equal(providerId, manifest.ProviderId);
        Assert.Equal(new VersionInfo(1, 0, 0), manifest.ProviderVersion);
        Assert.Equal(
            ["neutral.describe@1.0.0", "neutral.echo@1.0.0", "neutral.validate@1.0.0"],
            manifest.Capabilities.Select(entry => entry.Descriptor.Reference.ToString()));
        Assert.All(manifest.Capabilities, AssertNeutralEntry);
    }

    [Fact]
    public void Tools_implement_their_exact_typed_contracts()
    {
        var echo = new NeutralEchoTool();
        var describe = new NeutralDescribeTool();
        var validate = new NeutralValidateTool();

        Assert.IsAssignableFrom<ITool<NeutralEchoOperation, NeutralEchoResult>>(echo);
        Assert.IsAssignableFrom<ITool<NeutralDescribeOperation, NeutralDescribeResult>>(describe);
        Assert.IsAssignableFrom<ITool<NeutralValidateOperation, NeutralValidationResult>>(validate);
        Assert.Equal(NeutralCapabilities.Echo, echo.Capability);
        Assert.Equal(NeutralCapabilities.Describe, describe.Capability);
        Assert.Equal(NeutralCapabilities.Validate, validate.Capability);
    }

    [Fact]
    public async Task Echo_returns_the_received_text_and_a_public_functional_failure_for_empty_text()
    {
        var tool = new NeutralEchoTool();

        var success = await tool.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), "hello neutral"),
            CancellationToken.None);
        var failure = await tool.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), string.Empty),
            CancellationToken.None);

        Assert.True(success.IsSuccess);
        Assert.Equal("hello neutral", success.Value.Text);
        Assert.True(failure.IsFailure);
        Assert.Equal("neutral.echo.empty-text", failure.Error?.Code);
        Assert.Equal("Neutral echo text cannot be empty.", failure.Error?.Message);
        AssertPublicError(failure.Error!);
    }

    [Fact]
    public async Task Describe_returns_public_safe_metadata()
    {
        var result = await new NeutralDescribeTool().ExecuteAsync(
            new NeutralDescribeOperation(OperationId.New()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.ProviderDescription));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.CapabilityDescription));
        AssertPublicText(result.Value.ProviderDescription);
        AssertPublicText(result.Value.CapabilityDescription);
    }

    [Fact]
    public async Task Validate_returns_typed_valid_and_invalid_results()
    {
        var tool = new NeutralValidateTool();

        var valid = await tool.ExecuteAsync(
            new NeutralValidateOperation(OperationId.New(), "abc", maxLength: 3),
            CancellationToken.None);
        var invalid = await tool.ExecuteAsync(
            new NeutralValidateOperation(OperationId.New(), "abcd", maxLength: 3),
            CancellationToken.None);

        Assert.True(valid.IsSuccess);
        Assert.True(valid.Value.IsValid);
        Assert.Null(valid.Value.Reason);
        Assert.True(invalid.IsSuccess);
        Assert.False(invalid.Value.IsValid);
        Assert.Equal("Input exceeds the requested maximum length.", invalid.Value.Reason);
        AssertPublicText(invalid.Value.Reason!);
    }

    [Fact]
    public async Task Tools_reject_null_operations_and_obey_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new NeutralEchoTool().ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new NeutralEchoTool().ExecuteAsync(
                new NeutralEchoOperation(OperationId.New(), "cancelled"),
                source.Token));
    }

    [Fact]
    public async Task Runtime_executes_echo_with_explicit_provider_through_public_facade()
    {
        var providerId = NeutralSimulatorProvider.Id;
        using var runtime = BuildRuntime(providerId);
        var operation = new NeutralEchoOperation(OperationId.New(), "explicit");

        var result = await runtime.ExecuteAsync(operation, new ExecutionOptions(providerId), TestContext.Current.CancellationToken);

        AssertSuccess(result, providerId, operation, "explicit");
    }

    [Fact]
    public async Task Runtime_executes_echo_with_implicit_single_provider_through_public_facade()
    {
        var providerId = NeutralSimulatorProvider.Id;
        using var runtime = BuildRuntime(providerId);
        var operation = new NeutralEchoOperation(OperationId.New(), "implicit");

        var result = await runtime.ExecuteAsync(operation, cancellationToken: TestContext.Current.CancellationToken);

        AssertSuccess(result, providerId, operation, "implicit");
    }

    [Fact]
    public async Task Runtime_preserves_public_functional_failure_from_simulator_tool()
    {
        var providerId = NeutralSimulatorProvider.Id;
        using var runtime = BuildRuntime(providerId);

        var result = await runtime.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), string.Empty),
            new ExecutionOptions(providerId),
            TestContext.Current.CancellationToken);

        Assert.Equal(OperationExecutionStatus.FunctionalFailure, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal(typeof(NeutralEchoResult), result.DeclaredResultType);
        Assert.Null(result.Value);
        Assert.Equal("neutral.echo.empty-text", result.Error?.Code);
        Assert.Equal("Neutral echo text cannot be empty.", result.Error?.Message);
        AssertPublicError(result.Error!);
    }

    [Fact]
    public async Task Runtime_rejects_implicit_execution_when_two_simulator_providers_are_executable()
    {
        var providerA = ProviderId.Parse("provider.simulator-a");
        var providerB = ProviderId.Parse("provider.simulator-b");
        using var runtime = BuildRuntime(providerA, providerB);

        var result = await runtime.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), "ambiguous"),
            cancellationToken: TestContext.Current.CancellationToken);

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            AmbiguousProviderCode,
            AmbiguousProviderMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
    }

    [Fact]
    public async Task Runtime_rejects_missing_explicit_provider_without_fallback()
    {
        using var runtime = BuildRuntime(NeutralSimulatorProvider.Id);

        var result = await runtime.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), "missing provider"),
            new ExecutionOptions(ProviderId.Parse("provider.missing-simulator")),
            TestContext.Current.CancellationToken);

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            ProviderNotFoundCode,
            ProviderNotFoundMessage);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
    }

    [Fact]
    public async Task Runtime_rejects_unavailable_neutral_capability()
    {
        var providerId = ProviderId.Parse("provider.unavailable-simulator");
        using var runtime = new GeesNORuntimeBuilder()
            .RegisterManifest(CreateUnavailableEchoManifest(providerId))
            .Build();

        var result = await runtime.ExecuteAsync(
            new NeutralEchoOperation(OperationId.New(), "unavailable"),
            new ExecutionOptions(providerId),
            TestContext.Current.CancellationToken);

        AssertPublicFailure(
            result,
            OperationExecutionStatus.Rejected,
            CapabilityNotExecutableCode,
            CapabilityNotExecutableMessage);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Null(result.DeclaredResultType);
    }

    [Fact]
    public void Runtime_public_surface_remains_the_current_allowlist()
    {
        var exportedTypeNames = typeof(GeesNORuntime).Assembly.GetExportedTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ExecutionOptions",
                "GeesNORuntime",
                "GeesNORuntimeBuilder",
                "GeesNORuntimeConfigurationException",
                "OperationExecutionResult`1",
                "OperationExecutionStatus",
                "RuntimeCapability"
            ],
            exportedTypeNames);
    }

    [Fact]
    public void Contracts_preserve_the_neutral_profile_surface()
    {
        var exportedTypeNames = typeof(NeutralCapabilities).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "GeesNO.Core.Contracts.Neutral")
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "NeutralCapabilities",
                "NeutralDescribeOperation",
                "NeutralDescribeResult",
                "NeutralEchoOperation",
                "NeutralEchoResult",
                "NeutralValidateOperation",
                "NeutralValidationResult"
            ],
            exportedTypeNames);
    }

    [Fact]
    public void Runtime_results_expose_no_exceptions_stack_traces_or_boxed_values()
    {
        var publicProperties = typeof(OperationExecutionResult<NeutralEchoResult>).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.DoesNotContain(publicProperties, property => property.PropertyType == typeof(object));
        Assert.DoesNotContain(publicProperties, property => typeof(Exception).IsAssignableFrom(property.PropertyType));
        Assert.DoesNotContain(publicProperties, property => property.Name.Contains("Exception", StringComparison.Ordinal));
        Assert.DoesNotContain(publicProperties, property => property.Name.Contains("StackTrace", StringComparison.Ordinal));
        Assert.DoesNotContain(publicProperties, property => property.Name == "SuccessfulValue");
    }

    private static GeesNORuntime BuildRuntime(params ProviderId[] providerIds)
    {
        var builder = new GeesNORuntimeBuilder();

        foreach (var providerId in providerIds)
        {
            builder
                .RegisterManifest(NeutralSimulatorManifestFactory.Create(providerId))
                .RegisterTool(providerId, new NeutralEchoTool())
                .RegisterTool(providerId, new NeutralDescribeTool())
                .RegisterTool(providerId, new NeutralValidateTool());
        }

        return builder.Build();
    }

    private static CapabilityManifest CreateUnavailableEchoManifest(ProviderId providerId)
    {
        var unavailableSupport = new CapabilitySupport(
            NeutralCapabilities.Echo,
            CapabilitySupportLevel.Unavailable,
            UndoSupportLevel.None,
            [],
            "Unavailable for the reference simulator test.");

        return new CapabilityManifest(
            providerId,
            new VersionInfo(1, 0, 0),
            [new CapabilityManifestEntry(NeutralCapabilities.EchoDescriptor, unavailableSupport)]);
    }

    private static void AssertNeutralEntry(CapabilityManifestEntry entry)
    {
        Assert.Equal(RiskLevel.ReadOnly, entry.Descriptor.RiskLevel);
        Assert.False(entry.Descriptor.HasSideEffects);
        Assert.Equal(ConfirmationPolicy.NotRequired, entry.Descriptor.ConfirmationPolicy);
        Assert.Equal(CapabilitySupportLevel.Native, entry.Support.SupportLevel);
        Assert.Equal(UndoSupportLevel.None, entry.Support.UndoSupportLevel);
        Assert.Empty(entry.Support.Restrictions);
        Assert.Null(entry.Support.UnavailableReason);
    }

    private static void AssertSuccess(
        OperationExecutionResult<NeutralEchoResult> result,
        ProviderId providerId,
        NeutralEchoOperation operation,
        string expectedText)
    {
        Assert.Equal(OperationExecutionStatus.Success, result.Status);
        Assert.True(result.IsSuccess);
        Assert.Equal(operation.Id, result.OperationId);
        Assert.Equal(operation.Capability, result.Capability);
        Assert.Equal(providerId, result.ProviderId);
        Assert.Equal(typeof(NeutralEchoResult), result.DeclaredResultType);
        Assert.Equal(expectedText, result.Value?.Text);
        Assert.Null(result.Error);
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
        Assert.Null(result.Value);
        Assert.Equal(expectedCode, result.Error?.Code);
        Assert.Equal(expectedMessage, result.Error?.Message);
        AssertPublicError(result.Error!);
    }

    private static void AssertPublicError(Error error)
    {
        AssertPublicText(error.Code);
        AssertPublicText(error.Message);
    }

    private static void AssertPublicText(string text)
    {
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("E:\\", text, StringComparison.OrdinalIgnoreCase);
    }
}
