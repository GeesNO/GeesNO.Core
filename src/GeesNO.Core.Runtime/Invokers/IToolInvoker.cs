using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Runtime.Invokers;

internal interface IToolInvoker
{
    ProviderCapabilityKey Key { get; }

    Type OperationType { get; }

    Type ResultType { get; }

    Task<InternalInvocationResult> InvokeAsync(
        IOperation operation,
        CancellationToken cancellationToken);
}
