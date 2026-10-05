namespace GeesNO.Sample.Cli;

/// <summary>
/// Defines the stable exit codes used by the non-normative sample CLI.
/// </summary>
public static class CliExitCodes
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Command syntax or arguments are invalid.</summary>
    public const int InvalidUsage = 2;

    /// <summary>Runtime rejected execution before a tool ran.</summary>
    public const int RejectedExecution = 3;

    /// <summary>A tool ran and returned a functional failure.</summary>
    public const int FunctionalFailure = 4;

    /// <summary>Runtime or integration failure.</summary>
    public const int RuntimeFailure = 5;

    /// <summary>Execution was cancelled.</summary>
    public const int Cancellation = 6;
}
