using System.Reflection;
using System.Text.RegularExpressions;
using GeesNO.Core.Tests.Architecture;
using GeesNO.Sample.Cli;

namespace GeesNO.Core.Tests.Sample.Cli;

public sealed class SampleCliArchitectureTests
{
    [Fact]
    public void Sample_cli_project_is_under_samples_and_uses_non_core_namespace()
    {
        var project = ArchitectureProjectFile.Load("GeesNO.Sample.Cli");
        var expectedPath = Path.Combine("samples", "GeesNO.Sample.Cli", "GeesNO.Sample.Cli.csproj");

        Assert.EndsWith(expectedPath, project.FullPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("GeesNO.Sample.Cli", project.PropertyValue("AssemblyName"));
        Assert.Equal("GeesNO.Sample.Cli", project.PropertyValue("RootNamespace"));

        var exportedTypes = SampleAssembly.GetExportedTypes();
        Assert.NotEmpty(exportedTypes);
        Assert.All(exportedTypes, type =>
        {
            Assert.StartsWith("GeesNO.Sample.Cli", type.Namespace, StringComparison.Ordinal);
            Assert.False(type.Namespace?.StartsWith("GeesNO.Core.", StringComparison.Ordinal) == true);
        });
    }

    [Fact]
    public void Sample_cli_references_only_public_runtime_contracts_and_reference_simulator()
    {
        var project = ArchitectureProjectFile.Load("GeesNO.Sample.Cli");
        var references = project.ProjectReferences
            .Select(reference => reference.Replace('\\', '/'))
            .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(
            [
                "../../src/GeesNO.Core.Contracts/GeesNO.Core.Contracts.csproj",
                "../../src/GeesNO.Core.Runtime/GeesNO.Core.Runtime.csproj",
                "../../src/GeesNO.Reference.Simulator/GeesNO.Reference.Simulator.csproj"
            ],
            references);
        Assert.Empty(project.PackageReferences);

        var geesnoReferences = SampleAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("GeesNO.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("GeesNO.Core.Contracts", geesnoReferences);
        Assert.Contains("GeesNO.Core.Runtime", geesnoReferences);
        Assert.Contains("GeesNO.Reference.Simulator", geesnoReferences);
        Assert.DoesNotContain("GeesNO.Core.Host", geesnoReferences);
    }

    [Fact]
    public void Sample_cli_uses_no_runtime_or_host_internals()
    {
        var source = ArchitectureSourceReader.ProjectSourceText("GeesNO.Sample.Cli");
        var forbiddenFragments = new[]
        {
            "GeesNO.Core.Runtime.Catalog",
            "GeesNO.Core.Runtime.Execution",
            "GeesNO.Core.Runtime.Invokers",
            "ToolEngine",
            "RuntimeCatalogBuilder",
            "RuntimeCatalogSnapshot",
            "RuntimeExecutionRequest",
            "RuntimeExecutionResult",
            "RuntimeObservability",
            "InProcessEventBus",
            "InMemoryExecutionJournal",
            "SystemPingTool",
            "SystemPingManifestFactory",
            "RegisteredTool",
            "ToolInvokerFactory",
            "IToolInvoker",
            "InternalInvocationResult",
            "ProviderCapabilityKey",
            "GeesNO.Core.Host",
            "InternalsVisibleTo",
            "dynamic",
            "MethodInfo.Invoke",
            "Activator"
        };

        Assert.All(forbiddenFragments, fragment => Assert.DoesNotContain(fragment, source, StringComparison.Ordinal));
    }

    [Fact]
    public void Sample_cli_contains_no_disallowed_product_or_vendor_dependencies()
    {
        var source = ArchitectureSourceReader.ProjectSourceText("GeesNO.Sample.Cli");
        var project = ArchitectureProjectFile.Load("GeesNO.Sample.Cli");
        var combined = string.Join(Environment.NewLine, source, project.Text);
        var forbiddenFragments = new[]
        {
            "Microsoft.Extensions",
            "System.CommandLine",
            "Spectre.Console",
            "Newtonsoft.Json",
            "OpenTelemetry",
            "MCP",
            "LLM",
            "CAD",
            "BIM",
            "3D",
            "cloud",
            "COM"
        };

        Assert.All(forbiddenFragments, fragment => Assert.False(
            ContainsForbiddenReference(combined, fragment),
            $"Sample CLI must not reference {fragment}."));
    }

    private static bool ContainsForbiddenReference(string content, string fragment)
    {
        if (fragment.Contains('.', StringComparison.Ordinal) || fragment == "cloud")
        {
            return content.Contains(fragment, StringComparison.OrdinalIgnoreCase);
        }

        return Regex.IsMatch(
            content,
            $@"(?<![A-Za-z0-9]){Regex.Escape(fragment)}(?![A-Za-z0-9])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static Assembly SampleAssembly => typeof(CliApplication).Assembly;
}