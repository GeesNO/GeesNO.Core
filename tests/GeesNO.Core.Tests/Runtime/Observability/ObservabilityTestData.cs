using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;

namespace GeesNO.Core.Tests.Runtime.Observability;

internal static class ObservabilityTestData
{
    internal static DateTimeOffset UtcNow { get; } =
        new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    internal static OperationId OperationId()
    {
        return GeesNO.Core.Contracts.Operations.OperationId.New();
    }

    internal static CapabilityReference Capability()
    {
        return new CapabilityReference(
            CapabilityId.Parse("system.observability-test"),
            new VersionInfo(1, 0, 0));
    }

    internal static ProviderId Provider()
    {
        return ProviderId.Parse("provider.observability-test");
    }

    internal static Error FunctionalError()
    {
        return new Error("test.functional-failure", "The test operation failed.");
    }

    internal static OperationReceivedEvent Received(long sequence = 1)
    {
        return new OperationReceivedEvent(sequence, UtcNow, OperationId(), Capability());
    }

    internal static ExecutionJournalEntry JournalEntry(
        long sequence = 1,
        RuntimeExecutionStatus status = RuntimeExecutionStatus.Success)
    {
        var provider = status is RuntimeExecutionStatus.Success
            or RuntimeExecutionStatus.FunctionalFailure
            or RuntimeExecutionStatus.UnexpectedFailure
            ? Provider()
            : null;
        var resultType = provider is null ? null : typeof(string);
        var error = status switch
        {
            RuntimeExecutionStatus.Rejected => new Error("runtime.provider-not-found", "Provider not found."),
            RuntimeExecutionStatus.FunctionalFailure => FunctionalError(),
            RuntimeExecutionStatus.ContractMismatch => RuntimeInvocationErrors.OperationContractMismatch(),
            RuntimeExecutionStatus.UnexpectedFailure => RuntimeInvocationErrors.UnexpectedToolFailure(),
            _ => null
        };

        return new ExecutionJournalEntry(
            sequence,
            OperationId(),
            Capability(),
            status,
            UtcNow,
            TimeSpan.FromMilliseconds(10),
            provider,
            resultType,
            error);
    }
}
