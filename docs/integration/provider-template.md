# Provider Template

Status: informative integration template.

This template shows the smallest public shape of a capability provider. It is not a project template and does not define conformance.

## Purpose

A provider owns a stable `ProviderId`, a provider version and one or more `CapabilityManifestEntry` values. The manifest declares what the provider supports. Executable tools are registered separately through the Runtime public facade.

## Candidate files

```text
ExampleProvider.cs
ExampleProviderManifestFactory.cs
ExampleProviderTests.cs
```

These names are suggestions only. They are not required by GeesNO.Core.

## Dependencies

Allowed:

```text
GeesNO.Core.Contracts
GeesNO.Core.Primitives
```

Do not depend on Runtime internals, Host internals, implementation-specific state, proprietary application APIs, a transport, a CLI or a future SDK.

## Public manifest pattern

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Primitives;

public static class ExampleProvider
{
    public static ProviderId Id { get; } = ProviderId.Parse("provider.example");

    public static VersionInfo Version { get; } = new(1, 0, 0);
}

public static class ExampleProviderManifestFactory
{
    public static CapabilityManifest Create(ProviderId? providerId = null)
    {
        return new CapabilityManifest(
            providerId ?? ExampleProvider.Id,
            ExampleProvider.Version,
            [NeutralCapabilities.EchoEntry]);
    }
}
```

The snippet uses `NeutralCapabilities.EchoEntry` because the neutral profile is already public. A third-party profile may define its own `CapabilityReference`, `CapabilityDescriptor`, `CapabilitySupport` and `CapabilityManifestEntry` using the same public contracts.

## Unavailable capability pattern

Use `CapabilitySupportLevel.Unavailable` when a capability is declared but cannot execute in the current provider context.

```csharp
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Primitives;

var support = new CapabilitySupport(
    NeutralCapabilities.Echo,
    CapabilitySupportLevel.Unavailable,
    UndoSupportLevel.None,
    Array.Empty<CapabilityRestriction>(),
    unavailableReason: "Unavailable in the current provider context.");

var manifest = new CapabilityManifest(
    ExampleProvider.Id,
    new VersionInfo(1, 0, 0),
    [new CapabilityManifestEntry(NeutralCapabilities.EchoDescriptor, support)]);
```

Do not register a tool for a capability that is intentionally unavailable.

## Minimum recommended tests

- ProviderId is canonical and strong.
- Manifest entries are deterministic.
- Descriptor and support reference the same capability.
- Read-only capabilities have `HasSideEffects == false` and `UndoSupportLevel.None`.
- Unavailable capabilities include a public reason and no tool registration.
