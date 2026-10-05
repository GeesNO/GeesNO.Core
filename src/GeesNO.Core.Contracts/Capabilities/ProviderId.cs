using System.Diagnostics.CodeAnalysis;
using GeesNO.Core.Contracts.Internal;

namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Identifies a capability provider using a canonical dotted ASCII value.
/// </summary>
public sealed record ProviderId
{
    private ProviderId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Parses a canonical provider identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <returns>The parsed provider identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not canonical.</exception>
    public static ProviderId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out var providerId)
            ? providerId
            : throw new FormatException("The value is not a valid provider identifier.");
    }

    /// <summary>
    /// Attempts to parse a canonical provider identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <param name="providerId">The parsed identifier when successful; otherwise, null.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(
        string? value,
        [NotNullWhen(true)] out ProviderId? providerId)
    {
        if (!DottedIdentifierRules.IsValid(value))
        {
            providerId = null;
            return false;
        }

        providerId = new ProviderId(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
