# Handoff para la PC de casa

**Para:** la IA/persona que continúe en la PC con SQL Server.  
**Fecha:** 2026-11-27.  
**Repo:** `C:\proyectos\gimnasio-dotnet` (GitHub `DanielCB096/gym-dotnet`).  
**Regla:** en esta PC no hay SQL Server; aquí solo se compila y se corren pruebas unitarias. La prueba real y el despliegue se hacen en casa.

## 1. Estado del repositorio

- `main` = `a742fa1` (runbook, MSI y `npm ci`; D se revirtió de main hasta probarlo).
- Rama **`feat/membership-history`** = `1575767`:
  - tabla `GYM_MEMBERSHIP_EVENTS` (42 tablas GYM);
  - eventos en alta, renovación, congelamiento, reanudación, cancelación y pago;
  - endpoint `GET /api/members/{id}/memberships` devuelve `events`;
  - modal de membresía con línea de tiempo.
  - **No fusionada**: falta prueba en vivo, backfill y merge.
- Rama **`feat/collections-shadow`**: C1 en modo aviso, endpoint `/api/members/collections` y modal **Cobranza**. No bloquea accesos; pendiente prueba con SQL Server.

## 2. Entorno

```powershell
$env:GYM_LIBS_ROOT = "C:\repositorios\pilot\ANXO LIBRARIES"
cd C:\proyectos\gimnasio-dotnet
dotnet build GymService.slnx -c Release
dotnet test GymService.Tests\GymService.Tests.csproj -c Release --no-build
```

- Backend: .NET 10, SQL Server (Express sirve), login `gym_app`, base `GymManager`.
- Frontend: `cd GymServiceFront; npm ci; npm run lint; npm run build`.
- Pruebas de integración: definir `GYM_TEST_DB_SERVER`, `GYM_TEST_DB_USER`, `GYM_TEST_DB_PASSWORD` y ejecutar la suite completa. La app **no** se prueba contra producción.
- CI: el runner self-hosted está caído; los runs de GitHub salen cancelados. La referencia local es build + unitarias (138) + lint/build del frontend.

## 3. Desplegar 1.7.13 (primero)

- Runbook: [`release-1.7.13.md`](release-1.7.13.md).
- MSI listo: `artifacts/GymService-1.7.13-win-x64.msi` (48.35 MB).
- Requiere UAC/administrador y una copia de producción para el ensayo.
- El servidor sigue en **1.7.9**; los paquetes 1.7.10–1.7.13 están pendientes de ventana.
- No desplegar D ni C1 hasta que 1.7.13 esté estable.

## 4. Probar D (historial)

1. Restaurar una copia de producción o crear una base temporal.
2. Arrancar la rama `feat/membership-history` contra esa base.
3. Confirmar que `gym_membership_events` se crea al iniciar.
4. Socio de prueba: alta → renovar dos veces → congelar → reanudar → cancelar.
5. Abrir **Miembros → Membresía → Historial**: debe mostrar alta, renovaciones, congelamiento, reanudación, cancelación y pagos, con fecha, plan, importe y usuario.
6. Probar que un fallo al escribir el evento revierte la operación (base temporal).
7. Borrar datos temporales y registrar evidencia en `docs/pruebas-en-vivo.md`.
8. Revisar que el historial anterior siga visible cuando no haya eventos (compatibilidad).
9. Hacer backfill marcado como `source=backfill` (fase 2 del diseño) y revisar el reporte de calidad; **no** usarlo para bloqueos.

Diseño completo: [`membership-history-d.md`](membership-history-d.md).

## 5. C1 en modo aviso

- Lista de socios vencidos y próximos a vencer, saldo pendiente y último pago.
- **No bloquear accesos** todavía; solo marcar y reportar.
- Activar el bloqueo duro solo con aprobación del dueño y después de 1–2 semanas en sombra.
- Separar en la UI: saldo pendiente, membresía vencida y acceso bloqueado.

## 6. Cuidados

- No usar la base de producción para pruebas.
- No reiniciar secuencias si hay filas; usar `deploy/maintenance/Sync-Sequences.ps1 -Apply`.
- No commitear `appsettings.json`, contraseñas, PFX, bases ni datos de socios.
- Backup antes de cualquier migración; las migraciones son aditivas al arrancar.
- Registrar evidencia de cada prueba en vivo; no marcar como terminado solo por compilar.

## 7. Archivos tocados por D

- `GymService/Models/_ModelGym.xml`
- `GymService/Models/Generated/GYM_MEMBERSHIP_EVENTS.cs`
- `GymService/Models/Generated/_ModelGym.cs`
- `GymService/Modules/Members/MembersModule.cs`
- `GymServiceFront/src/components/MembershipModal.jsx`
- `GymService.Tests/ModelGymTests.cs`, `GymService.Tests/MappingSchemaTests.cs`
- `GymService.Tests/Integration/SchemaTests.cs`
- `README.md`, `CHANGELOG.md`, `docs/pendientes.md`, `docs/membership-history-d.md`
