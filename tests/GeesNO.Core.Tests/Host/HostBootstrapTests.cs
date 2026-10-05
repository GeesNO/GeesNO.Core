using GeesNO.Core.Host;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Tests.Host;

public sealed class HostBootstrapTests
{
    [Fact]
    public async Task Bootstrap_composes_runtime_executes_ping_observes_and_stops()
    {
        var output = new StringWriter();
        var bootstrapper = new RuntimeHostBootstrapper();

        var result = await bootstrapper.RunAsync(
            new HostRuntimeOptions(),
            output,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.PingExecuted);
        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal("GeesNO.Core.Runtime", result.RuntimeName);
        Assert.Equal(new VersionInfo(0, 1, 0), result.RuntimeVersion);
        Assert.True(result.ObservedEvents > 0);
        Assert.Equal(1, result.JournalEntries);
        Assert.Equal(ToolEngineState.Stopped, result.FinalEngineState);
        Assert.Equal(1, result.StopCalls);
    }

    [Fact]
    public async Task Bootstrap_output_is_minimal_and_sanitized()
    {
        var output = new StringWriter();

        await new RuntimeHostBootstrapper().RunAsync(
            new HostRuntimeOptions(),
            output,
            CancellationToken.None);

        var text = output.ToString();
        Assert.Contains("GeesNO.Core.Host starting", text, StringComparison.Ordinal);
        Assert.Contains("Runtime started", text, StringComparison.Ordinal);
        Assert.Contains("system.ping succeeded", text, StringComparison.Ordinal);
        Assert.Contains("RuntimeName: GeesNO.Core.Runtime", text, StringComparison.Ordinal);
        Assert.Contains("RuntimeVersion: 0.1.0", text, StringComparison.Ordinal);
        Assert.Contains("ObservedEvents:", text, StringComparison.Ordinal);
        Assert.Contains("JournalEntries: 1", text, StringComparison.Ordinal);
        Assert.Contains("GeesNO.Core.Host stopped", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemPingOperation", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimeExecutionResult", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("{", text, StringComparison.Ordinal);
    }
}
