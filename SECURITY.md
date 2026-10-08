# Security

Status: public Developer Preview security policy.

GeesNO.Core is a headless execution layer for typed operations and declared
capabilities. The current Developer Preview profile is neutral, read-only, and
software-independent.

This document is not a security certification, does not create a vulnerability
response SLA, and does not make the project production-ready.

## Supported Status

The current public-release preparation line is:

```text
0.1.0-preview.1 planned
```

The GitHub repository exists but remains private. It still has no published
GitHub Release and no published NuGet packages. Security reporting becomes
operational only after the repository is publicly accessible and private
vulnerability reporting has been enabled.

## Reporting a Vulnerability

Do not report suspected security vulnerabilities through public issues, pull
requests, discussions, comments, or social channels.

The intended private disclosure channel is:

```text
GitHub Private Vulnerability Reporting / GitHub Security Advisories
```

This channel must be enabled as part of the GitHub publication step before the
Developer Preview release is considered externally ready. Until that happens,
the official private vulnerability reporting channel is a release gate, not an
active channel.

Do not invent or use an unofficial security email address for this project.

## Vulnerabilities vs Bugs

Use private vulnerability reporting for issues that may allow or enable:

- bypassing capability, provider, validation, or execution boundaries
- executing an operation that should be rejected
- leaking secrets, local paths, stack traces, private payloads, or machine data
- unexpected execution of unsafe or side-effecting behavior
- unsafe handling of untrusted tool, adapter, or provider code
- supply-chain, package, provenance, or release-integrity risks

Use ordinary public bugs only for non-sensitive defects, such as documentation
mistakes, build failures, sample behavior defects, test issues, or public API
usability problems that do not disclose a security risk.

## Current Scope

Current Developer Preview scope includes:

```text
typed operations
capability manifests
provider selection
public Runtime execution result/status
sanitized public errors
internal journal and observability
neutral read-only profile
reference simulator
sample CLI
```

Current Developer Preview scope excludes:

```text
real external adapters
dangerous write operations
arbitrary dynamic plugin loading
sandboxing of third-party code
public security certification
production support
public vulnerability response SLA
```

## Trust Boundaries

### Typed-operation boundary

Operations are typed contracts. The Runtime must validate operation and
capability shape before execution. AI, humans, CLIs, apps, and transports may
propose operations, but none receive special trust.

### Provider/tool boundary

Registered tools are trusted in-process .NET code for the current Developer
Preview. GeesNO.Core does not claim to sandbox arbitrary third-party code.
Dynamic plugin loading is not part of this preview.

### Adapter boundary

Real external adapters are outside the current preview. Before adapters that
touch external software, files, networks, credentials, or destructive operations
are introduced, they need explicit permission, risk, and trust-boundary design.

## Implemented Safety Properties

The current code and tests cover:

```text
strong identifier validation
capability availability/support checks
provider selection, including explicit, implicit, missing and ambiguous providers
malformed operation rejection
functional error results
sanitized public errors without stack traces in normal public results
cancellation handling
neutral input length limits
read-only/no-side-effect neutral profile
internal journal and observability
```

The Runtime is intended to fail closed for missing providers, unknown
capabilities, unknown versions, non-executable capabilities, unavailable
capabilities, missing tools, and ambiguous provider selection.

## Exception Containment

Ordinary tool failures are normalized into public error results. Fatal
infrastructure exceptions are not treated as ordinary functional failures.
Public results must avoid leaking exception objects, stack traces, local paths,
secrets, private payloads, or arbitrary object values.

## Known Limitations

The Developer Preview does not claim:

```text
security certification
sandbox for arbitrary third-party code
safe execution of untrusted plugins
production readiness
public vulnerability response SLA
real external adapter safety
formal supply-chain provenance
published SBOM
published checksums
```

These are release and roadmap gates for later publication or post-preview work,
not properties completed by this Developer Preview snapshot.
