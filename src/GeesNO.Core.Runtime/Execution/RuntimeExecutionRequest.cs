using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Runtime.Execution;

internal sealed class RuntimeExecutionRequest
{
    internal RuntimeExecutionRequest(
        IOperation operation,
        ProviderId? providerId = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Operation = operation;
        ProviderId = providerId;
    }

    internal IOperation Operation { get; }

    internal ProviderId? ProviderId { get; }
}
