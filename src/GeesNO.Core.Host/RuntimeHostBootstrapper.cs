using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;
using GeesNO.Core.Runtime.System;

namespace GeesNO.Core.Host;

internal sealed class RuntimeHostBootstrapper
{
    internal delegate void RuntimeCatalogConfigurator(
        RuntimeCatalogBuilder builder,
        ProviderId providerId,
        VersionInfo providerVersion,
        HostRuntimeOptions options);

    private ToolEngine? _engine;
    private int _stopCalls;
    private int _stopped;

    internal ToolEngineState? CurrentEngineState => _engine?.State;

    internal int StopCalls => Volatile.Read(ref _stopCalls);

    internal async Task<HostBootstrapResult> RunAsync(
        HostRuntimeOptions options,
        TextWriter output,
        CancellationToken cancellationToken,
        RuntimeCatalogConfigurator? configureCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        var effectiveOptions = options.Normalize();
        var observedEvents = 0;
        var journalEntries = 0;
        RuntimeExecutionStatus? status = null;
        string? errorCode = null;
        string? runtimeName = null;
        VersionInfo? runtimeVersion = null;
        var pingExecuted = false;
        var exitCode = 1;
        InMemoryExecutionJournal? journal = null;
        HostConsoleEventObserver? observer = null;

        await output.WriteLineAsync("GeesNO.Core.Host starting").ConfigureAwait(false);

        try
        {
            runtimeVersion = effectiveOptions.ParseRuntimeVersion();

            var providerId = ProviderId.Parse("geesno.core-host");
            var providerVersion = runtimeVersion;
            var builder = new RuntimeCatalogBuilder();
            (configureCatalog ?? ConfigureDefaultCatalog)(builder, providerId, providerVersion, effectiveOptions);
            var snapshot = builder.Seal();

            var diagnostics = new RuntimeObservabilityDiagnostics();
            journal = new InMemoryExecutionJournal();
            observer = new HostConsoleEventObserver();
            var eventBus = new InProcessEventBus([observer], diagnostics);
            var observability = new RuntimeObservability(
                TimeProvider.System,
                eventBus,
                journal,
                diagnostics);

            _engine = new ToolEngine(snapshot, observability);
            _engine.Start();
            await output.WriteLineAsync("Runtime started").ConfigureAwait(false);

            if (!effectiveOptions.RunPingOnStartup)
            {
                exitCode = 0;
            }
            else
            {
                pingExecuted = true;
                var operation = new SystemPingOperation(OperationId.New());
                var result = await _engine.ExecuteAsync(
                    new RuntimeExecutionRequest(operation),
                    cancellationToken).ConfigureAwait(false);

                status = result.Status;
                if (result.Status == RuntimeExecutionStatus.Success
                    && result.SuccessfulValue is SystemPingResult pingResult)
                {
                    runtimeName = pingResult.RuntimeName;
                    runtimeVersion = pingResult.RuntimeVersion;
                    exitCode = 0;
                    await output.WriteLineAsync("system.ping succeeded").ConfigureAwait(false);
                    await output.WriteLineAsync($"RuntimeName: {runtimeName}").ConfigureAwait(false);
                    await output.WriteLineAsync($"RuntimeVersion: {runtimeVersion}").ConfigureAwait(false);
                }
                else
                {
                    errorCode = result.Error?.Code
                        ?? (result.Status == RuntimeExecutionStatus.Success
                            ? "host.unexpected-ping-result"
                            : null);
                    await output.WriteLineAsync("system.ping failed").ConfigureAwait(false);
                    await output.WriteLineAsync($"Status: {result.Status}").ConfigureAwait(false);
                    if (errorCode is not null)
                    {
                        await output.WriteLineAsync($"Error: {errorCode}").ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            status = RuntimeExecutionStatus.Cancelled;
            await output.WriteLineAsync("system.ping failed").ConfigureAwait(false);
            await output.WriteLineAsync("Status: Cancelled").ConfigureAwait(false);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            errorCode = exception is FormatException
                ? "host.invalid-runtime-version"
                : "host.bootstrap-failed";
            await output.WriteLineAsync("GeesNO.Core.Host failed").ConfigureAwait(false);
            await output.WriteLineAsync($"Error: {errorCode}").ConfigureAwait(false);
        }
        finally
        {
            Stop();
            observedEvents = observer?.ObservedEvents ?? 0;
            journalEntries = journal?.GetEntries().Count ?? 0;
            await output.WriteLineAsync($"ObservedEvents: {observedEvents}").ConfigureAwait(false);
            await output.WriteLineAsync($"JournalEntries: {journalEntries}").ConfigureAwait(false);
            await output.WriteLineAsync("GeesNO.Core.Host stopped").ConfigureAwait(false);
        }

        return new HostBootstrapResult(
            exitCode,
            pingExecuted,
            status,
            errorCode,
            runtimeName,
            runtimeVersion,
            observedEvents,
            journalEntries,
            CurrentEngineState,
            StopCalls);
    }

    internal void Stop()
    {
        var engine = _engine;
        if (engine is null)
        {
            return;
        }

        if (Interlocked.Exchange(ref _stopped, 1) == 1)
        {
            return;
        }

        engine.Stop();
        Interlocked.Increment(ref _stopCalls);
    }

    private static void ConfigureDefaultCatalog(
        RuntimeCatalogBuilder builder,
        ProviderId providerId,
        VersionInfo providerVersion,
        HostRuntimeOptions options)
    {
        var effectiveOptions = options.Normalize();
        var runtimeVersion = effectiveOptions.ParseRuntimeVersion();

        builder.RegisterManifest(SystemPingManifestFactory.Create(providerId, providerVersion));
        builder.RegisterTool(providerId, new SystemPingTool(effectiveOptions.RuntimeName, runtimeVersion));
    }

    private HostBootstrapResult CreateResult(
        int exitCode,
        bool pingExecuted,
        RuntimeExecutionStatus? status,
        string? errorCode,
        string? runtimeName,
        VersionInfo? runtimeVersion,
        HostConsoleEventObserver? observer,
        InMemoryExecutionJournal? journal)
    {
        return new HostBootstrapResult(
            exitCode,
            pingExecuted,
            status,
            errorCode,
            runtimeName,
            runtimeVersion,
            observer?.ObservedEvents ?? 0,
            journal?.GetEntries().Count ?? 0,
            CurrentEngineState,
            StopCalls);
    }
}
