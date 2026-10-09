# Quick Start

Status: Developer Preview integration guide.

This guide starts from the point of view of an external developer who receives
only the public packages and documentation. It does not require repository
internals, `GeesNO.Reference.Simulator`, Host internals, sample code, or project
references.

The package version used here is:

```text
0.1.0-preview.1
```

The packages are prepared locally but are not published yet. The NuGet commands
below become valid after `0.1.0-preview.1` is published.

## Requirements

- .NET 8 SDK.
- A project that can reference prerelease NuGet packages.
- Public GeesNO.Core packages after publication.

## Choose Packages

For an executable Runtime consumer:

```bat
dotnet add package GeesNO.Core.Runtime --version 0.1.0-preview.1
```

`GeesNO.Core.Runtime` depends on `GeesNO.Core.Contracts` and
`GeesNO.Core.Primitives`, so you do not need to add all three packages directly
for a normal executable consumer.

For a library that defines operation profiles, manifests, or tools without
building a Runtime:

```bat
dotnet add package GeesNO.Core.Contracts --version 0.1.0-preview.1
```

Install `GeesNO.Core.Primitives` directly only when your package intentionally
uses primitives without Contracts or Runtime:

```bat
dotnet add package GeesNO.Core.Primitives --version 0.1.0-preview.1
```

## Minimal Executable Example

Create a .NET 8 console project:

```bat
dotnet new console --framework net8.0
dotnet add package GeesNO.Core.Runtime --version 0.1.0-preview.1
```

Use this `Program.cs`:

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime;

var providerId = ProviderId.Parse("example.echo-provider");

using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(new CapabilityManifest(
        providerId,
        new VersionInfo(1, 0, 0),
        new[] { NeutralCapabilities.EchoEntry }))
    .RegisterTool(providerId, new EchoTool())
    .Build();

var operation = new NeutralEchoOperation(
    OperationId.New(),
    "hello from an external provider");

var result = await runtime.ExecuteAsync(
    operation,
    new ExecutionOptions(providerId));

if (result.IsSuccess)
{
    Console.WriteLine(result.Value!.Text);
}
else
{
    Console.WriteLine($"{result.Status}: {result.Error?.Code} - {result.Error?.Message}");
}

internal sealed class EchoTool : ITool<NeutralEchoOperation, NeutralEchoResult>
{
    public CapabilityReference Capability => NeutralCapabilities.Echo;

    public Task<Result<NeutralEchoResult>> ExecuteAsync(
        NeutralEchoOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(operation.Text))
        {
            return Task.FromResult(Result<NeutralEchoResult>.Failure(
                new Error("example.echo.empty-text", "Text is required.")));
        }

        return Task.FromResult(Result<NeutralEchoResult>.Success(
            new NeutralEchoResult(operation.Text)));
    }
}
```

Run it:

```bat
dotnet run
```

Expected output:

```text
hello from an external provider
```

## What The Example Does

- `ProviderId.Parse("example.echo-provider")` creates a canonical provider
  identifier.
- `CapabilityManifest` declares that the provider exposes
  `neutral.echo@1.0.0`.
- `EchoTool` implements `ITool<NeutralEchoOperation, NeutralEchoResult>` and
  returns `Result<NeutralEchoResult>`.
- `GeesNORuntimeBuilder` registers the manifest and tool, then `Build()` seals
  the Runtime catalog and starts the Runtime.
- `ExecuteAsync` executes one typed operation and returns
  `OperationExecutionResult<NeutralEchoResult>`.

The neutral profile is read-only, has no side effects, requires no
confirmation, and declares no undo support.

## Provider Selection

Use explicit provider selection when you know which provider should run:

```csharp
var result = await runtime.ExecuteAsync(
    operation,
    new ExecutionOptions(providerId));
```

Implicit provider selection is allowed only when exactly one executable
provider is available for the operation capability:

```csharp
var result = await runtime.ExecuteAsync(operation);
```

If no provider is available, a requested provider is missing, the capability is
not executable, or more than one executable provider could satisfy an implicit
request, the Runtime rejects the operation instead of guessing.

## Result Handling

Use public result properties only:

```csharp
if (result.Status == OperationExecutionStatus.Success)
{
    var value = result.Value;
}
else
{
    var status = result.Status;
    var error = result.Error;
}
```

Important statuses:

| Status | Meaning |
|---|---|
| `Success` | The tool executed and returned a typed value. |
| `Rejected` | The Runtime rejected execution before the tool ran. |
| `FunctionalFailure` | The tool ran and returned `Result<T>.Failure`. |
| `ContractMismatch` | The tool contract did not match the requested operation/result type. |
| `UnexpectedFailure` | The Runtime sanitized an unexpected tool failure. |
| `Cancelled` | Execution was cancelled. |

Public errors expose stable `Error.Code` and `Error.Message` only. They must
not expose exception objects, stack traces, local paths, secrets, private
payloads, or arbitrary object values.

## Next Steps

- Run the
  [Minimal External Integration Sample](../../samples/GeesNO.Minimal.Integration/README.md)
  when you want a complete project that uses only public packages and shows
  capability enumeration, positive execution, and fail-closed rejection.
- Read the [integration guide](../integration/README.md).
- Check the [public API map](../integration/public-api.md).
- Review [VERSIONING.md](../../VERSIONING.md) and
  [COMPATIBILITY.md](../../COMPATIBILITY.md).
- Review [SECURITY.md](../../SECURITY.md) before building adapters that touch
  external software, files, credentials, networks, or destructive operations.
