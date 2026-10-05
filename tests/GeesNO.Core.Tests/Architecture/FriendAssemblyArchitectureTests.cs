using System.Reflection;
using System.Runtime.CompilerServices;
using GeesNO.Core.Host;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Architecture;

public sealed class FriendAssemblyArchitectureTests
{
    [Fact]
    public void Runtime_friend_assemblies_are_exactly_Tests_and_Host()
    {
        var friendAssemblies = FriendAssemblies(typeof(RuntimeCatalogBuilder).Assembly);

        Assert.Equal(
            ["GeesNO.Core.Host", "GeesNO.Core.Tests"],
            friendAssemblies.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void Host_friend_assemblies_are_limited_to_Tests()
    {
        var friendAssemblies = FriendAssemblies(typeof(Program).Assembly);

        Assert.Equal(["GeesNO.Core.Tests"], friendAssemblies);
    }

    [Fact]
    public void Friend_assembly_source_uses_only_approved_names()
    {
        var runtimeAssemblyInfo = ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Runtime",
            "Properties",
            "AssemblyInfo.cs");
        var hostAssemblyInfo = ArchitectureSourceReader.ReadProjectSource(
            "GeesNO.Core.Host",
            "Properties",
            "AssemblyInfo.cs");

        Assert.Contains("[assembly: InternalsVisibleTo(\"GeesNO.Core.Tests\")]", runtimeAssemblyInfo, StringComparison.Ordinal);
        Assert.Contains("[assembly: InternalsVisibleTo(\"GeesNO.Core.Host\")]", runtimeAssemblyInfo, StringComparison.Ordinal);
        Assert.Contains("[assembly: InternalsVisibleTo(\"GeesNO.Core.Tests\")]", hostAssemblyInfo, StringComparison.Ordinal);
        Assert.DoesNotContain("GeesNO.Core.Hosting", runtimeAssemblyInfo, StringComparison.Ordinal);
        Assert.DoesNotContain("GeesNO.Core.Runtime", hostAssemblyInfo, StringComparison.Ordinal);
    }

    private static string[] FriendAssemblies(Assembly assembly)
    {
        return assembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => attribute.AssemblyName)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToArray();
    }
}
