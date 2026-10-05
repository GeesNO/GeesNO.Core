namespace GeesNO.Core.Tests.Architecture;

public sealed class DocumentationArchitectureTests
{
    private static readonly string[] PublicDocumentationFiles =
    [
        "README.md",
        "PROJECT_STATUS.md",
        "CHANGELOG.md",
        "RELEASE.md",
        "VERSIONING.md",
        "COMPATIBILITY.md",
        "SECURITY.md",
        "SUPPORT.md",
        "CONTRIBUTING.md",
        "TRADEMARKS.md",
        @"docs\RepositoryIndex.md",
        @"docs\NORMATIVE_AND_INFORMATIVE.md",
        @"docs\DEPENDENCY_INVENTORY.md",
        @"docs\getting-started\quick-start.md",
        @"docs\integration\README.md",
        @"docs\integration\public-api.md",
        @"docs\releases\0.1.0-preview.1.md"
    ];

    [Fact]
    public void Public_documentation_files_exist()
    {
        Assert.All(PublicDocumentationFiles, relativePath => Assert.True(
            File.Exists(Path.Combine(ArchitectureTestPaths.Root, relativePath)),
            $"{relativePath} must exist in the public snapshot."));
    }

    [Fact]
    public void Internal_phase_and_agent_documents_are_not_part_of_the_public_snapshot()
    {
        Assert.False(File.Exists(Path.Combine(ArchitectureTestPaths.Root, "AGENTS.md")));
        Assert.Empty(Directory.GetFiles(ArchitectureTestPaths.Root, "??_GEESNO_CORE_*.md"));
        Assert.False(Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, "docs", "archive")));
    }

    [Fact]
    public void Public_documentation_avoids_local_handoff_and_internal_branch_names()
    {
        var documentation = string.Join(
            Environment.NewLine,
            PublicDocumentationFiles.Select(path => File.ReadAllText(Path.Combine(ArchitectureTestPaths.Root, path))));
        var forbiddenFragments = new[]
        {
            "audit-baseline"
        };

        Assert.All(forbiddenFragments, fragment => Assert.DoesNotContain(
            fragment,
            documentation,
            StringComparison.OrdinalIgnoreCase));
    }
}
