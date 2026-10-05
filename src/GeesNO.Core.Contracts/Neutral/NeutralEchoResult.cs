namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Returns bounded neutral text from a neutral echo operation.
/// </summary>
public sealed record NeutralEchoResult
{
    /// <summary>
    /// Creates a neutral echo result.
    /// </summary>
    /// <param name="text">The bounded neutral text.</param>
    public NeutralEchoResult(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length > NeutralEchoOperation.MaximumTextLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                $"Neutral echo text cannot exceed {NeutralEchoOperation.MaximumTextLength} characters.");
        }

        Text = text;
    }

    /// <summary>Gets the neutral text.</summary>
    public string Text { get; }
}
