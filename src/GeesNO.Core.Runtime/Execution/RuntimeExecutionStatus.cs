namespace GeesNO.Core.Runtime.Execution;

internal enum RuntimeExecutionStatus
{
    Success,
    Rejected,
    FunctionalFailure,
    ContractMismatch,
    UnexpectedFailure,
    Cancelled
}
