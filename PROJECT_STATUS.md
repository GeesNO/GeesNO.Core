# Project Status

Status: `0.1.0-preview.1` Developer Preview snapshot prepared locally; the GitHub repository exists privately and public distribution is not completed.

## Current Scope

GeesNO.Core Foundation is a headless, vendor-neutral, software-neutral,
AI-neutral and domain-neutral execution layer for typed operations and declared
capabilities.

The repository currently contains:

```text
src\GeesNO.Core.Primitives
src\GeesNO.Core.Contracts
src\GeesNO.Core.Runtime
src\GeesNO.Core.Host
src\GeesNO.Reference.Simulator
samples\GeesNO.Sample.Cli
tests\GeesNO.Core.Tests
```

## Public Package Set

The planned Developer Preview package set is:

```text
GeesNO.Core.Primitives
GeesNO.Core.Contracts
GeesNO.Core.Runtime
```

Package version:

```text
0.1.0-preview.1
```

The packages are validated locally but are not published to NuGet.
Their `RepositoryUrl` and `PackageProjectUrl` metadata point to
`https://github.com/GeesNO/GeesNO.Core`.

`GeesNO.Core.Host`, `GeesNO.Reference.Simulator`, `GeesNO.Sample.Cli` and
`GeesNO.Core.Tests` are not public packages.

## Public Runtime Surface

`GeesNO.Core.Runtime` intentionally exposes only:

```text
GeesNORuntimeBuilder
GeesNORuntime
ExecutionOptions
RuntimeCapability
OperationExecutionResult<TResult>
OperationExecutionStatus
GeesNORuntimeConfigurationException
```

Runtime catalog, execution engine, invoker, observability, event bus, journal
and Host internals are implementation details.

## Reference And Sample Material

`GeesNO.Reference.Simulator` is non-normative reference material for the neutral
read-only profile. It is not a conformance authority and is not required by
third-party integrations.

`GeesNO.Sample.Cli` is a non-normative console sample that demonstrates public
Runtime consumption. It is not a product CLI.

## Release Readiness

Completed local readiness work includes:

- public policy files: `LICENSE`, `NOTICE`, `TRADEMARKS.md`, `CONTRIBUTING.md`,
  `DCO`, `SUPPORT.md` and `SECURITY.md`;
- public documentation: `README.md`, Quick Start, integration guide, public API
  map, versioning and compatibility notes;
- local NuGet packaging dry-run for the three public packages;
- isolated external consumption test from a clean .NET 8 project;
- GitHub issue and pull request templates prepared for future public use.

Remaining publication gates:

- public repository visibility;
- release tag;
- GitHub Release;
- NuGet publication;
- activation of GitHub Private Vulnerability Reporting / GitHub Security
  Advisories;
- explicit final publication authorization.

No release tag, GitHub Release or NuGet package has been published by this
snapshot.

## Validation Baseline

The maintained validation sequence is:

```bat
dotnet restore GeesNO.Core.sln
dotnet build GeesNO.Core.sln --no-restore
dotnet test GeesNO.Core.sln --no-build
dotnet run --project src\GeesNO.Core.Host
dotnet run --project samples\GeesNO.Sample.Cli --no-build -- echo release-prep-smoke
dotnet format GeesNO.Core.sln --verify-no-changes --no-restore
dotnet sln GeesNO.Core.sln list
git diff --check
git status --short
```

The release scripts provide the repeatable local package dry-run and isolated
external consumption test:

```bat
.\scripts\release\preview-dry-run.ps1
.\scripts\release\external-consumption-test.ps1
```

## Boundaries

The Foundation does not include CAD, BIM, 3D, document, construction, vendor,
LLM, MCP, cloud, marketplace, UI, COM, adapter-specific or domain-specific
requirements.

No formal conformance or certification program exists for this Developer
Preview.
