using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Runtime.Catalog;

public sealed class RuntimeCatalogValidationExceptionTests
{
    [Fact]
    public void Constructor_rejects_missing_issues()
    {
        Assert.Throws<ArgumentNullException>(() => new RuntimeCatalogValidationException(null!));
        Assert.Throws<ArgumentException>(() => new RuntimeCatalogValidationException([]));
        Assert.Throws<ArgumentException>(() => new RuntimeCatalogValidationException(["issue", " "]));
    }

    [Fact]
    public void Issues_are_sorted_copied_and_read_only()
    {
        var source = new List<string> { "runtime.z", "runtime.a" };
        var exception = new RuntimeCatalogValidationException(source);

        source.Add("runtime.extra");

        Assert.Equal(["runtime.a", "runtime.z"], exception.Issues);
        Assert.Contains("2 issue(s)", exception.Message, StringComparison.Ordinal);
        var exposed = Assert.IsAssignableFrom<IList<string>>(exception.Issues);
        Assert.Throws<NotSupportedException>(() => exposed.Add("runtime.other"));
    }
}
