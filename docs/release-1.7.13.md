# Runbook — despliegue 1.7.9 → 1.7.13

**Estado:** preparado, pendiente de ventana y UAC.  
**Fecha:** 2026-11-27.  
**Alcance:** producción de Centro Fit Gym Manager (Windows + SQL Server Express + servicio `centrofitgymservice`).  
**Objetivo:** desplegar 1.7.10–1.7.13 sin pérdida de datos, con respaldo verificado y rollback probado.

> Regla: no se toca producción sin haber hecho el ensayo completo en una copia. El CI self-hosted está caído; la validación local (`build` + 137 pruebas) y el ensayo son la red de seguridad.

## 0. Datos que hay que confirmar antes

- Edición y modelo de recuperación de SQL Server (`SELECT SERVERPROPERTY('Edition'), SERVERPROPERTY('RecoveryModel')`). En este equipo es **Express**; los respaldos usan FULL + LOG.
- Si las migraciones se aplican **al arrancar** (sí: `DatabaseService` crea/valida de forma aditiva). Eso significa que el primer arranque de 1.7.13 modifica el esquema.
- Si hay torniquete o lector físico: **no**; el acceso es kiosco/recepción. Por eso el antipassback queda fuera.
- Ruta real de la instalación (`C:\Program Files\Centro Fit\GymService` por defecto), carpeta de fotos/PDFs y carpeta de respaldos (`D:\GymBackups`).
- Cuenta del servicio (`NT SERVICE\centrofitgymservice`) y permisos sobre appsettings, logs, PFX, fotos y PDFs.
- Quién tiene la clave MFA y los códigos de respaldo del owner. No puede ser un punto único de fallo.

## 1. Ensayo en copia (obligatorio, 1–2 días antes)

1. Tomar el respaldo completo más reciente y verificado de producción.
2. Restaurarlo en la misma instancia como `GymManager_Staging` (con `MOVE` a archivos temporales; no tocar `GymManager`).
3. Publicar 1.7.13 en una carpeta aparte y apuntar una instancia de prueba a la copia:

```powershell
$env:Database__Database = "GymManager_Staging"
$env:WebServer__Port = "5056"
# Arrancar en consola, no como servicio:
.\GymService.exe -console
```

4. Anotar el tiempo de cada migración y los conteos antes/después:

```sql
SELECT COUNT(*) FROM gym_members;
SELECT COUNT(*) FROM gym_memberships;
SELECT COUNT(*) FROM gym_payments;
SELECT COUNT(*) FROM gym_access_logs;
SELECT COUNT(*) FROM gym_sales;
```

5. Correr la lista de humo de la sección 4 contra la copia.
6. **Probar el rollback**: restaurar el respaldo sobre `GymManager_Staging`, arrancar 1.7.9 y confirmar login/acceso/pago. Medir el tiempo total; ese tiempo define el límite de aborto en producción.
7. Registrar evidencia: versión, fecha, tiempos, conteos y resultado de cada prueba.

## 2. Preflight de producción (sin tocar nada)

- [ ] Espacio libre en disco para datos, log, respaldos y copia local del binario anterior.
- [ ] Runtime .NET 10 instalado (o publicación self-contained/MSI).
- [ ] `appsettings.json` de producción respaldado fuera del servidor; diff contra las claves nuevas de 1.7.13 (MFA, respaldos, monitoreo, SMTP, reportes).
- [ ] Certificado HTTPS y PFX con permiso de lectura para la cuenta del servicio.
- [ ] UAC resuelto: ventana con credenciales de administrador local.
- [ ] Respaldos programados (`GymService-Full`, `GymService-Log`, `GymService-Offsite`) con último resultado 0.
- [ ] Copia de la carpeta actual `1.7.9` y de `appsettings.json` en una ruta de rollback.
- [ ] Aviso a recepción: sistema fuera de línea X minutos; plan manual para registrar accesos y pagos durante la ventana.
- [ ] Códigos de respaldo MFA del owner a la mano, fuera del servidor.

## 3. Ventana de despliegue (domingo noche o madrugada)

1. **Detener el servicio** para congelar escrituras:

```powershell
Stop-Service centrofitgymservice
```

2. **Respaldo completo** con copia fuera del servidor:

```powershell
.\backup.ps1 -CopyTo "\\ruta-externa\CentroFit"
# Verificar el .bak con RESTORE VERIFYONLY; no continuar si falla.
```

3. **Respaldar archivos**: carpeta actual de la app, `appsettings.json`, certificados, fotos y PDFs.
4. **Registrar estado**: `sc qc centrofitgymservice`, `sc qfailure centrofitgymservice`, versión actual y conteos de la sección 1.
5. **Instalar 1.7.13** con el MSI/ZIP generado (UAC) o copiar a una carpeta versionada. No sobrescribir la carpeta 1.7.9.
6. **Primer arranque**: iniciar el servicio y vigilar logs. Las migraciones son aditivas y automáticas; si falla el arranque, no reintentar a ciegas: revisar el log y decidir rollback.
7. **Humo inmediato** (sección 4). Si acceso o cobros fallan, rollback antes de 45 minutos.

```powershell
Start-Service centrofitgymservice
Invoke-WebRequest https://localhost:5055/health -UseBasicParsing
```

## 4. Lista de humo

**Acceso (crítico)**
- [ ] Login del owner con MFA; probar un código de respaldo si es necesario.
- [ ] Login de recepción (sin MFA) y del admin.
- [ ] Socio activo: el kiosco concede y registra `granted`.
- [ ] Socio vencido y socio sin membresía: deniega con mensaje correcto.
- [ ] Un acceso concedido y uno denegado aparecen en Auditoría.

**Cobros (crítico)**
- [ ] Pago de prueba (socio de prueba) con método, importe y vigencia correctos.
- [ ] Recibo de membresía y factura imprimible (recordar: no es CFDI).
- [ ] Corte de caja del día coincide con el conteo previo.
- [ ] El pago y la factura quedan en el historial del socio.

**Operación**
- [ ] Búsqueda global (socio, plan, factura) y campana de notificaciones.
- [ ] Ficha del socio con foto y rutina PDF.
- [ ] Gráficas de progreso.
- [ ] Reporte programado de prueba (o ejecución manual).
- [ ] Respaldo local nuevo y subida offsite; verificar que el archivo se puede descifrar/restaurar en staging.
- [ ] Monitoreo reporta salud y la alerta llega a una persona real.

## 5. Criterios de aborto y rollback

**Abortar si en 45 minutos:** no hay login, el acceso concede a vencidos o niega a activos de forma masiva, un pago no se registra, el corte no cuadra o el servicio no arranca de forma estable.

**Rollback de binarios** (si no se aplicaron migraciones o son compatibles hacia atrás):
1. Detener el servicio.
2. Apuntar el servicio a la carpeta 1.7.9 y restaurar su `appsettings`.
3. Iniciar y repetir humo de acceso/cobros.

**Rollback completo** (si ya se aplicaron migraciones incompatibles):
1. Detener el servicio.
2. `RESTORE DATABASE GymManager FROM DISK=... WITH REPLACE` (con `SINGLE_USER`/`ROLLBACK IMMEDIATE` si hay conexiones).
3. Volver a binarios 1.7.9.
4. Comparar conteos con los registrados y repetir humo.

Se pierde lo capturado entre el respaldo y el rollback. Por eso la ventana se hace con el servicio detenido.

## 6. Después del despliegue

- Vigilar 24–48 h: logs, accesos denegados inesperados, pagos y respaldos.
- Confirmar que las tareas `GymService-Full`, `GymService-Log` y `GymService-Offsite` siguen con resultado 0.
- Guardar evidencia: versión instalada, `health`, capturas de humo y conteos.
- Actualizar `docs/pendientes.md` y `CHANGELOG.md` con la versión desplegada.
- Recién entonces retomar el historial de membresía (D) y el control de adeudos (C1).

## 7. Riesgos conocidos

| Riesgo | Mitigación |
|---|---|
| MFA bloquea al único owner | Códigos de respaldo fuera del servidor; reset documentado; probar en staging |
| Migración cambia vigencia/estatus | Conteos antes/después y humo de activo/vencido |
| Zona horaria o fechas | Probar un pago cerca de medianoche y un vencimiento del día |
| `appsettings` incompleto | Diff contra la plantilla y revisar logs de arranque |
| Permisos de la cuenta de servicio | Probar con la cuenta real: fotos, PDFs, respaldos, PFX |
| Log de SQL Server llena el disco | Verificar espacio y respaldo de log antes de la ventana |
| CI caído | Timebox de 1 día; build + pruebas locales como sustituto documentado |
