using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Runtime.Invokers;

internal sealed class ToolInvoker<TOperation, TResult> : IToolInvoker
    where TOperation : IOperation<TResult>
    where TResult : notnull
{
    private readonly ITool<TOperation, TResult> _tool;
    private readonly Func<TOperation, CancellationToken, Task<Result<TResult>>> _executeAsync;

    internal ToolInvoker(
        ProviderCapabilityKey key,
        ITool<TOperation, TResult> tool)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tool);

        if (tool.Capability is null || tool.Capability != key.Capability)
        {
            throw new ArgumentException(
                $"{RuntimeInvocationErrors.ToolContractMismatchCode}: The tool capability does not match the key.",
                nameof(tool));
        }

        Key = key;
        _tool = tool;
        _executeAsync = tool.ExecuteAsync;
    }

    public ProviderCapabilityKey Key { get; }

    public Type OperationType => typeof(TOperation);

    public Type ResultType => typeof(TResult);

    public async Task<InternalInvocationResult> InvokeAsync(
        IOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var operationId = operation.Id
            ?? throw new ArgumentException("The operation identifier cannot be null.", nameof(operation));

        if (operation.GetType() != typeof(TOperation)
            || operation.Capability is null
            || operation.Capability != Key.Capability)
        {
            return OperationMismatch(operationId);
        }

        try
        {
            if (_tool.Capability is null || _tool.Capability != Key.Capability)
            {
                return ToolMismatch(operationId);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return InternalInvocationResult.Cancelled(
                    operationId,
                    Key.ProviderId,
                    Key.Capability,
                    ResultType);
            }

            var executionTask = _executeAsync((TOperation)operation, cancellationToken);
            if (executionTask is null)
            {
                return ToolMismatch(operationId);
            }

            var result = await executionTask.ConfigureAwait(false);
            if (result is null)
            {
                return ToolMismatch(operationId);
            }

            if (result.IsSuccess)
            {
                var value = result.Value;
                return value is null
                    ? ToolMismatch(operationId)
                    : InternalInvocationResult.Success(
                        operationId,
                        Key.ProviderId,
                        Key.Capability,
                        ResultType,
                        value);
            }

            return result.Error is null
                ? ToolMismatch(operationId)
                : InternalInvocationResult.FunctionalFailure(
                    operationId,
                    Key.ProviderId,
                    Key.Capability,
                    ResultType,
                    result.Error);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            return InternalInvocationResult.Cancelled(
                operationId,
                Key.ProviderId,
                Key.Capability,
                ResultType,
                exception);
        }
        catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
        {
            return InternalInvocationResult.UnexpectedFailure(
                operationId,
                Key.ProviderId,
                Key.Capability,
                ResultType,
                exception);
        }
    }

    private InternalInvocationResult OperationMismatch(OperationId operationId)
    {
        return InternalInvocationResult.ContractMismatch(
            operationId,
            Key.ProviderId,
            Key.Capability,
            ResultType,
            RuntimeInvocationErrors.OperationContractMismatch());
    }

    private InternalInvocationResult ToolMismatch(OperationId operationId)
    {
        return InternalInvocationResult.ContractMismatch(
            operationId,
            Key.ProviderId,
            Key.Capability,
            ResultType,
            RuntimeInvocationErrors.ToolContractMismatch());
    }
}
