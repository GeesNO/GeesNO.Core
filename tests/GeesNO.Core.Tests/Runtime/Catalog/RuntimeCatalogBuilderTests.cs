using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Runtime.Catalog;

public sealed class RuntimeCatalogBuilderTests
{
    [Fact]
    public void Build_state_contains_only_building_and_sealed()
    {
        Assert.Equal(["Building", "Sealed"], Enum.GetNames<RuntimeCatalogBuildState>());
        Assert.Equal(RuntimeCatalogBuildState.Building, new RuntimeCatalogBuilder().State);
    }

    [Fact]
    public void RegisterManifest_accepts_valid_and_empty_manifests()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var populated = RuntimeCatalogTestData.Manifest(
            "provider.a",
            [RuntimeCatalogTestData.Entry(reference, CapabilitySupportLevel.Unsupported)]);
        var empty = RuntimeCatalogTestData.Manifest("provider.b", []);

        builder.RegisterManifest(populated);
        builder.RegisterManifest(empty);
        var snapshot = builder.Seal();

        Assert.Equal(RuntimeCatalogBuildState.Sealed, builder.State);
        Assert.Equal(2, snapshot.Manifests.Count);
    }

    [Fact]
    public void RegisterManifest_rejects_null_and_duplicate_provider()
    {
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest("provider.a", []));

        Assert.Throws<ArgumentNullException>(() => builder.RegisterManifest(null!));
        Assert.Throws<ArgumentException>(() => builder.RegisterManifest(
            RuntimeCatalogTestData.Manifest("provider.a", [])));
        Assert.Throws<ArgumentException>(() => builder.RegisterManifest(
            RuntimeCatalogTestData.Manifest("provider.a", [], versionMajor: 2)));
    }

    [Fact]
    public void Different_providers_can_declare_the_same_capability_with_equivalent_descriptors()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var firstDescriptor = RuntimeCatalogTestData.Descriptor(reference);
        var secondDescriptor = RuntimeCatalogTestData.Descriptor(RuntimeCatalogTestData.Reference());

        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.a",
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Unsupported,
                descriptor: firstDescriptor)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.b",
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Unsupported,
                descriptor: secondDescriptor)]));

        var snapshot = builder.Seal();

        Assert.Equal(2, snapshot.GetImplementations(reference).Count);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("risk")]
    [InlineData("effects")]
    [InlineData("confirmation")]
    public void Seal_rejects_each_universal_descriptor_difference(string difference)
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var first = RuntimeCatalogTestData.Descriptor(
            reference,
            riskLevel: RiskLevel.Low,
            confirmationPolicy: ConfirmationPolicy.RiskBased);
        var second = DifferentDescriptor(reference, difference);

        builder.RegisterManifest(UnsupportedManifest("provider.a", first));
        builder.RegisterManifest(UnsupportedManifest("provider.b", second));

        var exception = Assert.Throws<RuntimeCatalogValidationException>(() => builder.Seal());

        Assert.Contains(exception.Issues, issue =>
            issue.StartsWith("runtime.descriptor-incompatible:", StringComparison.Ordinal));
        Assert.Equal(RuntimeCatalogBuildState.Building, builder.State);
    }

    [Fact]
    public void Provider_specific_support_and_undo_do_not_affect_descriptor_compatibility()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference("system.update");
        var descriptorA = RuntimeCatalogTestData.Descriptor(reference, hasSideEffects: true);
        var descriptorB = RuntimeCatalogTestData.Descriptor(reference, hasSideEffects: true);
        var providerA = ProviderId.Parse("provider.a");
        var providerB = ProviderId.Parse("provider.b");

        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerA.Value,
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Native,
                UndoSupportLevel.NativeUndo,
                descriptorA)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerB.Value,
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Partial,
                UndoSupportLevel.CompensatingAction,
                descriptorB)]));
        builder.RegisterTool(providerA, new CatalogTestTool(reference));
        builder.RegisterTool(providerB, new CatalogTestTool(reference));

        Assert.Equal(2, builder.Seal().GetExecutableImplementations(reference).Count);
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Native)]
    [InlineData(CapabilitySupportLevel.Partial)]
    [InlineData(CapabilitySupportLevel.Emulated)]
    public void RegisterTool_accepts_executable_support(CapabilitySupportLevel supportLevel)
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value,
            [RuntimeCatalogTestData.Entry(reference, supportLevel)]));

        builder.RegisterTool(provider, new CatalogTestTool(reference));

        Assert.True(builder.Seal().TryGetTool(
            new ProviderCapabilityKey(provider, reference),
            out _));
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Unsupported)]
    [InlineData(CapabilitySupportLevel.Unavailable)]
    public void RegisterTool_rejects_nonexecutable_support(CapabilitySupportLevel supportLevel)
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value,
            [RuntimeCatalogTestData.Entry(reference, supportLevel)]));

        Assert.Throws<ArgumentException>(() =>
            builder.RegisterTool(provider, new CatalogTestTool(reference)));
    }

    [Fact]
    public void RegisterTool_rejects_nulls_unknown_provider_and_null_capability()
    {
        var builder = new RuntimeCatalogBuilder();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(provider.Value, []));

        Assert.Throws<ArgumentNullException>(() => builder.RegisterTool(null!, new CatalogTestTool(
            RuntimeCatalogTestData.Reference())));
        Assert.Throws<ArgumentNullException>(() => builder.RegisterTool(provider, null!));
        Assert.Throws<InvalidOperationException>(() => builder.RegisterTool(
            ProviderId.Parse("provider.missing"),
            new CatalogTestTool(RuntimeCatalogTestData.Reference())));
        Assert.Throws<ArgumentException>(() => builder.RegisterTool(provider, new CatalogTestTool(null!)));
    }

    [Fact]
    public void RegisterTool_rejects_capability_absent_from_manifest_and_duplicate_key()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value,
            [RuntimeCatalogTestData.Entry(reference)]));

        Assert.Throws<ArgumentException>(() => builder.RegisterTool(
            provider,
            new CatalogTestTool(RuntimeCatalogTestData.Reference("system.health"))));

        builder.RegisterTool(provider, new CatalogTestTool(reference));
        Assert.Throws<ArgumentException>(() =>
            builder.RegisterTool(provider, new CatalogTestTool(reference)));
    }

    [Fact]
    public void Same_capability_can_have_one_tool_per_provider()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var providerA = ProviderId.Parse("provider.a");
        var providerB = ProviderId.Parse("provider.b");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerA.Value, [RuntimeCatalogTestData.Entry(reference)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerB.Value, [RuntimeCatalogTestData.Entry(reference)]));

        builder.RegisterTool(providerA, new CatalogTestTool(reference));
        builder.RegisterTool(providerB, new CatalogTestTool(reference));

        Assert.Equal(2, builder.Seal().GetExecutableImplementations(reference).Count);
    }

    [Fact]
    public void Seal_detects_tool_capability_changed_after_registration()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        var tool = new MutableCatalogTestTool(reference);
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value, [RuntimeCatalogTestData.Entry(reference)]));
        builder.RegisterTool(provider, tool);

        tool.ChangeCapability(RuntimeCatalogTestData.Reference("system.health"));
        var exception = Assert.Throws<RuntimeCatalogValidationException>(() => builder.Seal());

        Assert.Contains(exception.Issues, issue =>
            issue.StartsWith("runtime.tool-capability-changed:", StringComparison.Ordinal));
    }

    [Fact]
    public void Seal_accumulates_missing_tools_in_deterministic_order()
    {
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.z",
            [RuntimeCatalogTestData.Entry(RuntimeCatalogTestData.Reference("system.zeta"))]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.a",
            [RuntimeCatalogTestData.Entry(RuntimeCatalogTestData.Reference("system.alpha"))]));

        var exception = Assert.Throws<RuntimeCatalogValidationException>(() => builder.Seal());

        Assert.Equal(2, exception.Issues.Count);
        Assert.Equal(
            exception.Issues.OrderBy(issue => issue, StringComparer.Ordinal),
            exception.Issues);
        Assert.Equal(RuntimeCatalogBuildState.Building, builder.State);
    }

    [Fact]
    public void Failed_seal_can_be_corrected_and_retried_without_partial_snapshot()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value, [RuntimeCatalogTestData.Entry(reference)]));

        Assert.Throws<RuntimeCatalogValidationException>(() => builder.Seal());
        Assert.Equal(RuntimeCatalogBuildState.Building, builder.State);

        builder.RegisterTool(provider, new CatalogTestTool(reference));
        var snapshot = builder.Seal();

        Assert.Equal(RuntimeCatalogBuildState.Sealed, builder.State);
        Assert.True(snapshot.TryGetTool(new ProviderCapabilityKey(provider, reference), out _));
    }

    [Fact]
    public void Successful_seal_is_idempotent_and_rejects_later_mutations()
    {
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest("provider.a", []));

        var first = builder.Seal();
        var second = builder.Seal();

        Assert.Same(first, second);
        var manifestException = Assert.Throws<InvalidOperationException>(() =>
            builder.RegisterManifest(RuntimeCatalogTestData.Manifest("provider.b", [])));
        var toolException = Assert.Throws<InvalidOperationException>(() =>
            builder.RegisterTool(ProviderId.Parse("provider.a"), new CatalogTestTool(
                RuntimeCatalogTestData.Reference())));
        Assert.Contains("runtime.registry-sealed", manifestException.Message, StringComparison.Ordinal);
        Assert.Contains("runtime.registry-sealed", toolException.Message, StringComparison.Ordinal);
    }

    private static CapabilityManifest UnsupportedManifest(
        string providerId,
        CapabilityDescriptor descriptor)
    {
        return RuntimeCatalogTestData.Manifest(
            providerId,
            [RuntimeCatalogTestData.Entry(
                descriptor.Reference,
                CapabilitySupportLevel.Unsupported,
                descriptor: descriptor)]);
    }

    private static CapabilityDescriptor DifferentDescriptor(
        CapabilityReference reference,
        string difference)
    {
        return difference switch
        {
            "name" => RuntimeCatalogTestData.Descriptor(
                reference, name: "Other", riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "description" => RuntimeCatalogTestData.Descriptor(
                reference, description: "Other", riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "risk" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.High,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "effects" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.Low, hasSideEffects: true,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "confirmation" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.NotRequired),
            _ => throw new InvalidOperationException()
        };
    }

    [Fact]
    public void Contract_discovery_failure_is_atomic_and_registration_can_be_retried()
    {
        var builder = new RuntimeCatalogBuilder();
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            provider.Value, [RuntimeCatalogTestData.Entry(reference)]));

        var exception = Assert.Throws<ArgumentException>(() =>
            builder.RegisterTool(provider, new BaseOnlyTestTool(reference)));

        Assert.Contains("runtime.tool-contract-mismatch", exception.Message, StringComparison.Ordinal);
        builder.RegisterTool(provider, new CatalogTestTool(reference));
        Assert.True(builder.Seal().TryGetInvoker(
            new ProviderCapabilityKey(provider, reference),
            out _));
    }

    private sealed class BaseOnlyTestTool : ITool
    {
        internal BaseOnlyTestTool(CapabilityReference capability)
        {
            Capability = capability;
        }

        public CapabilityReference Capability { get; }
    }
}
