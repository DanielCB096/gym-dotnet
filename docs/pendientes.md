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
      su `apiUrl` a `https://192.168.100.53:5055`.

## 2. Otros pendientes de entrega

- [ ] Cambiar la contraseña del `owner` y su correo de prueba por datos reales; activar MFA.
- [ ] Limpiar datos demo (6 socios, 2 productos, 1 acceso).
- [ ] Configurar SMTP real y probar el flujo OTP.
- [ ] Copia offsite/NAS de los respaldos (hoy `D:` es otro disco físico, no otra ubicación).
- [ ] Evaluar cuenta dedicada para el servicio en lugar de `LocalSystem`.
