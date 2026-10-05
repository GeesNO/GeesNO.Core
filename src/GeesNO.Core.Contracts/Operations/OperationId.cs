using GeesNO.Core.Primitives;

namespace GeesNO.Core.Contracts.Operations;

/// <summary>
/// Identifies one operation without exposing its underlying GUID as the primary contract.
/// </summary>
public sealed record OperationId
{
    /// <summary>
    /// Creates an operation identifier from an object identifier.
    /// </summary>
    /// <param name="value">The underlying object identifier.</param>
    public OperationId(ObjectId value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>Gets the underlying object identifier.</summary>
    public ObjectId Value { get; }

    /// <summary>Creates a new operation identifier.</summary>
    /// <returns>A new operation identifier.</returns>
    public static OperationId New()
    {
        return new OperationId(ObjectId.New());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
