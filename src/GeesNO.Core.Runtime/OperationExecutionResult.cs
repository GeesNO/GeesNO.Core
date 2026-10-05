using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Represents the normalized public result of a typed Runtime operation execution.
/// </summary>
/// <typeparam name="TResult">The public result type.</typeparam>
public sealed class OperationExecutionResult<TResult>
    where TResult : notnull
{
    private OperationExecutionResult(
        OperationExecutionStatus status,
        OperationId operationId,
        CapabilityReference capability,
        ProviderId? providerId,
        Type? declaredResultType,
        TResult? value,
        Error? error)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(capability);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The execution status is not defined.");
        }

        ValidateState(status, providerId, declaredResultType, value, error);

        Status = status;
        OperationId = operationId;
        Capability = capability;
        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets the public normalized execution status.
    /// </summary>
    public OperationExecutionStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether the execution succeeded.
    /// </summary>
    public bool IsSuccess => Status == OperationExecutionStatus.Success;

    /// <summary>
    /// Gets the executed operation identifier.
    /// </summary>
    public OperationId OperationId { get; }

    /// <summary>
    /// Gets the capability requested by the operation.
    /// </summary>
    public CapabilityReference Capability { get; }

    /// <summary>
    /// Gets the provider that executed or rejected the operation, when resolved.
    /// </summary>
    public ProviderId? ProviderId { get; }

    /// <summary>
    /// Gets the result type declared by the resolved tool, when available.
    /// </summary>
    public Type? DeclaredResultType { get; }

    /// <summary>
    /// Gets the typed successful value, when execution succeeded.
    /// </summary>
    public TResult? Value { get; }

    /// <summary>
    /// Gets the public error, when execution did not succeed and an error applies.
    /// </summary>
    public Error? Error { get; }

    internal static OperationExecutionResult<TResult> FromRuntimeResult(RuntimeExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            RuntimeExecutionStatus.Success => CreateSuccess(result),
            RuntimeExecutionStatus.Rejected => CreateFailure(OperationExecutionStatus.Rejected, result),
            RuntimeExecutionStatus.FunctionalFailure => CreateFailure(OperationExecutionStatus.FunctionalFailure, result),
            RuntimeExecutionStatus.ContractMismatch => CreateFailure(OperationExecutionStatus.ContractMismatch, result),
            RuntimeExecutionStatus.UnexpectedFailure => CreateFailure(OperationExecutionStatus.UnexpectedFailure, result),
            RuntimeExecutionStatus.Cancelled => new OperationExecutionResult<TResult>(
                OperationExecutionStatus.Cancelled,
                result.OperationId,
                result.Capability,
                result.ProviderId,
                result.DeclaredResultType,
                value: default,
                error: null),
            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                result.Status,
                "The internal execution status is not defined.")
        };
    }

    private static OperationExecutionResult<TResult> CreateSuccess(RuntimeExecutionResult result)
    {
        if (result.SuccessfulValue is TResult value)
        {
            return new OperationExecutionResult<TResult>(
                OperationExecutionStatus.Success,
                result.OperationId,
                result.Capability,
                result.ProviderId,
                result.DeclaredResultType,
                value,
                error: null);
        }

        return new OperationExecutionResult<TResult>(
            OperationExecutionStatus.ContractMismatch,
            result.OperationId,
            result.Capability,
            result.ProviderId,
            result.DeclaredResultType,
            value: default,
            RuntimeInvocationErrors.ToolContractMismatch());
    }

    private static OperationExecutionResult<TResult> CreateFailure(
        OperationExecutionStatus status,
        RuntimeExecutionResult result)
    {
        return new OperationExecutionResult<TResult>(
            status,
            result.OperationId,
            result.Capability,
            result.ProviderId,
            result.DeclaredResultType,
            value: default,
            result.Error ?? RuntimeInvocationErrors.UnexpectedToolFailure());
    }

    private static void ValidateState(
        OperationExecutionStatus status,
        ProviderId? providerId,
        Type? declaredResultType,
        TResult? value,
        Error? error)
    {
        switch (status)
        {
            case OperationExecutionStatus.Success:
                ArgumentNullException.ThrowIfNull(providerId);
                ArgumentNullException.ThrowIfNull(declaredResultType);
                ArgumentNullException.ThrowIfNull(value);
                if (error is not null)
                {
                    throw new ArgumentException("The successful execution state is invalid.");
                }

                break;

            case OperationExecutionStatus.Cancelled:
                if (value is not null || error is not null)
                {
                    throw new ArgumentException("The cancelled execution state is invalid.");
                }

                break;

            default:
                ArgumentNullException.ThrowIfNull(error);
                if (value is not null)
                {
                    throw new ArgumentException($"The {status} execution state is invalid.");
                }

                break;
        }
    }
}