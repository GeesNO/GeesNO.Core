using System.Globalization;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Runtime;
using GeesNO.Reference.Simulator;
using GeesNO.Sample.Cli;

namespace GeesNO.Core.Tests.Sample.Cli;

public sealed class SampleCliTests
{
    [Fact]
    public async Task Help_returns_success_and_usage()
    {
        var result = await RunAsync("--help");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Contains("usage=geesno-sample --help", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("validate.defaultMaxLength=4096", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(result.Stderr);
    }

    [Fact]
    public async Task Missing_arguments_return_invalid_usage()
    {
        var result = await RunAsync();

        Assert.Equal(CliExitCodes.InvalidUsage, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=invalid-usage", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.code=sample.invalid-usage", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_command_returns_invalid_usage()
    {
        var result = await RunAsync("unknown");

        Assert.Equal(CliExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("status=invalid-usage", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.message=Unknown command.", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Capabilities_lists_public_runtime_capabilities_deterministically()
    {
        var result = await RunAsync("capabilities");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Stderr);
        Assert.Equal(
            [
                "status=success",
                "capability.count=3",
                "capability.0.provider=geesno.reference-simulator",
                "capability.0.reference=neutral.describe@1.0.0",
                "capability.0.support=Native",
                "capability.1.provider=geesno.reference-simulator",
                "capability.1.reference=neutral.echo@1.0.0",
                "capability.1.support=Native",
                "capability.2.provider=geesno.reference-simulator",
                "capability.2.reference=neutral.validate@1.0.0",
                "capability.2.support=Native"
            ],
            Lines(result.Stdout));
    }

    [Fact]
    public async Task Echo_uses_implicit_provider_and_returns_public_success_output()
    {
        var result = await RunAsync("echo", "hello");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Stderr);
        Assert.Contains("status=success", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("operation=neutral.echo", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("capability=neutral.echo@1.0.0", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("provider=geesno.reference-simulator", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("value.text=hello", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Echo_accepts_explicit_provider()
    {
        var result = await RunAsync("echo", "hello", "--provider", "geesno.reference-simulator");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Contains("provider=geesno.reference-simulator", result.Stdout, StringComparison.Ordinal);
        Assert.Empty(result.Stderr);
    }

    [Fact]
    public async Task Describe_returns_public_descriptions_from_runtime_execution()
    {
        var result = await RunAsync("describe");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Stderr);
        Assert.Contains("operation=neutral.describe", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("capability=neutral.describe@1.0.0", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("value.providerDescription=GeesNO neutral reference simulator", result.Stdout, StringComparison.Ordinal);
        Assert.Contains(
            "value.capabilityDescription=Reference implementation of neutral echo, describe and validate capabilities.",
            result.Stdout,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_without_max_length_uses_the_contract_maximum_as_sample_default()
    {
        var result = await RunAsync("validate", "hello");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Stderr);
        Assert.Contains("operation=neutral.validate", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("capability=neutral.validate@1.0.0", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("value.isValid=true", result.Stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("value.reason=", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_with_max_length_preserves_invalid_result_as_successful_execution()
    {
        var result = await RunAsync("validate", "hello world", "--max-length", "5");

        Assert.Equal(CliExitCodes.Success, result.ExitCode);
        Assert.Empty(result.Stderr);
        Assert.Contains("value.isValid=false", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("value.reason=Input exceeds the requested maximum length.", result.Stdout, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-an-integer")]
    [InlineData("0")]
    [InlineData("4097")]
    public async Task Invalid_max_length_returns_invalid_usage(string maxLength)
    {
        var result = await RunAsync("validate", "hello", "--max-length", maxLength);

        Assert.Equal(CliExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("status=invalid-usage", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("Max length must be an integer from 1 to 4096.", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Malformed_provider_id_returns_invalid_usage()
    {
        var result = await RunAsync("echo", "hello", "--provider", "Provider.Missing");

        Assert.Equal(CliExitCodes.InvalidUsage, result.ExitCode);
        Assert.Contains("status=invalid-usage", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("Provider identifier is not valid.", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_missing_provider_is_rejected_by_runtime_without_fallback()
    {
        var result = await RunAsync("echo", "hello", "--provider", "provider.missing");

        Assert.Equal(CliExitCodes.RejectedExecution, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=rejected", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("capability=neutral.echo@1.0.0", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.code=runtime.provider-not-found", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ambiguous_implicit_provider_is_rejected_by_runtime()
    {
        var providerA = ProviderId.Parse("provider.sample-a");
        var providerB = ProviderId.Parse("provider.sample-b");
        var app = new CliApplication(() => BuildRuntime(providerA, providerB));

        var result = await RunAsync(app, CancellationToken.None, "echo", "ambiguous");

        Assert.Equal(CliExitCodes.RejectedExecution, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=rejected", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.code=runtime.ambiguous-provider", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Functional_failure_maps_to_exit_code_4()
    {
        var result = await RunAsync("echo", string.Empty);

        Assert.Equal(CliExitCodes.FunctionalFailure, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=functional-failure", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("provider=geesno.reference-simulator", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.code=neutral.echo.empty-text", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Runtime_cancellation_maps_to_exit_code_6()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var result = await RunAsync(new CliApplication(), source.Token, "echo", "cancelled");

        Assert.Equal(CliExitCodes.Cancellation, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=cancelled", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("capability=neutral.echo@1.0.0", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unexpected_failure_at_process_boundary_is_sanitized_and_maps_to_exit_code_5()
    {
        var app = new CliApplication(() => throw new InvalidOperationException("C:\\secret\\failure Exception StackTrace"));

        var result = await RunAsync(app, CancellationToken.None, "capabilities");

        Assert.Equal(CliExitCodes.RuntimeFailure, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.Contains("status=unexpected-failure", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.code=sample.unexpected-failure", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("error.message=The sample command failed unexpectedly.", result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", result.Stderr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", result.Stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Null_arguments_are_rejected_by_the_sample_runner_contract()
    {
        var app = new CliApplication();
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        await Assert.ThrowsAsync<ArgumentNullException>(() => app.RunAsync(null!, output, error, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => app.RunAsync(Array.Empty<string>(), null!, error, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => app.RunAsync(Array.Empty<string>(), output, null!, TestContext.Current.CancellationToken));
    }

    private static async Task<CliRunResult> RunAsync(params string[] args)
    {
        return await RunAsync(new CliApplication(), CancellationToken.None, args).ConfigureAwait(false);
    }

    private static async Task<CliRunResult> RunAsync(
        CliApplication app,
        CancellationToken cancellationToken,
        params string[] args)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = await app.RunAsync(args, output, error, cancellationToken).ConfigureAwait(false);

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
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

    private static string[] Lines(string text)
    {
        return text
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private sealed record CliRunResult(int ExitCode, string Stdout, string Stderr);
}
