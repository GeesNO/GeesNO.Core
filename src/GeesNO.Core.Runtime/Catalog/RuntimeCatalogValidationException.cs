using System.Collections.ObjectModel;

namespace GeesNO.Core.Runtime.Catalog;

internal sealed class RuntimeCatalogValidationException : InvalidOperationException
{
    internal RuntimeCatalogValidationException(IReadOnlyList<string> issues)
        : this(ValidateAndCopy(issues))
    {
    }

    private RuntimeCatalogValidationException(ReadOnlyCollection<string> issues)
        : base($"Runtime catalog validation failed with {issues.Count} issue(s).")
    {
        Issues = issues;
    }

    internal IReadOnlyList<string> Issues { get; }

    private static ReadOnlyCollection<string> ValidateAndCopy(IReadOnlyList<string> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (issues.Count == 0)
        {
            throw new ArgumentException("At least one validation issue is required.", nameof(issues));
        }

        if (issues.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Validation issues cannot contain empty values.", nameof(issues));
        }

        var orderedIssues = issues.OrderBy(issue => issue, StringComparer.Ordinal).ToArray();
        return Array.AsReadOnly(orderedIssues);
    }
}
