# Dependency Inventory

Status: Developer Preview dependency inventory.

No web lookup was used. License names are not inferred here. Every external dependency remains pending external/legal verification.

## Runtime and SDK

| Name | Version from repo | Consumer project(s) | Scope | Purpose | License review status |
|---|---:|---|---|---|---|
| .NET SDK/runtime/BCL | inherited from installed SDK/runtime | all active projects | production/test | target framework `net8.0`, build and runtime base class libraries | pending external/legal verification |

## NuGet package references

| Name | Version from repo | Consumer project(s) | Scope | Purpose | License review status |
|---|---:|---|---|---|---|
| Microsoft.Extensions.Hosting | 8.0.1 | `src/GeesNO.Core.Host` | production host only | executable composition root hosting | pending external/legal verification |
| Microsoft.NET.Test.Sdk | 17.8.0 | `tests/GeesNO.Core.Tests` | test-only | test execution | pending external/legal verification |
| xunit | 2.5.3 | `tests/GeesNO.Core.Tests` | test-only | test framework | pending external/legal verification |
| xunit.runner.visualstudio | 2.5.3 | `tests/GeesNO.Core.Tests` | test-only | Visual Studio/VSTest integration | pending external/legal verification |

## Removed central versions

These central package versions are intentionally absent because no active
tracked project consumes them:

```text
Microsoft.Extensions.Logging.Console 8.0.1
Microsoft.Extensions.Configuration.Json 8.0.1
Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0
```

No package versions were updated and no packages were added.

## Active source projects

```text
src/GeesNO.Core.Primitives
src/GeesNO.Core.Contracts
src/GeesNO.Core.Runtime
src/GeesNO.Core.Host
src/GeesNO.Reference.Simulator
samples/GeesNO.Sample.Cli
tests/GeesNO.Core.Tests
```

No inactive source project is kept in the public snapshot for dependency
justification.
