using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.System;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Runtime.System;

internal sealed class SystemPingTool : ITool<SystemPingOperation, SystemPingResult>
{
    private readonly string _runtimeName;
    private readonly VersionInfo _runtimeVersion;

    internal SystemPingTool(
        string runtimeName,
        VersionInfo runtimeVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeName);
        ArgumentNullException.ThrowIfNull(runtimeVersion);

        _runtimeName = runtimeName.Trim();
        _runtimeVersion = runtimeVersion;
    }

    public CapabilityReference Capability => SystemPingOperation.ContractCapability;

    public Task<Result<SystemPingResult>> ExecuteAsync(
        SystemPingOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Result<SystemPingResult>>(cancellationToken);
        }

        return Task.FromResult(Result<SystemPingResult>.Success(
            new SystemPingResult(_runtimeName, _runtimeVersion)));
    }
}
