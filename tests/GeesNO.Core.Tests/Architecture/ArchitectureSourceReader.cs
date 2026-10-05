namespace GeesNO.Core.Tests.Architecture;

internal sealed record ArchitectureSourceFile(
    string FullPath,
    string RelativePath,
    string Content);

internal static class ArchitectureSourceReader
{
    internal static IReadOnlyList<ArchitectureSourceFile> ProjectSources(string projectName)
    {
        var projectDirectory = ArchitectureTestPaths.ProjectDirectory(projectName);

        return Directory
            .GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(IsAuthoredSource)
            .Select(path => new ArchitectureSourceFile(
                path,
                ArchitectureTestPaths.RelativeToRoot(path),
                File.ReadAllText(path)))
            .OrderBy(source => source.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ProjectSourceText(string projectName)
    {
        return string.Join(
            Environment.NewLine,
            ProjectSources(projectName).Select(source => source.Content));
    }

    internal static IReadOnlyList<ArchitectureSourceFile> SourcesUnder(params string[] relativeSegments)
    {
        var directory = Path.Combine([ArchitectureTestPaths.Root, .. relativeSegments]);

        return Directory
            .GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(IsAuthoredSource)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".props" or ".targets" or ".json" or ".md")
            .Select(path => new ArchitectureSourceFile(
                path,
                ArchitectureTestPaths.RelativeToRoot(path),
                File.ReadAllText(path)))
            .OrderBy(source => source.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ReadProjectSource(string projectName, params string[] relativeSegments)
    {
        var path = Path.Combine(
            [ArchitectureTestPaths.ProjectDirectory(projectName), .. relativeSegments]);

        Assert.True(IsAuthoredSource(path), $"Expected authored source: {ArchitectureTestPaths.RelativeToRoot(path)}");
        return File.ReadAllText(path);
    }

    internal static string ReadRootFile(params string[] relativeSegments)
    {
        return File.ReadAllText(Path.Combine([ArchitectureTestPaths.Root, .. relativeSegments]));
    }

    internal static bool IsAuthoredSource(string path)
    {
        var relativePath = ArchitectureTestPaths.RelativeToRoot(path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return !segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            && !segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }
}
