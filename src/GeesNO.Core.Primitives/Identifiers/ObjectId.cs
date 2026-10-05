namespace GeesNO.Core.Primitives;

/// <summary>
/// Identifies an object without coupling its identity to a domain or external system.
/// </summary>
public sealed record ObjectId
{
    /// <summary>
    /// Creates an identifier from a non-empty GUID.
    /// </summary>
    /// <param name="value">The underlying identifier value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public ObjectId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An object identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the underlying non-empty GUID.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new identifier.
    /// </summary>
    public static ObjectId New()
    {
        return new ObjectId(Guid.NewGuid());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
