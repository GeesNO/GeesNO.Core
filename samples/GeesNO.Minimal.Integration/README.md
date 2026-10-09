# GeesNO.Minimal.Integration

Status: minimal external integration sample.

This sample shows the smallest complete external integration path for
GeesNO.Core using only public packages and public API.

It demonstrates:

- a minimal provider identity;
- a custom `sample.greeting@1.0.0` capability;
- a typed operation and typed result;
- a typed tool implementing `ITool<TOperation,TResult>`;
- `GeesNORuntimeBuilder` registration;
- public capability enumeration;
- a successful execution path;
- a fail-closed rejected path for an undeclared capability.

It does not depend on `GeesNO.Reference.Simulator`, `GeesNO.Core.Host`,
`GeesNO.Sample.Cli`, project references, internals, linked source files or any
external application.

## Requirements

- .NET 8 SDK.
- `GeesNO.Core.Runtime` version `0.1.0-preview.1`.

`GeesNO.Core.Runtime` brings `GeesNO.Core.Contracts` and
`GeesNO.Core.Primitives` transitively.

## Package Source

After `0.1.0-preview.1` is published to NuGet.org, this sample can be restored
and run directly:

```bat
dotnet restore samples\GeesNO.Minimal.Integration\GeesNO.Minimal.Integration.csproj
dotnet run --project samples\GeesNO.Minimal.Integration\GeesNO.Minimal.Integration.csproj
```

Before publication, the repository validates this sample against the local
package feed produced by `scripts\release\preview-dry-run.ps1`. The sample
project itself does not contain a hardcoded local feed or machine path.

## Expected Output Shape

Operation identifiers are generated at runtime, but the output shape is:

```text
sample=geesno-minimal-integration
capability.count=1
capability.0.providerId=sample.minimal-provider
capability.0.reference=sample.greeting@1.0.0
capability.0.support=Native
positive=passed status=Success
positive.operationId=<generated>
positive.capability=sample.greeting@1.0.0
positive.providerId=sample.minimal-provider
positive.status=Success
positive.declaredResultType=GreetingResult
positive.result.message=Hello, external developer.
positive.tool.executions=1
fail_closed=passed status=Rejected error=runtime.capability-not-found
fail_closed.operationId=<generated>
fail_closed.capability=sample.unsupported@1.0.0
fail_closed.providerId=sample.minimal-provider
fail_closed.status=Rejected
fail_closed.declaredResultType=<none>
fail_closed.error.code=runtime.capability-not-found
fail_closed.tool.executions=1
sample.result=passed
```

If a required assertion fails, the process exits with a non-zero exit code.
