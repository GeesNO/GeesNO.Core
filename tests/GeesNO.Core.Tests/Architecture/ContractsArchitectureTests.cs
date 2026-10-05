using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Tests.Architecture;

public sealed class ContractsArchitectureTests
{
    private static readonly string[] ForbiddenDependencyFragments =
    [
        "BricsCAD",
        "CAD",
        "LLM",
        "MCP",
        "COM",
        "GeesNO.Core.Runtime",
        "GeesNO.Core.Host",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "System.Text.Json",
        "Newtonsoft.Json"
    ];

    private static readonly string[] ForbiddenNamespaces =
    [
        "GeesNO.Core.Contracts.Events",
        "GeesNO.Core.Contracts.Journal",
        "GeesNO.Core.Contracts.Policies",
        "GeesNO.Core.Contracts.Validation"
    ];

    [Fact]
    public void Contracts_references_only_primitives_internally()
    {
        var references = ContractsAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
        var internalReferences = references
            .Where(name => name.StartsWith("GeesNO.", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(["GeesNO.Core.Primitives"], internalReferences);

        foreach (var fragment in ForbiddenDependencyFragments)
        {
            Assert.DoesNotContain(
                references,
                name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Contracts_contains_no_runtime_policy_or_validation_namespaces_or_types()
    {
        var types = ContractsAssembly.GetTypes();

        foreach (var forbiddenNamespace in ForbiddenNamespaces)
        {
            Assert.DoesNotContain(types, type =>
                string.Equals(type.Namespace, forbiddenNamespace, StringComparison.Ordinal)
                || (type.Namespace?.StartsWith($"{forbiddenNamespace}.", StringComparison.Ordinal) ?? false));
        }

        Assert.DoesNotContain(types, type => type.Name is "IToolRegistry" or "ICapabilityRegistry" or "IOperationExecutor");
        Assert.DoesNotContain(types, type => type.Name.Contains("Runtime", StringComparison.Ordinal));
        Assert.DoesNotContain(types, type => type.Name.Contains("Cad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Public_API_contains_no_forbidden_signature_types()
    {
        foreach (var type in ContractsAssembly.GetExportedTypes())
        {
            Assert.False(ContainsForbiddenType(type), $"Forbidden public type: {type}");

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                AssertParametersAreAllowed(constructor, constructor.GetParameters());
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                AssertAllowed(property.PropertyType, $"property {type.FullName}.{property.Name}");
                AssertNoDynamicAttribute(property.CustomAttributes, $"property {type.FullName}.{property.Name}");
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                AssertAllowed(field.FieldType, $"field {type.FullName}.{field.Name}");
                AssertNoDynamicAttribute(field.CustomAttributes, $"field {type.FullName}.{field.Name}");
            }

            foreach (var eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                AssertAllowed(eventInfo.EventHandlerType, $"event {type.FullName}.{eventInfo.Name}");
                AssertNoDynamicAttribute(eventInfo.CustomAttributes, $"event {type.FullName}.{eventInfo.Name}");
            }

            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.GetBaseDefinition().DeclaringType == typeof(object))
                {
                    continue;
                }

                AssertAllowed(method.ReturnType, $"return type of {type.FullName}.{method.Name}");
                AssertNoDynamicAttribute(method.ReturnParameter.CustomAttributes, $"return type of {type.FullName}.{method.Name}");
                AssertParametersAreAllowed(method, method.GetParameters());
            }
        }
    }

    [Fact]
    public void Strong_identifiers_declare_no_conversion_operators()
    {
        var identifierTypes = new[]
        {
            typeof(CapabilityId),
            typeof(ProviderId),
            typeof(global::GeesNO.Core.Contracts.Operations.OperationId)
        };

        Assert.All(identifierTypes, type => Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name is "op_Implicit" or "op_Explicit"));
    }

    [Fact]
    public void Capability_contracts_match_the_current_public_shape()
    {
        var descriptorConstructor = Assert.Single(typeof(CapabilityDescriptor).GetConstructors());
        var supportConstructor = Assert.Single(typeof(CapabilitySupport).GetConstructors());

        Assert.Equal(
            [
                typeof(CapabilityReference),
                typeof(string),
                typeof(string),
                typeof(RiskLevel),
                typeof(bool),
                typeof(ConfirmationPolicy)
            ],
            descriptorConstructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Null(typeof(CapabilityDescriptor).GetProperty("UndoSupportLevel"));

        Assert.Equal(
            [
                typeof(CapabilityReference),
                typeof(CapabilitySupportLevel),
                typeof(UndoSupportLevel),
                typeof(IEnumerable<CapabilityRestriction>),
                typeof(string)
            ],
            supportConstructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(
            typeof(UndoSupportLevel),
            typeof(CapabilitySupport).GetProperty("UndoSupportLevel")?.PropertyType);
    }

    [Fact]
    public void Exported_types_match_the_current_public_surface()
    {
        string[] expectedTypes =
        [
            "GeesNO.Core.Contracts.Capabilities.CapabilityDescriptor",
            "GeesNO.Core.Contracts.Capabilities.CapabilityId",
            "GeesNO.Core.Contracts.Capabilities.CapabilityManifest",
            "GeesNO.Core.Contracts.Capabilities.CapabilityManifestEntry",
            "GeesNO.Core.Contracts.Capabilities.CapabilityReference",
            "GeesNO.Core.Contracts.Capabilities.CapabilityRestriction",
            "GeesNO.Core.Contracts.Capabilities.CapabilitySupport",
            "GeesNO.Core.Contracts.Capabilities.CapabilitySupportLevel",
            "GeesNO.Core.Contracts.Capabilities.ConfirmationPolicy",
            "GeesNO.Core.Contracts.Capabilities.ProviderId",
            "GeesNO.Core.Contracts.Capabilities.RiskLevel",
            "GeesNO.Core.Contracts.Capabilities.UndoSupportLevel",
            "GeesNO.Core.Contracts.Neutral.NeutralCapabilities",
            "GeesNO.Core.Contracts.Neutral.NeutralDescribeOperation",
            "GeesNO.Core.Contracts.Neutral.NeutralDescribeResult",
            "GeesNO.Core.Contracts.Neutral.NeutralEchoOperation",
            "GeesNO.Core.Contracts.Neutral.NeutralEchoResult",
            "GeesNO.Core.Contracts.Neutral.NeutralValidateOperation",
            "GeesNO.Core.Contracts.Neutral.NeutralValidationResult",
            "GeesNO.Core.Contracts.Operations.IOperation",
            "GeesNO.Core.Contracts.Operations.IOperation`1",
            "GeesNO.Core.Contracts.Operations.OperationId",
            "GeesNO.Core.Contracts.System.SystemPingOperation",
            "GeesNO.Core.Contracts.System.SystemPingResult",
            "GeesNO.Core.Contracts.Tools.ITool",
            "GeesNO.Core.Contracts.Tools.ITool`2"
        ];
        var actualTypes = ContractsAssembly.GetExportedTypes()
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(expectedTypes.OrderBy(name => name, StringComparer.Ordinal), actualTypes);
    }

    private static Assembly ContractsAssembly => typeof(CapabilityId).Assembly;

    private static void AssertParametersAreAllowed(MethodBase member, IEnumerable<ParameterInfo> parameters)
    {
        foreach (var parameter in parameters)
        {
            AssertAllowed(parameter.ParameterType, $"parameter {member.DeclaringType?.FullName}.{member.Name}:{parameter.Name}");
            AssertNoDynamicAttribute(
                parameter.CustomAttributes,
                $"parameter {member.DeclaringType?.FullName}.{member.Name}:{parameter.Name}");
        }
    }

    private static void AssertAllowed(Type? type, string location)
    {
        Assert.False(ContainsForbiddenType(type), $"Forbidden signature type at {location}: {type}");
    }

    private static void AssertNoDynamicAttribute(IEnumerable<CustomAttributeData> attributes, string location)
    {
        Assert.DoesNotContain(
            attributes,
            attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.DynamicAttribute");
    }

    private static bool ContainsForbiddenType(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.IsByRef || type.IsPointer || type.IsArray)
        {
            return ContainsForbiddenType(type.GetElementType());
        }

        if (type == typeof(object)
            || type == typeof(IServiceProvider)
            || type == typeof(ValueTask)
            || type.FullName == "System.Text.Json.JsonElement")
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        if (type.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return true;
        }

        return type.GetGenericArguments().Any(ContainsForbiddenType);
    }
}
