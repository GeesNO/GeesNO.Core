using System.Collections.ObjectModel;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Represents a sanitized public Runtime configuration error.
/// </summary>
public sealed class GeesNORuntimeConfigurationException : InvalidOperationException
{
    private const string ConfigurationInvalidCode = "runtime.configuration-invalid";

    /// <summary>
    /// Initializes a new instance of the <see cref="GeesNORuntimeConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The public error message.</param>
    public GeesNORuntimeConfigurationException(string message)
        : this(message, Array.Empty<Error>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeesNORuntimeConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The public error message.</param>
    /// <param name="errors">The public configuration errors.</param>
    public GeesNORuntimeConfigurationException(string message, IEnumerable<Error> errors)
        : base(NormalizeMessage(message))
    {
        Errors = CopyErrors(errors);
    }

    /// <summary>
    /// Gets the public configuration errors associated with this exception.
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    internal static GeesNORuntimeConfigurationException FromValidationIssues(IReadOnlyList<string> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var errors = issues
            .Select(issue => new Error(ConfigurationInvalidCode, NormalizeMessage(issue)))
            .ToArray();

        return new GeesNORuntimeConfigurationException(
            "The Runtime configuration is invalid.",
            errors);
    }

    private static string NormalizeMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return message.Trim();
    }

    private static ReadOnlyCollection<Error> CopyErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var copiedErrors = errors.ToArray();
        foreach (var error in copiedErrors)
        {
            ArgumentNullException.ThrowIfNull(error);
        }

        return Array.AsReadOnly(copiedErrors);
    }
}