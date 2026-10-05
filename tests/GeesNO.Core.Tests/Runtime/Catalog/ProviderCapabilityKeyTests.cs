using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Runtime.Catalog;

public sealed class ProviderCapabilityKeyTests
{
    [Fact]
    public void Constructor_rejects_null_values()
    {
        Assert.Throws<ArgumentNullException>(() => new ProviderCapabilityKey(
            null!, RuntimeCatalogTestData.Reference()));
        Assert.Throws<ArgumentNullException>(() => new ProviderCapabilityKey(
            ProviderId.Parse("provider.a"), null!));
    }

    [Fact]
    public void Equality_and_hashing_are_structural()
    {
        var first = new ProviderCapabilityKey(
            ProviderId.Parse("provider.a"),
            RuntimeCatalogTestData.Reference());
        var second = new ProviderCapabilityKey(
            ProviderId.Parse("provider.a"),
            RuntimeCatalogTestData.Reference());

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_includes_provider_and_capability()
    {
        var baseline = new ProviderCapabilityKey(
            ProviderId.Parse("provider.a"),
            RuntimeCatalogTestData.Reference("system.ping"));

        Assert.NotEqual(
            baseline,
            new ProviderCapabilityKey(
                ProviderId.Parse("provider.b"),
                RuntimeCatalogTestData.Reference("system.ping")));
        Assert.NotEqual(
            baseline,
            new ProviderCapabilityKey(
                ProviderId.Parse("provider.a"),
                RuntimeCatalogTestData.Reference("system.health")));
        Assert.NotEqual(
            baseline,
            new ProviderCapabilityKey(
                ProviderId.Parse("provider.a"),
                RuntimeCatalogTestData.Reference("system.ping", major: 2)));
    }

    [Fact]
    public void ToString_is_diagnostic_and_unambiguous()
    {
        var key = new ProviderCapabilityKey(
            ProviderId.Parse("provider.a"),
            RuntimeCatalogTestData.Reference("system.ping"));

        Assert.Equal("provider.a::system.ping@1.0.0", key.ToString());
    }

    [Fact]
    public void Key_declares_no_parsing_or_conversion_API()
    {
        var methods = typeof(ProviderCapabilityKey).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        Assert.DoesNotContain(methods, method => method.Name is "Parse" or "TryParse");
        Assert.DoesNotContain(methods, method => method.Name is "op_Implicit" or "op_Explicit");
    }
}
