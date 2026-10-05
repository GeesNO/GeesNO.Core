using System.Collections;
using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Neutral;

public sealed class NeutralProfileContractTests
{
    [Fact]
    public void Capability_references_are_exact_and_versioned()
    {
        AssertCapability(NeutralCapabilities.Echo, "neutral.echo");
        AssertCapability(NeutralCapabilities.Describe, "neutral.describe");
        AssertCapability(NeutralCapabilities.Validate, "neutral.validate");

        Assert.Equal("neutral.echo@1.0.0", NeutralCapabilities.Echo.ToString());
        Assert.Equal("neutral.describe@1.0.0", NeutralCapabilities.Describe.ToString());
        Assert.Equal("neutral.validate@1.0.0", NeutralCapabilities.Validate.ToString());
    }

    [Fact]
    public void Descriptors_are_read_only_without_side_effects_or_confirmation()
    {
        AssertDescriptor(NeutralCapabilities.EchoDescriptor, NeutralCapabilities.Echo);
        AssertDescriptor(NeutralCapabilities.DescribeDescriptor, NeutralCapabilities.Describe);
        AssertDescriptor(NeutralCapabilities.ValidateDescriptor, NeutralCapabilities.Validate);
    }

    [Fact]
    public void Native_support_uses_no_undo_restrictions_or_unavailable_reason()
    {
        foreach (var capability in Capabilities())
        {
            var support = NeutralCapabilities.NativeSupport(capability);

            Assert.Same(capability, support.Capability);
            Assert.Equal(CapabilitySupportLevel.Native, support.SupportLevel);
            Assert.Equal(UndoSupportLevel.None, support.UndoSupportLevel);
            Assert.Empty(support.Restrictions);
            Assert.Null(support.UnavailableReason);
        }
    }

    [Fact]
    public void Manifest_entries_pair_descriptors_with_native_support()
    {
        AssertEntry(NeutralCapabilities.EchoEntry, NeutralCapabilities.EchoDescriptor);
        AssertEntry(NeutralCapabilities.DescribeEntry, NeutralCapabilities.DescribeDescriptor);
        AssertEntry(NeutralCapabilities.ValidateEntry, NeutralCapabilities.ValidateDescriptor);

        Assert.Equal(
            ["neutral.echo@1.0.0", "neutral.describe@1.0.0", "neutral.validate@1.0.0"],
            Entries().Select(entry => entry.Descriptor.Reference.ToString()));
    }

    [Fact]
    public void Neutral_support_rejects_capabilities_outside_the_profile()
    {
        var externalCapability = new CapabilityReference(
            CapabilityId.Parse("neutral.other"),
            new VersionInfo(1, 0, 0));

        Assert.Throws<ArgumentNullException>(() => NeutralCapabilities.NativeSupport(null!));
        Assert.Throws<ArgumentException>(() => NeutralCapabilities.NativeSupport(externalCapability));
    }

    [Fact]
    public void Operations_implement_their_typed_contracts_and_fixed_capabilities()
    {
        var echoId = OperationId.New();
        var describeId = OperationId.New();
        var validateId = OperationId.New();

        var echo = new NeutralEchoOperation(echoId, "hello");
        var describe = new NeutralDescribeOperation(describeId);
        var validate = new NeutralValidateOperation(validateId, "abc", maxLength: 10);

        Assert.IsAssignableFrom<IOperation<NeutralEchoResult>>(echo);
        Assert.IsAssignableFrom<IOperation<NeutralDescribeResult>>(describe);
        Assert.IsAssignableFrom<IOperation<NeutralValidationResult>>(validate);

        Assert.Same(echoId, echo.Id);
        Assert.Same(describeId, describe.Id);
        Assert.Same(validateId, validate.Id);
        Assert.Same(NeutralCapabilities.Echo, echo.Capability);
        Assert.Same(NeutralCapabilities.Describe, describe.Capability);
        Assert.Same(NeutralCapabilities.Validate, validate.Capability);
        Assert.Equal("hello", echo.Text);
        Assert.Equal("abc", validate.Input);
        Assert.Equal(10, validate.MaxLength);
    }

    [Fact]
    public void Operations_reject_null_or_invalid_values()
    {
        Assert.Throws<ArgumentNullException>(() => new NeutralEchoOperation(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => new NeutralEchoOperation(OperationId.New(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralEchoOperation(
            OperationId.New(),
            new string('a', NeutralEchoOperation.MaximumTextLength + 1)));

        Assert.Throws<ArgumentNullException>(() => new NeutralDescribeOperation(null!));

        Assert.Throws<ArgumentNullException>(() => new NeutralValidateOperation(null!, "input", 1));
        Assert.Throws<ArgumentNullException>(() => new NeutralValidateOperation(OperationId.New(), null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralValidateOperation(
            OperationId.New(),
            new string('a', NeutralValidateOperation.MaximumSupportedLength + 1),
            NeutralValidateOperation.MaximumSupportedLength));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralValidateOperation(OperationId.New(), "input", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralValidateOperation(
            OperationId.New(),
            "input",
            NeutralValidateOperation.MaximumSupportedLength + 1));
    }

    [Fact]
    public void Results_are_typed_bounded_and_sanitized()
    {
        var echo = new NeutralEchoResult("hello");
        var describe = new NeutralDescribeResult(" Provider ", " Capability ");

        Assert.Equal("hello", echo.Text);
        Assert.Equal("Provider", describe.ProviderDescription);
        Assert.Equal("Capability", describe.CapabilityDescription);

        Assert.Throws<ArgumentNullException>(() => new NeutralEchoResult(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralEchoResult(
            new string('a', NeutralEchoOperation.MaximumTextLength + 1)));
        Assert.ThrowsAny<ArgumentException>(() => new NeutralDescribeResult(null!, "Capability"));
        Assert.ThrowsAny<ArgumentException>(() => new NeutralDescribeResult("Provider", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralDescribeResult(
            new string('a', NeutralDescribeResult.MaximumDescriptionLength + 1),
            "Capability"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralDescribeResult(
            "Provider",
            new string('a', NeutralDescribeResult.MaximumDescriptionLength + 1)));
    }

    [Fact]
    public void Validation_result_enforces_reason_invariants()
    {
        var valid = new NeutralValidationResult(isValid: true, reason: null);
        var invalid = new NeutralValidationResult(isValid: false, reason: " Too long. ");

        Assert.True(valid.IsValid);
        Assert.Null(valid.Reason);
        Assert.False(invalid.IsValid);
        Assert.Equal("Too long.", invalid.Reason);

        Assert.Throws<ArgumentException>(() => new NeutralValidationResult(isValid: true, "Unexpected reason."));
        Assert.ThrowsAny<ArgumentException>(() => new NeutralValidationResult(isValid: false, null));
        Assert.ThrowsAny<ArgumentException>(() => new NeutralValidationResult(isValid: false, " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeutralValidationResult(
            isValid: false,
            new string('a', NeutralValidationResult.MaximumReasonLength + 1)));
    }

    [Fact]
    public void Neutral_public_surface_exposes_no_object_dictionary_exception_or_path_state()
    {
        var types = new[]
        {
            typeof(NeutralCapabilities),
            typeof(NeutralEchoOperation),
            typeof(NeutralEchoResult),
            typeof(NeutralDescribeOperation),
            typeof(NeutralDescribeResult),
            typeof(NeutralValidateOperation),
            typeof(NeutralValidationResult)
        };

        foreach (var type in types)
        {
            Assert.DoesNotContain(type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                member => member.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
                          || member.Name.Contains("StackTrace", StringComparison.OrdinalIgnoreCase)
                          || member.Name.Contains("Exception", StringComparison.OrdinalIgnoreCase));

            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                property =>
                {
                    Assert.Null(property.SetMethod);
                    Assert.False(IsForbiddenPayloadType(property.PropertyType), $"{property.DeclaringType?.FullName}.{property.Name}");
                });

            Assert.All(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                field => Assert.False(IsForbiddenPayloadType(field.FieldType), $"{field.DeclaringType?.FullName}.{field.Name}"));
        }
    }

    private static void AssertCapability(CapabilityReference reference, string expectedId)
    {
        Assert.Equal(expectedId, reference.Id.Value);
        Assert.Equal(new VersionInfo(1, 0, 0), reference.Version);
    }

    private static CapabilityReference[] Capabilities()
    {
        return
        [
            NeutralCapabilities.Echo,
            NeutralCapabilities.Describe,
            NeutralCapabilities.Validate
        ];
    }

    private static CapabilityManifestEntry[] Entries()
    {
        return
        [
            NeutralCapabilities.EchoEntry,
            NeutralCapabilities.DescribeEntry,
            NeutralCapabilities.ValidateEntry
        ];
    }

    private static void AssertDescriptor(CapabilityDescriptor descriptor, CapabilityReference reference)
    {
        Assert.Same(reference, descriptor.Reference);
        Assert.Equal(RiskLevel.ReadOnly, descriptor.RiskLevel);
        Assert.False(descriptor.HasSideEffects);
        Assert.Equal(ConfirmationPolicy.NotRequired, descriptor.ConfirmationPolicy);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Name));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Description));
    }

    private static void AssertEntry(CapabilityManifestEntry entry, CapabilityDescriptor descriptor)
    {
        Assert.Same(descriptor, entry.Descriptor);
        Assert.Equal(descriptor.Reference, entry.Support.Capability);
        Assert.Equal(CapabilitySupportLevel.Native, entry.Support.SupportLevel);
        Assert.Equal(UndoSupportLevel.None, entry.Support.UndoSupportLevel);
        Assert.Empty(entry.Support.Restrictions);
        Assert.Null(entry.Support.UnavailableReason);
    }

    private static bool IsForbiddenPayloadType(Type type)
    {
        if (type == typeof(object)
            || typeof(Exception).IsAssignableFrom(type)
            || typeof(IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        return type.IsGenericType
               && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }
}
