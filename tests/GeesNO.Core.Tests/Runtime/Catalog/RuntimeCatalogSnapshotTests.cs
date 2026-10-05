using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Catalog;

public sealed class RuntimeCatalogSnapshotTests
{
    [Fact]
    public void Manifests_and_implementations_are_ordered_deterministically()
    {
        var context = CreateSnapshot();

        Assert.Equal(
            ["provider.a", "provider.m", "provider.z"],
            context.Snapshot.Manifests.Select(manifest => manifest.ProviderId.Value));
        Assert.Equal(
            ["provider.a", "provider.m", "provider.z"],
            context.Snapshot.GetImplementations(context.Reference)
                .Select(key => key.ProviderId.Value));
        Assert.Equal(
            ["provider.a"],
            context.Snapshot.GetExecutableImplementations(context.Reference)
                .Select(key => key.ProviderId.Value));
    }

    [Fact]
    public void Snapshot_supports_manifest_entry_and_tool_lookups()
    {
        var context = CreateSnapshot();
        var provider = ProviderId.Parse("provider.a");
        var key = new ProviderCapabilityKey(provider, context.Reference);

        Assert.True(context.Snapshot.TryGetManifest(provider, out var manifest));
        Assert.Equal(provider, manifest.ProviderId);
        Assert.True(context.Snapshot.TryGetEntry(key, out var entry));
        Assert.Equal(context.Reference, entry.Descriptor.Reference);
        Assert.True(context.Snapshot.TryGetTool(key, out var tool));
        Assert.Same(context.Tool, tool);
        Assert.True(context.Snapshot.TryGetInvoker(key, out var firstInvoker));
        Assert.True(context.Snapshot.TryGetInvoker(key, out var secondInvoker));
        Assert.Same(firstInvoker, secondInvoker);
        Assert.Equal(key, firstInvoker.Key);
    }

    [Fact]
    public void Canonical_descriptor_comes_from_lowest_ordinal_provider_after_validation()
    {
        var context = CreateSnapshot();

        Assert.True(context.Snapshot.TryGetCanonicalDescriptor(
            context.Reference,
            out var descriptor));
        Assert.Same(context.CanonicalDescriptor, descriptor);
    }

    [Fact]
    public void Missing_references_return_false_and_empty_collections()
    {
        var context = CreateSnapshot();
        var missing = RuntimeCatalogTestData.Reference("system.missing");
        var key = new ProviderCapabilityKey(ProviderId.Parse("provider.a"), missing);

        Assert.False(context.Snapshot.TryGetManifest(ProviderId.Parse("provider.missing"), out _));
        Assert.False(context.Snapshot.TryGetEntry(key, out _));
        Assert.False(context.Snapshot.TryGetTool(key, out _));
        Assert.False(context.Snapshot.TryGetInvoker(key, out _));
        Assert.False(context.Snapshot.TryGetCanonicalDescriptor(missing, out _));
        Assert.Empty(context.Snapshot.GetImplementations(missing));
        Assert.Empty(context.Snapshot.GetExecutableImplementations(missing));
    }

    [Fact]
    public void Exposed_collections_are_read_only()
    {
        var context = CreateSnapshot();
        var manifests = Assert.IsAssignableFrom<IList<CapabilityManifest>>(context.Snapshot.Manifests);
        var implementations = Assert.IsAssignableFrom<IList<ProviderCapabilityKey>>(
            context.Snapshot.GetImplementations(context.Reference));
        var missing = Assert.IsAssignableFrom<IList<ProviderCapabilityKey>>(
            context.Snapshot.GetImplementations(RuntimeCatalogTestData.Reference("system.missing")));

        Assert.Throws<NotSupportedException>(() => manifests.Add(
            RuntimeCatalogTestData.Manifest("provider.other", [])));
        Assert.Throws<NotSupportedException>(() => implementations.Add(
            new ProviderCapabilityKey(
                ProviderId.Parse("provider.other"),
                context.Reference)));
        Assert.Throws<NotSupportedException>(() => missing.Add(
            new ProviderCapabilityKey(
                ProviderId.Parse("provider.other"),
                context.Reference)));
    }

    [Fact]
    public void Snapshot_does_not_change_when_manifest_source_collection_changes()
    {
        var reference = RuntimeCatalogTestData.Reference();
        var entries = new List<CapabilityManifestEntry>
        {
            RuntimeCatalogTestData.Entry(reference, CapabilitySupportLevel.Unsupported)
        };
        var manifest = RuntimeCatalogTestData.Manifest("provider.a", entries);
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(manifest);
        var snapshot = builder.Seal();

        entries.Add(RuntimeCatalogTestData.Entry(
            RuntimeCatalogTestData.Reference("system.health"),
            CapabilitySupportLevel.Unsupported));

        Assert.Single(snapshot.Manifests);
        Assert.Single(snapshot.Manifests[0].Capabilities);
        Assert.Single(snapshot.GetImplementations(reference));
    }

    [Fact]
    public void Different_providers_retain_distinct_invokers_for_the_same_capability()
    {
        var reference = RuntimeCatalogTestData.Reference();
        var providerA = ProviderId.Parse("provider.a");
        var providerB = ProviderId.Parse("provider.b");
        var keyA = new ProviderCapabilityKey(providerA, reference);
        var keyB = new ProviderCapabilityKey(providerB, reference);
        var builder = new RuntimeCatalogBuilder();
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerA.Value, [RuntimeCatalogTestData.Entry(reference)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            providerB.Value, [RuntimeCatalogTestData.Entry(reference)]));
        builder.RegisterTool(providerA, new CatalogTestTool(reference));
        builder.RegisterTool(providerB, new CatalogTestTool(reference));

        var snapshot = builder.Seal();

        Assert.True(snapshot.TryGetInvoker(keyA, out var invokerA));
        Assert.True(snapshot.TryGetInvoker(keyB, out var invokerB));
        Assert.NotSame(invokerA, invokerB);
        Assert.Equal(keyA, invokerA.Key);
        Assert.Equal(keyB, invokerB.Key);
    }

    [Fact]
    public void Snapshot_copies_the_exact_registered_invoker_without_recreating_it()
    {
        var reference = RuntimeCatalogTestData.Reference();
        var provider = ProviderId.Parse("provider.a");
        var entry = RuntimeCatalogTestData.Entry(reference);
        var manifest = RuntimeCatalogTestData.Manifest(provider.Value, [entry]);
        var key = new ProviderCapabilityKey(provider, reference);
        var tool = new CatalogTestTool(reference);
        var invoker = ToolInvokerFactory.Create(key, tool);
        var registered = new RegisteredTool(key, entry, tool, invoker);
        var snapshot = new RuntimeCatalogSnapshot(
            [manifest],
            new Dictionary<ProviderCapabilityKey, RegisteredTool> { [key] = registered });

        Assert.True(snapshot.TryGetInvoker(key, out var stored));
        Assert.Same(invoker, stored);
    }

    private static SnapshotContext CreateSnapshot()
    {
        var reference = RuntimeCatalogTestData.Reference("system.update");
        var canonicalDescriptor = RuntimeCatalogTestData.Descriptor(reference, hasSideEffects: true);
        var middleDescriptor = RuntimeCatalogTestData.Descriptor(reference, hasSideEffects: true);
        var lastDescriptor = RuntimeCatalogTestData.Descriptor(reference, hasSideEffects: true);
        var tool = new CatalogTestTool(reference);
        var builder = new RuntimeCatalogBuilder();

        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.z",
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Unsupported,
                descriptor: lastDescriptor)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.a",
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Native,
                UndoSupportLevel.NativeUndo,
                canonicalDescriptor)]));
        builder.RegisterManifest(RuntimeCatalogTestData.Manifest(
            "provider.m",
            [RuntimeCatalogTestData.Entry(
                reference,
                CapabilitySupportLevel.Unavailable,
                descriptor: middleDescriptor)]));
        builder.RegisterTool(ProviderId.Parse("provider.a"), tool);

        return new SnapshotContext(builder.Seal(), reference, canonicalDescriptor, tool);
    }

    private sealed record SnapshotContext(
        RuntimeCatalogSnapshot Snapshot,
        CapabilityReference Reference,
        CapabilityDescriptor CanonicalDescriptor,
        ITool Tool);

}
