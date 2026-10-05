namespace GeesNO.Core.Tests.Architecture;

public sealed class PackageArchitectureTests
{
    [Fact]
    public void Package_references_are_confined_to_Host_and_Tests()
    {
        Assert.Empty(ArchitectureProjectFile.Load("GeesNO.Core.Primitives").PackageReferences);
        Assert.Empty(ArchitectureProjectFile.Load("GeesNO.Core.Contracts").PackageReferences);
        Assert.Empty(ArchitectureProjectFile.Load("GeesNO.Core.Runtime").PackageReferences);
        Assert.Empty(ArchitectureProjectFile.Load("GeesNO.Sample.Cli").PackageReferences);

        Assert.Equal(
            ["Microsoft.Extensions.Hosting"],
            ArchitectureProjectFile.Load("GeesNO.Core.Host").PackageReferences);
        Assert.Equal(
            ["Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio"],
            ArchitectureProjectFile.Load("GeesNO.Core.Tests").PackageReferences);
    }

    [Fact]
    public void Microsoft_extensions_are_not_referenced_below_Host()
    {
        foreach (var projectName in new[]
                 {
                     "GeesNO.Core.Primitives",
                     "GeesNO.Core.Contracts",
                     "GeesNO.Core.Runtime",
                     "GeesNO.Sample.Cli"
                 })
        {
            var project = ArchitectureProjectFile.Load(projectName);
            var source = ArchitectureSourceReader.ProjectSourceText(projectName);

            Assert.DoesNotContain("Microsoft.Extensions", project.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.Extensions", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_is_the_only_GeesNO_project_that_references_hosting_package()
    {
        var packageUsers = new[]
            {
                "GeesNO.Core.Primitives",
                "GeesNO.Core.Contracts",
                "GeesNO.Core.Runtime",
                "GeesNO.Core.Host",
                "GeesNO.Reference.Simulator",
                "GeesNO.Sample.Cli",
                "GeesNO.Core.Tests"
            }
            .Where(projectName => ArchitectureProjectFile
                .Load(projectName)
                .PackageReferences
                .Contains("Microsoft.Extensions.Hosting", StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(["GeesNO.Core.Host"], packageUsers);
    }
}