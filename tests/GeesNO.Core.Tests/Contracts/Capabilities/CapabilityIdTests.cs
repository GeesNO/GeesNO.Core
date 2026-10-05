using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class CapabilityIdTests
{
    [Theory]
    [InlineData("system.ping")]
    [InlineData("cad.layer.create")]
    [InlineData("cad.geometry.line.create")]
    [InlineData("cad.document.get-active")]
    [InlineData("provider1.adapter-v2")]
    public void Parse_accepts_canonical_values(string value)
    {
        var capabilityId = CapabilityId.Parse(value);

        Assert.Equal(value, capabilityId.Value);
        Assert.Equal(value, capabilityId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("system")]
    [InlineData("System.Ping")]
    [InlineData("system..ping")]
    [InlineData(".system.ping")]
    [InlineData("system.ping.")]
    [InlineData("system_ping")]
    [InlineData("system.-ping")]
    [InlineData("system.ping-")]
    [InlineData("system.p--ing")]
    [InlineData("cad.láyer.create")]
    [InlineData("cad/layer.create")]
    [InlineData("cad:layer.create")]
    [InlineData(" system.ping")]
    [InlineData("system.ping ")]
    public void Parse_rejects_non_canonical_values(string value)
    {
        Assert.Throws<FormatException>(() => CapabilityId.Parse(value));
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => CapabilityId.Parse(null!));
    }

    [Fact]
    public void TryParse_returns_false_and_null_for_invalid_values()
    {
        Assert.False(CapabilityId.TryParse(null, out var nullResult));
        Assert.Null(nullResult);

        Assert.False(CapabilityId.TryParse("System.Ping", out var invalidResult));
        Assert.Null(invalidResult);
    }

    [Fact]
    public void TryParse_returns_the_identifier_for_a_valid_value()
    {
        var success = CapabilityId.TryParse("system.ping", out var capabilityId);

        Assert.True(success);
        Assert.NotNull(capabilityId);
        Assert.Equal("system.ping", capabilityId.Value);
    }

    [Fact]
    public void Parse_accepts_length_128_and_rejects_length_129()
    {
        var valid = $"a.{new string('b', 126)}";
        var invalid = $"a.{new string('b', 127)}";

        Assert.Equal(128, valid.Length);
        Assert.Equal(valid, CapabilityId.Parse(valid).Value);
        Assert.Equal(129, invalid.Length);
        Assert.Throws<FormatException>(() => CapabilityId.Parse(invalid));
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        var first = CapabilityId.Parse("system.ping");
        var second = CapabilityId.Parse("system.ping");
        var different = CapabilityId.Parse("system.health");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void Type_declares_no_conversion_operators()
    {
        Assert.DoesNotContain(
            typeof(CapabilityId).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name is "op_Implicit" or "op_Explicit");
    }
}
