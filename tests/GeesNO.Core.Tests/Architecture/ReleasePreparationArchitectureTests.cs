using System.Xml.Linq;

namespace GeesNO.Core.Tests.Architecture;

public sealed class ReleasePreparationArchitectureTests
{
    private static readonly string[] ActiveProjectNames =
    [
        "GeesNO.Core.Primitives",
        "GeesNO.Core.Contracts",
        "GeesNO.Core.Runtime",
        "GeesNO.Core.Host",
        "GeesNO.Reference.Simulator",
        "GeesNO.Sample.Cli",
        "GeesNO.Core.Tests"
    ];

    [Fact]
    public void Solution_contains_exactly_the_current_seven_projects()
    {
        Assert.Equal(7, ArchitectureSolutionFile.ProjectPaths().Count);
    }

    [Fact]
    public void Active_projects_do_not_reference_unapproved_source_projects()
    {
        var approvedProjectNames = ActiveProjectNames
            .Select(name => $"{name}.csproj")
            .ToArray();

        foreach (var projectName in ActiveProjectNames)
        {
            var project = ArchitectureProjectFile.Load(projectName);

            Assert.All(project.ProjectReferences, reference => Assert.Contains(
                Path.GetFileName(reference),
                approvedProjectNames,
                StringComparer.Ordinal));
        }
    }

    [Fact]
    public void Central_package_versions_match_active_package_consumers()
    {
        var packageVersions = XDocument
            .Load(ArchitectureTestPaths.DirectoryPackagesPropsPath)
            .Descendants("PackageVersion")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Select(value => value!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "Microsoft.Extensions.Hosting",
                "Microsoft.NET.Test.Sdk",
                "xunit.runner.visualstudio",
                "xunit.v3"
            ],
            packageVersions);
    }

    [Fact]
    public void Public_policy_files_exist_without_conformance_claim()
    {
        var requiredRootFiles = new[]
        {
            "LICENSE",
            "NOTICE",
            "TRADEMARKS.md",
            "CONTRIBUTING.md",
            "DCO",
            "SUPPORT.md",
            "SECURITY.md"
        };
        var forbiddenRootEntries = new[]
        {
            "LICENSES",
            "CONFORMANCE.md"
        };

        Assert.All(requiredRootFiles, entry => Assert.True(
            File.Exists(Path.Combine(ArchitectureTestPaths.Root, entry)),
            $"{entry} must exist in the Developer Preview snapshot."));
        Assert.All(forbiddenRootEntries, entry => Assert.False(
            File.Exists(Path.Combine(ArchitectureTestPaths.Root, entry))
            || Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, entry)),
            $"{entry} must not be created in this Developer Preview snapshot."));
    }

    [Fact]
    public void Packaging_surface_is_limited_to_public_Core_packages()
    {
        var expectedPackableProjects = new[]
        {
            "GeesNO.Core.Primitives",
            "GeesNO.Core.Contracts",
            "GeesNO.Core.Runtime"
        };
        var expectedNonPackableProjects = ActiveProjectNames
            .Except(expectedPackableProjects, StringComparer.Ordinal)
            .ToArray();

        Assert.All(expectedPackableProjects, projectName =>
        {
            var project = ArchitectureProjectFile.Load(projectName);

            Assert.Equal("true", project.PropertyValue("IsPackable"));
            Assert.Equal(projectName, project.PropertyValue("PackageId"));
            Assert.Equal("$(GeesNODeveloperPreviewPackageVersion)", project.PropertyValue("PackageVersion"));
            Assert.Equal("PACKAGE_README.md", project.PropertyValue("PackageReadmeFile"));
        });

        Assert.All(expectedNonPackableProjects, projectName =>
        {
            var project = ArchitectureProjectFile.Load(projectName);

            Assert.Equal("false", project.PropertyValue("IsPackable"));
            Assert.Null(project.PropertyValue("PackageId"));
        });
    }
}
