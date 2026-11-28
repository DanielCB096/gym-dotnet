# Pruebas de staging — 1.7.13 e historial de membresía (2026-11-28)

**Equipo:** PC con SQL Server local (`MSSQLSERVER`, puerto 1433).  
**Servicio de producción:** `centrofitgymservice` en **1.7.9**, healthy.  
**Base de staging:** `GymManager_Staging`, restaurada desde `D:\GymBackups\GymManager_20261128_081015.bak` (42 tablas).  
**Regla:** no se tocó `GymManager` ni el servicio de producción.

## 1. Paquete 1.7.13

- Se extrajo `artifacts/GymService-1.7.13-win-x64.zip` a una carpeta temporal.
- Se copió `appsettings.json` de la instalación y se apuntó a `GymManager_Staging`.
- Se ejecutó en consola en el puerto **5056** con `WebServer__Urls=http://localhost:5056`.
- `/health` respondió **200**:
  - `readiness.version = 1.7.13`
  - base healthy, 14/14 módulos, Kestrel healthy.
- Migración aditiva aplicada en staging:
  - 42 tablas.
  - Permiso `audit.view` presente en `GYM_PERMISSIONS`.
- Endpoints sin token:
  - `/api/audit` → **401**
  - `/api/members` → **401**
- Sin errores en el log de la instancia.

**Resultado:** el binario 1.7.13 arranca y migra correctamente sobre una copia de producción.

## 2. Historial de membresía (D)

- Rama **`feat/membership-history-reapply`** creada desde `main` (`3271bcf`).
- Se revirtió el revert `a742fa1` (`c862a5c`), reaplicando la tabla, eventos, endpoint y UI.
- Grafo actualizado en `c11739f`.
- Build: **0 errores** (129 advertencias preexistentes).
- Pruebas: **138/138**.
- Instancia de staging en puerto **5057** contra `GymManager_Staging`:
  - `/health` **200**, versión **1.7.13**.
  - 43 tablas.
  - Tabla `GYM_MEMBERSHIP_EVENTS` creada con:
    - `ID`, `MEMBER_ID`, `MEMBERSHIP_ID`, `EVENT_TYPE`, `EVENT_AT`, `PLAN_ID`, fechas, importe, moneda, `PAYMENT_ID`, motivo, usuario, `SOURCE`, `DETAILS` y columnas de auditoría.
    - Índices: `GYM_MEMBERSHIP_EVENTS_PK`, `IX_..._MEMBER`, `IX_..._MEMBERSHIP`, `IX_..._AT`, `IX_..._TYPE`.
  - No se creó trigger de inmutabilidad; la protección es por código. El diseño dice “triggers”; queda como diferencia a decidir.

**Resultado:** D compila, pasa pruebas y su esquema se crea correctamente en staging. **No se probó el flujo vivo** de alta, renovación, congelamiento, reanudación, cancelación y pago porque requiere credenciales de la instalación.

## 3. Pendiente para cerrar D

1. Probar en vivo con una copia de producción y credenciales autorizadas:
   - alta, dos renovaciones, congelar, reanudar y cancelar;
   - abrir **Miembros → Membresía → Historial** y verificar línea de tiempo, importes, usuario y motivo.
2. Probar que un fallo al escribir el evento revierte la operación.
3. Revisar compatibilidad cuando no hay eventos.
4. Backfill marcado `source=backfill` y reporte de calidad.
5. Decidir si se agrega trigger de inmutabilidad o se documenta la protección por código.

## 4. Limpieza

- Se detuvieron las instancias de staging.
- Se eliminó `GymManager_Staging` después de registrar la evidencia.
- No se modificó la base de producción ni el servicio 1.7.9.

## 5. Siguiente paso

1. Desplegar **1.7.13** en producción en una ventana con UAC, siguiendo `release-1.7.13.md`.
2. Probar D en vivo y, si pasa, fusionar `feat/membership-history-reapply`.
3. Después, evaluar C1 en modo sombra (`feat/collections-shadow`).
