using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Execution;

internal sealed class RuntimeExecutionResult
{
    private RuntimeExecutionResult(
        RuntimeExecutionStatus status,
        OperationId operationId,
        CapabilityReference capability,
        ProviderId? providerId,
        Type? declaredResultType,
        object? successfulValue,
        Error? error,
        Exception? internalException)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(capability);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The execution status is not defined.");
        }

        ValidateState(
            status,
            providerId,
            declaredResultType,
            successfulValue,
            error,
            internalException);

        Status = status;
        OperationId = operationId;
        Capability = capability;
        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
        SuccessfulValue = successfulValue;
        Error = error;
        InternalException = internalException;
    }

    internal RuntimeExecutionStatus Status { get; }

    internal OperationId OperationId { get; }

    internal CapabilityReference Capability { get; }

    internal ProviderId? ProviderId { get; }

    internal Type? DeclaredResultType { get; }

    internal object? SuccessfulValue { get; }

    internal Error? Error { get; }

    internal Exception? InternalException { get; }

    internal static RuntimeExecutionResult Success(
        OperationId operationId,
        CapabilityReference capability,
        ProviderId providerId,
        Type declaredResultType,
        object successfulValue)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.Success,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue,
            error: null,
            internalException: null);
    }

    internal static RuntimeExecutionResult Rejected(
        OperationId operationId,
        CapabilityReference capability,
        Error error,
        ProviderId? providerId = null,
        Type? declaredResultType = null)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.Rejected,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue: null,
            error,
            internalException: null);
    }

    internal static RuntimeExecutionResult FunctionalFailure(
        OperationId operationId,
        CapabilityReference capability,
        ProviderId providerId,
        Type declaredResultType,
        Error error)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.FunctionalFailure,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue: null,
            error,
            internalException: null);
    }

    internal static RuntimeExecutionResult ContractMismatch(
        OperationId operationId,
        CapabilityReference capability,
        Error error,
        ProviderId? providerId = null,
        Type? declaredResultType = null)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.ContractMismatch,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue: null,
            error,
            internalException: null);
    }

    internal static RuntimeExecutionResult UnexpectedFailure(
        OperationId operationId,
        CapabilityReference capability,
        ProviderId providerId,
        Type declaredResultType,
        Exception internalException)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.UnexpectedFailure,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue: null,
            RuntimeInvocationErrors.UnexpectedToolFailure(),
            internalException);
    }

    internal static RuntimeExecutionResult Cancelled(
        OperationId operationId,
        CapabilityReference capability,
        ProviderId? providerId = null,
        Type? declaredResultType = null,
        OperationCanceledException? internalException = null)
    {
        return new RuntimeExecutionResult(
            RuntimeExecutionStatus.Cancelled,
            operationId,
            capability,
            providerId,
            declaredResultType,
            successfulValue: null,
            error: null,
            internalException);
    }

    internal static RuntimeExecutionResult FromInvocation(InternalInvocationResult invocationResult)
    {
        ArgumentNullException.ThrowIfNull(invocationResult);

        return invocationResult.Status switch
        {
            InternalInvocationStatus.Success => Success(
                invocationResult.OperationId,
                invocationResult.Capability,
                invocationResult.ProviderId,
                invocationResult.DeclaredResultType,
                invocationResult.SuccessfulValue!),
            InternalInvocationStatus.FunctionalFailure => FunctionalFailure(
                invocationResult.OperationId,
                invocationResult.Capability,
                invocationResult.ProviderId,
                invocationResult.DeclaredResultType,
                invocationResult.Error!),
            InternalInvocationStatus.ContractMismatch => ContractMismatch(
                invocationResult.OperationId,
                invocationResult.Capability,
                invocationResult.Error!,
                invocationResult.ProviderId,
                invocationResult.DeclaredResultType),
            InternalInvocationStatus.UnexpectedFailure => new RuntimeExecutionResult(
                RuntimeExecutionStatus.UnexpectedFailure,
                invocationResult.OperationId,
                invocationResult.Capability,
                invocationResult.ProviderId,
                invocationResult.DeclaredResultType,
                successfulValue: null,
                invocationResult.Error,
                invocationResult.InternalException),
            InternalInvocationStatus.Cancelled => Cancelled(
                invocationResult.OperationId,
                invocationResult.Capability,
                invocationResult.ProviderId,
                invocationResult.DeclaredResultType,
                invocationResult.InternalException as OperationCanceledException),
            _ => throw new ArgumentOutOfRangeException(
                nameof(invocationResult),
                invocationResult.Status,
                "The invocation status is not defined.")
        };
    }

    private static void ValidateState(
        RuntimeExecutionStatus status,
        ProviderId? providerId,
        Type? declaredResultType,
        object? successfulValue,
        Error? error,
        Exception? internalException)
    {
        switch (status)
        {
            case RuntimeExecutionStatus.Success:
                EnsureResolved(providerId, declaredResultType);
                ArgumentNullException.ThrowIfNull(successfulValue);
                if (!declaredResultType!.IsInstanceOfType(successfulValue)
                    || error is not null
                    || internalException is not null)
                {
                    throw new ArgumentException("The successful execution state is invalid.");
                }

                break;

            case RuntimeExecutionStatus.Rejected:
                ArgumentNullException.ThrowIfNull(error);
                if (!RuntimeExecutionErrors.IsRejectedCode(error.Code)
                    || successfulValue is not null
                    || internalException is not null)
                {
                    throw new ArgumentException("The rejected execution state is invalid.");
                }

                break;

            case RuntimeExecutionStatus.FunctionalFailure:
                EnsureResolved(providerId, declaredResultType);
                ArgumentNullException.ThrowIfNull(error);
                EnsureNoValueOrException(successfulValue, internalException, status);
                break;

            case RuntimeExecutionStatus.ContractMismatch:
                ArgumentNullException.ThrowIfNull(error);
                if (error.Code is not RuntimeInvocationErrors.OperationContractMismatchCode
                    and not RuntimeInvocationErrors.ToolContractMismatchCode)
                {
                    throw new ArgumentException("The contract mismatch error code is invalid.", nameof(error));
                }

                EnsureNoValueOrException(successfulValue, internalException, status);
                break;

            case RuntimeExecutionStatus.UnexpectedFailure:
                EnsureResolved(providerId, declaredResultType);
                ArgumentNullException.ThrowIfNull(error);
                ArgumentNullException.ThrowIfNull(internalException);
                if (error.Code != RuntimeInvocationErrors.UnexpectedToolFailureCode
                    || successfulValue is not null)
                {
                    throw new ArgumentException("The unexpected execution state is invalid.");
                }

                break;

            case RuntimeExecutionStatus.Cancelled:
                if (successfulValue is not null
                    || error is not null
                    || internalException is not null and not OperationCanceledException)
                {
                    throw new ArgumentException("The cancelled execution state is invalid.");
                }

                break;
        }
    }

    private static void EnsureResolved(ProviderId? providerId, Type? declaredResultType)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(declaredResultType);
    }

    private static void EnsureNoValueOrException(
        object? successfulValue,
        Exception? internalException,
        RuntimeExecutionStatus status)
    {
        if (successfulValue is not null || internalException is not null)
        {
            throw new ArgumentException($"The {status} execution state is invalid.");
        }
    }
}
