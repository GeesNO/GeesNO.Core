# GeesNO.Core

GeesNO.Core is a headless, vendor-neutral execution core for typed operations.
It lets an application, CLI, adapter, AI system, or other host submit an
already-resolved operation to a Runtime that validates the operation capability,
selects an explicit or unambiguous provider, executes a typed tool, and returns
a normalized public result.

GeesNO.Core is not tied to a specific vendor, host application, AI provider,
transport, UI, cloud service, CAD/BIM product, or domain model.

## Developer Preview Status

GeesNO.Core is prepared for a Developer Preview package line:

```text
0.1.0-preview.1
```

The packages are prepared locally but are not published yet. The installation
commands below are the intended commands after `0.1.0-preview.1` is published.

The public API is still in the `0.x` line. Breaking changes may occur before
1.0 when they are documented with rationale and migration guidance where
relevant. See [VERSIONING.md](VERSIONING.md) and
[COMPATIBILITY.md](COMPATIBILITY.md).

## What GeesNO.Core Is

GeesNO.Core provides:

- strongly typed operation contracts;
- capability identifiers and exact capability versions;
- provider capability manifests;
- typed tools that execute one operation contract;
- a minimal public Runtime facade;
- normalized execution statuses and public errors;
- a neutral read-only profile for evaluation.

The Core converts typed operation requests into validated, capability-aware
execution through registered providers. It does not infer user intent and does
not give special trust to AI-generated input.

## What GeesNO.Core Is Not

GeesNO.Core is not:

- a CAD, BIM, 3D, document, or construction-domain SDK;
- a BricsCAD, AutoCAD, Revit, Office, Blender, MCP, LLM, or cloud adapter;
- a product CLI or end-user application;
- a marketplace, plugin host, sandbox, or permission system for untrusted code;
- a formal conformance or certification program;
- a production-ready 1.0 API.

Adapters and domain profiles can be built outside Core by third parties. They
must use public API only and remain outside the normative Foundation unless a
future phase explicitly changes that boundary.

## Public Packages

The first public NuGet package set is:

| Package | Purpose |
|---|---|
| `GeesNO.Core.Primitives` | Shared primitives such as `VersionInfo`, `Error`, `Result<T>`, and object identifiers. |
| `GeesNO.Core.Contracts` | Public capability, operation, tool, neutral-profile, and system diagnostic contracts. Depends on `Primitives`. |
| `GeesNO.Core.Runtime` | Public headless Runtime facade used to register manifests/tools, inspect capabilities, and execute operations. Depends on `Contracts` and `Primitives`. |

`GeesNO.Core.Host`, `GeesNO.Reference.Simulator`, `GeesNO.Sample.Cli`,
`GeesNO.Minimal.Integration`, and `GeesNO.Core.Tests` are not part of the
initial public package set. The Host is a composition executable, the simulator
is non-normative reference material, the sample projects are informative, and
tests are evidence rather than a conformance suite.

## Installation

After `0.1.0-preview.1` is published, install the package that matches your
role:

```bat
dotnet add package GeesNO.Core.Primitives --version 0.1.0-preview.1
dotnet add package GeesNO.Core.Contracts --version 0.1.0-preview.1
dotnet add package GeesNO.Core.Runtime --version 0.1.0-preview.1
```

You usually do not need to install all three directly:

- a library that only defines operation profiles, manifests, or tools normally
  references `GeesNO.Core.Contracts`, which brings `Primitives` transitively;
- an executable Runtime consumer normally references `GeesNO.Core.Runtime`,
  which brings `Contracts` and `Primitives` transitively;
- direct `Primitives` references are useful only when a package intentionally
  exposes or consumes primitive types without using Contracts or Runtime.

Until publication, these packages exist only as local artifacts created during
release preparation.

## Quick Start

The shortest external path is:

1. create a .NET 8 project;
2. reference `GeesNO.Core.Runtime` after publication;
3. define or reuse public operation contracts;
4. register a provider `CapabilityManifest`;
5. register a typed `ITool<TOperation,TResult>`;
6. execute through `GeesNORuntime`.

See [docs/getting-started/quick-start.md](docs/getting-started/quick-start.md)
for a complete minimal example that uses only public packages and implements a
small provider/tool without depending on `GeesNO.Reference.Simulator`.

For a complete runnable project, see the
[Minimal External Integration Sample](samples/GeesNO.Minimal.Integration/README.md).
It uses a direct `PackageReference` to `GeesNO.Core.Runtime` and only public API.

## Integration Model

The Runtime entry point is:

```csharp
using GeesNO.Core.Runtime;

using var runtime = new GeesNORuntimeBuilder()
    .RegisterManifest(manifest)
    .RegisterTool(providerId, tool)
    .Build();
```

Execution uses typed operations:

```csharp
var result = await runtime.ExecuteAsync(operation, new ExecutionOptions(providerId));
```

Handle `OperationExecutionResult<T>` through public state only:

```csharp
if (result.IsSuccess)
{
    var value = result.Value;
}
else
{
    var status = result.Status;
    var error = result.Error;
}
```

For adapter authors, start with:

- [Quick Start](docs/getting-started/quick-start.md)
- [Minimal External Integration Sample](samples/GeesNO.Minimal.Integration/README.md)
- [Integration Guide](docs/integration/README.md)
- [Public API Map](docs/integration/public-api.md)
- [Normative and Informative Material](docs/NORMATIVE_AND_INFORMATIVE.md)

## System Ping

`SystemPingOperation` and `SystemPingResult` are public system diagnostic
contracts for the `system.ping@1.0.0` capability. They are not an industrial,
CAD, BIM, or vendor-domain example.

The Runtime/Host implementation contains an internal system ping provider for
local diagnostics. Third-party adapters should not depend on Runtime internals;
if a host wants its own diagnostic provider, it must declare and register a
public manifest and tool through the normal public API.

## Build And Test From Source

```bat
dotnet restore GeesNO.Core.sln
dotnet build GeesNO.Core.sln --no-restore
dotnet test GeesNO.Core.sln --no-build
dotnet run --project src\GeesNO.Core.Host
dotnet run --project samples\GeesNO.Sample.Cli -- --help
dotnet format GeesNO.Core.sln --verify-no-changes --no-restore
```

The sample CLI demonstrates public Runtime consumption with the non-normative
reference simulator. It is not required for third-party adapters.

The minimal integration sample demonstrates the external package-consumption
path with a `PackageReference` to `GeesNO.Core.Runtime`. It is intentionally not
part of `GeesNO.Core.sln` until the package is available from a public feed.

## Support, Security, And Contributions

- Bugs and feature requests: see [SUPPORT.md](SUPPORT.md). Public issue forms
  are prepared under `.github/ISSUE_TEMPLATE` for use after the repository is
  publicly accessible.
- Vulnerabilities: do not report vulnerabilities through public issues, pull
  requests, comments, or discussions. The intended private channel is GitHub
  Private Vulnerability Reporting / GitHub Security Advisories after the
  repository is publicly accessible and that feature is enabled. See
  [SECURITY.md](SECURITY.md).
- Contributions: Developer Preview contributions use DCO sign-off. See
  [CONTRIBUTING.md](CONTRIBUTING.md) and [DCO](DCO).
- Trademarks: code licensing does not grant general trademark rights. See
  [TRADEMARKS.md](TRADEMARKS.md).

Developer Preview support is best effort and has no SLA.

## Release And Publication Gates

The GitHub repository exists at `https://github.com/GeesNO/GeesNO.Core` and
remains private during this preparation phase. No GitHub Release exists, no
package is published, and no publication tag is created by this snapshot.
`RepositoryUrl` and `PackageProjectUrl` point to that repository URL.

Completed local publication-preparation gates include:

- release dry-run;
- external consumption test from a clean project using packages;
- minimal external integration sample validation from a local package feed;

Before public distribution, the remaining gates include:

- private vulnerability reporting activation;
- final release/tag/package publication authorization.

See [RELEASE.md](RELEASE.md), [PROJECT_STATUS.md](PROJECT_STATUS.md), and
[CHANGELOG.md](CHANGELOG.md) for the current state.
