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
      restauraron a 1; el kiosco sigue activo. El **audio de concedido/denegado** también
      quedó verificado en el tablet.

## 2. Otros pendientes de entrega

- [x] Correo real del owner y contraseña fuerte; login verificado. Las credenciales se
      guardan fuera de Git y el archivo temporal debe moverse a un gestor de contraseñas.
- [ ] **Implementar el flujo de login MFA** antes de activarlo: el backend devuelve
      `requiresMfa` sin token/sesión, `mfa/verify` exige una sesión autenticada y el frontend
      no tiene pantalla de MFA/OTP. Activarlo hoy dejaría al owner sin acceso.
- [x] Limpiar datos demo (2026-11-21): 6 socios, 2 productos, 2 membresías, 1 acceso,
      1 crédito, 1 OTP y 11 sesiones eliminados. Se conservaron owner, 27 permisos y los
      3 planes (Mensual/Trimestral/Anual). Respaldo verificado en
      `D:\GymBackups\GymManager_20261121_101854.bak`; dashboard y tablas operativas en 0.
- [x] SMTP real configurado y OTP por correo verificado (envío, verificación y uso único).
- [ ] Copia offsite/NAS de los respaldos (hoy `D:` es otro disco físico, no otra ubicación).
- [ ] Evaluar cuenta dedicada para el servicio en lugar de `LocalSystem`.
