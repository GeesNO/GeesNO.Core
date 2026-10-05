using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Architecture;

public sealed class PrimitivesDependencyTests
{
    private static readonly string[] ForbiddenDependencyFragments =
    [
        "BricsCAD",
        "CAD",
        "LLM",
        "MCP"
    ];

    [Fact]
    public void Primitives_has_no_internal_or_forbidden_dependencies()
    {
        var references = typeof(ObjectId).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            references,
            name => name.StartsWith("GeesNO.", StringComparison.Ordinal));

        foreach (var fragment in ForbiddenDependencyFragments)
        {
            Assert.DoesNotContain(
                references,
                name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
