# Pendientes de entrega

> Actualizado: 2026-11-21. Lista viva de lo que falta para producción.
> No sustituye a `CHANGELOG.md` (historial) ni a `docs/reglas-negocio.md`.

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
- [x] Copia offsite cifrada en OneDrive (2026-11-21): 36 archivos `.7z` AES-256 en
      `C:\Users\PC\OneDrive\GymServiceBackups`; tarea `GymService-Offsite` cada 15 min como
      `SYSTEM` (`LastTaskResult=0`), retención 30/7. Restauración verificada (hash idéntico y
      42 tablas). La contraseña está en un archivo protegido del servidor y en una copia del
      Escritorio; debe guardarse en un gestor y borrarse el archivo temporal.
- [x] **Cuenta dedicada del servicio** (2026-11-21, versión 1.7.1): el servicio corre como
      `NT SERVICE\centrofitgymservice` (cuenta virtual), no `LocalSystem`. ACLs mínimas sobre
      aplicación, logs, `appsettings.json` y PFX; el proceso se verificó con
      `GetOwner = NT SERVICE\centrofitgymservice`, `/health` healthy, login, SMTP real y tarea
      offsite con resultado 0. Script de aplicación/rollback:
      `deploy/security/Set-ServiceAccount.ps1`; MSI e instaladores actualizados.
