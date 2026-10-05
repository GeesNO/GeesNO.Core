using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime.Execution;

internal static class RuntimeExecutionErrors
{
    internal const string NotReadyCode = "runtime.not-ready";
    internal const string InvalidLifecycleTransitionCode = "runtime.invalid-lifecycle-transition";
    internal const string ProviderNotFoundCode = "runtime.provider-not-found";
    internal const string CapabilityNotFoundCode = "runtime.capability-not-found";
    internal const string CapabilityNotExecutableCode = "runtime.capability-not-executable";
    internal const string AmbiguousProviderCode = "runtime.ambiguous-provider";
    internal const string ToolNotFoundCode = "runtime.tool-not-found";
    internal const string ConfirmationRequiredCode = "runtime.confirmation-required";

    internal static Error NotReady() => new(
        NotReadyCode,
        "The Runtime is not ready to execute operations.");

    internal static Error InvalidLifecycleTransition() => new(
        InvalidLifecycleTransitionCode,
        "The Tool Engine cannot perform the requested lifecycle transition.");

    internal static Error ProviderNotFound() => new(
        ProviderNotFoundCode,
        "The requested provider is not registered.");

    internal static Error CapabilityNotFound() => new(
        CapabilityNotFoundCode,
        "The requested capability is not declared.");

    internal static Error CapabilityNotExecutable() => new(
        CapabilityNotExecutableCode,
        "The requested capability is not executable.");

    internal static Error AmbiguousProvider() => new(
        AmbiguousProviderCode,
        "The requested capability has multiple executable providers.");

    internal static Error ToolNotFound() => new(
        ToolNotFoundCode,
        "The resolved capability has no executable tool.");

    internal static Error ConfirmationRequired() => new(
        ConfirmationRequiredCode,
        "The capability requires confirmation that is unavailable in this Runtime.");

    internal static bool IsRejectedCode(string code)
    {
        return code is NotReadyCode
            or ProviderNotFoundCode
            or CapabilityNotFoundCode
            or CapabilityNotExecutableCode
            or AmbiguousProviderCode
            or ToolNotFoundCode
            or ConfirmationRequiredCode;
    }
}
