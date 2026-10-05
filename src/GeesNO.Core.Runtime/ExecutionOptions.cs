using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Provides the minimal public options supported by Runtime execution.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionOptions"/> class.
    /// </summary>
    /// <param name="providerId">The optional exact provider to use for execution.</param>
    public ExecutionOptions(ProviderId? providerId = null)
    {
        ProviderId = providerId;
    }

    /// <summary>
    /// Gets the optional exact provider to use for execution.
    /// </summary>
    public ProviderId? ProviderId { get; }
}