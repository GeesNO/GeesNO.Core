using System.Reflection;
using GeesNO.Core.Tests.Architecture;
using GeesNO.Reference.Simulator;

namespace GeesNO.Core.Tests.Reference.Simulator;

public sealed class NeutralSimulatorArchitectureTests
{
    [Fact]
    public void Reference_simulator_project_is_in_solution_and_depends_only_on_contracts()
    {
        Assert.Contains(
            @"src\GeesNO.Reference.Simulator\GeesNO.Reference.Simulator.csproj",
            ArchitectureSolutionFile.ProjectPaths());

        var project = ArchitectureProjectFile.Load("GeesNO.Reference.Simulator");

        Assert.Equal("false", project.PropertyValue("IsPackable"));
        Assert.Equal("GeesNO.Reference.Simulator", project.PropertyValue("AssemblyName"));
        Assert.Equal("GeesNO.Reference.Simulator", project.PropertyValue("RootNamespace"));
        Assert.Empty(project.PackageReferences);
        var reference = Assert.Single(project.ProjectReferences);
        Assert.Equal(@"..\GeesNO.Core.Contracts\GeesNO.Core.Contracts.csproj", reference);
        Assert.DoesNotContain(project.ProjectReferences, item => item.Contains("Runtime", StringComparison.Ordinal));
        Assert.DoesNotContain(project.ProjectReferences, item => item.Contains("Host", StringComparison.Ordinal));
        Assert.DoesNotContain(project.ProjectReferences, item => item.Contains("Primitives", StringComparison.Ordinal));
    }

    [Fact]
    public void Reference_simulator_exports_only_the_approved_public_types()
    {
        var exportedTypes = typeof(NeutralSimulatorProvider).Assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "GeesNO.Reference.Simulator.NeutralDescribeTool",
                "GeesNO.Reference.Simulator.NeutralEchoTool",
                "GeesNO.Reference.Simulator.NeutralSimulatorManifestFactory",
                "GeesNO.Reference.Simulator.NeutralSimulatorProvider",
                "GeesNO.Reference.Simulator.NeutralValidateTool"
            ],
            exportedTypes);
    }

    [Fact]
    public void Reference_simulator_assembly_does_not_reference_runtime_host_or_external_packages()
    {
        var references = typeof(NeutralSimulatorProvider).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("GeesNO.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["GeesNO.Core.Contracts", "GeesNO.Core.Primitives"], references);
        Assert.DoesNotContain("GeesNO.Core.Runtime", references);
        Assert.DoesNotContain("GeesNO.Core.Host", references);
    }

    [Fact]
    public void Reference_simulator_source_stays_vendor_domain_ai_and_io_neutral()
    {
        var sourceText = ArchitectureSourceReader.ProjectSourceText("GeesNO.Reference.Simulator");
        var forbiddenFragments = new[]
        {
            "AutoCAD",
            "BIM",
            "Blender",
            "BricsCAD",
            "CAD",
            "Claude",
            "COM",
            "Console",
            "CYPE",
            "DateTime",
            "Directory.",
            "Environment",
            "File.",
            "HttpClient",
            "LLM",
            "MCP",
            "OpenAI",
            "Process",
            "Revit",
            "System.IO",
            "System.Net",
            "GeesNO.Core.Host",
            "GeesNO.Core.Runtime"
        };

        Assert.All(forbiddenFragments, fragment => Assert.DoesNotContain(
            fragment,
            sourceText,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Reference_simulator_does_not_create_sdk_mcp_ai_domain_cli_or_real_adapter_projects()
    {
        var forbiddenDirectories = new[]
        {
            @"src\GeesNO.Adapter",
            @"src\GeesNO.Adapter.BricsCAD",
            @"src\GeesNO.Adapter.Simulator",
            @"src\GeesNO.AI",
            @"src\GeesNO.Cli",
            @"src\GeesNO.Core.Hosting",
            @"src\GeesNO.Domain.Cad.Contracts",
            @"src\GeesNO.Domain.Cad.Runtime",
            @"src\GeesNO.Sdk",
            @"src\GeesNO.Transport.Mcp"
        };

        Assert.All(forbiddenDirectories, relativePath => Assert.False(
            Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, relativePath)),
            $"{relativePath} must not be created by the reference simulator."));
    }

    [Fact]
    public void Reference_simulator_public_members_do_not_expose_machine_or_exception_surface()
    {
        var publicTypes = typeof(NeutralSimulatorProvider).Assembly.GetExportedTypes();

        foreach (var type in publicTypes)
        {
            Assert.DoesNotContain(type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                member => member.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                          || member.Name.Contains("StackTrace", StringComparison.OrdinalIgnoreCase)
                          || member.Name.Contains("Exception", StringComparison.OrdinalIgnoreCase));
        }
    }
}
