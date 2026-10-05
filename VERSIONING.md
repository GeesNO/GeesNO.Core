# Versioning

Status: Developer Preview versioning policy.

GeesNO.Core follows Semantic Versioning direction for public releases.

Current line:

```text
0.x
```

Candidate source-repository developer preview:

```text
0.1.0
```

This snapshot does not create a release tag named `v0.1.0`, publish packages or
publish a GitHub Release. The `0.1.0` value identifies the source
repository/product candidate prepared before external publication.

Planned first external NuGet Developer Preview package version:

```text
0.1.0-preview.1
```

This package version is prepared locally only. It does not publish packages,
create a GitHub Release or create release tags.

## Major zero

Major version zero means:

```text
public API is provisional
breaking changes MAY occur before 1.0
breaking changes MUST be documented
migration guidance SHOULD be provided when relevant
```

Do not interpret `0.1.0` as API 1.0 stability, certification, final conformance or production certification.

## Version categories

### Repository/product release version

The repository/product release version identifies the whole source repository
state prepared for evaluation. The source candidate is `0.1.0`. The local NuGet
package preview is `0.1.0-preview.1`.

### Capability version

A capability version identifies the semantics of one capability, for example:

```text
neutral.echo@1.0.0
neutral.describe@1.0.0
neutral.validate@1.0.0
system.ping@1.0.0
```

Capability versions do not automatically change when the repository/product version changes. A capability semantic incompatibility requires a new capability version.

### Assembly/internal implementation version

Assembly metadata, runtime display values and implementation details are not the language-independent specification. Runtime display metadata may identify the current developer preview candidate, but it does not redefine capability semantics.

## Breaking changes before 1.0

Before 1.0, a breaking change may be accepted when it improves the Core model or corrects an architectural defect. Each breaking change should document:

```text
what changed
why it changed
who is affected
migration guidance when relevant
```

## What remains stable in v0 evaluation

For the v0 developer preview, consumers should rely only on documented public API and public contracts:

```text
GeesNO.Core.Primitives public types
GeesNO.Core.Contracts public types
GeesNO.Core.Runtime public facade
GeesNO.Core.Contracts.Neutral profile types
```

Runtime internals, Host internals, tests, migration history, repository layout and reference/sample implementations are not long-term compatibility promises.
