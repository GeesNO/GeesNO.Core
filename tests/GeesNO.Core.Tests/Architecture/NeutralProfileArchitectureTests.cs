using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Runtime;

namespace GeesNO.Core.Tests.Architecture;

public sealed class NeutralProfileArchitectureTests
{
    private static readonly string[] ApprovedPublicRuntimeTypeNames =
    [
        "ExecutionOptions",
        "GeesNORuntime",
        "GeesNORuntimeBuilder",
        "GeesNORuntimeConfigurationException",
        "OperationExecutionResult`1",
        "OperationExecutionStatus",
        "RuntimeCapability"
    ];

    [Fact]
    public void Neutral_profile_adds_only_public_contract_types()
    {
        var exportedTypeNames = typeof(NeutralCapabilities).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "GeesNO.Core.Contracts.Neutral")
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "NeutralCapabilities",
                "NeutralDescribeOperation",
                "NeutralDescribeResult",
                "NeutralEchoOperation",
                "NeutralEchoResult",
                "NeutralValidateOperation",
                "NeutralValidationResult"
            ],
            exportedTypeNames);
    }

    [Fact]
    public void Runtime_public_surface_remains_the_current_allowlist()
    {
        var exportedTypeNames = typeof(GeesNORuntime).Assembly.GetExportedTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ApprovedPublicRuntimeTypeNames, exportedTypeNames);
    }

    [Fact]
    public void Solution_contains_exactly_seven_projects()
    {
        Assert.Equal(7, ArchitectureSolutionFile.ProjectPaths().Count);
    }

    [Fact]
    public void Neutral_profile_does_not_create_production_tools_sdk_mcp_cli_or_domain()
    {
        var forbiddenDirectories = new[]
        {
            @"src\GeesNO.Adapter",
            @"src\GeesNO.Adapter.Simulator",
            @"src\GeesNO.Adapter.BricsCAD",
            @"src\GeesNO.AI",
            @"src\GeesNO.Cli",
            @"src\GeesNO.Domain",
            @"src\GeesNO.Domain.Cad.Contracts",
            @"src\GeesNO.Domain.Cad.Runtime",
            @"src\GeesNO.Sdk",
            @"src\GeesNO.Transport.Mcp"
        };

        Assert.All(forbiddenDirectories, relativePath => Assert.False(
            Directory.Exists(Path.Combine(ArchitectureTestPaths.Root, relativePath)),
            $"{relativePath} must not be created by the neutral profile."));

        var productionSources = new[]
            {
                "GeesNO.Core.Primitives",
                "GeesNO.Core.Contracts",
                "GeesNO.Core.Runtime",
                "GeesNO.Core.Host"
            }
            .SelectMany(ArchitectureSourceReader.ProjectSources)
            .ToArray();
        var forbiddenTypeNames = new[]
        {
            "NeutralEchoTool",
            "NeutralDescribeTool",
            "NeutralValidateTool",
            "Simulator",
            "ReferenceAdapter"
        };

        Assert.All(productionSources, source => Assert.All(
            forbiddenTypeNames,
            fragment => Assert.DoesNotContain(fragment, source.Content, StringComparison.Ordinal)));
    }
}
