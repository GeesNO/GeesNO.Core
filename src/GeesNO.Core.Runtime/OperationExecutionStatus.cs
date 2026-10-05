namespace GeesNO.Core.Runtime;

/// <summary>
/// Defines the normalized public status of a Runtime operation execution.
/// </summary>
public enum OperationExecutionStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The operation was rejected before tool execution.
    /// </summary>
    Rejected,

    /// <summary>
    /// The tool ran and returned a functional failure.
    /// </summary>
    FunctionalFailure,

    /// <summary>
    /// The operation or tool contract did not match the registered capability.
    /// </summary>
    ContractMismatch,

    /// <summary>
    /// The tool failed unexpectedly and the failure was sanitized.
    /// </summary>
    UnexpectedFailure,

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    Cancelled
}