using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GeesNO.Core.Host;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddGeesNOCoreHost(
        this IServiceCollection services,
        IConfiguration configuration,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(output);

        services.AddSingleton(CreateOptions(configuration));
        services.AddSingleton(output);
        services.AddSingleton<RuntimeHostBootstrapper>();
        services.AddHostedService<RuntimeHostedService>();

        return services;
    }

    private static HostRuntimeOptions CreateOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("GeesNO:Runtime");
        var runtimeName = section["RuntimeName"];
        var runtimeVersion = section["RuntimeVersion"];
        var runPingOnStartup = HostRuntimeOptions.DefaultRunPingOnStartup;

        if (bool.TryParse(section["RunPingOnStartup"], out var configuredRunPingOnStartup))
        {
            runPingOnStartup = configuredRunPingOnStartup;
        }

        return new HostRuntimeOptions
        {
            RuntimeName = string.IsNullOrWhiteSpace(runtimeName)
                ? HostRuntimeOptions.DefaultRuntimeName
                : runtimeName,
            RuntimeVersion = string.IsNullOrWhiteSpace(runtimeVersion)
                ? HostRuntimeOptions.DefaultRuntimeVersion
                : runtimeVersion,
            RunPingOnStartup = runPingOnStartup
        }.Normalize();
    }
}
