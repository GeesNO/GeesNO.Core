using Microsoft.Extensions.Hosting;

namespace GeesNO.Core.Host;

internal sealed class RuntimeHostedService : IHostedService
{
    private static int _exitCode;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly RuntimeHostBootstrapper _bootstrapper;
    private readonly HostRuntimeOptions _options;
    private readonly TextWriter _output;

    public RuntimeHostedService(
        IHostApplicationLifetime applicationLifetime,
        RuntimeHostBootstrapper bootstrapper,
        HostRuntimeOptions options,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        ArgumentNullException.ThrowIfNull(bootstrapper);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        _applicationLifetime = applicationLifetime;
        _bootstrapper = bootstrapper;
        _options = options;
        _output = output;
    }

    internal static int ExitCode => Volatile.Read(ref _exitCode);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await _bootstrapper
            .RunAsync(_options, _output, cancellationToken)
            .ConfigureAwait(false);

        Volatile.Write(ref _exitCode, result.ExitCode);
        _applicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _bootstrapper.Stop();
        return Task.CompletedTask;
    }
}
