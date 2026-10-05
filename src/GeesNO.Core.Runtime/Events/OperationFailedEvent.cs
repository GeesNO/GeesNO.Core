using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Events;

internal sealed class OperationFailedEvent : OperationRuntimeEvent
{
    internal OperationFailedEvent(
        long sequence,
        DateTimeOffset occurredAtUtc,
        OperationId operationId,
        CapabilityReference capability,
        RuntimeExecutionStatus status,
        Error error,
        ProviderId? providerId = null,
        Type? declaredResultType = null)
        : base(sequence, occurredAtUtc, operationId, capability)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The failure status is not defined.");
        }

        ValidateState(status, error, providerId, declaredResultType);

        Status = status;
        Error = error;
        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
    }

    internal ProviderId? ProviderId { get; }

    internal Type? DeclaredResultType { get; }

    internal RuntimeExecutionStatus Status { get; }

    internal Error Error { get; }

    private static void ValidateState(
        RuntimeExecutionStatus status,
        Error error,
        ProviderId? providerId,
        Type? declaredResultType)
    {
        switch (status)
        {
            case RuntimeExecutionStatus.Rejected:
                return;

            case RuntimeExecutionStatus.FunctionalFailure:
                EnsureResolved(providerId, declaredResultType, status);
                return;

            case RuntimeExecutionStatus.ContractMismatch:
                if (error.Code is not RuntimeInvocationErrors.OperationContractMismatchCode
                    and not RuntimeInvocationErrors.ToolContractMismatchCode)
                {
                    throw new ArgumentException("The contract mismatch error code is invalid.", nameof(error));
                }

                return;

            case RuntimeExecutionStatus.UnexpectedFailure:
                EnsureResolved(providerId, declaredResultType, status);
                if (error.Code != RuntimeInvocationErrors.UnexpectedToolFailureCode)
                {
                    throw new ArgumentException("The unexpected failure error code is invalid.", nameof(error));
                }

                return;

            case RuntimeExecutionStatus.Success:
            case RuntimeExecutionStatus.Cancelled:
                throw new ArgumentException($"The {status} status is not a failure.", nameof(status));

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "The failure status is not defined.");
        }
    }

    private static void EnsureResolved(
        ProviderId? providerId,
        Type? declaredResultType,
        RuntimeExecutionStatus status)
    {
        if (providerId is null || declaredResultType is null)
        {
            throw new ArgumentException($"The {status} failure must contain resolved provider and result type.");
        }
    }
}
