# Runtime Consumer Template

Status: informative integration template.

A Runtime consumer registers public manifests and tools, builds a `GeesNORuntime`, executes typed operations and handles `OperationExecutionResult<TResult>`.

This is a fragment template, not a CLI, executable application or product
sample.

## Dependencies

Allowed:

```text
GeesNO.Core.Contracts
GeesNO.Core.Runtime public facade
provider contracts or tools chosen by the integrator
```

Do not depend on Host internals, Runtime internals, a command parser, a transport, a future SDK or an executable sample layout.

## Explicit provider execution

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime;

var providerId = ProviderId.Parse("provider.example");

using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(ExampleProviderManifestFactory.Create(providerId))
    .RegisterTool(providerId, new ExampleEchoTool())
    .Build();

var operation = new NeutralEchoOperation(OperationId.New(), "hello");
var cancellationToken = CancellationToken.None;
var result = await runtime.ExecuteAsync(
    operation,
    new ExecutionOptions(providerId),
    cancellationToken);

if (result.Status == OperationExecutionStatus.Success)
{
    var value = result.Value;
    // Use value.Text here.
}
else
{
    var error = result.Error;
    // Use error.Code and error.Message here.
}
```

`ExampleProviderManifestFactory` and `ExampleEchoTool` are the public patterns from the provider and tool templates. They are not SDK helpers.

## Implicit provider execution

Implicit execution is appropriate only when exactly one executable provider is registered for the operation capability.

```csharp
var result = await runtime.ExecuteAsync(
    new NeutralEchoOperation(OperationId.New(), "single provider"),
    cancellationToken: cancellationToken);
```

If more than one executable provider can satisfy the capability, Runtime returns a rejected public result instead of guessing.

## Result handling

Check these public properties:

```text
Status
IsSuccess
OperationId
Capability
ProviderId
DeclaredResultType
Value
Error.Code
Error.Message
```

Do not inspect Runtime internals to understand a result. A failed result has no successful `Value`; a cancelled result has no `Value` and no `Error`.

## Minimum recommended tests

- Explicit provider executes the selected provider.
- Implicit provider executes when only one provider is executable.
- Ambiguous implicit providers are rejected.
- Missing explicit provider is rejected without fallback.
- Unavailable capability is rejected before tool execution.
- Functional failure preserves a public tool error.
- Unexpected failures are sanitized by Runtime.
