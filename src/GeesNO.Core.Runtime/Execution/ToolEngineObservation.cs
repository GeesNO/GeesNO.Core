using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Runtime.Execution;

internal sealed class ToolEngineObservation
{
    private readonly RuntimeObservability _observability;
    private readonly OperationId _operationId;
    private readonly CapabilityReference _capability;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly long _startingTimestamp;

    private ToolEngineObservation(
        RuntimeObservability observability,
        OperationId operationId,
        CapabilityReference capability,
        DateTimeOffset startedAtUtc,
        long startingTimestamp)
    {
        _observability = observability;
        _operationId = operationId;
        _capability = capability;
        _startedAtUtc = startedAtUtc;
        _startingTimestamp = startingTimestamp;
    }

    internal static async Task<ToolEngineObservation?> TryStartAsync(
        RuntimeObservability observability,
        OperationId operationId,
        CapabilityReference capability)
    {
        ArgumentNullException.ThrowIfNull(observability);
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(capability);

        if (!observability.IsEnabled
            || !observability.TryGetUtcNow(out var startedAtUtc)
            || !observability.TryGetTimestamp(out var startingTimestamp))
        {
            return null;
        }

        var observation = new ToolEngineObservation(
            observability,
            operationId,
            capability,
            startedAtUtc,
            startingTimestamp);

        await observation.PublishAsync((sequence, occurredAtUtc) =>
            new OperationReceivedEvent(
                sequence,
                occurredAtUtc,
                operationId,
                capability)).ConfigureAwait(false);

        return observation;
    }

    internal Task PublishResolvedAsync(ProviderId providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        return PublishAsync((sequence, occurredAtUtc) => new OperationResolvedEvent(
            sequence,
            occurredAtUtc,
            _operationId,
            _capability,
            providerId));
    }

    internal Task PublishValidatedAsync(ProviderId providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        return PublishAsync((sequence, occurredAtUtc) => new OperationValidatedEvent(
            sequence,
            occurredAtUtc,
            _operationId,
            _capability,
            providerId));
    }

    internal Task PublishStartedAsync(ProviderId providerId, Type declaredResultType)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(declaredResultType);

        return PublishAsync((sequence, occurredAtUtc) => new OperationStartedEvent(
            sequence,
            occurredAtUtc,
            _operationId,
            _capability,
            providerId,
            declaredResultType));
    }

    internal async Task CompleteAsync(RuntimeExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        AppendJournal(result);
        await PublishTerminalAsync(result).ConfigureAwait(false);
    }

    private void AppendJournal(RuntimeExecutionResult result)
    {
        if (!_observability.TryGetElapsedTime(_startingTimestamp, out var duration)
            || !TryNextSequence(out var sequence))
        {
            return;
        }

        try
        {
            var entry = new ExecutionJournalEntry(
                sequence,
                result.OperationId,
                result.Capability,
                result.Status,
                _startedAtUtc,
                duration,
                result.ProviderId,
                result.DeclaredResultType,
                result.Error);

            _observability.AppendSafely(entry);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            // Invalid observability artifacts never replace a functional result.
        }
    }

    private Task PublishTerminalAsync(RuntimeExecutionResult result)
    {
        return result.Status switch
        {
            RuntimeExecutionStatus.Success => PublishAsync((sequence, occurredAtUtc) =>
                new OperationSucceededEvent(
                    sequence,
                    occurredAtUtc,
                    result.OperationId,
                    result.Capability,
                    result.ProviderId!,
                    result.DeclaredResultType!)),
            RuntimeExecutionStatus.Rejected
                or RuntimeExecutionStatus.FunctionalFailure
                or RuntimeExecutionStatus.ContractMismatch
                or RuntimeExecutionStatus.UnexpectedFailure => PublishAsync((sequence, occurredAtUtc) =>
                    new OperationFailedEvent(
                        sequence,
                        occurredAtUtc,
                        result.OperationId,
                        result.Capability,
                        result.Status,
                        result.Error!,
                        result.ProviderId,
                        result.DeclaredResultType)),
            RuntimeExecutionStatus.Cancelled => PublishAsync((sequence, occurredAtUtc) =>
                new OperationCancelledEvent(
                    sequence,
                    occurredAtUtc,
                    result.OperationId,
                    result.Capability,
                    result.ProviderId,
                    result.DeclaredResultType)),
            _ => Task.CompletedTask
        };
    }

    private async Task PublishAsync(Func<long, DateTimeOffset, RuntimeEvent> eventFactory)
    {
        try
        {
            if (!_observability.TryGetUtcNow(out var occurredAtUtc)
                || !TryNextSequence(out var sequence))
            {
                return;
            }

            var runtimeEvent = eventFactory(sequence, occurredAtUtc);
            await _observability.PublishAsync(runtimeEvent).ConfigureAwait(false);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            // Ordinary observability failures never alter pipeline semantics.
        }
    }

    private bool TryNextSequence(out long sequence)
    {
        try
        {
            sequence = _observability.NextSequence();
            return sequence > 0;
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            sequence = default;
            return false;
        }
    }
}
