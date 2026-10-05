# ADR-0001: GeesNO.Core assembly layout

Status: Accepted

## Context

GeesNO.Core necesita una base pequena, compilable, testeable y libre de CAD,
IA, MCP, UI, COM y APIs propietarias dentro del Core minimo.

## Decision

La solucion se organiza en cuatro assemblies Core de produccion, un proyecto de
referencia, un sample y un proyecto de pruebas:

- `GeesNO.Core.Primitives`: tipos fundamentales, identificadores, resultados, errores, versiones y metadatos basicos.
- `GeesNO.Core.Contracts`: contratos publicos tipados para capacidades, operaciones y herramientas.
- `GeesNO.Core.Runtime`: implementacion interna del catalogo, invocadores, ToolEngine, observabilidad interna y `system.ping`.
- `GeesNO.Core.Host`: ejecutable y composition root.
- `GeesNO.Reference.Simulator`: referencia no normativa del perfil neutral.
- `GeesNO.Sample.Cli`: sample no normativo de consumo publico.
- `GeesNO.Core.Tests`: pruebas unitarias y de arquitectura.

Runtime exporta una fachada publica minima. Tests son friend assembly para
validar internals. Host tambien es friend assembly para componer y ejecutar el
flujo diagnostico local.

No se crea `GeesNO.Core.Hosting`. CAD, IA y MCP quedan fuera del Core minimo y requeriran decisiones posteriores con contenido verificable y pruebas.

## Consequences

- El Core queda pequeno y validable.
- Contracts puede mantenerse estable sin acoplarse a Runtime o Host.
- Runtime puede evolucionar internamente sin prometer una fachada publica prematura.
- Host demuestra ejecucion real mediante `system.ping`, pero su amistad con Runtime debe revisarse cuando exista una API publica justificada.
- Los dominios CAD, adaptadores, SDK, IA y MCP no pueden aparecer como carpetas o proyectos vacios.
