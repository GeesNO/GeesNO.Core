using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime.Invokers;

internal sealed class InternalInvocationResult
{
    private InternalInvocationResult(
        InternalInvocationStatus status,
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        object? successfulValue,
        Error? error,
        Exception? internalException)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(declaredResultType);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The invocation status is not defined.");
        }

        ValidateState(status, declaredResultType, successfulValue, error, internalException);

        Status = status;
        OperationId = operationId;
        ProviderId = providerId;
        Capability = capability;
        DeclaredResultType = declaredResultType;
        SuccessfulValue = successfulValue;
        Error = error;
        InternalException = internalException;
    }

    internal InternalInvocationStatus Status { get; }

    internal OperationId OperationId { get; }

    internal ProviderId ProviderId { get; }

    internal CapabilityReference Capability { get; }

    internal Type DeclaredResultType { get; }

    internal object? SuccessfulValue { get; }

    internal Error? Error { get; }

    internal Exception? InternalException { get; }

    internal static InternalInvocationResult Success(
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        object successfulValue)
    {
        return new InternalInvocationResult(
            InternalInvocationStatus.Success,
            operationId,
            providerId,
            capability,
            declaredResultType,
            successfulValue,
            error: null,
            internalException: null);
    }

    internal static InternalInvocationResult FunctionalFailure(
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        Error error)
    {
        return new InternalInvocationResult(
            InternalInvocationStatus.FunctionalFailure,
            operationId,
            providerId,
            capability,
            declaredResultType,
            successfulValue: null,
            error,
            internalException: null);
    }

    internal static InternalInvocationResult ContractMismatch(
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        Error error)
    {
        return new InternalInvocationResult(
            InternalInvocationStatus.ContractMismatch,
            operationId,
            providerId,
            capability,
            declaredResultType,
            successfulValue: null,
            error,
            internalException: null);
    }

    internal static InternalInvocationResult UnexpectedFailure(
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        Exception internalException)
    {
        return new InternalInvocationResult(
            InternalInvocationStatus.UnexpectedFailure,
            operationId,
            providerId,
            capability,
            declaredResultType,
            successfulValue: null,
            RuntimeInvocationErrors.UnexpectedToolFailure(),
            internalException);
    }

    internal static InternalInvocationResult Cancelled(
        OperationId operationId,
        ProviderId providerId,
        CapabilityReference capability,
        Type declaredResultType,
        OperationCanceledException? internalException = null)
    {
        return new InternalInvocationResult(
            InternalInvocationStatus.Cancelled,
            operationId,
            providerId,
            capability,
            declaredResultType,
            successfulValue: null,
            error: null,
            internalException);
    }

    private static void ValidateState(
        InternalInvocationStatus status,
        Type declaredResultType,
        object? successfulValue,
        Error? error,
        Exception? internalException)
    {
        switch (status)
        {
            case InternalInvocationStatus.Success:
                ArgumentNullException.ThrowIfNull(successfulValue);
                if (!declaredResultType.IsInstanceOfType(successfulValue))
                {
                    throw new ArgumentException(
                        "The successful value is incompatible with the declared result type.",
                        nameof(successfulValue));
                }

                if (error is not null || internalException is not null)
                {
                    throw new ArgumentException("A successful invocation cannot contain an error or exception.");
                }

                break;

            case InternalInvocationStatus.FunctionalFailure:
                ArgumentNullException.ThrowIfNull(error);
                EnsureNoValueOrException(successfulValue, internalException, status);
                break;

            case InternalInvocationStatus.ContractMismatch:
                ArgumentNullException.ThrowIfNull(error);
                if (error.Code is not RuntimeInvocationErrors.OperationContractMismatchCode
                    and not RuntimeInvocationErrors.ToolContractMismatchCode)
                {
                    throw new ArgumentException("The contract mismatch error code is invalid.", nameof(error));
                }

                EnsureNoValueOrException(successfulValue, internalException, status);
                break;

            case InternalInvocationStatus.UnexpectedFailure:
                ArgumentNullException.ThrowIfNull(error);
                ArgumentNullException.ThrowIfNull(internalException);
                if (error.Code != RuntimeInvocationErrors.UnexpectedToolFailureCode || successfulValue is not null)
                {
                    throw new ArgumentException("The unexpected failure state is invalid.");
                }

                break;

            case InternalInvocationStatus.Cancelled:
                if (successfulValue is not null || error is not null
                    || internalException is not null and not OperationCanceledException)
                {
                    throw new ArgumentException("The cancelled invocation state is invalid.");
                }

                break;
        }
    }

    private static void EnsureNoValueOrException(
        object? successfulValue,
        Exception? internalException,
        InternalInvocationStatus status)
    {
        if (successfulValue is not null || internalException is not null)
        {
            throw new ArgumentException($"The {status} invocation state is invalid.");
        }
    }
}
