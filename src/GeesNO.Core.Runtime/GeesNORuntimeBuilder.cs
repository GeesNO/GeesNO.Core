using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Builds a public Runtime facade from public manifests and tools.
/// </summary>
public sealed class GeesNORuntimeBuilder
{
    private const string AlreadyBuiltMessage = "The Runtime builder has already created a Runtime.";

    private readonly RuntimeCatalogBuilder _catalogBuilder = new();
    private int _buildStarted;

    /// <summary>
    /// Registers a provider capability manifest.
    /// </summary>
    /// <param name="manifest">The manifest to register.</param>
    /// <returns>The current builder instance.</returns>
    public GeesNORuntimeBuilder RegisterManifest(CapabilityManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        EnsureNotBuilt();

        try
        {
            _catalogBuilder.RegisterManifest(manifest);
        }
        catch (InvalidOperationException exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            throw new GeesNORuntimeConfigurationException(exception.Message);
        }

        return this;
    }

    /// <summary>
    /// Registers an executable tool for an already registered provider.
    /// </summary>
    /// <param name="providerId">The exact provider identifier.</param>
    /// <param name="tool">The tool to register.</param>
    /// <returns>The current builder instance.</returns>
    public GeesNORuntimeBuilder RegisterTool(ProviderId providerId, ITool tool)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(tool);
        EnsureNotBuilt();

        try
        {
            _catalogBuilder.RegisterTool(providerId, tool);
        }
        catch (InvalidOperationException exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            throw new GeesNORuntimeConfigurationException(exception.Message);
        }

        return this;
    }

    /// <summary>
    /// Creates a started Runtime facade and seals the underlying catalog.
    /// </summary>
    /// <returns>A Runtime ready to execute operations.</returns>
    public GeesNORuntime Build()
    {
        if (Interlocked.Exchange(ref _buildStarted, 1) != 0)
        {
            throw new GeesNORuntimeConfigurationException(AlreadyBuiltMessage);
        }

        try
        {
            var snapshot = _catalogBuilder.Seal();
            var engine = new ToolEngine(snapshot);
            engine.Start();

            return new GeesNORuntime(snapshot, engine);
        }
        catch (RuntimeCatalogValidationException exception)
        {
            throw GeesNORuntimeConfigurationException.FromValidationIssues(exception.Issues);
        }
        catch (InvalidOperationException exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            throw new GeesNORuntimeConfigurationException(exception.Message);
        }
    }

    private void EnsureNotBuilt()
    {
        if (Volatile.Read(ref _buildStarted) != 0)
        {
            throw new GeesNORuntimeConfigurationException(AlreadyBuiltMessage);
        }
    }
}