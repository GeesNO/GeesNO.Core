# Normative And Informative Material

Status: Developer Preview material classification.

This repository is prepared as a `0.1.0` developer preview candidate. The split below is provisional and does not create a formal 1.0 specification or conformance program.

## Candidate normative semantics

The candidate normative semantics are the behaviors and contracts an independent implementation would need to understand:

```text
public identifiers and strong validation
CapabilityId semantics
CapabilityReference and capability version semantics
CapabilityDescriptor
CapabilityManifest
CapabilitySupport and support levels
ProviderId semantics
typed operation contracts
ITool<TOperation,TResult> conceptual behavior
Result/Error semantics
public Runtime result/status behavior
provider selection semantics where specified
neutral profile semantics
versioning and compatibility rules
```

Candidate normative does not mean final 1.0 stable.

The C# implementation is the official .NET implementation in this repository, but C# signatures alone are not a complete language-independent specification.

## Informative/reference material

The following material is informative:

```text
GeesNO.Reference.Simulator
GeesNO.Sample.Cli
docs/integration guides and templates
GeesNO.Core.Host implementation
Runtime internals
tests
repository layout
release-preparation scripts
```

Rules:

```text
official implementation != specification
reference simulator != conformance authority
tests != certification
sample != product CLI
integration guides != SDK
```

## No formal conformance claim

This Developer Preview does not create `CONFORMANCE.md`, a certification badge,
a formal conformance suite or official compatibility certification.
