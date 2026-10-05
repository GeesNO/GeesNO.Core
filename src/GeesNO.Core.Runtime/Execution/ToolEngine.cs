using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Runtime.Execution;

internal sealed class ToolEngine
{
    private readonly RuntimeCatalogSnapshot _snapshot;
    private readonly RuntimeObservability _observability;
    private int _state = (int)ToolEngineState.Sealed;

    internal ToolEngine(
        RuntimeCatalogSnapshot snapshot,
        RuntimeObservability? observability = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
        _observability = observability ?? RuntimeObservability.Disabled;
    }

    internal ToolEngineState State => (ToolEngineState)Volatile.Read(ref _state);

    internal void Start()
    {
        var previous = Interlocked.CompareExchange(
            ref _state,
            (int)ToolEngineState.Running,
            (int)ToolEngineState.Sealed);

        if (previous != (int)ToolEngineState.Sealed)
        {
            var error = RuntimeExecutionErrors.InvalidLifecycleTransition();
            throw new InvalidOperationException(
                $"{error.Code}: {error.Message}");
        }
    }

    internal void Stop()
    {
        while (true)
        {
            var current = Volatile.Read(ref _state);
            if (current == (int)ToolEngineState.Stopped)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref _state,
                    (int)ToolEngineState.Stopped,
                    current) == current)
            {
                return;
            }
        }
    }

    internal async Task<RuntimeExecutionResult> ExecuteAsync(
        RuntimeExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var operation = request.Operation;
        var operationId = operation.Id
            ?? throw new ArgumentException("The operation identifier cannot be null.", nameof(request));
        var capability = operation.Capability
            ?? throw new ArgumentException(
                "The operation capability cannot be null because the execution result requires exact capability identity.",
                nameof(request));

        if (State != ToolEngineState.Running)
        {
            return RuntimeExecutionResult.Rejected(
                operationId,
                capability,
                RuntimeExecutionErrors.NotReady());
        }

        var observation = await ToolEngineObservation
            .TryStartAsync(_observability, operationId, capability)
            .ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            return await CompleteObservedAsync(
                RuntimeExecutionResult.Cancelled(operationId, capability),
                observation).ConfigureAwait(false);
        }

        var resolution = Resolve(request, operationId, capability);
        if (resolution.Failure is not null)
        {
            return await CompleteObservedAsync(
                resolution.Failure,
                observation).ConfigureAwait(false);
        }

        var key = resolution.Key!;
        await PublishResolvedAsync(observation, key.ProviderId).ConfigureAwait(false);

        if (!_snapshot.TryGetManifest(key.ProviderId, out _)
            || !_snapshot.TryGetEntry(key, out var entry))
        {
            return await CompleteObservedAsync(
                RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.CapabilityNotFound(),
                    key.ProviderId),
                observation).ConfigureAwait(false);
        }

        if (!CapabilitySupportRules.IsExecutable(entry.Support.SupportLevel))
        {
            return await CompleteObservedAsync(
                RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.CapabilityNotExecutable(),
                    key.ProviderId),
                observation).ConfigureAwait(false);
        }

        if (!_snapshot.TryGetTool(key, out var tool)
            || !_snapshot.TryGetInvoker(key, out var invoker))
        {
            return await CompleteObservedAsync(
                RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.ToolNotFound(),
                    key.ProviderId),
                observation).ConfigureAwait(false);
        }

        var declaredResultType = invoker.ResultType;

        try
        {
            if (operation.Capability != key.Capability
                || entry.Descriptor.Reference != key.Capability
                || entry.Support.Capability != key.Capability
                || tool.Capability is null
                || tool.Capability != key.Capability
                || invoker.Key != key
                || invoker.OperationType != operation.GetType()
                || !CapabilitySupportRules.IsConsistent(entry.Descriptor, entry.Support))
            {
                var error = invoker.OperationType != operation.GetType()
                            || operation.Capability != key.Capability
                    ? RuntimeInvocationErrors.OperationContractMismatch()
                    : RuntimeInvocationErrors.ToolContractMismatch();

                return await CompleteObservedAsync(
                    RuntimeExecutionResult.ContractMismatch(
                        operationId,
                        capability,
                        error,
                        key.ProviderId,
                        declaredResultType),
                    observation).ConfigureAwait(false);
            }

            var safetyError = MinimumSafetyRules.Evaluate(entry.Descriptor, entry.Support);
            if (safetyError is not null)
            {
                return await CompleteObservedAsync(
                    RuntimeExecutionResult.Rejected(
                        operationId,
                        capability,
                        safetyError,
                        key.ProviderId,
                        declaredResultType),
                    observation).ConfigureAwait(false);
            }

            await PublishValidatedAsync(observation, key.ProviderId).ConfigureAwait(false);
            await PublishStartedAsync(
                observation,
                key.ProviderId,
                declaredResultType).ConfigureAwait(false);

            var invocationResult = await invoker
                .InvokeAsync(operation, cancellationToken)
                .ConfigureAwait(false);

            return await CompleteObservedAsync(
                RuntimeExecutionResult.FromInvocation(invocationResult),
                observation).ConfigureAwait(false);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            return await CompleteObservedAsync(
                RuntimeExecutionResult.UnexpectedFailure(
                    operationId,
                    capability,
                    key.ProviderId,
                    declaredResultType,
                    exception),
                observation).ConfigureAwait(false);
        }
    }

    private static async Task<RuntimeExecutionResult> CompleteObservedAsync(
        RuntimeExecutionResult result,
        ToolEngineObservation? observation)
    {
        if (observation is not null)
        {
            await observation.CompleteAsync(result).ConfigureAwait(false);
        }

        return result;
    }

    private static Task PublishResolvedAsync(
        ToolEngineObservation? observation,
        ProviderId providerId)
    {
        return observation?.PublishResolvedAsync(providerId) ?? Task.CompletedTask;
    }

    private static Task PublishValidatedAsync(
        ToolEngineObservation? observation,
        ProviderId providerId)
    {
        return observation?.PublishValidatedAsync(providerId) ?? Task.CompletedTask;
    }

    private static Task PublishStartedAsync(
        ToolEngineObservation? observation,
        ProviderId providerId,
        Type declaredResultType)
    {
        return observation?.PublishStartedAsync(providerId, declaredResultType) ?? Task.CompletedTask;
    }

    private Resolution Resolve(
        RuntimeExecutionRequest request,
        OperationId operationId,
        CapabilityReference capability)
    {
        if (request.ProviderId is not null)
        {
            var providerId = request.ProviderId;
            if (!_snapshot.TryGetManifest(providerId, out _))
            {
                return Resolution.Failed(RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.ProviderNotFound()));
            }

            var key = new ProviderCapabilityKey(providerId, capability);
            if (!_snapshot.TryGetEntry(key, out var entry))
            {
                return Resolution.Failed(RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.CapabilityNotFound(),
                    providerId));
            }

            if (!CapabilitySupportRules.IsExecutable(entry.Support.SupportLevel))
            {
                return Resolution.Failed(RuntimeExecutionResult.Rejected(
                    operationId,
                    capability,
                    RuntimeExecutionErrors.CapabilityNotExecutable(),
                    providerId));
            }

            return Resolution.Succeeded(key);
        }

        var implementations = _snapshot.GetImplementations(capability);
        if (implementations.Count == 0)
        {
            return Resolution.Failed(RuntimeExecutionResult.Rejected(
                operationId,
                capability,
                RuntimeExecutionErrors.CapabilityNotFound()));
        }

        var executableImplementations = _snapshot.GetExecutableImplementations(capability);
        if (executableImplementations.Count == 0)
        {
            return Resolution.Failed(RuntimeExecutionResult.Rejected(
                operationId,
                capability,
                RuntimeExecutionErrors.CapabilityNotExecutable()));
        }

        if (executableImplementations.Count > 1)
        {
            return Resolution.Failed(RuntimeExecutionResult.Rejected(
                operationId,
                capability,
                RuntimeExecutionErrors.AmbiguousProvider()));
        }

        return Resolution.Succeeded(executableImplementations[0]);
    }

    private sealed record Resolution(
        ProviderCapabilityKey? Key,
        RuntimeExecutionResult? Failure)
    {
        internal static Resolution Succeeded(ProviderCapabilityKey key) => new(key, null);

        internal static Resolution Failed(RuntimeExecutionResult failure) => new(null, failure);
    }
}
