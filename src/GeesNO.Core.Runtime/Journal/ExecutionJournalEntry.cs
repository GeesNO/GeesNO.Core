using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Runtime.Journal;

internal sealed class ExecutionJournalEntry
{
    internal ExecutionJournalEntry(
        long sequence,
        OperationId operationId,
        CapabilityReference capability,
        RuntimeExecutionStatus status,
        DateTimeOffset startedAtUtc,
        TimeSpan duration,
        ProviderId? providerId = null,
        Type? declaredResultType = null,
        Error? error = null)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "The journal sequence must be positive.");
        }

        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(capability);

        if (startedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The journal start time must use the UTC offset.", nameof(startedAtUtc));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "The journal duration cannot be negative.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The execution status is not defined.");
        }

        ValidateState(status, providerId, declaredResultType, error);

        Sequence = sequence;
        OperationId = operationId;
        Capability = capability;
        ProviderId = providerId;
        DeclaredResultType = declaredResultType;
        Status = status;
        Error = error;
        StartedAtUtc = startedAtUtc;
        Duration = duration;
    }

    internal long Sequence { get; }

    internal OperationId OperationId { get; }

    internal CapabilityReference Capability { get; }

    internal ProviderId? ProviderId { get; }

    internal Type? DeclaredResultType { get; }

    internal RuntimeExecutionStatus Status { get; }

    internal Error? Error { get; }

    internal DateTimeOffset StartedAtUtc { get; }

    internal TimeSpan Duration { get; }

    private static void ValidateState(
        RuntimeExecutionStatus status,
        ProviderId? providerId,
        Type? declaredResultType,
        Error? error)
    {
        switch (status)
        {
            case RuntimeExecutionStatus.Success:
                EnsureResolved(providerId, declaredResultType, status);
                EnsureNoError(error, status);
                return;

            case RuntimeExecutionStatus.Rejected:
                EnsureError(error, status);
                return;

            case RuntimeExecutionStatus.FunctionalFailure:
                EnsureResolved(providerId, declaredResultType, status);
                EnsureError(error, status);
                return;

            case RuntimeExecutionStatus.ContractMismatch:
                EnsureError(error, status);
                if (error!.Code is not RuntimeInvocationErrors.OperationContractMismatchCode
                    and not RuntimeInvocationErrors.ToolContractMismatchCode)
                {
                    throw new ArgumentException("The contract mismatch error code is invalid.", nameof(error));
                }

                return;

            case RuntimeExecutionStatus.UnexpectedFailure:
                EnsureResolved(providerId, declaredResultType, status);
                EnsureError(error, status);
                if (error!.Code != RuntimeInvocationErrors.UnexpectedToolFailureCode)
                {
                    throw new ArgumentException("The unexpected failure error code is invalid.", nameof(error));
                }

                return;

            case RuntimeExecutionStatus.Cancelled:
                EnsureNoError(error, status);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "The execution status is not terminal.");
        }
    }

    private static void EnsureResolved(
        ProviderId? providerId,
        Type? declaredResultType,
        RuntimeExecutionStatus status)
    {
        if (providerId is null || declaredResultType is null)
        {
            throw new ArgumentException($"The {status} entry must contain resolved provider and result type.");
        }
    }

    private static void EnsureError(Error? error, RuntimeExecutionStatus status)
    {
        if (error is null)
        {
            throw new ArgumentException($"The {status} entry must contain an error.", nameof(error));
        }
    }

    private static void EnsureNoError(Error? error, RuntimeExecutionStatus status)
    {
        if (error is not null)
        {
            throw new ArgumentException($"The {status} entry cannot contain an error.", nameof(error));
        }
    }
}
