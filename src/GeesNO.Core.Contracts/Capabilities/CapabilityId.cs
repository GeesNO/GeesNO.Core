using System.Diagnostics.CodeAnalysis;
using GeesNO.Core.Contracts.Internal;

namespace GeesNO.Core.Contracts.Capabilities;

/// <summary>
/// Identifies a version-independent capability using a canonical dotted ASCII value.
/// </summary>
public sealed record CapabilityId
{
    private CapabilityId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the canonical identifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Parses a canonical capability identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <returns>The parsed capability identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not canonical.</exception>
    public static CapabilityId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out var capabilityId)
            ? capabilityId
            : throw new FormatException("The value is not a valid capability identifier.");
    }

    /// <summary>
    /// Attempts to parse a canonical capability identifier.
    /// </summary>
    /// <param name="value">The identifier text.</param>
    /// <param name="capabilityId">The parsed identifier when successful; otherwise, null.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(
        string? value,
        [NotNullWhen(true)] out CapabilityId? capabilityId)
    {
        if (!DottedIdentifierRules.IsValid(value))
        {
            capabilityId = null;
            return false;
        }

        capabilityId = new CapabilityId(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
