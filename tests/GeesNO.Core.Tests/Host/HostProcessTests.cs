using System.Diagnostics;

namespace GeesNO.Core.Tests.Host;

public sealed class HostProcessTests
{
    [Fact]
    public async Task Dotnet_run_host_finishes_successfully_with_minimal_output()
    {
        var root = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
            ?? throw new InvalidOperationException("Could not determine the test build configuration.");
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(configuration);
        process.StartInfo.ArgumentList.Add("--no-build");
        process.StartInfo.ArgumentList.Add("--project");
        process.StartInfo.ArgumentList.Add(Path.Combine("src", "GeesNO.Core.Host", "GeesNO.Core.Host.csproj"));

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("GeesNO.Core.Host starting", stdout, StringComparison.Ordinal);
        Assert.Contains("system.ping succeeded", stdout, StringComparison.Ordinal);
        Assert.Contains("JournalEntries: 1", stdout, StringComparison.Ordinal);
        Assert.Contains("GeesNO.Core.Host stopped", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", stdout, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(stderr), stderr);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GeesNO.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
