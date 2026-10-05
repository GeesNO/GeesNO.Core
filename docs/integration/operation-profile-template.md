# Operation Profile Template

Status: informative integration template.

An operation profile groups capability references, descriptors, operation contracts and result contracts. Profiles are optional extension material unless they are part of a public specification. They are not Runtime internals.

## Purpose

Use a profile when several providers should implement the same public operation contracts. Keep the profile small, typed and explicit.

## Candidate files

```text
ExampleCapabilities.cs
ExampleEchoOperation.cs
ExampleEchoResult.cs
ExampleProfileTests.cs
```

## Dependencies

Allowed:

```text
GeesNO.Core.Contracts
GeesNO.Core.Primitives
```

Do not depend on Runtime internals, Host internals, provider implementation details, transport code or a future SDK.

## Capability and descriptor pattern

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

public static class ExampleCapabilities
{
    public static CapabilityReference Echo { get; } = new(
        CapabilityId.Parse("example.echo"),
        new VersionInfo(1, 0, 0));

    public static CapabilityDescriptor EchoDescriptor { get; } = new(
        Echo,
        "Example Echo",
        "Returns bounded neutral text through a typed operation.",
        RiskLevel.ReadOnly,
        hasSideEffects: false,
        ConfirmationPolicy.NotRequired);

    public static CapabilityManifestEntry EchoEntry { get; } = new(
        EchoDescriptor,
        new CapabilitySupport(
            Echo,
            CapabilitySupportLevel.Native,
            UndoSupportLevel.None,
            Array.Empty<CapabilityRestriction>(),
            unavailableReason: null));
}
```

## Operation and result pattern

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;

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

public sealed record ExampleEchoResult(string Text);
```

The operation exposes `CapabilityReference` and implements `IOperation<TResult>`. The result is typed and does not use `object`.

## Minimum recommended tests

- CapabilityId is canonical.
- Version is exact and explicit.
- Descriptor is read-only when the operation has no side effects.
- Operation returns the profile capability.
- Result has a stable typed public shape.
- The profile does not include domain-specific or vendor-specific semantics unless it is intentionally an external profile outside Core.
