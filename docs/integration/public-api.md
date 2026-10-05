# Public API Map

Status: Developer Preview public API map for `0.1.0-preview.1`.

This is a lightweight map, not a generated API browser. It identifies the public
surface intended for third-party integration during the Developer Preview and
separates it from implementation details.

## Public Packages

```text
GeesNO.Core.Primitives
GeesNO.Core.Contracts
GeesNO.Core.Runtime
```

`GeesNO.Reference.Simulator`, `GeesNO.Core.Host`, `GeesNO.Sample.Cli`, and
`GeesNO.Core.Tests` are not public package targets for `0.1.0-preview.1`.

## GeesNO.Core.Primitives

Supported public primitives:

```text
GeesNO.Core.Primitives.ObjectId
GeesNO.Core.Primitives.VersionInfo
GeesNO.Core.Primitives.Error
GeesNO.Core.Primitives.Result
GeesNO.Core.Primitives.Result<T>
```

Use these for identifiers, version values, stable public errors, and functional
success/failure results.

## GeesNO.Core.Contracts

Capability and provider contracts:

```text
GeesNO.Core.Contracts.Capabilities.CapabilityId
GeesNO.Core.Contracts.Capabilities.CapabilityReference
GeesNO.Core.Contracts.Capabilities.CapabilityDescriptor
GeesNO.Core.Contracts.Capabilities.CapabilityManifest
GeesNO.Core.Contracts.Capabilities.CapabilityManifestEntry
GeesNO.Core.Contracts.Capabilities.CapabilitySupport
GeesNO.Core.Contracts.Capabilities.CapabilityRestriction
GeesNO.Core.Contracts.Capabilities.ProviderId
GeesNO.Core.Contracts.Capabilities.RiskLevel
GeesNO.Core.Contracts.Capabilities.ConfirmationPolicy
GeesNO.Core.Contracts.Capabilities.UndoSupportLevel
GeesNO.Core.Contracts.Capabilities.CapabilitySupportLevel
```

Operation contracts:

```text
GeesNO.Core.Contracts.Operations.IOperation
GeesNO.Core.Contracts.Operations.IOperation<TResult>
GeesNO.Core.Contracts.Operations.OperationId
```

Tool contracts:

```text
GeesNO.Core.Contracts.Tools.ITool
GeesNO.Core.Contracts.Tools.ITool<TOperation,TResult>
```

Neutral profile contracts:

```text
GeesNO.Core.Contracts.Neutral.NeutralCapabilities
GeesNO.Core.Contracts.Neutral.NeutralEchoOperation
GeesNO.Core.Contracts.Neutral.NeutralEchoResult
GeesNO.Core.Contracts.Neutral.NeutralDescribeOperation
GeesNO.Core.Contracts.Neutral.NeutralDescribeResult
GeesNO.Core.Contracts.Neutral.NeutralValidateOperation
GeesNO.Core.Contracts.Neutral.NeutralValidationResult
```

System diagnostic contracts:

```text
GeesNO.Core.Contracts.System.SystemPingOperation
GeesNO.Core.Contracts.System.SystemPingResult
```

`SystemPingOperation` is a system diagnostic contract for
`system.ping@1.0.0`. It is not a domain-specific capability.

## GeesNO.Core.Runtime

Runtime public facade:

```text
GeesNO.Core.Runtime.GeesNORuntimeBuilder
GeesNO.Core.Runtime.GeesNORuntime
GeesNO.Core.Runtime.ExecutionOptions
GeesNO.Core.Runtime.RuntimeCapability
GeesNO.Core.Runtime.OperationExecutionResult<TResult>
GeesNO.Core.Runtime.OperationExecutionStatus
GeesNO.Core.Runtime.GeesNORuntimeConfigurationException
```

Use these types to register manifests/tools, build a Runtime, inspect public
capabilities, execute typed operations, and handle normalized execution results.

## Implementation Details

Do not depend on:

```text
ToolEngine
RuntimeCatalogBuilder
RuntimeCatalogSnapshot
RuntimeExecutionRequest
RuntimeExecutionResult
RuntimeObservability
InProcessEventBus
InMemoryExecutionJournal
SystemPingTool
SystemPingManifestFactory
RegisteredTool
ToolInvokerFactory
IToolInvoker
InternalInvocationResult
ProviderCapabilityKey
Host internals
Sample internals
test helpers
```

These names may appear in the source repository because they are implementation
details of the official .NET implementation. They are not public integration
API and must not be used by third-party adapters.

## Informative Material

The following are useful examples but not normative dependencies:

```text
GeesNO.Reference.Simulator
GeesNO.Sample.Cli
docs/integration templates
tests
```

An adapter that can only be built by copying or referencing these informative
components has not proven external package consumption yet. Use the external
consumption release script to validate that path.

## Stability Expectations

This API map is for `0.1.0-preview.1`. The package line is pre-1.0:

- public API may evolve before 1.0;
- breaking changes must be documented;
- capability versions are separate from package versions;
- a capability version such as `neutral.echo@1.0.0` does not mean the package
  API is stable 1.0;
- no formal conformance or certification program exists in this preview.
