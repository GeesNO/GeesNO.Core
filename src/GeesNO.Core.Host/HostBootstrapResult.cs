using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Host;

internal sealed class HostBootstrapResult
{
    internal HostBootstrapResult(
        int exitCode,
        bool pingExecuted,
        RuntimeExecutionStatus? status,
        string? errorCode,
        string? runtimeName,
        VersionInfo? runtimeVersion,
        int observedEvents,
        int journalEntries,
        ToolEngineState? finalEngineState,
        int stopCalls)
    {
        ExitCode = exitCode;
        PingExecuted = pingExecuted;
        Status = status;
        ErrorCode = errorCode;
        RuntimeName = runtimeName;
        RuntimeVersion = runtimeVersion;
        ObservedEvents = observedEvents;
        JournalEntries = journalEntries;
        FinalEngineState = finalEngineState;
        StopCalls = stopCalls;
    }

    internal int ExitCode { get; }

    internal bool PingExecuted { get; }

    internal RuntimeExecutionStatus? Status { get; }

    internal string? ErrorCode { get; }

    internal string? RuntimeName { get; }

    internal VersionInfo? RuntimeVersion { get; }

    internal int ObservedEvents { get; }

    internal int JournalEntries { get; }

    internal ToolEngineState? FinalEngineState { get; }

    internal int StopCalls { get; }
}
