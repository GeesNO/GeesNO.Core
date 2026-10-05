# Integration Testing Template

Status: informative integration template.

These tests are recommended integration patterns. They are not a conformance suite, certification framework or compatibility badge.

## Purpose

Use tests to prove that a provider can be consumed through public GeesNO.Core API without relying on implementation details.

## Dependencies

Allowed:

```text
GeesNO.Core.Contracts
GeesNO.Core.Runtime public facade
the provider under test
```

Do not use Runtime internals, Host internals, reflection into internals, private state or generated build output.

## Explicit provider pattern

```csharp
var providerId = ProviderId.Parse("provider.example");
using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(ExampleProviderManifestFactory.Create(providerId))
    .RegisterTool(providerId, new ExampleEchoTool())
    .Build();

var operation = new NeutralEchoOperation(OperationId.New(), "explicit");
var result = await runtime.ExecuteAsync(operation, new ExecutionOptions(providerId));

Assert.Equal(OperationExecutionStatus.Success, result.Status);
Assert.Equal(providerId, result.ProviderId);
Assert.Equal(typeof(NeutralEchoResult), result.DeclaredResultType);
Assert.Equal("explicit", result.Value?.Text);
Assert.Null(result.Error);
```

## Implicit single-provider pattern

```csharp
var result = await runtime.ExecuteAsync(
    new NeutralEchoOperation(OperationId.New(), "implicit"));

Assert.Equal(OperationExecutionStatus.Success, result.Status);
Assert.Equal("implicit", result.Value?.Text);
```

Use this only when the test runtime registers one executable provider for the capability.

## Ambiguity rejection pattern

```csharp
var providerA = ProviderId.Parse("provider.example-a");
var providerB = ProviderId.Parse("provider.example-b");
using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(ExampleProviderManifestFactory.Create(providerA))
    .RegisterTool(providerA, new ExampleEchoTool())
    .RegisterManifest(ExampleProviderManifestFactory.Create(providerB))
    .RegisterTool(providerB, new ExampleEchoTool())
    .Build();

var result = await runtime.ExecuteAsync(
    new NeutralEchoOperation(OperationId.New(), "ambiguous"));

Assert.Equal(OperationExecutionStatus.Rejected, result.Status);
Assert.Equal("runtime.ambiguous-provider", result.Error?.Code);
Assert.Null(result.ProviderId);
Assert.Null(result.DeclaredResultType);
Assert.Null(result.Value);
```

Runtime rejects ambiguous implicit execution instead of selecting a provider silently.

## Unavailable capability pattern

```csharp
var providerId = ProviderId.Parse("provider.unavailable-example");
var support = new CapabilitySupport(
    NeutralCapabilities.Echo,
    CapabilitySupportLevel.Unavailable,
    UndoSupportLevel.None,
    Array.Empty<CapabilityRestriction>(),
    unavailableReason: "Unavailable in this test context.");

var manifest = new CapabilityManifest(
    providerId,
    new VersionInfo(1, 0, 0),
    [new CapabilityManifestEntry(NeutralCapabilities.EchoDescriptor, support)]);

using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(manifest)
    .Build();

var result = await runtime.ExecuteAsync(
    new NeutralEchoOperation(OperationId.New(), "unavailable"),
    new ExecutionOptions(providerId));

Assert.Equal(OperationExecutionStatus.Rejected, result.Status);
Assert.Equal("runtime.capability-not-executable", result.Error?.Code);
Assert.Equal(providerId, result.ProviderId);
Assert.Null(result.DeclaredResultType);
Assert.Null(result.Value);
```

## Public surface checks

Recommended checks:

```text
- no dependency on Runtime internals;
- no dependency on Host internals;
- no generated bin/obj content;
- no SDK or CLI requirement;
- no package publication requirement;
- public errors expose only stable code and message;
- public results do not expose exceptions, stack traces or boxed object values.
```

These checks help keep an integration honest, but they do not define final conformance.
