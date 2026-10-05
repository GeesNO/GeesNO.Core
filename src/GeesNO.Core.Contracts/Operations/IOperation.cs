using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Contracts.Operations;

/// <summary>
/// Exposes the identity and exact capability of an operation.
/// </summary>
public interface IOperation
{
    /// <summary>Gets the operation identifier.</summary>
    OperationId Id { get; }

    /// <summary>Gets the exact capability requested by the operation.</summary>
    CapabilityReference Capability { get; }
}

/// <summary>
/// Associates an operation with its non-null result type.
/// </summary>
/// <typeparam name="TResult">The operation result type.</typeparam>
public interface IOperation<TResult> : IOperation
    where TResult : notnull
{
}
