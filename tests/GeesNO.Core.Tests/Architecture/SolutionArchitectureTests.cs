namespace GeesNO.Core.Tests.Architecture;

public sealed class SolutionArchitectureTests
{
    private static readonly string[] ExpectedSolutionProjects =
    [
        @"samples\GeesNO.Sample.Cli\GeesNO.Sample.Cli.csproj",
        @"src\GeesNO.Core.Contracts\GeesNO.Core.Contracts.csproj",
        @"src\GeesNO.Core.Host\GeesNO.Core.Host.csproj",
        @"src\GeesNO.Core.Primitives\GeesNO.Core.Primitives.csproj",
        @"src\GeesNO.Core.Runtime\GeesNO.Core.Runtime.csproj",
        @"src\GeesNO.Reference.Simulator\GeesNO.Reference.Simulator.csproj",
        @"tests\GeesNO.Core.Tests\GeesNO.Core.Tests.csproj"
    ];

    [Fact]
    public void Solution_contains_exactly_the_current_GeesNO_projects_and_sample()
    {
        var solutionProjects = ArchitectureSolutionFile.ProjectPaths();

        Assert.Equal(
            ExpectedSolutionProjects.OrderBy(path => path, StringComparer.OrdinalIgnoreCase),
            solutionProjects);
    }

    [Fact]
    public void GeesNO_projects_on_disk_are_not_missing_from_solution()
    {
        var discoveredProjects = Directory
            .GetFiles(ArchitectureTestPaths.Root, "GeesNO.Core.*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(
                ArchitectureTestPaths.Root,
                "GeesNO.Reference.Simulator.csproj",
                SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(
                ArchitectureTestPaths.Root,
                "GeesNO.Sample.Cli.csproj",
                SearchOption.AllDirectories))
            .Where(path => ArchitectureSourceReader.IsAuthoredSource(path))
            .Select(ArchitectureTestPaths.RelativeToRoot)
            .Select(ArchitectureSolutionFile.NormalizePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(
            ExpectedSolutionProjects.OrderBy(path => path, StringComparer.OrdinalIgnoreCase),
            discoveredProjects);
    }

    [Fact]
    public void Hosting_library_and_legacy_folder_are_not_created()
    {
        Assert.False(Directory.Exists(Path.Combine(
            ArchitectureTestPaths.SourceDirectory,
            "GeesNO.Core.Hosting")));
        Assert.False(Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, "legacy")));

        var solution = File.ReadAllText(ArchitectureTestPaths.SolutionPath);
        Assert.DoesNotContain("GeesNO.Core.Hosting", solution, StringComparison.Ordinal);
        Assert.DoesNotContain("legacy", solution, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Directory_build_props_keeps_the_shared_foundation_settings()
    {
        var buildProps = ArchitectureSourceReader.ReadRootFile("Directory.Build.props");

        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", buildProps, StringComparison.Ordinal);
        Assert.Contains("<LangVersion>12.0</LangVersion>", buildProps, StringComparison.Ordinal);
        Assert.Contains("<Nullable>enable</Nullable>", buildProps, StringComparison.Ordinal);
        Assert.Contains("<ImplicitUsings>enable</ImplicitUsings>", buildProps, StringComparison.Ordinal);
        Assert.Contains("<Deterministic>true</Deterministic>", buildProps, StringComparison.Ordinal);
    }
}
