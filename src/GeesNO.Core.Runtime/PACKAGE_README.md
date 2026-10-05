# GeesNO.Core.Runtime

GeesNO.Core.Runtime contains the public headless Runtime facade used to register
capability manifests and tools, inspect available Runtime capabilities, and
execute typed GeesNO.Core operations.

The package depends on the corresponding GeesNO.Core.Contracts and
GeesNO.Core.Primitives packages. It exposes the public Runtime facade only;
Runtime internals such as ToolEngine, catalog snapshots, observability, event
bus, and journal implementations are not public API.

This package is part of the GeesNO.Core Developer Preview. The public API is
pre-1.0 and may change before a stable release. See the repository
documentation included with the GeesNO.Core source distribution for versioning,
compatibility, security, contribution, and trademark policies.
