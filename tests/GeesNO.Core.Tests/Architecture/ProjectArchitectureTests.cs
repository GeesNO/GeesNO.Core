namespace GeesNO.Core.Tests.Architecture;

public sealed class ProjectArchitectureTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["GeesNO.Core.Primitives"] = [],
            ["GeesNO.Core.Contracts"] =
            [
                "../GeesNO.Core.Primitives/GeesNO.Core.Primitives.csproj"
            ],
            ["GeesNO.Core.Runtime"] =
            [
                "../GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
                "../GeesNO.Core.Primitives/GeesNO.Core.Primitives.csproj"
            ],
            ["GeesNO.Core.Host"] =
            [
                "../GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
                "../GeesNO.Core.Primitives/GeesNO.Core.Primitives.csproj",
                "../GeesNO.Core.Runtime/GeesNO.Core.Runtime.csproj"
            ],
            ["GeesNO.Reference.Simulator"] =
            [
                "../GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj"
            ],
            ["GeesNO.Sample.Cli"] =
            [
                "../../src/GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
                "../../src/GeesNO.Core.Runtime/GeesNO.Core.Runtime.csproj",
                "../../src/GeesNO.Reference.Simulator/GeesNO.Reference.Simulator.csproj"
            ],
            ["GeesNO.Core.Tests"] =
            [
                "../../src/GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
                "../../src/GeesNO.Core.Host/GeesNO.Core.Host.csproj",
                "../../src/GeesNO.Core.Primitives/GeesNO.Core.Primitives.csproj",
                "../../src/GeesNO.Core.Runtime/GeesNO.Core.Runtime.csproj",
                "../../src/GeesNO.Reference.Simulator/GeesNO.Reference.Simulator.csproj",
                "../../samples/GeesNO.Sample.Cli/GeesNO.Sample.Cli.csproj"
            ]
        };

    private static readonly string[] ExecutableProjects =
    [
        "GeesNO.Core.Host",
        "GeesNO.Sample.Cli"
    ];

    [Fact]
    public void Project_references_match_the_current_public_graph()
    {
        foreach (var (projectName, expectedReferences) in ExpectedProjectReferences)
        {
            var project = ArchitectureProjectFile.Load(projectName);
            var references = project.ProjectReferences
                .Select(NormalizeReference)
                .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Equal(
                expectedReferences.OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase),
                references);
        }
    }

    [Fact]
    public void Production_projects_do_not_reference_tests()
    {
        foreach (var projectName in ExpectedProjectReferences.Keys.Where(name => name != "GeesNO.Core.Tests"))
        {
            var project = ArchitectureProjectFile.Load(projectName);
            var projectText = project.Text;

            Assert.DoesNotContain("GeesNO.Core.Tests", projectText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Only_Host_and_the_non_normative_sample_are_executable_projects()
    {
        foreach (var projectName in ExpectedProjectReferences.Keys)
        {
            var project = ArchitectureProjectFile.Load(projectName);
            var outputType = project.PropertyValue("OutputType");

            if (ExecutableProjects.Contains(projectName, StringComparer.Ordinal))
            {
                Assert.Equal("Exe", outputType);
                continue;
            }

            Assert.True(string.IsNullOrWhiteSpace(outputType), $"{projectName} must not be executable.");
        }
    }

    [Fact]
    public void Project_identity_properties_match_assembly_names()
    {
        foreach (var projectName in ExpectedProjectReferences.Keys.Where(name => name != "GeesNO.Core.Tests"))
        {
            var project = ArchitectureProjectFile.Load(projectName);

            Assert.Equal(projectName, project.PropertyValue("AssemblyName"));
            Assert.Equal(projectName, project.PropertyValue("RootNamespace"));
        }
    }

    [Fact]
    public void Projects_have_authored_source_files()
    {
        foreach (var projectName in ExpectedProjectReferences.Keys)
        {
            Assert.NotEmpty(ArchitectureSourceReader.ProjectSources(projectName));
        }
    }

    private static string NormalizeReference(string reference)
    {
        return reference.Replace('\\', '/');
    }
}
