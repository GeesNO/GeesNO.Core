using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Primitives;

public sealed class VersionInfoTests
{
    [Fact]
    public void Constructor_accepts_non_negative_components()
    {
        var version = new VersionInfo(1, 2, 3);

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal("1.2.3", version.ToString());
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Constructor_rejects_negative_components(int major, int minor, int patch)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionInfo(major, minor, patch));
    }

    [Fact]
    public void Equal_versions_have_equal_hash_codes()
    {
        var first = new VersionInfo(1, 2, 3);
        var second = new VersionInfo(1, 2, 3);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData(2, 0, 0, 1, 9, 9)]
    [InlineData(1, 2, 0, 1, 1, 9)]
    [InlineData(1, 2, 4, 1, 2, 3)]
    public void CompareTo_orders_by_major_minor_and_patch(
        int major,
        int minor,
        int patch,
        int otherMajor,
        int otherMinor,
        int otherPatch)
    {
        var version = new VersionInfo(major, minor, patch);
        var other = new VersionInfo(otherMajor, otherMinor, otherPatch);

        Assert.True(version.CompareTo(other) > 0);
    }
}
