using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Primitives;

public sealed class ErrorTests
{
    [Fact]
    public void Constructor_normalizes_valid_values()
    {
        var error = new Error(" core.invalid ", " Invalid value. ");

        Assert.Equal("core.invalid", error.Code);
        Assert.Equal("Invalid value.", error.Message);
    }

    [Theory]
    [InlineData(null, "message")]
    [InlineData("", "message")]
    [InlineData(" ", "message")]
    [InlineData("code", null)]
    [InlineData("code", "")]
    [InlineData("code", " ")]
    public void Constructor_rejects_missing_values(string? code, string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code!, message!));
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        var first = new Error("core.invalid", "Invalid value.");
        var second = new Error("core.invalid", "Invalid value.");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
