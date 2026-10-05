# Contributing

Status: initial Developer Preview contribution policy.

GeesNO.Core is in Developer Preview. Contributions are welcome, but the public
API, package shape, governance model, and compatibility rules are still
pre-1.0 and may change with documented rationale.

## Before You Start

For ordinary bugs or documentation improvements, prepare a small focused change.

For architecture, public API, package shape, dependency, capability, security,
or breaking-change proposals, open discussion first after the public repository
exists. Until then, keep such changes out of pull requests unless maintainers
explicitly request them.

Do not report suspected security vulnerabilities through public issues, pull
requests, discussions, or comments. See [SECURITY.md](SECURITY.md).

Issue forms and a pull request template are prepared under `.github`. They
describe the expected public workflow after publication, but they do not
activate a public security-reporting path or an external contribution process by
themselves.

## Local Validation

Run the normal validation sequence from the repository root:

```bat
dotnet restore GeesNO.Core.sln
dotnet build GeesNO.Core.sln --no-restore
dotnet test GeesNO.Core.sln --no-build
dotnet run --project src\GeesNO.Core.Host
dotnet run --project samples\GeesNO.Sample.Cli --no-build -- echo release-prep-smoke
dotnet format GeesNO.Core.sln --verify-no-changes --no-restore
git diff --check
git ls-files | rg "(^|/)(bin|obj)/"
dotnet sln GeesNO.Core.sln list
git status --short
```

Tests are expected for behavior changes. Documentation-only changes should still
avoid breaking build, formatting, links, or release-readiness documentation.

## Architectural Boundaries

GeesNO.Core Foundation must remain headless, vendor-neutral, AI-neutral,
software-neutral, and domain-neutral.

Do not introduce into Foundation:

- CAD, BIM, 3D, document, construction, or vendor-specific semantics
- mandatory MCP, LLM, cloud, marketplace, UI, COM, or proprietary APIs
- dependencies on specific adapters or external software
- Runtime internals in public samples, templates, or providers
- broad abstractions without an approved current consumer

Provider and adapter work belongs outside the normative Core unless a future
phase explicitly authorizes a Core change.

## Dependencies

Do not add package dependencies casually. New dependencies require architecture
review and license review. `Microsoft.Extensions.*` remains confined to the Host
unless separately authorized.

## Breaking Changes

GeesNO.Core is pre-1.0. Breaking changes may be accepted before 1.0, but they
must be explicit and documented. A breaking-change proposal should explain:

- what changes
- why the change is necessary
- who is affected
- migration guidance when relevant

Capability semantic incompatibilities require a new capability version.

## Developer Certificate of Origin

This project uses the Developer Certificate of Origin (DCO), not a CLA, for
Developer Preview contributions.

By signing off a contribution, you certify the DCO in [DCO](DCO).

Use a `Signed-off-by` line in every commit:

```text
Signed-off-by: Your Name <you@example.com>
```

You can add it with:

```bat
git commit -s
```

The name and email in the sign-off should identify the contributor who is making
the certification.

## Support and Questions

Developer Preview support is best effort and has no SLA. See [SUPPORT.md](SUPPORT.md) for
how bugs, feature requests, security vulnerabilities, and general questions are
classified.
