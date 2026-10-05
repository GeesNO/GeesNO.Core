using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

namespace GeesNO.Core.Contracts.Neutral;

/// <summary>
/// Requests that bounded neutral text be returned through the typed execution pipeline.
/// </summary>
public sealed record NeutralEchoOperation : IOperation<NeutralEchoResult>
{
    /// <summary>Gets the maximum accepted text length.</summary>
    public const int MaximumTextLength = 1024;

    /// <summary>
    /// Creates a neutral echo operation.
    /// </summary>
    /// <param name="id">The operation identifier.</param>
    /// <param name="text">The bounded neutral text.</param>
    public NeutralEchoOperation(OperationId id, string text)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length > MaximumTextLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(text),
                $"Neutral echo text cannot exceed {MaximumTextLength} characters.");
        }

        Id = id;
        Text = text;
    }

    /// <summary>Gets the operation identifier.</summary>
    public OperationId Id { get; }

    /// <summary>Gets the exact neutral echo capability.</summary>
    public CapabilityReference Capability => NeutralCapabilities.Echo;

    /// <summary>Gets the neutral text.</summary>
    public string Text { get; }
}
