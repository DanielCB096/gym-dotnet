# AGENTS.md — GymService (.NET)

> Guía corta para agentes que trabajen en este repositorio. Las reglas de negocio completas
> están en [`docs/reglas-negocio.md`](docs/reglas-negocio.md).

## Antes de escribir código

1. Leer `README.md`, `docs/reglas-negocio.md` y el módulo que se va a tocar.
2. Revisar `CHANGELOG.md` para conocer el estado y las decisiones recientes.
3. Comprobar el estado del árbol de trabajo; no sobrescribir cambios ajenos.
4. Si una regla de negocio no está clara, preguntar antes de implementar.

## Stack y estructura

- Backend: .NET 10 + ASP.NET Core Kestrel + SQL Server mediante Anxo.
- Frontend: React 19 + Vite + Tailwind CSS 4.
- Esquema: `GymService/Models/_ModelGym.xml`; entidades generadas en `Models/Generated/`.
- Lógica de negocio en `GymService/Modules/`; los controladores solo validan y delegan.
- Lecturas con SQL parametrizado (`?`). Escrituras con `Session.InsertObject`,
  `InsertObjectWithID`, `UpdateObjectChanges` y `DeleteObject`.

## Convenciones

- Nombres de código en inglés; textos visibles en español.
- Dinero con `decimal` exacto; nunca `float`.
- Fechas de negocio en fecha local del gimnasio; auditoría con la fecha del servidor.
- Mantener las claves y formas de respuesta que consume el frontend.
- Archivos de texto en UTF-8 sin BOM.
- Cambios pequeños y verificables; no reformatear archivos completos sin motivo.

## Reglas no negociables

1. El acceso solo se concede con cuenta activa y membresía vigente, no congelada.
2. Sin periodo de gracia: el acceso se bloquea en la fecha de vencimiento.
3. Una renovación extiende desde `max(hoy, vencimiento actual)`.
4. Una membresía congelada no se renueva ni permite acceso.
5. La activación sin cobro (cortesía) debe ser explícita y quedar auditada.
6. Los históricos no se borran: se desactivan, archivan o marcan con estado.
7. Los permisos se validan en backend; el frontend solo oculta acciones.
8. Nunca guardar datos de tarjeta (PAN/CVV) ni secretos en el repositorio.
9. Los cobros manuales registran método, importe, fecha y usuario.
10. Toda ruta nueva debe tener su entrada en `RolePermissionMiddleware`.

## Pruebas y verificación

- **Toda funcionalidad debe probarse en vivo antes de darse por terminada.**
  Ver [`docs/pruebas-en-vivo.md`](docs/pruebas-en-vivo.md).
- `dotnet build GymService.slnx -c Release`
- Pruebas unitarias: `dotnet test GymService.Tests\GymService.Tests.csproj -c Release --no-build`
- Integración: definir `GYM_TEST_DB_SERVER`, `GYM_TEST_DB_USER` y `GYM_TEST_DB_PASSWORD`.
- Frontend: `npm run build` y `npm run lint` en `GymServiceFront/`.
- Probar el flujo real en API y navegador (incluyendo modales, validaciones y consola).
  Usar base temporal o datos autorizados para acciones destructivas.

## Documentación obligatoria

- Actualizar `README.md` si cambia el comportamiento visible.
- Actualizar `docs/reglas-negocio.md` si cambia una regla de negocio.
- Añadir entrada en `CHANGELOG.md`.
- Indicar en la respuesta final qué se cambió, qué se verificó y qué quedó pendiente.

## Git

- `main` estable; ramas `feat/...`, `fix/...`, `docs/...`, `chore/...`.
- Commits pequeños y claros.
- No subir `.env`, credenciales, llaves, bases de datos locales ni datos reales.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. When the answer is under a Markdown heading, `graphify section "<heading>"` prints that section's text, so the .md need not be opened. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.
