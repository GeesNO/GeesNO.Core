namespace GeesNO.Core.Tests.Architecture;

public sealed class RepositoryHygieneArchitectureTests
{
    private static readonly string[] ExpectedSourceProjectDirectories =
    [
        "GeesNO.Core.Contracts",
        "GeesNO.Core.Host",
        "GeesNO.Core.Primitives",
        "GeesNO.Core.Runtime",
        "GeesNO.Reference.Simulator"
    ];

    private static readonly string[] ForbiddenFutureProjectDirectories =
    [
        @"src\GeesNO.AI",
        @"src\GeesNO.Sdk",
        @"src\GeesNO.Transport.Mcp",
        @"src\GeesNO.Core.Hosting",
        @"src\GeesNO.Domain.Cad.Contracts",
        @"src\GeesNO.Domain.Cad.Runtime",
        @"src\GeesNO.Adapter",
        @"src\GeesNO.Adapter.Simulator",
        @"src\GeesNO.Adapter.BricsCAD"
    ];

    [Fact]
    public void Source_directory_contains_only_the_current_public_foundation_projects()
    {
        var directories = Directory
            .GetDirectories(ArchitectureTestPaths.SourceDirectory)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedSourceProjectDirectories, directories);
    }

    [Fact]
    public void Scripts_directory_contains_only_release_validation_scripts()
    {
        var scriptsPath = Path.Combine(ArchitectureTestPaths.Root, "scripts");
        var files = Directory
            .GetFiles(scriptsPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(scriptsPath, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var directories = Directory
            .GetDirectories(scriptsPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(scriptsPath, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "release/external-consumption-test.ps1",
                "release/preview-dry-run.ps1"
            ],
            files);
        Assert.Equal(["release"], directories);
    }

    [Fact]
    public void Github_directory_contains_only_public_workflow_and_templates()
    {
        var githubPath = Path.Combine(ArchitectureTestPaths.Root, ".github");
        var files = Directory
            .GetFiles(githubPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(githubPath, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var directories = Directory
            .GetDirectories(githubPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(githubPath, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ISSUE_TEMPLATE/bug_report.yml",
                "ISSUE_TEMPLATE/config.yml",
                "ISSUE_TEMPLATE/feature_request.yml",
                "dependabot.yml",
                "pull_request_template.md",
                "workflows/dotnet.yml",
                "workflows/nuget-publish.yml"
            ],
            files);
        Assert.Equal(["ISSUE_TEMPLATE", "workflows"], directories);
    }

    [Fact]
    public void Public_snapshot_does_not_keep_internal_archive_or_placeholder_directories()
    {
        var forbiddenRootEntries = new[]
        {
            "legacy",
            "assets",
            "tools",
            "installer",
            "build",
            "LICENSES",
            "CONFORMANCE.md",
            "GOVERNANCE.md",
            "CODE_OF_CONDUCT.md"
        };

        Assert.All(forbiddenRootEntries, entry => Assert.False(
            File.Exists(Path.Combine(ArchitectureTestPaths.Root, entry))
            || Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, entry)),
            $"{entry} must not exist in this Developer Preview snapshot."));
        Assert.False(Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, "docs", "archive")));
        Assert.All(ForbiddenFutureProjectDirectories, relativePath => Assert.False(
            Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, relativePath)),
            $"{relativePath} must not be created in the Foundation snapshot."));
    }
}
