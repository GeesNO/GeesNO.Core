# GeesNO.Core Integration Guide

Status: public Developer Preview guide.

This guide explains how a third-party developer can build an integration,
provider, or adapter using only public GeesNO.Core API. It is informative and
does not create a formal conformance program.

Use it together with:

- [Quick Start](../getting-started/quick-start.md)
- [Public API Map](public-api.md)
- [Versioning](../../VERSIONING.md)
- [Compatibility](../../COMPATIBILITY.md)
- [Security](../../SECURITY.md)

## Mental Model

GeesNO.Core separates the integration model into a few explicit parts:

| Concept | Role |
|---|---|
| Primitives | Shared value types such as `VersionInfo`, `Error`, `Result<T>`, and object identifiers. |
| Contracts | Public capability, provider, operation, tool, neutral-profile, and system diagnostic contracts. |
| Operation | A typed request implementing `IOperation<TResult>`. It declares an `OperationId` and exact `CapabilityReference`. |
| Capability | A versioned ability such as `neutral.echo@1.0.0`. It is identified by `CapabilityId` plus `VersionInfo`. |
| Provider | An integration that declares capability support with a `CapabilityManifest`. |
| Tool | In-process trusted code implementing `ITool<TOperation,TResult>` for one exact operation/result pair. |
| Runtime | The headless execution facade that registers manifests/tools and executes typed operations. |

Core receives an already-resolved operation. It does not infer intent, parse
natural language, inspect external application state, or make vendor-specific
decisions.

## Boundary For External Adapters

An adapter may use:

```text
GeesNO.Core.Primitives
GeesNO.Core.Contracts
GeesNO.Core.Runtime public facade
```

An adapter must not depend on:

- `internal` types;
- Runtime internals such as ToolEngine, catalog snapshots, invokers, internal
  journal, observability, or event bus;
- Host internals;
- `GeesNO.Core.Host`;
- `GeesNO.Sample.Cli`;
- implementation-specific tests;
- `GeesNO.Reference.Simulator` as a normative dependency.

`GeesNO.Reference.Simulator` remains useful example material, but a real
third-party adapter must be buildable without it.

## Public Packages

Use packages by role:

- `GeesNO.Core.Contracts` for operation profiles, provider manifests, tools,
  and adapter libraries;
- `GeesNO.Core.Runtime` for an executable consumer that builds a Runtime and
  executes operations;
- `GeesNO.Core.Primitives` directly only when a package needs primitive types
  without Contracts or Runtime.

The first package version is planned as `0.1.0-preview.1`. It is prerelease and
not a 1.0 stability promise.

## Capabilities

A capability is an exact pair:

```text
CapabilityId + VersionInfo
```

For example:

```text
neutral.echo@1.0.0
system.ping@1.0.0
```

Package versions and capability versions are different concepts. A package can
be `0.1.0-preview.1` while a capability remains `neutral.echo@1.0.0`.

Providers declare support through `CapabilityManifestEntry`, which combines:

- `CapabilityDescriptor`: public semantics, risk, side-effect flag, and
  confirmation policy;
- `CapabilitySupport`: provider support level, undo support, restrictions, and
  optional unavailable reason.

Unknown capability IDs or versions must fail safely. Runtime must not execute by
guessing an approximate capability.

Support levels:

| Support level | Runtime meaning |
|---|---|
| `Native` | Executable if a matching tool is registered. |
| `Partial` | Executable with declared restrictions if a matching tool is registered. |
| `Emulated` | Executable with declared differences if a matching tool is registered. |
| `Unsupported` | Not executable. |
| `Unavailable` | Temporarily not executable and must include a public reason. |

If multiple executable providers can satisfy an implicit request, Runtime
rejects the operation instead of choosing silently. Use `ExecutionOptions` with
an explicit `ProviderId` when selection matters.

## Operations

An operation implements `IOperation<TResult>`:

```csharp
public sealed record ExampleEchoOperation : IOperation<ExampleEchoResult>
{
    public ExampleEchoOperation(OperationId id, string text)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(text);

        Id = id;
        Text = text;
    }

    public OperationId Id { get; }

    public CapabilityReference Capability => ExampleCapabilities.Echo;

    public string Text { get; }
}
```

The result type is explicit and typed. Do not use boxed `object` payloads as a
public operation result shape.

## Tools

A tool implements one exact operation/result pair:

```csharp
public sealed class ExampleEchoTool : ITool<ExampleEchoOperation, ExampleEchoResult>
{
    public CapabilityReference Capability => ExampleCapabilities.Echo;

    public Task<Result<ExampleEchoResult>> ExecuteAsync(
        ExampleEchoOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result<ExampleEchoResult>.Success(
            new ExampleEchoResult(operation.Text)));
    }
}
```

Functional failures should return `Result<TResult>.Failure(new Error(code,
message))`. Unexpected exceptions are not a public error contract; Runtime
normalizes ordinary unexpected failures into sanitized public results.

## Runtime

The current public Runtime facade is intentionally small:

- `GeesNORuntimeBuilder`
- `GeesNORuntime`
- `ExecutionOptions`
- `RuntimeCapability`
- `OperationExecutionResult<TResult>`
- `OperationExecutionStatus`
- `GeesNORuntimeConfigurationException`

Typical setup:

```csharp
using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(manifest)
    .RegisterTool(providerId, tool)
    .Build();
```

`Build()` seals the catalog and starts the Runtime. The builder is no longer
usable after `Build()`.

Execution:

```csharp
var result = await runtime.ExecuteAsync(
    operation,
    new ExecutionOptions(providerId),
    cancellationToken);
```

Inspect capabilities:

```csharp
foreach (var capability in runtime.Capabilities)
{
    var provider = capability.ProviderId;
    var descriptor = capability.Descriptor;
    var support = capability.Support;
}
```

Handle only public result state:

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

## Lifecycle

The implemented lifecycle is:

1. create public provider IDs, capability descriptors, support declarations,
   and manifests;
2. implement typed tools;
3. register manifests and tools with `GeesNORuntimeBuilder`;
4. call `Build()`;
5. execute typed operations with `ExecuteAsync`;
6. observe public `OperationExecutionResult<T>`;
7. call `Stop()` or dispose the Runtime.

There are no public lifecycle hooks, plugin loading APIs, public journal APIs,
public observability APIs, sandbox APIs, or advanced permission APIs in this
Developer Preview.

`GeesNORuntime` implements `IDisposable`. `Stop()` is idempotent and terminal.
Tools should respect the `CancellationToken` supplied to `ExecuteAsync`.

## Security Posture

The Developer Preview posture is fail-closed / deny-by-default:

- unknown capability or version: reject;
- missing provider: reject;
- ambiguous implicit provider selection: reject;
- unavailable or unsupported capability: reject;
- missing or mismatched tool: reject;
- ordinary tool failure: normalize to a public result;
- public errors: stable code and message only.

The current neutral profile is read-only, has no side effects, requires no
confirmation, and declares no undo support.

The preview does not claim:

- sandboxing of arbitrary third-party code;
- dynamic plugin safety;
- production security certification;
- public vulnerability response SLA;
- advanced permissions or confirmations;
- safety of real external adapters that touch files, networks, credentials,
  external applications, or destructive operations.

Those capabilities require future design before they are presented as Core
guarantees.

## SystemPingOperation

`SystemPingOperation` and `SystemPingResult` are public contracts in
`GeesNO.Core.Contracts.System`.

They represent `system.ping@1.0.0`, a system diagnostic capability for checking
local typed Runtime execution. It is not a domain, industrial, CAD, BIM, or
vendor capability.

The Runtime/Host implementation includes an internal system ping tool and
manifest for local diagnostics. External adapters must not depend on those
internal implementation classes. If an external host wants a diagnostic ping, it
can implement its own public `ITool<SystemPingOperation,SystemPingResult>` and
manifest through the normal public API.

## Informative Templates

The focused integration templates remain useful as examples:

- [provider-template.md](provider-template.md)
- [operation-profile-template.md](operation-profile-template.md)
- [tool-template.md](tool-template.md)
- [runtime-consumer-template.md](runtime-consumer-template.md)
- [integration-testing-template.md](integration-testing-template.md)

They are not installable templates, not an SDK, and not a conformance suite.

## External Adapter Checklist

Before treating an adapter as ready for preview evaluation, check:

- it compiles against public packages only;
- it does not reference Host, Sample, Tests, Runtime internals, or the reference
  simulator as a normative dependency;
- every operation has a stable typed result;
- every capability has an exact version;
- unsupported and unavailable cases are tested;
- explicit provider selection is tested;
- ambiguous implicit provider selection is tested when relevant;
- public errors do not leak secrets, local paths, stack traces, or private
  payloads;
- security-sensitive behavior is reported privately, not through public issues.
