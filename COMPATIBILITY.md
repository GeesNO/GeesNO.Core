# Compatibility

Status: Developer Preview compatibility policy.

GeesNO.Core `0.x` is a source-repository developer preview line. It is suitable for evaluation and experimentation, not for long-term binary compatibility promises.

## Pre-1.0 policy

```text
0.x does not guarantee long-term binary compatibility
breaking public API changes are possible before 1.0
breaking changes must be documented
migration guidance should be provided when relevant
```

## Source compatibility

Source compatibility is a goal for documented public contracts during a v0 line, but it is not guaranteed before 1.0. Consumers should avoid Runtime internals, Host internals and implementation-specific tests.

## Binary compatibility

Binary compatibility is not guaranteed for 0.x. Consumers evaluating GeesNO.Core should build from source or pin a specific commit/tag.

## Behavioral compatibility

Documented public behavior should remain coherent within a v0 evaluation line. If behavior changes in a way that affects consumers, the change should be called out in documentation and changelog entries.

## Capability version compatibility

Capabilities use versioned references such as:

```text
capability-id@major.minor.patch
```

A semantic incompatibility in a capability requires a new capability version. Repository/product version changes do not automatically change capability versions.

Unknown capability IDs or unknown capability versions must fail safely. The Runtime must not execute an operation by guessing an approximate capability.

## Provider compatibility

Providers must consume public API only:

```text
GeesNO.Core.Contracts
GeesNO.Core.Runtime public facade
```

Providers must not depend on Runtime internals, Host internals, ToolEngine, catalog snapshots, invokers, internal journal, internal event bus or implementation-specific test helpers.

## Sample and reference compatibility

`GeesNO.Reference.Simulator` and `GeesNO.Sample.Cli` are informative. They are useful integration evidence, but they are not conformance authorities and do not define product CLI compatibility.

## Conformance

No formal conformance program exists in v0. No compatibility badge,
certification or formal conformance claim is created by this Developer Preview.
