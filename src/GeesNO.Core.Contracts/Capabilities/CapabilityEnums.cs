namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Classifies the execution risk of a capability.
/// </summary>
public enum RiskLevel
{
    /// <summary>The capability only reads state.</summary>
    ReadOnly,

    /// <summary>The capability has low execution risk.</summary>
    Low,

    /// <summary>The capability has high execution risk.</summary>
    High,

    /// <summary>The capability has critical execution risk.</summary>
    Critical
}

/// <summary>
/// Declares the minimum confirmation expected by a capability.
/// </summary>
public enum ConfirmationPolicy
{
    /// <summary>The capability does not require confirmation by itself.</summary>
    NotRequired,

    /// <summary>Confirmation is selected according to effective risk policy.</summary>
    RiskBased,

    /// <summary>The capability always requires confirmation.</summary>
    AlwaysRequired
}

/// <summary>
/// Describes the undo mechanism available for a capability.
/// </summary>
public enum UndoSupportLevel
{
    /// <summary>No undo mechanism is declared.</summary>
    None,

    /// <summary>The provider exposes a native undo mechanism.</summary>
    NativeUndo,

    /// <summary>The provider can execute a compensating action.</summary>
    CompensatingAction,

    /// <summary>The operation participates in a transaction.</summary>
    Transactional,

    /// <summary>The provider offers a best-effort undo mechanism.</summary>
    BestEffort
}

/// <summary>
/// Describes how a provider supports a capability.
/// </summary>
public enum CapabilitySupportLevel
{
    /// <summary>The provider supports the capability natively and completely.</summary>
    Native,

    /// <summary>The provider supports a restricted subset of the capability.</summary>
    Partial,

    /// <summary>The provider emulates the capability with declared differences.</summary>
    Emulated,

    /// <summary>The provider does not support the capability.</summary>
    Unsupported,

    /// <summary>The capability is temporarily unavailable in the current context.</summary>
    Unavailable
}
