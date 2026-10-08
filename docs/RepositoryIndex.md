# Repository Index

## Root

```text
GeesNO.Core.sln
.github\workflows\dotnet.yml
.github\ISSUE_TEMPLATE\bug_report.yml
.github\ISSUE_TEMPLATE\feature_request.yml
.github\ISSUE_TEMPLATE\config.yml
.github\pull_request_template.md
scripts\release\preview-dry-run.ps1
scripts\release\external-consumption-test.ps1
Directory.Build.props
Directory.Packages.props
README.md
CHANGELOG.md
PROJECT_STATUS.md
LICENSE
NOTICE
TRADEMARKS.md
CONTRIBUTING.md
DCO
SUPPORT.md
VERSIONING.md
COMPATIBILITY.md
SECURITY.md
RELEASE.md
```

## Solution

`GeesNO.Core.sln` contains exactly:

```text
src\GeesNO.Core.Primitives
src\GeesNO.Core.Contracts
src\GeesNO.Core.Runtime
src\GeesNO.Core.Host
src\GeesNO.Reference.Simulator
samples\GeesNO.Sample.Cli
tests\GeesNO.Core.Tests
```

## Public Packages

```text
GeesNO.Core.Primitives
GeesNO.Core.Contracts
GeesNO.Core.Runtime
```

Package README files:

```text
src\GeesNO.Core.Primitives\PACKAGE_README.md
src\GeesNO.Core.Contracts\PACKAGE_README.md
src\GeesNO.Core.Runtime\PACKAGE_README.md
```

## Source Projects

`GeesNO.Core.Primitives` owns shared value types, identifiers, errors, results
and version values.

`GeesNO.Core.Contracts` owns public capability, provider, operation, tool,
neutral-profile and system diagnostic contracts.

`GeesNO.Core.Runtime` owns the public Runtime facade and internal execution
implementation.

`GeesNO.Core.Host` is an executable composition root and diagnostic host, not a
public SDK.

`GeesNO.Reference.Simulator` is non-normative reference material for the neutral
profile.

## Samples And Tests

```text
samples\GeesNO.Sample.Cli
tests\GeesNO.Core.Tests
```

The sample is informative. Tests provide implementation evidence but do not
create a formal conformance suite.

## Documentation

```text
docs\getting-started\quick-start.md
docs\integration\README.md
docs\integration\public-api.md
docs\integration\provider-template.md
docs\integration\operation-profile-template.md
docs\integration\tool-template.md
docs\integration\runtime-consumer-template.md
docs\integration\integration-testing-template.md
docs\NORMATIVE_AND_INFORMATIVE.md
docs\DEPENDENCY_INVENTORY.md
docs\adr\ADR-0001-geesno-core-assembly-layout.md
docs\releases\0.1.0-preview.1.md
```

## CI

```text
.github\workflows\dotnet.yml
```

The workflow runs restore, build, tests, Host smoke run, sample CLI smoke run,
format check, solution listing, whitespace check and tracked `bin`/`obj`
protection. It does not publish packages or release artifacts.

## Release Scripts

```text
scripts\release\preview-dry-run.ps1
scripts\release\external-consumption-test.ps1
```

Generated release and external-consumption artifacts are local-only and must not
be committed.

## Publication State

The GitHub repository exists at `https://github.com/GeesNO/GeesNO.Core` and
remains private, but this snapshot does not tag, publish NuGet packages or
create a GitHub Release.
