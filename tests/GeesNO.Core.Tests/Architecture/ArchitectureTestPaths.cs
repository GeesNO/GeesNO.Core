namespace GeesNO.Core.Tests.Architecture;

internal static class ArchitectureTestPaths
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    internal static string Root => RepositoryRoot.Value;

    internal static string SolutionPath => Path.Combine(Root, "GeesNO.Core.sln");

    internal static string SourceDirectory => Path.Combine(Root, "src");

    internal static string SamplesDirectory => Path.Combine(Root, "samples");

    internal static string TestsDirectory => Path.Combine(Root, "tests");

    internal static string DirectoryBuildPropsPath => Path.Combine(Root, "Directory.Build.props");

    internal static string DirectoryPackagesPropsPath => Path.Combine(Root, "Directory.Packages.props");

    internal static string ProjectDirectory(string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        return projectName switch
        {
            "GeesNO.Core.Tests" => Path.Combine(TestsDirectory, projectName),
            "GeesNO.Sample.Cli" => Path.Combine(SamplesDirectory, projectName),
            _ => Path.Combine(SourceDirectory, projectName)
        };
    }

    internal static string ProjectPath(string projectName)
    {
        return Path.Combine(ProjectDirectory(projectName), $"{projectName}.csproj");
    }

    internal static string DocumentationPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.Combine(Root, fileName);
    }

    internal static string RelativeToRoot(string path)
    {
        return Path.GetRelativePath(Root, path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GeesNO.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}