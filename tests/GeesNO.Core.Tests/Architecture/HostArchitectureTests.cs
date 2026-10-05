using System.Reflection;
using GeesNO.Core.Host;

namespace GeesNO.Core.Tests.Architecture;

public sealed class HostArchitectureTests
{
    private static readonly string[] ForbiddenTypeNames =
    [
        "IRuntime",
        "IOperationExecutor",
        "RuntimeFacade",
        "RuntimeBuilder",
        "RuntimeExecutionResult",
        "RuntimeStartedEvent",
        "RuntimeStoppedEvent"
    ];

    private static readonly string[] ForbiddenSourceFragments =
    [
        "BricsCAD",
        "COM",
        "LLM",
        "MCP",
        "OpenTelemetry",
        "System.Text.Json",
        "Newtonsoft.Json",
        "RuntimeFacade",
        "IOperationExecutor",
        "RuntimeBuilder",
        "RuntimeExecutionResult",
        "RuntimeStartedEvent",
        "RuntimeStoppedEvent",
        "GetExecutableImplementations",
        "GetImplementations"
    ];

    [Fact]
    public void Host_exists_as_an_independent_executable_assembly()
    {
        Assert.Equal("GeesNO.Core.Host", HostAssembly.GetName().Name);
        Assert.Contains(HostAssembly.GetTypes(), type => type == typeof(Program));
        Assert.Equal([typeof(Program)], HostAssembly.GetExportedTypes());
    }

    [Fact]
    public void Host_references_runtime_contracts_primitives_and_hosting()
    {
        var references = HostAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Contains("GeesNO.Core.Runtime", references);
        Assert.Contains("GeesNO.Core.Contracts", references);
        Assert.Contains("GeesNO.Core.Primitives", references);
        Assert.Contains("Microsoft.Extensions.Hosting.Abstractions", references);
        Assert.DoesNotContain("GeesNO.Core.Hosting", references);
    }

    [Fact]
    public void Host_defines_no_runtime_facade_or_lifecycle_events()
    {
        var types = HostAssembly.GetTypes();

        Assert.DoesNotContain(types, type => ForbiddenTypeNames.Contains(type.Name, StringComparer.Ordinal));
        Assert.DoesNotContain(types, type => type.Namespace is not null
            && type.Namespace.Contains("GeesNO.Core.Hosting", StringComparison.Ordinal));
    }

    [Fact]
    public void Host_source_contains_no_forbidden_domains_or_alternative_provider_selection()
    {
        var authoredSource = ArchitectureSourceReader.ProjectSourceText("GeesNO.Core.Host");

        Assert.All(ForbiddenSourceFragments, fragment => Assert.DoesNotContain(
            fragment,
            authoredSource,
            StringComparison.Ordinal));
        Assert.Contains("SystemPingOperation", authoredSource, StringComparison.Ordinal);
        Assert.Contains("SystemPingTool", authoredSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeObservability", authoredSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependency_direction_keeps_lower_layers_independent_from_host()
    {
        var runtimeReferences = RuntimeAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);
        var contractReferences = ContractsAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);
        var primitiveReferences = PrimitivesAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);

        Assert.DoesNotContain("GeesNO.Core.Host", runtimeReferences);
        Assert.DoesNotContain("GeesNO.Core.Host", contractReferences);
        Assert.DoesNotContain("GeesNO.Core.Host", primitiveReferences);
        Assert.DoesNotContain("GeesNO.Core.Runtime", contractReferences);
        Assert.DoesNotContain("GeesNO.Core.Runtime", primitiveReferences);
    }

    private static Assembly HostAssembly => typeof(Program).Assembly;

    private static Assembly RuntimeAssembly => typeof(GeesNO.Core.Runtime.Catalog.RuntimeCatalogBuilder).Assembly;

    private static Assembly ContractsAssembly => typeof(GeesNO.Core.Contracts.Capabilities.CapabilityId).Assembly;

    private static Assembly PrimitivesAssembly => typeof(GeesNO.Core.Primitives.ObjectId).Assembly;

}
