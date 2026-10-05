using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.Tools;

/// <summary>
/// Exposes the exact capability implemented by a tool.
/// </summary>
public interface ITool
{
    /// <summary>Gets the exact capability implemented by the tool.</summary>
    CapabilityReference Capability { get; }
}

/// <summary>
/// Executes one exact operation type and returns its associated non-null result type.
/// </summary>
/// <typeparam name="TOperation">The exact operation type.</typeparam>
/// <typeparam name="TResult">The operation result type.</typeparam>
public interface ITool<TOperation, TResult> : ITool
    where TOperation : IOperation<TResult>
    where TResult : notnull
{
    /// <summary>
    /// Executes an operation asynchronously.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A required cooperative cancellation token.</param>
    /// <returns>A task containing the functional execution result.</returns>
    Task<Result<TResult>> ExecuteAsync(
        TOperation operation,
        CancellationToken cancellationToken);
}
