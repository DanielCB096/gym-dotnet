# Pendientes de entrega

> Actualizado: 2026-11-21. Lista viva de lo que falta para producción.
> No sustituye a `CHANGELOG.md` (historial) ni a `docs/reglas-negocio.md`.

## 1. Rebranding: retirar AX / ANXO / ANXO LIBRARIES

**Estado:** pendiente de decisión. **No se ha renombrado nada todavía.**

### Inventario actual

| Categoría | Dónde aparece | Volumen aproximado |
|---|---|---|
| Marca visible | MSI (`Manufacturer="ANXO / Centro Fit"`), servicio (`anxogymmservice`, "Anxo Gym Manager Service"), ruta `C:\Program Files\Anxo\GymService`, tareas de respaldo, documentación | ~75 referencias en archivos versionados |
| Comentarios generados | 42 modelos en `GymService/Models/Generated` ("Anxo Code Generator", "Anxo Tecnologías de México") | 1.359 líneas |
| Build / repo | `ANXO_ROOT`, `AnxoRoot`, `Tools/AnxoModelGenerator`, `third_party/anxo`, `ANXO LIBRARIES` en README/docs | ~40 |
| Librerías | Proyectos `AnxoDatabase`, `AnxoLibrary`, `AnxoObjectDatabaseManager`, `AnxoServiceTools`, `AnxoDatabaseGenerator`; 229 archivos `.cs`, 201 `namespace Ax*`, 245 `using Ax` | alto |
| Código de la app | 165 `using Ax...` + referencias a tipos `Ax*` | alto |
| Frontend | Sin "Anxo" visible; el título es "GymService" | 0 |

Notas:

- `third_party/anxo` está **fuera de Git** (copia local para compilar). Cualquier fork
  white-label debe versionarse en el repo o publicarse como paquete; hoy el build depende
  de una carpeta local.
- No hay archivo `LICENSE` en `third_party/anxo`. Antes de renombrar namespaces o
  ensamblados hay que confirmar que el código es propio o que la licencia lo permite.
- La base `GymManager`, las tablas `GYM_*` y la URL no contienen la marca; no hace falta
  tocarlas.

### Opciones

**A. Marca visible (recomendada para entregar ya)**

- Renombrar servicio, nombre visible, ruta de instalación, `Manufacturer`/`Product` del
  MSI, tareas, documentación y comentarios generados.
- Mantener los ensamblados y namespaces `Ax*` de la librería propietaria.
- Esfuerzo: ~1 día + ventana de mantenimiento. Riesgo: medio (migración del servicio,
  ruta y tareas).

**B. Marca visible + nombres de build/repo**

- Además de A: `ANXO_ROOT` → `GYM_LIBS_ROOT` (con fallback temporal), `AnxoRoot` →
  `GymLibsRoot`, `Tools/AnxoModelGenerator` → `Tools/GymModelGenerator`,
  `third_party/anxo` → `third_party/gym-core`.
- Mantener ensamblados/namespaces de la librería.
- Esfuerzo: 1–2 días. Riesgo: bajo-medio.

**C. White-label completo (fork de librerías)**

- Forkear las 5 librerías y renombrar proyectos, ensamblados y namespaces
  (`AxDatabase` → `GymCore.Database`, `AxLogger` → `GymCore.Logging`, etc.).
- Actualizar 165 `using` y las referencias a tipos, la plantilla del generador
  (`third_party/anxo/AnxoDatabaseGenerator/CSharpGenerator.cs`) y los builds.
- Solo si se tiene propiedad o licencia del código. Esfuerzo: 3–5 días. Riesgo: alto;
  requiere regresión completa.

### Nombres propuestos

| Ámbito | Propuesta |
|---|---|
| Marca visible | **Centro Fit** (ya está en el MSI y en los correos) |
| Producto | **Centro Fit Gym Manager** |
| Servicio | `centrofitgymservice` / "Centro Fit Gym Service" |
| Ruta de instalación | `C:\Program Files\Centro Fit\GymService` |
| Prefijo técnico | **GymCore** (`GymCore.Database`, `GymCore.ObjectDatabase`, `GymCore.Logging`, `GymCore.ServiceTools`) |
| Build / repo | `GYM_LIBS_ROOT`, `third_party/gym-core`, `GymCore.ModelGenerator` |

Alternativa si se va a comercializar a varios gimnasios: usar un nombre neutro
(**GymCore**) como producto y "Centro Fit" solo como instancia/cliente.

### Migración (si se aprueba A, B o C)

1. Ventana de mantenimiento y respaldo completo verificado.
2. Detener y eliminar el servicio `anxogymmservice`; instalar el servicio nuevo.
3. Migrar `appsettings.json` y `logs` a la ruta nueva conservando ACL.
4. Re-registrar `GymService-Full` y `GymService-Log` apuntando a la ruta nueva.
5. Actualizar MSI, `install.ps1`, `uninstall.ps1`, documentación y accesos directos.
6. Verificar `/health`, login, respaldo, restauración y tareas programadas.

Riesgo específico del MSI: al cambiar el nombre del servicio, el `ServiceControl` del
instalador nuevo no detiene el servicio viejo. Hay que desinstalar/migrar el anterior de
forma explícita; la base de datos no se toca.

### Criterio de aceptación

- `Select-String -Pattern 'anxo'` sin resultados en archivos versionados y textos
  visibles (o solo dentro de `third_party` si se elige A/B).
- `services.msc`, "Aplicaciones instaladas", ruta de instalación y tareas sin AX/ANXO.
- Build, 130 pruebas, `/health`, login, respaldo y restauración en verde.

## 2. Otros pendientes de entrega

- [ ] Cambiar la contraseña del `owner` y su correo de prueba por datos reales; activar MFA.
- [ ] Limpiar datos demo (6 socios, 2 productos, 1 acceso).
- [ ] Configurar SMTP real y probar el flujo OTP.
- [ ] Copia offsite/NAS de los respaldos (hoy `D:` es otro disco físico, no otra ubicación).
- [ ] Evaluar cuenta dedicada para el servicio en lugar de `LocalSystem`.
- [ ] Instalar la CA raíz de HTTPS en todos los equipos cliente y en el kiosco; actualizar
      su `apiUrl` a `https://192.168.100.53:5055`.
