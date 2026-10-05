using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class CapabilityReferenceTests
{
    [Fact]
    public void Constructor_exposes_exact_id_and_version()
    {
        var id = CapabilityId.Parse("system.ping");
        var version = new VersionInfo(1, 2, 3);

        var reference = new CapabilityReference(id, version);

        Assert.Same(id, reference.Id);
        Assert.Same(version, reference.Version);
        Assert.Equal("system.ping@1.2.3", reference.ToString());
    }

    [Fact]
    public void Constructor_rejects_null_values()
    {
        var id = CapabilityId.Parse("system.ping");
        var version = new VersionInfo(1, 0, 0);

        Assert.Throws<ArgumentNullException>(() => new CapabilityReference(null!, version));
        Assert.Throws<ArgumentNullException>(() => new CapabilityReference(id, null!));
    }

    [Fact]
    public void Equality_and_hashing_include_id_and_version()
    {
        var first = Reference("system.ping", 1);
        var same = Reference("system.ping", 1);
        var differentId = Reference("system.health", 1);
        var differentVersion = Reference("system.ping", 2);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, differentId);
        Assert.NotEqual(first, differentVersion);
    }

    private static CapabilityReference Reference(string id, int major)
    {
        return new CapabilityReference(CapabilityId.Parse(id), new VersionInfo(major, 0, 0));
    }
}
