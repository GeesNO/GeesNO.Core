namespace GeesNO.Core.Primitives;

/// <summary>
/// Describes a stable error code and a human-readable message.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Creates an error with a non-empty code and message.
    /// </summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    public Error(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code.Trim();
        Message = message.Trim();
    }

    /// <summary>
    /// Gets the stable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; }
}
