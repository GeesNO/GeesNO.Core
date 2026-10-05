using GeesNO.Core.Runtime.Invokers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeesNO.Core.Host;

/// <summary>
/// Entry point for the GeesNO.Core executable host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the Host composition root and returns the process exit code.
    /// </summary>
    /// <param name="args">Command-line arguments handled by Generic Host.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddGeesNOCoreHost(builder.Configuration, Console.Out);

            using var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
            return RuntimeHostedService.ExitCode;
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            Console.Out.WriteLine("GeesNO.Core.Host failed");
            Console.Out.WriteLine("Error: host.unhandled-failure");
            return 1;
        }
    }
}
