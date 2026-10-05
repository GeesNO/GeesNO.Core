# Tool Template

Status: informative integration template.

A tool executes exactly one typed operation contract and returns `Result<TResult>`. This template is not a SDK abstraction and does not hide GeesNO.Core identities.

## Purpose

Use this pattern when a provider supports a capability with executable code.

## Candidate files

```text
ExampleEchoTool.cs
ExampleEchoToolTests.cs
```

## Dependencies

Allowed:

```text
GeesNO.Core.Contracts
GeesNO.Core.Contracts.Neutral when using the neutral profile
GeesNO.Core.Primitives
```

Do not depend on Runtime internals, Host internals, reflection into internals, free-form dispatch strings or boxed `object` payloads.

## Typed tool pattern

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;

public sealed class ExampleEchoTool : ITool<NeutralEchoOperation, NeutralEchoResult>
{
    private static readonly Error EmptyTextError = new(
        "example.echo.empty-text",
        "Text is required.");

    public CapabilityReference Capability => NeutralCapabilities.Echo;

    public Task<Result<NeutralEchoResult>> ExecuteAsync(
        NeutralEchoOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(operation.Text))
        {
            return Task.FromResult(Result<NeutralEchoResult>.Failure(EmptyTextError));
        }

        return Task.FromResult(Result<NeutralEchoResult>.Success(
            new NeutralEchoResult(operation.Text)));
    }
}
```

## Rules

- `Capability` must match the operation capability.
- Functional failures should return `Result<TResult>.Failure(new Error(code, message))`.
- Unexpected exceptions are not a public error contract.
- Respect the provided `CancellationToken`.
- Do not dispatch by comparing arbitrary tool names.
- Do not expose local paths, stack traces, secrets or private payloads in public errors.

## Minimum recommended tests

- The tool implements `ITool<TOperation,TResult>`.
- `Capability` equals the exact capability reference.
- Success returns the expected typed result.
- Functional failure returns a stable public `Error`.
- Null operation is rejected.
- Cancellation is observed before doing work.
