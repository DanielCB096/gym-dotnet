# Pendientes de entrega

> Actualizado: 2026-11-21. Lista viva de lo que falta para producción.
> No sustituye a `CHANGELOG.md` (historial) ni a `docs/reglas-negocio.md`.

## 0. Despliegue 1.7.13

Runbook y MSI 1.7.13 preparados. Artefacto: `artifacts/GymService-1.7.13-win-x64.msi` (48.35 MB). Pendiente: ventana, UAC y ensayo en copia de producción.

## 1. Rebranding: retirar la marca anterior

**Estado:** **A + B aplicado** el 2026-11-21 (versión 1.6.0). **Fase C pendiente de decisión.**

### Hecho (A + B)

| Ámbito | Resultado |
|---|---|
| Marca visible | Servicio `centrofitgymservice` / "Centro Fit Gym Service"; ruta `C:\Program Files\Centro Fit\GymService`; MSI "Centro Fit Gym Manager" / `Manufacturer="Centro Fit"`; licencia, tareas y documentación |
| Build / repo | `GYM_LIBS_ROOT` / `-GymLibsRoot`; `Tools/GymModelGenerator`; `third_party/gym-core`; scripts y documentación |
| Comentarios generados | 42 modelos con "GymCore Code Generator" / "Centro Fit"; plantilla local del generador actualizada |
| Versión | 1.6.0 |

### Restos deliberados (fase C)

- Ensamblados/proyectos `AnxoDatabase`, `AnxoLibrary`, `AnxoObjectDatabaseManager`,
  `AnxoServiceTools`, `AnxoDatabaseGenerator` y namespaces `Ax*`.
- Tabla `ANXO_SEQUENCES`: es el nombre de esquema que usa la librería; renombrarla exige
  migración de datos y cambios en la librería.
- Entradas históricas de `CHANGELOG.md` (se conservan como registro).
- `third_party/gym-core` sigue **fuera de Git** y **sin LICENSE**. Antes de un fork
  white-label hay que confirmar propiedad/licencia y versionar la copia.

### Fase C (opcional, white-label completo)

Forkear las 5 librerías y renombrar proyectos, ensamblados y namespaces
(`AxDatabase` → `GymCore.Database`, `AxLogger` → `GymCore.Logging`, etc.). Actualizar
165 `using`, referencias a tipos, `ANXO_SEQUENCES`, el generador y los builds.
Esfuerzo: 3–5 días. Riesgo: alto; requiere regresión completa. Solo si el código es propio
o la licencia lo permite.

### Pendiente operativo del despliegue

- [x] Migrar la instalación actual al servicio/ruta nuevos (2026-11-21): `centrofitgymservice`,
      `C:\Program Files\Centro Fit\GymService`, versión 1.6.0; `/health`, login, respaldos y
      restauración verificados. El MSI 1.5.1 se desinstaló y queda una sola regla de firewall.
- [ ] Instalar la CA raíz de HTTPS en todos los equipos cliente y en el kiosco; actualizar
      su `apiUrl` a `https://192.168.100.53:5055`. El paquete `CentroFit-CA.zip` está
      generado y probado en Windows; la CA ya se instaló y verificó en un tablet Android 16
      (panel y kiosco cargan por HTTPS). Falta el resto de equipos.
- [x] Registrar y validar el kiosco (2026-11-21): dispositivo **Kiosco recepcion** (id 1),
      tablet Android 16 configurado con URL HTTPS y token; token válido hasta **2027-02-19**.
      Un código inválido devuelve “Código inválido o expirado”. **Código válido probado**:
      el kiosco mostró “Acceso concedido — Prueba Kiosco”, el log quedó `granted` y el código
      `used`. El socio/membresía/código/acceso temporales se eliminaron y las secuencias se
      sincronizaron con `max(ID)` mediante `deploy/maintenance/Sync-Sequences.ps1`; el kiosco
      sigue activo. El **audio de concedido/denegado** también quedó verificado en el tablet.
- [x] Kiosco local en este PC (2026-11-21): dispositivo **Kiosco PC** (id 501), perfil
      dedicado de Edge, lanzador `deploy/kiosk-pc/Start-Kiosk.ps1` que detecta la segunda
      pantalla y accesos directos en Escritorio/Inicio. Teclado físico verificado
      (`000000` + Enter → “Código inválido o expirado”) y **acceso válido** probado en
      ventana: “Acceso concedido — Prueba Kiosco”; log `granted`, código `used` y datos
      temporales eliminados. Solo hay un monitor conectado; falta probar la apertura
      fullscreen en una segunda pantalla física.

## 2. Otros pendientes de entrega

- [x] Correo real del owner y contraseña fuerte; login verificado. Las credenciales se
      guardan fuera de Git y el archivo temporal debe moverse a un gestor de contraseñas.
- [x] **MFA completo** (2026-11-21, versión 1.7.0): login con desafío firmado de un solo uso,
      verificación TOTP o código de respaldo, pantalla **Seguridad** con QR/clave manual/códigos,
      activación, desactivación y estado. Probado 21/21 por API HTTPS y en navegador (activar,
      login en dos pasos y desactivar). **Owner con MFA activo** desde 2026-11-21; el login real
      devuelve `requiresMfa` sin sesión. Quedan 9 códigos de respaldo de un solo uso: deben
      guardarse fuera del servidor y borrar los archivos temporales del Escritorio.
- [x] Limpiar datos demo (2026-11-21): 6 socios, 2 productos, 2 membresías, 1 acceso,
      1 crédito, 1 OTP y 11 sesiones eliminados. Se conservaron owner, 27 permisos y los
      3 planes (Mensual/Trimestral/Anual). Respaldo verificado en
      `D:\GymBackups\GymManager_20261121_101854.bak`; dashboard y tablas operativas en 0.
      **Corrección:** no se deben reiniciar las secuencias a 1 si quedan filas; usar
      `deploy/maintenance/Sync-Sequences.ps1 -Apply` y reiniciar el servicio.
- [x] SMTP real configurado y OTP por correo verificado (envío, verificación y uso único).
- [x] Copia offsite cifrada en OneDrive (2026-11-21): **41 archivos** `.7z` AES-256 en
      `C:\Users\PC\OneDrive\GymServiceBackups`; tarea `GymService-Offsite` cada 15 min como
      `SYSTEM` (`LastTaskResult=0`), retención 30/7. Restauración verificada (hash idéntico,
      42 tablas) y **recuperación real**: se descifró el último respaldo, se restauró en una
      base temporal, se arrancó una segunda instancia de la app, se validó login/panel/dashboard
      y se eliminó todo. La contraseña está en un archivo protegido del servidor y en una copia
      del Escritorio; debe guardarse en un gestor y borrarse el archivo temporal.
- [x] **Sincronización OneDrive corregida** (2026-11-21): los `.7z` se movían desde
      `ProgramData` conservando ACL de `SYSTEM`, por lo que OneDrive los dejaba en
      “Sincronización pendiente”. `Sync-OffsiteBackups.ps1` ahora ejecuta `icacls /reset`
      después de mover; los 40 archivos existentes se corrigieron y los 41 quedaron
      “Disponible en este dispositivo”. Archivo nuevo validado con `7z t`.

- [x] **Cuenta dedicada del servicio** (2026-11-21, versión 1.7.1): el servicio corre como
      `NT SERVICE\centrofitgymservice` (cuenta virtual), no `LocalSystem`. ACLs mínimas sobre
      aplicación, logs, `appsettings.json` y PFX; el proceso se verificó con
      `GetOwner = NT SERVICE\centrofitgymservice`, `/health` healthy, login, SMTP real y tarea
      offsite con resultado 0. Script de aplicación/rollback:
      `deploy/security/Set-ServiceAccount.ps1`; MSI e instaladores actualizados.
- [x] **Login `sa` deshabilitado** (2026-11-21): `ALTER LOGIN sa DISABLE` con la cuenta Windows
      administradora del servidor. `gym_app` sigue operando la aplicación y los respaldos;
      `GymService-Full`, `GymService-Log` y `GymService-Offsite` terminaron con resultado 0 y
      OneDrive quedó con 43/43 archivos disponibles. La administración SQL se hace con
      autenticación Windows del servidor.

## 2.1 Historial de membresía (D)

Implementado en `feat/membership-history`: tabla `gym_membership_events`, escritura en alta/renovación/congelamiento/reanudación/cancelación/pago, API `events` y línea de tiempo en el modal. 138 pruebas unitarias; **pendiente prueba en vivo con SQL Server, backfill y merge**. Ver [`membership-history-d.md`](membership-history-d.md).

## 3. Hallazgos del QA completo (2026-11-21)

Informe detallado: [`qa-2026-11-21.md`](qa-2026-11-21.md).

- [x] **P0 — Clave MFA respaldada y recuperación** (2026-11-21, versión 1.7.2): el fallback de
      códigos de respaldo funciona aunque la clave de cifrado sea distinta; el offsite genera
      `mfa-key-*.7z` cifrado y se verificó que coincide con `appsettings.json`; reset administrativo
      por API y UI, con revocación de sesiones y protección del owner. Pruebas: 133/133.
- [x] **P1 — Ciclo de vida de usuarios** (2026-11-22, versión 1.7.3): cambio de contraseña
      propia, reset administrativo, edición de correo/nombre/rol y activación/desactivación,
      con revocación de sesiones y protecciones del owner/self. Probado por API y UI; 135/135.
- [x] **P1 — Endurecimiento mínimo** (2026-11-22, versión 1.7.4): Swagger apagado por defecto
      (`WebServer:EnableSwagger=false`, `/swagger` 404) y sin `DeveloperExceptionPage`; verificado
      panel/API/health. CSP/cabeceras completas, HSTS, CORS y `QueueLimit=0` quedan **aplazados**
      por riesgo bajo en LAN privada; reconsiderar si hay portal público o multi-sucursal.
- [x] **P2 - Reportes programados** (2026-11-22, versión 1.7.6): ejecución real por hosted
      service, Excel adjunto por SMTP, destinatarios en UI, `NEXT_RUN`/`LAST_RUN`; desplegado y
      probado en vivo con envío real y adjunto.
- [x] **P2 - Impresión de rutinas** (2026-11-23, versión 1.7.7): vista previa A4 desde
      Asignaciones, con socio, rutina, días, series, reps, equipo y notas; Imprimir/Guardar PDF.
      Probada en instancia temporal y en producción; datos temporales eliminados.
- [x] **P2 - Ficha completa del socio** (2026-11-23, versión 1.7.8): CURP, RFC, dirección,
      emergencia, foto (URL) y notas guardados y editables; ficha A4 con membresía y notas,
      Imprimir/Guardar PDF. Probada en vivo; datos temporales eliminados.
- [x] **P2 - Foto real del socio** (2026-11-24, versión 1.7.9): subida JPG/PNG/WebP hasta 5 MB,
      validación de firma, almacenamiento GUID, avatar y ficha con foto. Subida/descarga probadas
      por API; build/lint correctos.
- [x] **P2 - Comprobante imprimible de facturas** (2026-11-25, versión 1.7.10): subtotal, IVA,
      total, estado y nota de que no sustituye CFDI; Imprimir/Guardar PDF. Probado en instancia
      temporal; **despliegue pendiente de UAC** (servidor sigue en 1.7.9).
- [x] **P2 - Gráficas de progreso** (2026-11-25, versión 1.7.11): peso por ejercicio y evolución
      por medida, SVG sin dependencias, selector de socio/tipo. Probado en instancia temporal con
      3 puntos por gráfica; build/lint correctos. **Despliegue pendiente de UAC.**
- [x] **P2 - Búsqueda global y campana** (2026-11-25, versión 1.7.12): búsqueda de socios, planes
      y facturas; campana con membresías por vencer y stock bajo. Probado en instancia temporal;
      **despliegue pendiente de UAC.**
- [x] **P2 - Visor de auditoría** (2026-11-25, versión 1.7.13): sesiones con estado
      activa/expirada/revocada, accesos con filtros y resumen de 24 h; permiso `audit.view` solo
      para OWNER/ADMIN. Migración idempotente al arrancar. Probado por API y navegador;
      **despliegue pendiente de UAC.**
- [ ] **P2 - Otras funciones anunciadas.** Push sin emisor y PDF de facturas sin generación.
- [x] **P2 — Monitoreo de respaldos** (2026-11-22, versión 1.7.5): monitor diario con alerta
      SMTP, verificación semanal de restauración real, control de espacio local y copia de clave
      MFA; el respaldo completo ahora tiene `WakeToRun`. Queda pendiente consultar la **cuota de
      OneDrive** (requiere Graph/API); el resto está cubierto.
- [ ] **Ventana:** reinicio completo del servidor e instalación del MSI en una VM limpia.
