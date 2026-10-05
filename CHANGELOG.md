# Changelog

## 0.1.0-preview.1 - Unreleased

### Added

- Initial public Developer Preview package set:
  `GeesNO.Core.Primitives`, `GeesNO.Core.Contracts` and `GeesNO.Core.Runtime`.
- Public primitives for version values, object identifiers, stable errors and
  functional results.
- Public capability, provider, operation and tool contracts.
- Neutral read-only profile:
  `neutral.echo@1.0.0`, `neutral.describe@1.0.0` and
  `neutral.validate@1.0.0`.
- Public system diagnostic contracts for `system.ping@1.0.0`.
- Minimal public Runtime facade for registering manifests/tools, inspecting
  capabilities and executing typed operations.
- Non-normative reference simulator and sample CLI.
- Public integration documentation, Quick Start, API map, versioning,
  compatibility, security, support and contribution policies.
- Local release dry-run and isolated external package consumption scripts.

### Changed

- The repository snapshot is presented as a product-oriented Developer Preview
  rather than an internal migration log.
- CI is prepared for the future public `main` branch and pull requests.
- Release automation no longer depends on an internal branch name or a specific
  local handoff file.

### Not Published

- No NuGet package has been published.
- No GitHub Release has been created.
- No release tag has been created by this snapshot.
- No formal conformance or certification program exists.

### Validation

- The three public packages are produced and inspected by the local dry-run.
- A clean external .NET 8 consumer can install only `GeesNO.Core.Runtime` from
  a local feed and resolve `Contracts` and `Primitives` transitively.
- Runtime positive execution and fail-closed rejection are validated through
  public API.
