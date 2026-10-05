using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Catalog;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.System;

namespace GeesNO.Core.Tests.Runtime.System;

public sealed class SystemPingTests
{
    [Fact]
    public void Manifest_has_exact_approved_identity_descriptor_and_support()
    {
        var provider = ProviderId.Parse("provider.system");
        var version = new VersionInfo(2, 3, 4);

        var manifest = SystemPingManifestFactory.Create(provider, version);

        Assert.Equal(provider, manifest.ProviderId);
        Assert.Equal(version, manifest.ProviderVersion);
        var entry = Assert.Single(manifest.Capabilities);
        Assert.Same(SystemPingOperation.ContractCapability, entry.Descriptor.Reference);
        Assert.Equal("system.ping", entry.Descriptor.Reference.Id.Value);
        Assert.Equal(new VersionInfo(1, 0, 0), entry.Descriptor.Reference.Version);
        Assert.Equal("System Ping", entry.Descriptor.Name);
        Assert.Contains("local typed Runtime execution", entry.Descriptor.Description, StringComparison.Ordinal);
        Assert.Equal(RiskLevel.ReadOnly, entry.Descriptor.RiskLevel);
        Assert.False(entry.Descriptor.HasSideEffects);
        Assert.Equal(ConfirmationPolicy.NotRequired, entry.Descriptor.ConfirmationPolicy);
        Assert.Equal(CapabilitySupportLevel.Native, entry.Support.SupportLevel);
        Assert.Equal(UndoSupportLevel.None, entry.Support.UndoSupportLevel);
        Assert.Empty(entry.Support.Restrictions);
        Assert.Null(entry.Support.UnavailableReason);
    }

    [Fact]
    public void Manifest_factory_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => SystemPingManifestFactory.Create(
            null!,
            new VersionInfo(1, 0, 0)));
        Assert.Throws<ArgumentNullException>(() => SystemPingManifestFactory.Create(
            ProviderId.Parse("provider.system"),
            null!));
    }

    [Fact]
    public async Task Tool_has_immutable_capability_and_returns_trimmed_runtime_identity()
    {
        var version = new VersionInfo(3, 2, 1);
        var tool = new SystemPingTool("  GeesNO Test  ", version);

        var result = await tool.ExecuteAsync(
            new SystemPingOperation(OperationId.New()),
            CancellationToken.None);

        Assert.Same(SystemPingOperation.ContractCapability, tool.Capability);
        Assert.True(result.IsSuccess);
        Assert.Equal("GeesNO Test", result.Value.RuntimeName);
        Assert.Equal(version, result.Value.RuntimeVersion);
        var property = typeof(SystemPingTool).GetProperty(nameof(SystemPingTool.Capability));
        Assert.NotNull(property);
        Assert.Null(property.SetMethod);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tool_rejects_invalid_runtime_name(string? runtimeName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new SystemPingTool(
            runtimeName!,
            new VersionInfo(1, 0, 0)));
    }

    [Fact]
    public void Tool_rejects_null_runtime_version()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemPingTool("Runtime", null!));
    }

    [Fact]
    public async Task Tool_rejects_null_operation_and_respects_direct_cancellation()
    {
        var tool = new SystemPingTool("Runtime", new VersionInfo(1, 0, 0));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            tool.ExecuteAsync(null!, CancellationToken.None));
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            tool.ExecuteAsync(new SystemPingOperation(OperationId.New()), source.Token));
    }

    [Fact]
    public async Task Explicit_end_to_end_ping_preserves_operation_identity()
    {
        var provider = ProviderId.Parse("provider.system");
        var operation = new SystemPingOperation(OperationId.New());
        var engine = BuildEngine((provider, "GeesNO Runtime"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation, provider),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal(operation.Id, result.OperationId);
        Assert.Same(SystemPingOperation.ContractCapability, result.Capability);
        Assert.Equal(provider, result.ProviderId);
        Assert.Equal(typeof(SystemPingResult), result.DeclaredResultType);
        var value = Assert.IsType<SystemPingResult>(result.SuccessfulValue);
        Assert.Equal("GeesNO Runtime", value.RuntimeName);
        Assert.Equal(new VersionInfo(1, 2, 3), value.RuntimeVersion);
    }

    [Fact]
    public async Task Implicit_unique_provider_executes_end_to_end_ping()
    {
        var provider = ProviderId.Parse("provider.system");
        var engine = BuildEngine((provider, "Only Runtime"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(new SystemPingOperation(OperationId.New())),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Equal(provider, result.ProviderId);
        Assert.Equal("Only Runtime", Assert.IsType<SystemPingResult>(result.SuccessfulValue).RuntimeName);
    }

    [Fact]
    public async Task Two_ping_providers_are_ambiguous_without_explicit_selection()
    {
        var engine = BuildEngine(
            (ProviderId.Parse("provider.a"), "Runtime A"),
            (ProviderId.Parse("provider.b"), "Runtime B"));

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(new SystemPingOperation(OperationId.New())),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Equal(RuntimeExecutionErrors.AmbiguousProviderCode, result.Error?.Code);
    }

    [Fact]
    public async Task Multiple_concurrent_pings_complete_successfully()
    {
        var provider = ProviderId.Parse("provider.system");
        var engine = BuildEngine((provider, "Concurrent Runtime"));
        var operations = Enumerable.Range(0, 40)
            .Select(_ => new SystemPingOperation(OperationId.New()))
            .ToArray();

        var results = await Task.WhenAll(operations.Select(operation => engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation, provider),
            CancellationToken.None)));

        Assert.All(results, result => Assert.Equal(RuntimeExecutionStatus.Success, result.Status));
        Assert.Equal(
            operations.Select(operation => operation.Id).OrderBy(id => id.ToString()),
            results.Select(result => result.OperationId).OrderBy(id => id.ToString()));
    }

    [Fact]
    public void Tool_source_contains_no_io_clock_environment_or_task_run()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "GeesNO.Core.Runtime",
            "System",
            "SystemPingTool.cs"));

        Assert.All(
            new[] { "Task.Run", "File.", "Directory.", "HttpClient", "DateTime", "Environment.", "CAD" },
            fragment => Assert.DoesNotContain(fragment, source, StringComparison.Ordinal));
    }

    private static ToolEngine BuildEngine(params (ProviderId ProviderId, string RuntimeName)[] providers)
    {
        var builder = new RuntimeCatalogBuilder();
        foreach (var provider in providers)
        {
            builder.RegisterManifest(SystemPingManifestFactory.Create(
                provider.ProviderId,
                new VersionInfo(1, 0, 0)));
            builder.RegisterTool(provider.ProviderId, new SystemPingTool(
                provider.RuntimeName,
                new VersionInfo(1, 2, 3)));
        }

        var engine = new ToolEngine(builder.Seal());
        engine.Start();
        return engine;
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
