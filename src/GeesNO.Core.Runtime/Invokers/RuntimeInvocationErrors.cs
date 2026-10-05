using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime.Invokers;

internal static class RuntimeInvocationErrors
{
    internal const string OperationContractMismatchCode = "runtime.operation-contract-mismatch";
    internal const string ToolContractMismatchCode = "runtime.tool-contract-mismatch";
    internal const string UnexpectedToolFailureCode = "runtime.unexpected-tool-failure";

    internal static Error OperationContractMismatch()
    {
        return new Error(
            OperationContractMismatchCode,
            "The operation does not match the registered tool contract.");
    }

    internal static Error ToolContractMismatch()
    {
        return new Error(
            ToolContractMismatchCode,
            "The tool does not match the registered capability contract.");
    }

    internal static Error UnexpectedToolFailure()
    {
        return new Error(
            UnexpectedToolFailureCode,
            "The tool failed unexpectedly.");
    }
}
