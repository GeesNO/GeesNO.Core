namespace GeesNO.Core.Tests.Host;

public sealed class HostProjectTests
{
    [Fact]
    public void Host_project_exists_and_is_in_solution()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "GeesNO.Core.Host", "GeesNO.Core.Host.csproj");
        var solution = File.ReadAllText(Path.Combine(root, "GeesNO.Core.sln"));

        Assert.True(File.Exists(projectPath));
        Assert.Contains("GeesNO.Core.Host", solution, StringComparison.Ordinal);
        Assert.Contains(@"src\GeesNO.Core.Host\GeesNO.Core.Host.csproj", solution, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_project_shape_and_dependencies_are_approved()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(Path.Combine(
            root,
            "src",
            "GeesNO.Core.Host",
            "GeesNO.Core.Host.csproj"));

        Assert.Contains("<OutputType>Exe</OutputType>", project, StringComparison.Ordinal);
        Assert.Contains("<IsPackable>false</IsPackable>", project, StringComparison.Ordinal);
        Assert.Contains(@"..\GeesNO.Core.Runtime\GeesNO.Core.Runtime.csproj", project, StringComparison.Ordinal);
        Assert.Contains(@"..\GeesNO.Core.Contracts\GeesNO.Core.Contracts.csproj", project, StringComparison.Ordinal);
        Assert.Contains(@"..\GeesNO.Core.Primitives\GeesNO.Core.Primitives.csproj", project, StringComparison.Ordinal);
        Assert.Contains("PackageReference Include=\"Microsoft.Extensions.Hosting\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Hosting_library_does_not_exist_and_runtime_declares_no_packages()
    {
        var root = FindRepositoryRoot();
        var runtimeProject = File.ReadAllText(Path.Combine(
            root,
            "src",
            "GeesNO.Core.Runtime",
            "GeesNO.Core.Runtime.csproj"));

        Assert.False(Directory.Exists(Path.Combine(root, "src", "GeesNO.Core.Hosting")));
        Assert.DoesNotContain("PackageReference", runtimeProject, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Extensions.", runtimeProject, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GeesNO.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
