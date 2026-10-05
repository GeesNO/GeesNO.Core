using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class CapabilityManifestTests
{
    [Fact]
    public void Entry_accepts_descriptor_without_side_effects_and_without_undo()
    {
        var descriptor = Descriptor(Reference("system.ping", 1), hasSideEffects: false);
        var support = Support(descriptor.Reference, UndoSupportLevel.None);

        var entry = new CapabilityManifestEntry(descriptor, support);

        Assert.Same(descriptor, entry.Descriptor);
        Assert.Same(support, entry.Support);
    }

    [Fact]
    public void Entry_rejects_descriptor_without_side_effects_and_with_undo()
    {
        var descriptor = Descriptor(Reference("system.ping", 1), hasSideEffects: false);
        var support = Support(descriptor.Reference, UndoSupportLevel.NativeUndo);

        Assert.Throws<ArgumentException>(() => new CapabilityManifestEntry(descriptor, support));
    }

    [Fact]
    public void Entry_accepts_descriptor_with_side_effects_and_without_undo()
    {
        var descriptor = Descriptor(Reference("system.update", 1), hasSideEffects: true);
        var support = Support(descriptor.Reference, UndoSupportLevel.None);

        var entry = new CapabilityManifestEntry(descriptor, support);

        Assert.Same(support, entry.Support);
    }

    [Fact]
    public void Entry_accepts_descriptor_with_side_effects_and_native_undo()
    {
        var descriptor = Descriptor(Reference("system.update", 1), hasSideEffects: true);
        var support = Support(descriptor.Reference, UndoSupportLevel.NativeUndo);

        var entry = new CapabilityManifestEntry(descriptor, support);

        Assert.Equal(UndoSupportLevel.NativeUndo, entry.Support.UndoSupportLevel);
    }

    [Fact]
    public void Entry_rejects_null_or_inconsistent_values()
    {
        var descriptor = Descriptor(Reference("system.ping", 1), hasSideEffects: false);
        var support = Support(descriptor.Reference, UndoSupportLevel.None);

        Assert.Throws<ArgumentNullException>(() => new CapabilityManifestEntry(null!, support));
        Assert.Throws<ArgumentNullException>(() => new CapabilityManifestEntry(descriptor, null!));
        Assert.Throws<ArgumentException>(() => new CapabilityManifestEntry(
            descriptor,
            Support(Reference("system.health", 1), UndoSupportLevel.None)));
    }

    [Fact]
    public void Entry_uses_reference_equality()
    {
        var descriptor = Descriptor(Reference("system.ping", 1), hasSideEffects: false);
        var support = Support(descriptor.Reference, UndoSupportLevel.None);

        Assert.NotEqual(
            new CapabilityManifestEntry(descriptor, support),
            new CapabilityManifestEntry(descriptor, support));
        Assert.DoesNotContain(
            typeof(CapabilityManifestEntry).GetInterfaces(),
            type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }

    [Fact]
    public void Manifests_allow_provider_specific_undo_for_one_universal_descriptor()
    {
        var reference = Reference("system.update", 1);
        var descriptor = Descriptor(reference, hasSideEffects: true);
        var providerA = new CapabilityManifest(
            ProviderId.Parse("provider.a"),
            new VersionInfo(1, 0, 0),
            [new CapabilityManifestEntry(descriptor, Support(reference, UndoSupportLevel.NativeUndo))]);
        var providerB = new CapabilityManifest(
            ProviderId.Parse("provider.b"),
            new VersionInfo(1, 0, 0),
            [new CapabilityManifestEntry(descriptor, Support(reference, UndoSupportLevel.CompensatingAction))]);

        Assert.Same(descriptor, Assert.Single(providerA.Capabilities).Descriptor);
        Assert.Same(descriptor, Assert.Single(providerB.Capabilities).Descriptor);
        Assert.Equal(UndoSupportLevel.NativeUndo, providerA.Capabilities[0].Support.UndoSupportLevel);
        Assert.Equal(UndoSupportLevel.CompensatingAction, providerB.Capabilities[0].Support.UndoSupportLevel);
    }

    [Fact]
    public void Manifest_allows_empty_capabilities()
    {
        var providerId = ProviderId.Parse("geesno.core");
        var providerVersion = new VersionInfo(1, 0, 0);

        var manifest = new CapabilityManifest(providerId, providerVersion, []);

        Assert.Same(providerId, manifest.ProviderId);
        Assert.Same(providerVersion, manifest.ProviderVersion);
        Assert.Empty(manifest.Capabilities);
    }

    [Fact]
    public void Manifest_rejects_null_inputs_and_elements()
    {
        var providerId = ProviderId.Parse("geesno.core");
        var version = new VersionInfo(1, 0, 0);

        Assert.Throws<ArgumentNullException>(() => new CapabilityManifest(null!, version, []));
        Assert.Throws<ArgumentNullException>(() => new CapabilityManifest(providerId, null!, []));
        Assert.Throws<ArgumentNullException>(() => new CapabilityManifest(providerId, version, null!));
        Assert.Throws<ArgumentException>(() => new CapabilityManifest(providerId, version, [null!]));
    }

    [Fact]
    public void Manifest_rejects_duplicate_capability_references()
    {
        var reference = Reference("system.ping", 1);

        Assert.Throws<ArgumentException>(() => new CapabilityManifest(
            ProviderId.Parse("geesno.core"),
            new VersionInfo(1, 0, 0),
            [Entry(reference), Entry(Reference("system.ping", 1))]));
    }

    [Fact]
    public void Manifest_orders_by_id_then_version()
    {
        var entries = new List<CapabilityManifestEntry>
        {
            Entry(Reference("system.ping", 2)),
            Entry(Reference("cad.layer", 1)),
            Entry(Reference("system.ping", 1))
        };

        var manifest = new CapabilityManifest(
            ProviderId.Parse("geesno.core"),
            new VersionInfo(1, 0, 0),
            entries);

        Assert.Equal(
            ["cad.layer@1.0.0", "system.ping@1.0.0", "system.ping@2.0.0"],
            manifest.Capabilities.Select(entry => entry.Descriptor.Reference.ToString()));
    }

    [Fact]
    public void Capabilities_are_defensively_copied_and_exposed_read_only()
    {
        var entries = new List<CapabilityManifestEntry> { Entry(Reference("system.ping", 1)) };
        var manifest = new CapabilityManifest(
            ProviderId.Parse("geesno.core"),
            new VersionInfo(1, 0, 0),
            entries);

        entries.Add(Entry(Reference("system.health", 1)));

        Assert.Single(manifest.Capabilities);
        var exposed = Assert.IsAssignableFrom<IList<CapabilityManifestEntry>>(manifest.Capabilities);
        Assert.Throws<NotSupportedException>(() => exposed.Add(Entry(Reference("system.other", 1))));
    }

    private static CapabilityManifestEntry Entry(CapabilityReference reference)
    {
        var descriptor = Descriptor(reference, hasSideEffects: false);
        return new CapabilityManifestEntry(descriptor, Support(reference, UndoSupportLevel.None));
    }

    private static CapabilityDescriptor Descriptor(
        CapabilityReference reference,
        bool hasSideEffects)
    {
        return new CapabilityDescriptor(
            reference,
            "Capability",
            "Capability description",
            hasSideEffects ? RiskLevel.Low : RiskLevel.ReadOnly,
            hasSideEffects,
            hasSideEffects ? ConfirmationPolicy.RiskBased : ConfirmationPolicy.NotRequired);
    }

    private static CapabilitySupport Support(
        CapabilityReference reference,
        UndoSupportLevel undoSupportLevel)
    {
        return new CapabilitySupport(
            reference,
            CapabilitySupportLevel.Native,
            undoSupportLevel,
            [],
            null);
    }

    private static CapabilityReference Reference(string id, int major)
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(major, 0, 0));
    }
}
