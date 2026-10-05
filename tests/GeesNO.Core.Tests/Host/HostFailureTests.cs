using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Host;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.System;

namespace GeesNO.Core.Tests.Host;

public sealed class HostFailureTests
{
    [Fact]
    public async Task Functional_ping_failure_returns_non_zero_stops_and_does_not_retry()
    {
        var output = new StringWriter();
        var tool = new FailingPingTool();

        var result = await new RuntimeHostBootstrapper().RunAsync(
            new HostRuntimeOptions(),
            output,
            CancellationToken.None,
            (builder, providerId, providerVersion, _) => ConfigureFailingPing(builder, providerId, providerVersion, tool));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(RuntimeExecutionStatus.FunctionalFailure, result.Status);
        Assert.Equal("host.test-ping-failure", result.ErrorCode);
        Assert.Equal(1, tool.InvocationCount);
        Assert.Equal(ToolEngineState.Stopped, result.FinalEngineState);
        Assert.Equal(1, result.StopCalls);

        var text = output.ToString();
        Assert.Contains("system.ping failed", text, StringComparison.Ordinal);
        Assert.Contains("Status: FunctionalFailure", text, StringComparison.Ordinal);
        Assert.Contains("Error: host.test-ping-failure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_runtime_version_fails_without_creating_engine_or_printing_exception()
    {
        var output = new StringWriter();

        var result = await new RuntimeHostBootstrapper().RunAsync(
            new HostRuntimeOptions { RuntimeVersion = "invalid" },
            output,
            CancellationToken.None);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("host.invalid-runtime-version", result.ErrorCode);
        Assert.Null(result.FinalEngineState);
        Assert.Equal(0, result.StopCalls);
        Assert.DoesNotContain("Exception", output.ToString(), StringComparison.Ordinal);
    }

    private static void ConfigureFailingPing(
        RuntimeCatalogBuilder builder,
        ProviderId providerId,
        VersionInfo providerVersion,
        FailingPingTool tool)
    {
        builder.RegisterManifest(SystemPingManifestFactory.Create(providerId, providerVersion));
        builder.RegisterTool(providerId, tool);
    }

    private sealed class FailingPingTool : ITool<SystemPingOperation, SystemPingResult>
    {
        private int _invocationCount;

        public CapabilityReference Capability => SystemPingOperation.ContractCapability;

        internal int InvocationCount => Volatile.Read(ref _invocationCount);

        public Task<Result<SystemPingResult>> ExecuteAsync(
            SystemPingOperation operation,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _invocationCount);
            return Task.FromResult(Result<SystemPingResult>.Failure(new Error(
                "host.test-ping-failure",
                "The host test forced a ping failure.")));
        }
    }
}
