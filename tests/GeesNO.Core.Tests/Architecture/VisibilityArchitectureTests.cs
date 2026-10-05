using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Host;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Architecture;

public sealed class VisibilityArchitectureTests
{
    private static readonly string[] ApprovedPublicRuntimeTypeNames =
    [
        "ExecutionOptions",
        "GeesNORuntime",
        "GeesNORuntimeBuilder",
        "GeesNORuntimeConfigurationException",
        "OperationExecutionResult`1",
        "OperationExecutionStatus",
        "RuntimeCapability"
    ];

    private static readonly string[] ForbiddenPublicRuntimeFacadeTypeNames =
    [
        "IRuntime",
        "IOperationExecutor",
        "RuntimeFacade",
        "RuntimeBuilder",
        "RuntimeExecutionResult",
        "RuntimeStartedEvent",
        "RuntimeStoppedEvent",
        "IRuntimeEvent",
        "IEventBus",
        "IExecutionJournal"
    ];

    [Fact]
    public void Runtime_exports_only_the_current_public_facade_types()
    {
        var exportedTypeNames = typeof(RuntimeCatalogBuilder).Assembly.GetExportedTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedPublicRuntimeTypeNames, exportedTypeNames);
    }

    [Fact]
    public void Core_assemblies_export_no_public_Runtime_facade_or_lifecycle_types()
    {
        var exportedTypes = CoreAssemblies()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .ToArray();

        Assert.DoesNotContain(exportedTypes, type =>
            ForbiddenPublicRuntimeFacadeTypeNames.Contains(type.Name, StringComparer.Ordinal));
        Assert.DoesNotContain(exportedTypes, type =>
            type.Namespace?.Contains("GeesNO.Core.Hosting", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Runtime_source_does_not_make_execution_or_observability_internals_public()
    {
        var runtimeSource = ArchitectureSourceReader.ProjectSourceText("GeesNO.Core.Runtime");
        var forbiddenPublicDeclarations = new[]
        {
            "public interface IRuntime",
            "public interface IOperationExecutor",
            "public sealed class RuntimeFacade",
            "public sealed class RuntimeBuilder",
            "public sealed class RuntimeExecutionResult",
            "public abstract class RuntimeEvent",
            "public sealed class InProcessEventBus",
            "public sealed class InMemoryExecutionJournal",
            "public interface IExecutionJournal",
            "public interface IRuntimeEvent",
            "public sealed class RuntimeStartedEvent",
            "public sealed class RuntimeStoppedEvent"
        };

        Assert.All(forbiddenPublicDeclarations, declaration => Assert.DoesNotContain(
            declaration,
            runtimeSource,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Host_public_surface_is_limited_to_process_entrypoint()
    {
        var exportedTypes = typeof(Program).Assembly.GetExportedTypes();

        Assert.Equal([typeof(Program)], exportedTypes);
    }

    private static Assembly[] CoreAssemblies()
    {
        return
        [
            typeof(ObjectId).Assembly,
            typeof(CapabilityId).Assembly,
            typeof(RuntimeCatalogBuilder).Assembly,
            typeof(Program).Assembly
        ];
    }
}
