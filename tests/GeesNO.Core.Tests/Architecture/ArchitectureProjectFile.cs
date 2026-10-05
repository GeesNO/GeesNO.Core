using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GeesNO.Core.Tests.Architecture;

internal sealed class ArchitectureProjectFile
{
    private ArchitectureProjectFile(string projectName, string fullPath, XDocument document)
    {
        ProjectName = projectName;
        FullPath = fullPath;
        Document = document;
    }

    internal string ProjectName { get; }

    internal string FullPath { get; }

    internal XDocument Document { get; }

    internal string Text => File.ReadAllText(FullPath);

    internal IReadOnlyList<string> ProjectReferences => AttributeValues("ProjectReference", "Include");

    internal IReadOnlyList<string> PackageReferences => AttributeValues("PackageReference", "Include");

    internal string? PropertyValue(string name)
    {
        return Document
            .Descendants(name)
            .Select(element => element.Value)
            .FirstOrDefault();
    }

    internal static ArchitectureProjectFile Load(string projectName)
    {
        var projectPath = ArchitectureTestPaths.ProjectPath(projectName);

        return new ArchitectureProjectFile(
            projectName,
            projectPath,
            XDocument.Load(projectPath));
    }

    private IReadOnlyList<string> AttributeValues(string elementName, string attributeName)
    {
        return Document
            .Descendants(elementName)
            .Select(element => element.Attribute(attributeName)?.Value)
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();
    }
}

internal static partial class ArchitectureSolutionFile
{
    private static readonly Regex ProjectLineExpression = CreateProjectLineExpression();

    internal static IReadOnlyList<string> ProjectPaths()
    {
        return File.ReadAllLines(ArchitectureTestPaths.SolutionPath)
            .Select(line => ProjectLineExpression.Match(line))
            .Where(match => match.Success)
            .Select(match => NormalizePath(match.Groups["path"].Value))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string NormalizePath(string path)
    {
        return path.Replace('/', '\\');
    }

    [GeneratedRegex("^Project\\(\"[^\"]+\"\\) = \"[^\"]+\", \"(?<path>[^\"]+\\.csproj)\", \"[^\"]+\"$")]
    private static partial Regex CreateProjectLineExpression();
}
