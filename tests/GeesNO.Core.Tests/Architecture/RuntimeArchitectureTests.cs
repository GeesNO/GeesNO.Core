using System.Reflection;
using System.Runtime.CompilerServices;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Architecture;

public sealed class RuntimeArchitectureTests
{
    private static readonly string[] ForbiddenDependencyFragments =
    [
        "GeesNO.Core.Host",
        "CAD",
        "BricsCAD",
        "COM",
        "LLM",
        "MCP",
        "System.Text.Json",
        "Newtonsoft.Json",
        "Microsoft.Extensions.Logging",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "OpenTelemetry"
    ];

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

    private static readonly string[] RequiredInternalTypeNames =
    [
        "ProviderCapabilityKey",
        "RegisteredTool",
        "RuntimeCatalogBuilder",
        "RuntimeCatalogSnapshot",
        "IToolInvoker",
        "ToolInvoker`2",
        "InternalInvocationResult",
        "InternalInvocationStatus",
        "ToolInvokerFactory",
        "RuntimeExceptionPolicy",
        "ToolEngine",
        "ToolEngineObservation",
        "ToolEngineState",
        "RuntimeExecutionRequest",
        "RuntimeExecutionResult",
        "RuntimeExecutionStatus",
        "SystemPingTool",
        "SystemPingManifestFactory",
        "RuntimeEvent",
        "OperationRuntimeEvent",
        "OperationReceivedEvent",
        "OperationResolvedEvent",
        "OperationValidatedEvent",
        "OperationStartedEvent",
        "OperationSucceededEvent",
        "OperationFailedEvent",
        "OperationCancelledEvent",
        "IRuntimeEventObserver",
        "InProcessEventBus",
        "ExecutionJournalEntry",
        "IExecutionJournalSink",
        "InMemoryExecutionJournal",
        "RuntimeObservability",
        "RuntimeObservabilityDiagnostics",
        "RuntimeObservabilityFailure",
        "RuntimeObserverFailure",
        "RuntimeInfrastructureFailure",
        "RuntimeInfrastructureComponent"
    ];

    private static readonly string[] ForbiddenTypeNames =
    [
        "IOperationExecutor",
        "IRuntime",
        "RuntimeFacade",
        "RuntimeBuilder",
        "IProviderSelector",
        "IPolicyEvaluator",
        "IOperationValidator",
        "IEventBus",
        "IExecutionJournal",
        "IRuntimeEvent",
        "RuntimeStartedEvent",
        "RuntimeStoppedEvent"
    ];

    private static readonly string[] ForbiddenSourceFragments =
    [
        "System.Reflection",
        "GetInterfaces",
        "MakeGenericType",
        "MakeGenericMethod",
        "MethodInfo",
        "Activator",
        ".Invoke("
    ];

    [Fact]
    public void Runtime_references_only_contracts_and_primitives_internally()
    {
        var references = RuntimeAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
        var internalReferences = references
            .Where(name => name.StartsWith("GeesNO.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["GeesNO.Core.Contracts", "GeesNO.Core.Primitives"], internalReferences);
        Assert.All(ForbiddenDependencyFragments, fragment => Assert.DoesNotContain(
            references,
            name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Runtime_exports_only_the_current_public_facade_and_contains_approved_internals()
    {
        var types = RuntimeAssembly.GetTypes();
        var typeNames = types.Select(type => type.Name).ToArray();
        var exportedTypeNames = RuntimeAssembly.GetExportedTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedPublicRuntimeTypeNames, exportedTypeNames);
        Assert.All(RuntimeAssembly.GetExportedTypes(), type => Assert.Equal("GeesNO.Core.Runtime", type.Namespace));
        Assert.All(RequiredInternalTypeNames, name => Assert.Contains(name, typeNames));
        Assert.All(types.Where(type => RequiredInternalTypeNames.Contains(type.Name, StringComparer.Ordinal)),
            type => Assert.True(type.IsNotPublic || type.IsNestedAssembly || type.IsNestedPrivate));
        Assert.DoesNotContain(types, type => ForbiddenTypeNames.Contains(type.Name, StringComparer.Ordinal));
        Assert.DoesNotContain(types, type => type.Namespace is not null &&
            (type.Namespace.EndsWith(".Policies", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Validation", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".DependencyInjection", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Hosting", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Serialization", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Middleware", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Persistence", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".Logging", StringComparison.Ordinal)
             || type.Namespace.EndsWith(".OpenTelemetry", StringComparison.Ordinal)));
    }

    [Fact]
    public void Runtime_grants_internal_access_only_to_the_test_and_host_assemblies()
    {
        var friendAssemblies = RuntimeAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["GeesNO.Core.Host", "GeesNO.Core.Tests"], friendAssemblies);
    }

    [Fact]
    public void Runtime_reflection_is_confined_to_the_invoker_factory()
    {
        var runtimeDirectory = ArchitectureTestPaths.ProjectDirectory("GeesNO.Core.Runtime");
        var sources = ArchitectureSourceReader.ProjectSources("GeesNO.Core.Runtime");
        var allowedReflectionSource = Path.Combine(
            runtimeDirectory,
            "Invokers",
            "ToolInvokerFactory.cs");

        foreach (var source in sources)
        {
            if (string.Equals(source.FullPath, allowedReflectionSource, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Assert.All(ForbiddenSourceFragments, fragment => Assert.DoesNotContain(
                fragment,
                source.Content,
                StringComparison.Ordinal));
        }

        var factory = ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Runtime",
            "Invokers",
            "ToolInvokerFactory.cs");
        Assert.Contains("GetInterfaces", factory, StringComparison.Ordinal);
        Assert.Contains("MakeGenericType", factory, StringComparison.Ordinal);
        Assert.Contains("Activator", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("MethodInfo.Invoke", factory, StringComparison.Ordinal);

        var typedInvoker = ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Runtime",
            "Invokers",
            "ToolInvoker.cs");
        Assert.All(ForbiddenSourceFragments, fragment => Assert.DoesNotContain(
            fragment,
            typedInvoker,
            StringComparison.Ordinal));

        foreach (var relativePath in new[]
                 {
                     Path.Combine("Execution", "ToolEngine.cs"),
                     Path.Combine("Execution", "RuntimeExecutionResult.cs"),
                     Path.Combine("System", "SystemPingTool.cs"),
                     Path.Combine("Catalog", "RuntimeCatalogSnapshot.cs")
                 })
        {
            var source = ArchitectureSourceReader.ReadProjectSource(
                "GeesNO.Core.Runtime",
                relativePath);
            Assert.All(ForbiddenSourceFragments, fragment => Assert.DoesNotContain(
                fragment,
                source,
                StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Runtime_uses_strong_capability_identity_instead_of_string_dispatch()
    {
        var authoredSource = ArchitectureSourceReader.ProjectSourceText("GeesNO.Core.Runtime");

        Assert.DoesNotContain("\"system.ping\"", authoredSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CapabilityId.Parse", authoredSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StringComparison", ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Runtime",
            "Execution",
            "ToolEngine.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Event_and_journal_models_exclude_private_payload_types()
    {
        var runtimeTypes = RuntimeAssembly.GetTypes();
        var modelTypes = runtimeTypes.Where(type =>
                typeof(GeesNO.Core.Runtime.Events.RuntimeEvent).IsAssignableFrom(type)
                || type == typeof(GeesNO.Core.Runtime.Journal.ExecutionJournalEntry))
            .ToArray();

        Assert.NotEmpty(modelTypes);
        foreach (var type in modelTypes)
        {
            var properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var constructors = type.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.DoesNotContain(properties, property => IsForbiddenPayloadType(property.PropertyType));
            Assert.DoesNotContain(constructors.SelectMany(constructor => constructor.GetParameters()),
                parameter => IsForbiddenPayloadType(parameter.ParameterType));
        }

        var diagnosticExceptionProperties = runtimeTypes
            .Where(type => type.Namespace == "GeesNO.Core.Runtime.Observability")
            .SelectMany(type => type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(property => typeof(Exception).IsAssignableFrom(property.PropertyType))
            .ToArray();

        Assert.NotEmpty(diagnosticExceptionProperties);
        Assert.All(diagnosticExceptionProperties, property => Assert.True(
            typeof(GeesNO.Core.Runtime.Observability.RuntimeObservabilityFailure)
                .IsAssignableFrom(property.DeclaringType)));
    }

    [Fact]
    public void ToolEngine_depends_only_on_the_approved_observability_aggregate()
    {
        var toolEngine = ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Runtime",
            "Execution",
            "ToolEngine.cs");
        var forbiddenObservabilityComponents = new[]
        {
            "InProcessEventBus",
            "IExecutionJournalSink",
            "InMemoryExecutionJournal",
            "RuntimeObserverFailure",
            "RuntimeInfrastructureFailure",
            "System.Text.Json",
            "Microsoft.Extensions.Logging",
            "IServiceProvider"
        };

        Assert.Contains("RuntimeObservability", toolEngine, StringComparison.Ordinal);
        Assert.All(forbiddenObservabilityComponents, reference => Assert.DoesNotContain(
            reference,
            toolEngine,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_project_declares_no_packages()
    {
        var project = ArchitectureProjectFile.Load("GeesNO.Core.Runtime");

        Assert.Empty(project.PackageReferences);
        Assert.Equal(2, project.ProjectReferences.Count);
    }

    [Fact]
    public void Solution_contains_the_current_projects_and_no_future_placeholders()
    {
        var expectedProjects = new[]
            {
                @"src\GeesNO.Core.Contracts\GeesNO.Core.Contracts.csproj",
                @"src\GeesNO.Core.Host\GeesNO.Core.Host.csproj",
                @"src\GeesNO.Core.Primitives\GeesNO.Core.Primitives.csproj",
                @"src\GeesNO.Core.Runtime\GeesNO.Core.Runtime.csproj",
                @"src\GeesNO.Reference.Simulator\GeesNO.Reference.Simulator.csproj",
                @"samples\GeesNO.Sample.Cli\GeesNO.Sample.Cli.csproj",
                @"tests\GeesNO.Core.Tests\GeesNO.Core.Tests.csproj"
            }
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var forbiddenDirectories = new[]
        {
            @"src\GeesNO.Core.Hosting",
            @"src\GeesNO.Sdk",
            @"src\GeesNO.Transport.Mcp",
            @"src\GeesNO.Domain.Cad.Contracts",
            @"src\GeesNO.Domain.Cad.Runtime",
            @"src\GeesNO.Adapter",
            @"src\GeesNO.Adapter.Simulator",
            @"src\GeesNO.Adapter.BricsCAD",
            @"src\GeesNO.AI",
            @"src\GeesNO.Cli"
        };

        Assert.Equal(expectedProjects, ArchitectureSolutionFile.ProjectPaths());
        Assert.All(forbiddenDirectories, relativePath => Assert.False(
            Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, relativePath)),
            $"{relativePath} must not be created in the Foundation snapshot."));
    }

    [Fact]
    public void Lower_layers_do_not_reference_runtime()
    {
        var contractReferences = typeof(CapabilityId).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);
        var primitiveReferences = typeof(ObjectId).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);

        Assert.DoesNotContain("GeesNO.Core.Runtime", contractReferences);
        Assert.DoesNotContain(primitiveReferences, reference =>
            reference.StartsWith("GeesNO.", StringComparison.Ordinal));
    }

    private static Assembly RuntimeAssembly => typeof(RuntimeCatalogBuilder).Assembly;

    private static bool IsForbiddenPayloadType(Type type)
    {
        if (type == typeof(object)
            || typeof(GeesNO.Core.Contracts.Operations.IOperation).IsAssignableFrom(type)
            || typeof(Exception).IsAssignableFrom(type)
            || typeof(System.Collections.IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        return type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }
}
