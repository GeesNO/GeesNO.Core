using System.Reflection;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.System;

public sealed class SystemPingContractTests
{
    [Fact]
    public void Operation_exposes_the_exact_reused_contract_capability()
    {
        var first = new SystemPingOperation(OperationId.New());
        var second = new SystemPingOperation(OperationId.New());

        Assert.Equal("system.ping", SystemPingOperation.ContractCapability.Id.Value);
        Assert.Equal(new VersionInfo(1, 0, 0), SystemPingOperation.ContractCapability.Version);
        Assert.Equal("system.ping@1.0.0", SystemPingOperation.ContractCapability.ToString());
        Assert.Same(SystemPingOperation.ContractCapability, first.Capability);
        Assert.Same(first.Capability, second.Capability);
    }

    [Fact]
    public void Operation_requires_only_an_id_and_does_not_configure_capability()
    {
        var constructors = typeof(SystemPingOperation).GetConstructors();
        var constructor = Assert.Single(constructors);
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(OperationId), parameter.ParameterType);
        Assert.Throws<ArgumentNullException>(() => new SystemPingOperation(null!));
        Assert.Null(typeof(SystemPingOperation).GetProperty(nameof(SystemPingOperation.Capability))?.SetMethod);
    }

    [Fact]
    public void Result_exposes_trimmed_runtime_identity()
    {
        var version = new VersionInfo(2, 1, 0);

        var result = new SystemPingResult(" GeesNO.Core ", version);

        Assert.Equal("GeesNO.Core", result.RuntimeName);
        Assert.Same(version, result.RuntimeVersion);
    }

    [Fact]
    public void Result_rejects_invalid_values()
    {
        var version = new VersionInfo(1, 0, 0);

        Assert.ThrowsAny<ArgumentException>(() => new SystemPingResult(null!, version));
        Assert.ThrowsAny<ArgumentException>(() => new SystemPingResult(" ", version));
        Assert.Throws<ArgumentNullException>(() => new SystemPingResult("GeesNO.Core", null!));
    }

    [Fact]
    public void Public_system_contract_has_no_clock_network_or_host_surface()
    {
        var publicMemberNames = new[] { typeof(SystemPingOperation), typeof(SystemPingResult) }
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(member => member.Name)
            .ToArray();

        Assert.DoesNotContain(publicMemberNames, name => name.Contains("Timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMemberNames, name => name.Contains("Latency", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMemberNames, name => name.Contains("Machine", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMemberNames, name => name.Contains("Network", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMemberNames, name => name.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }
}
