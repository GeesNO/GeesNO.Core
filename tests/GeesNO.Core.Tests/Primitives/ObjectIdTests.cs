using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Primitives;

public sealed class ObjectIdTests
{
    [Fact]
    public void Constructor_accepts_non_empty_guid()
    {
        var value = Guid.Parse("2f824e0c-9e71-4dc9-a4fc-9c4c033d196f");

        var objectId = new ObjectId(value);

        Assert.Equal(value, objectId.Value);
        Assert.Equal(value.ToString("D"), objectId.ToString());
    }

    [Fact]
    public void Constructor_rejects_empty_guid()
    {
        Assert.Throws<ArgumentException>(() => new ObjectId(Guid.Empty));
    }

    [Fact]
    public void New_creates_non_empty_identifiers()
    {
        var first = ObjectId.New();
        var second = ObjectId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(Guid.Empty, second.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        var value = Guid.Parse("f29c50a9-a3e5-41fa-984f-41dd84979d62");
        var first = new ObjectId(value);
        var second = new ObjectId(value);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
