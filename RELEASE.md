# Release Preparation

Status: local Developer Preview release preparation for `0.1.0-preview.1`.

This document describes repeatable local validation. It does not publish a
release, create a tag, push to GitHub or publish NuGet packages.

## Package Version

```text
0.1.0-preview.1
```

Public package set:

```text
GeesNO.Core.Primitives
GeesNO.Core.Contracts
GeesNO.Core.Runtime
```

Non-package projects:

```text
GeesNO.Core.Host
GeesNO.Reference.Simulator
GeesNO.Sample.Cli
GeesNO.Core.Tests
```

## Required Publication Gates

Before public distribution, confirm:

- legal and policy files are final for the publication scope;
- GitHub Private Vulnerability Reporting / GitHub Security Advisories is
  enabled;
- the working tree is clean;
- validation, dry-run and external consumption tests pass from a known commit;
- package metadata, symbol packages and checksums are inspected;
- no generated package artifacts are tracked;
- no secrets, credentials, private paths, stack traces or private payloads are
  present in public material;
- final publication authorization exists.

## Normal Validation

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

## Local Package Dry-Run

For publication validation on the future public branch:

```bat
.\scripts\release\preview-dry-run.ps1 -ExpectedBranch main
```

For local pre-commit validation while edits are pending:

```bat
.\scripts\release\preview-dry-run.ps1 -AllowPendingChanges
```

The dry-run writes local artifacts under `artifacts/release-dry-run` unless an
alternate `-OutputRoot` is supplied. Generated artifacts must not be committed.

The dry-run validates:

- restore, build, tests, Host smoke run and sample CLI smoke run;
- format, solution listing and whitespace checks;
- packability metadata for only the three public packages;
- `.nupkg` and `.snupkg` contents;
- package dependencies and repository commit metadata;
- SHA-256 manifest generation.

`RepositoryUrl` and `PackageProjectUrl` remain unset until a public repository
URL is intentionally applied.

## External Consumption Test

```bat
.\scripts\release\external-consumption-test.ps1
```

The script runs the official dry-run, copies only `.nupkg` files to an external
temporary feed, creates a clean .NET 8 consumer outside the repository, isolates
NuGet package/cache locations, installs only `GeesNO.Core.Runtime`, verifies
transitive package resolution, and executes public Runtime paths.

Use `-AllowedUntrackedFile <path>` only for local validation when an explicitly
known non-release local file is present. Do not use that option for a final
publication run.

## Release Notes

Prepared release notes live at:

```text
docs\releases\0.1.0-preview.1.md
```

They may be reused for a future GitHub Release only after publication is
separately authorized.

## Publication Remains Separate

This repository state is a local publication-preparation snapshot. The next
steps of remote branch creation, release tag, GitHub Release, NuGet publish and
external announcement remain separate explicit actions.
