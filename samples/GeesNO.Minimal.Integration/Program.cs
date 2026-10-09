using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime;

var providerId = MinimalIntegrationProvider.Id;
var greetingTool = new GreetingTool();

using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(MinimalIntegrationProvider.CreateManifest())
    .RegisterTool(providerId, greetingTool)
    .Build();

Console.WriteLine("sample=geesno-minimal-integration");
WriteCapabilities(runtime);

var positiveOperation = new GreetingOperation(
    OperationId.New(),
    "external developer");

var positive = await runtime.ExecuteAsync(
    positiveOperation,
    new ExecutionOptions(providerId));

Require(positive.Status == OperationExecutionStatus.Success, "Positive execution should report Success.");
Require(positive.IsSuccess, "Positive execution should be successful.");
Require(positive.Value is not null, "Positive execution should return a value.");
Require(positive.Value!.Message == "Hello, external developer.", "Positive execution returned an unexpected value.");
Require(greetingTool.ExecutionCount == 1, "Greeting tool should execute exactly once for the positive path.");

Console.WriteLine($"positive=passed status={positive.Status}");
WriteResult("positive", positive);
Console.WriteLine($"positive.result.message={positive.Value.Message}");
Console.WriteLine($"positive.tool.executions={greetingTool.ExecutionCount}");

var executionsBeforeFailClosed = greetingTool.ExecutionCount;
var rejected = await runtime.ExecuteAsync(
    new UnsupportedOperation(OperationId.New()),
    new ExecutionOptions(providerId));

Require(rejected.Status == OperationExecutionStatus.Rejected, "Fail-closed execution should report Rejected.");
Require(!rejected.IsSuccess, "Fail-closed execution should not be successful.");
Require(rejected.Error?.Code == "runtime.capability-not-found", "Fail-closed execution returned an unexpected error code.");
Require(greetingTool.ExecutionCount == executionsBeforeFailClosed, "Fail-closed execution must not invoke the greeting tool.");

Console.WriteLine($"fail_closed=passed status={rejected.Status} error={rejected.Error!.Code}");
WriteResult("fail_closed", rejected);
Console.WriteLine($"fail_closed.tool.executions={greetingTool.ExecutionCount}");
Console.WriteLine("sample.result=passed");

static void WriteCapabilities(GeesNORuntime runtime)
{
    Console.WriteLine($"capability.count={runtime.Capabilities.Count}");

    for (var index = 0; index < runtime.Capabilities.Count; index++)
    {
        var capability = runtime.Capabilities[index];
        Console.WriteLine($"capability.{index}.providerId={capability.ProviderId}");
        Console.WriteLine($"capability.{index}.reference={capability.Descriptor.Reference}");
        Console.WriteLine($"capability.{index}.support={capability.Support.SupportLevel}");
    }
}

static void WriteResult<TResult>(
    string prefix,
    OperationExecutionResult<TResult> result)
    where TResult : notnull
{
    Console.WriteLine($"{prefix}.operationId={result.OperationId}");
    Console.WriteLine($"{prefix}.capability={result.Capability}");
    Console.WriteLine($"{prefix}.providerId={FormatProvider(result.ProviderId)}");
    Console.WriteLine($"{prefix}.status={result.Status}");
    Console.WriteLine($"{prefix}.declaredResultType={FormatType(result.DeclaredResultType)}");

    if (result.Error is not null)
    {
        Console.WriteLine($"{prefix}.error.code={result.Error.Code}");
    }
}

static string FormatProvider(ProviderId? providerId)
{
    return providerId?.Value ?? "<none>";
}

static string FormatType(Type? type)
{
    return type?.FullName ?? "<none>";
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static class MinimalIntegrationCapabilities
{
    public static CapabilityReference Greeting { get; } = new(
        CapabilityId.Parse("sample.greeting"),
        new VersionInfo(1, 0, 0));

    public static CapabilityDescriptor GreetingDescriptor { get; } = new(
        Greeting,
        "Sample Greeting",
        "Returns a deterministic greeting from a minimal external tool.",
        RiskLevel.ReadOnly,
        hasSideEffects: false,
        ConfirmationPolicy.NotRequired);

    public static CapabilityManifestEntry GreetingEntry { get; } = new(
        GreetingDescriptor,
        new CapabilitySupport(
            Greeting,
            CapabilitySupportLevel.Native,
            UndoSupportLevel.None,
            Array.Empty<CapabilityRestriction>(),
            unavailableReason: null));

    public static CapabilityReference Unsupported { get; } = new(
        CapabilityId.Parse("sample.unsupported"),
        new VersionInfo(1, 0, 0));
}

static class MinimalIntegrationProvider
{
    public static ProviderId Id { get; } = ProviderId.Parse("sample.minimal-provider");

    public static VersionInfo Version { get; } = new(1, 0, 0);

    public static CapabilityManifest CreateManifest()
    {
        return new CapabilityManifest(
            Id,
            Version,
            new[] { MinimalIntegrationCapabilities.GreetingEntry });
    }
}

sealed record GreetingOperation(OperationId Id, string Name) : IOperation<GreetingResult>
{
    public CapabilityReference Capability => MinimalIntegrationCapabilities.Greeting;
}

sealed record GreetingResult(string Message);

sealed record UnsupportedOperation(OperationId Id) : IOperation<UnsupportedResult>
{
    public CapabilityReference Capability => MinimalIntegrationCapabilities.Unsupported;
}

sealed record UnsupportedResult(string Message);

sealed class GreetingTool : ITool<GreetingOperation, GreetingResult>
{
    public int ExecutionCount { get; private set; }

    public CapabilityReference Capability => MinimalIntegrationCapabilities.Greeting;

    public Task<Result<GreetingResult>> ExecuteAsync(
        GreetingOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        ExecutionCount++;

        if (string.IsNullOrWhiteSpace(operation.Name))
        {
            return Task.FromResult(Result<GreetingResult>.Failure(
                new Error("sample.greeting.empty-name", "Name is required.")));
        }

        return Task.FromResult(Result<GreetingResult>.Success(
            new GreetingResult($"Hello, {operation.Name}.")));
    }
}
