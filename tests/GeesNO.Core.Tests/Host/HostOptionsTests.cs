using GeesNO.Core.Host;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Host;

public sealed class HostOptionsTests
{
    [Fact]
    public void Defaults_are_safe_and_minimal()
    {
        var options = new HostRuntimeOptions().Normalize();

        Assert.Equal("GeesNO.Core.Runtime", options.RuntimeName);
        Assert.Equal("0.1.0", options.RuntimeVersion);
        Assert.True(options.RunPingOnStartup);
        Assert.Equal(new VersionInfo(0, 1, 0), options.ParseRuntimeVersion());
    }

    [Fact]
    public void Empty_runtime_name_is_replaced_by_default()
    {
        var options = new HostRuntimeOptions
        {
            RuntimeName = "   ",
            RuntimeVersion = "1.2.3"
        }.Normalize();

        Assert.Equal("GeesNO.Core.Runtime", options.RuntimeName);
        Assert.Equal(new VersionInfo(1, 2, 3), options.ParseRuntimeVersion());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("1.0.x")]
    [InlineData("1.0.0.0")]
    public void Invalid_runtime_version_fails_controlled_parsing(string version)
    {
        var options = new HostRuntimeOptions { RuntimeVersion = version };

        Assert.Throws<FormatException>(() => options.ParseRuntimeVersion());
    }

    [Fact]
    public async Task RunPingOnStartup_false_skips_ping_and_still_stops_cleanly()
    {
        var output = new StringWriter();
        var bootstrapper = new RuntimeHostBootstrapper();

        var result = await bootstrapper.RunAsync(
            new HostRuntimeOptions { RunPingOnStartup = false },
            output,
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.False(result.PingExecuted);
        Assert.Null(result.Status);
        Assert.Equal(0, result.ObservedEvents);
        Assert.Equal(0, result.JournalEntries);
        Assert.Equal(GeesNO.Core.Runtime.Execution.ToolEngineState.Stopped, result.FinalEngineState);
        Assert.DoesNotContain("system.ping succeeded", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Options_surface_contains_no_external_domains_or_persistence()
    {
        var propertyNames = typeof(HostRuntimeOptions)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["RuntimeName", "RuntimeVersion", "RunPingOnStartup"], propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("CAD", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("LLM", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("MCP", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Plugin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Transport", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Journal", StringComparison.OrdinalIgnoreCase));
    }
}
