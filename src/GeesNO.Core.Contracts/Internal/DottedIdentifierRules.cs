using System.Diagnostics.CodeAnalysis;

namespace GeesNO.Core.Contracts.Internal;

internal static class DottedIdentifierRules
{
    internal const int MaximumLength = 128;

    internal static bool IsValid([NotNullWhen(true)] string? value)
    {
        if (value is null || value.Length is < 3 or > MaximumLength)
        {
            return false;
        }

        var separatorCount = 0;
        var atSegmentStart = true;
        var previousWasHyphen = false;

        foreach (var character in value)
        {
            if (IsAsciiLetterOrDigit(character))
            {
                atSegmentStart = false;
                previousWasHyphen = false;
                continue;
            }

            if (character == '-')
            {
                if (atSegmentStart || previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
                continue;
            }

            if (character == '.')
            {
                if (atSegmentStart || previousWasHyphen)
                {
                    return false;
                }

                separatorCount++;
                atSegmentStart = true;
                continue;
            }

            return false;
        }

        return separatorCount > 0 && !atSegmentStart && !previousWasHyphen;
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return value is >= 'a' and <= 'z' or >= '0' and <= '9';
    }
}
