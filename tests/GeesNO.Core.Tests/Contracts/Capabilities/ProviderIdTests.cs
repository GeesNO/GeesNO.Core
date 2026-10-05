using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class ProviderIdTests
{
    [Theory]
    [InlineData("system.ping")]
    [InlineData("cad.layer.create")]
    [InlineData("cad.geometry.line.create")]
    [InlineData("cad.document.get-active")]
    [InlineData("provider1.adapter-v2")]
    public void Parse_accepts_canonical_values(string value)
    {
        var providerId = ProviderId.Parse(value);

        Assert.Equal(value, providerId.Value);
        Assert.Equal(value, providerId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("provider")]
    [InlineData("Provider.Core")]
    [InlineData("provider..core")]
    [InlineData(".provider.core")]
    [InlineData("provider.core.")]
    [InlineData("provider_core")]
    [InlineData("provider.-core")]
    [InlineData("provider.core-")]
    [InlineData("provider.co--re")]
    [InlineData("proveïdér.core")]
    [InlineData("provider/core")]
    [InlineData("provider:core")]
    [InlineData(" provider.core")]
    [InlineData("provider.core ")]
    public void Parse_rejects_non_canonical_values(string value)
    {
        Assert.Throws<FormatException>(() => ProviderId.Parse(value));
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => ProviderId.Parse(null!));
    }

    [Fact]
    public void TryParse_returns_false_and_null_for_invalid_values()
    {
        Assert.False(ProviderId.TryParse(null, out var nullResult));
        Assert.Null(nullResult);

        Assert.False(ProviderId.TryParse("Provider.Core", out var invalidResult));
        Assert.Null(invalidResult);
    }

    [Fact]
    public void TryParse_returns_the_identifier_for_a_valid_value()
    {
        var success = ProviderId.TryParse("provider.core", out var providerId);

        Assert.True(success);
        Assert.NotNull(providerId);
        Assert.Equal("provider.core", providerId.Value);
    }

    [Fact]
    public void Parse_accepts_length_128_and_rejects_length_129()
    {
        var valid = $"a.{new string('b', 126)}";
        var invalid = $"a.{new string('b', 127)}";

        Assert.Equal(valid, ProviderId.Parse(valid).Value);
        Assert.Throws<FormatException>(() => ProviderId.Parse(invalid));
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        var first = ProviderId.Parse("provider.core");
        var second = ProviderId.Parse("provider.core");
        var different = ProviderId.Parse("provider.other");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void Type_declares_no_conversion_operators()
    {
        Assert.DoesNotContain(
            typeof(ProviderId).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name is "op_Implicit" or "op_Explicit");
    }
}
