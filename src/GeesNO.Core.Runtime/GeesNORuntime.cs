using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Provides the minimal public Runtime facade for registering and executing typed operations.
/// </summary>
public sealed class GeesNORuntime : IDisposable
{
    private readonly IReadOnlyList<RuntimeCapability> _capabilities;
    private readonly ToolEngine _engine;
    private int _stopped;

    internal GeesNORuntime(RuntimeCatalogSnapshot snapshot, ToolEngine engine)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(engine);

        _capabilities = CreateCapabilities(snapshot);
        _engine = engine;
    }

    /// <summary>
    /// Gets a deterministic immutable view of the capabilities registered in this Runtime.
    /// </summary>
    public IReadOnlyList<RuntimeCapability> Capabilities => _capabilities;

    /// <summary>
    /// Executes a typed operation through the Runtime facade.
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="options">The optional execution options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The normalized public execution result.</returns>
    public async Task<OperationExecutionResult<TResult>> ExecuteAsync<TResult>(
        IOperation<TResult> operation,
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(operation);

        var request = new RuntimeExecutionRequest(operation, options?.ProviderId);
        var result = await _engine.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return OperationExecutionResult<TResult>.FromRuntimeResult(result);
    }

    /// <summary>
    /// Stops the Runtime. This operation is idempotent and terminal.
    /// </summary>
    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) == 0)
        {
            _engine.Stop();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    private static IReadOnlyList<RuntimeCapability> CreateCapabilities(RuntimeCatalogSnapshot snapshot)
    {
        var capabilities = snapshot.Manifests
            .OrderBy(manifest => manifest.ProviderId.Value, StringComparer.Ordinal)
            .SelectMany(manifest => manifest.Capabilities
                .OrderBy(entry => entry.Descriptor.Reference.Id.Value, StringComparer.Ordinal)
                .ThenBy(entry => entry.Descriptor.Reference.Version)
                .Select(entry => new RuntimeCapability(
                    manifest.ProviderId,
                    entry.Descriptor,
                    entry.Support)))
            .ToArray();

        return Array.AsReadOnly(capabilities);
    }
}