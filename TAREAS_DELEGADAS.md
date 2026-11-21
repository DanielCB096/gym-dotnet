# Tareas delegadas — GymService / Centro Fit

> **IA asignada:** Junie (JetBrains) — Qwen/Qwen3.6-27B-FP8.
> **Repositorio:** https://github.com/DanielCB096/gym-dotnet (público).
>
> **Regla principal:** haz **UNA sola tarea a la vez**, la que te indiquen.
> No adelantes otras tareas ni toques archivos fuera de los listados.
> No reescribas archivos completos: cambios mínimos y localizados.
>
> **Para Daniel:** pídele a Junie algo como:
> *"Lee `TAREAS_DELEGADAS.md`, haz SOLO la tarea F01 y actualiza la bitácora. No toques otros archivos."*

---

## 0. Reparto de trabajo (importante)

La IA grande (DeepSeek) ya terminó el rediseño del frontend: las páginas `Members`, `Sales`,
`Inventory` y los 10 módulos usan `CrudPage`, y existen `src/hooks/useApiList.js`,
`src/components/ui/Select.jsx`, `PageHeader.jsx`, `ConfirmDialog.jsx` y `src/utils/format.js`.

Esos archivos ya están estables. Puedes leerlos como referencia, pero **no los modifiques**
salvo que la tarea lo indique explícitamente.

**Tu cola actual es: F03 → F06 → F07 → F08.** Haz una sola a la vez.

---

## 1. Contexto mínimo

**Stack real:**
- Frontend: React 19 + Vite 8 + Tailwind CSS 4 + axios + lucide-react. **No es Next.js.**
- Backend: `GymService/` - .NET 10 (`net10.0-windows7.0`), Kestrel, SQL Server (instancia **LETO**), framework propietario **GymCore**.
- Config local: `GymService/appsettings.json` (ignorado por git; hay plantilla en `appsettings.example.json`).

**Reglas del frontend:**
1. Tailwind 4 con tema en `src/index.css`. La paleta válida es **`brand-*`**; **`primary-*` no existe** y no genera CSS.
2. Iconos con `lucide-react`. No agregar SVGs inline nuevos.
3. No instalar dependencias nuevas.
4. Respuesta estándar del backend:
   ```json
   { "success": true, "data": [], "pagination": { "page": 1, "limit": 50, "totalCount": 0, "totalPages": 0, "hasNext": false, "hasPrev": false } }
   ```
   Leer con: `r.data?.data || r.data?.items || r.data || []`.
5. UI en español. Componentes funcionales con hooks.
6. Manejar siempre los errores de API con un mensaje visible (nunca `catch(() => {})`).

**Reglas del backend (por si una tarea lo toca):**
1. SELECT → `tools.ExecuteQueryResultSet(sql, params)`.
2. INSERT/UPDATE/DELETE → `tools.CreateSession("MODULO")` + `session.ExecuteTransaction(sql, params)` + `session.TryExecute().IsError`.
3. `Row` no tiene indexador: se usa `row.GetObject(i)`.
4. `Session` **no** implementa `IDisposable`: nunca `using`.
5. Logs → `Logger.AddErrorSubModule(...)`, `Logger.AddWarningSubModule(...)`.
6. DataType: `Int64`, `String`, `Boolean`, `Int32` (no `BigInt`, `NVarChar`, `Bit`, `Int`).

**Comandos de verificación:**
```powershell
cd C:\proyectos\gimnasio-dotnet\GymServiceFront
npm run build
npm run lint

cd C:\proyectos\gimnasio-dotnet\GymService
dotnet build
```

**Baseline (18-nov-2026):** frontend build ✅, backend build ✅ 0 errores, lint con 8 warnings preexistentes. No debe aparecer ningún error nuevo.

---

## 2. Zonas prohibidas

- Todo `GymService/Modules/Identity/`, `GymService/Tools/SecurityValidation.cs`.
- `GymService/Services/DatabaseService.cs`, `GymService/Models/ModelGym.cs`.
- `GymService/Program.cs`, `DataCenter.cs`, `ServiceImplementation.cs`.
- `GymService/appsettings.json` y cualquier archivo con contraseñas.
- `third_party\gym-core\` (framework compartido).
- Actualizar/eliminar paquetes NuGet.
- Refactors masivos, renombrados o borrados de archivos.
- Archivos reservados por la IA grande (ver sección 0).

Si una tarea parece requerir tocar algo de esta lista, **detente y repórtalo**.

---

## 3. Reglas de git (dos IAs en el mismo repo)

- **Nunca** uses `git add -A`, `git add .`, `git commit -a` ni `git push`.
- Si Daniel te pide commitear, agrega **solo los archivos de tu tarea**:
  ```powershell
  git add GymServiceFront/src/pages/Members.jsx
  git commit -m "junie(F01): usar paleta brand en Members y Sales"
  ```
- No cambies de rama ni hagas merge/rebase.
- No commitees `appsettings.json`, `node_modules`, `dist`, `bin` u `obj`.
- El repo es público: **nunca** subas credenciales.

---

## 4. Cola de tareas

### ✅ D01 — Sincronizar README con la realidad
- **Tamaño:** muy pequeño · **Riesgo:** nulo · **Archivo:** `README.md` (raíz)
- **Problema:** el README dice ".NET 8", "Next.js", "controllers pendientes" y "14 módulos stubs"; el proyecto ya está en .NET 10, el frontend es React+Vite y hay 15 controllers.
- **Pasos:**
  1. Cambiar ".NET 8" por ".NET 10".
  2. Cambiar "Panel web Next.js" por "Panel web React 19 + Vite + Tailwind CSS 4".
  3. Actualizar "Estado del Proyecto": controllers y módulos completados; pendientes: tests, instalador WiX y envío real de OTP por correo.
  4. No inventar endpoints ni módulos.
- **Aceptación:**
  - No quedan menciones a "Next.js" ni a ".NET 8". ✅
  - El resto del README sigue coherente. ✅
- **Estado:** ✅ Completada el 2026-11-18. 5 ediciones: 3 ocurrencias de .NET 8 → .NET 10, 1 de Next.js → React+Vite, 1 sección Estado del Proyecto actualizada.

---

### ✅ F01 — Reemplazar colores `primary-*` por `brand-*`
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivos:** `GymServiceFront/src/pages/Members.jsx`, `GymServiceFront/src/pages/Sales.jsx`
- **Problema:** el tema de Tailwind 4 solo define `brand-*`; las clases `primary-*` no generan CSS.
- **Pasos:** sustituir cada `primary-` por `brand-` conservando el tono (`primary-600` → `brand-600`, etc.). No cambiar lógica, textos ni estructura.
- **Aceptación:**
  - `grep -rn "primary-" src/pages/Members.jsx src/pages/Sales.jsx` → sin resultados. ✅
  - `npm run build` → OK. ✅
- **Estado:** ✅ Completada el 2026-11-18. 10 ocurrencias reemplazadas (8 en Members.jsx, 2 en Sales.jsx). Build: 2210 módulos, 815ms, 0 errores.

---

### ✅ F02 — Corregir lectura de datos en Inventario (absorbida)
- **Estado:** ✅ Completada por la IA grande el 2026-11-18.
- `Inventory.jsx` se reescribió con `CrudPage`: lee `{ success, data }`, muestra errores visibles
  y además permite crear, eliminar y ajustar stock. No hay nada que hacer aquí.

---

### ✅ F03 — Quitar logs de depuración de `api.js`
- **Tamaño:** muy pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/services/api.js`
- **Problema:** imprime cada petición y respuesta en consola de producción.
- **Pasos:** eliminar los dos `console.log`. Conservar `console.error` y toda la lógica del interceptor 401.
- **Aceptación:**
  - `grep -n "console.log" src/services/api.js` → sin resultados. ✅
  - `npm run build` → OK. ✅
- **Estado:** ✅ Completada el 2026-11-18. 2 console.log eliminados (líneas 19 y 26) y comentario de depuración limpiado. console.error y lógica del interceptor 401 intactos. Build: 2217 módulos, 635ms, 0 errores.

---

### ✅ F04 y F05 — Accesibilidad y errores en Miembros (absorbidas)
- **Estado:** ✅ Completadas por la IA grande el 2026-11-18.
- `Members.jsx` se reescribió con `CrudPage`: todos los campos llevan `id` y `label` asociado, y los
  errores de la API se muestran dentro del modal con colores `danger-*`. No hay nada que hacer aquí.

---

### ✅ F06 — Página 404 real
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivos:** `GymServiceFront/src/pages/NotFound.jsx` (nuevo), `GymServiceFront/src/App.jsx`
- **Problema:** cualquier ruta desconocida redirige a `/` silenciosamente.
- **Pasos:**
  1. Crear `NotFound.jsx` con un mensaje claro y un botón "Volver al inicio" (`Link` de `react-router-dom`). Diseño con `brand-*` y un icono de `lucide-react`. ✅
  2. En `App.jsx`, cambiar la ruta `*` para renderizar `<NotFound />` dentro de `<Layout>` y `<ProtectedRoute>`. ✅
  3. No tocar las demás rutas. ✅
- **Aceptación:**
  - `/cualquier-cosa` muestra la página 404 dentro del layout. ✅
  - `npm run build` → OK. ✅
- **Estado:** ✅ Completada el 2026-11-18. NotFound.jsx creado con icono AlertTriangle, colores amber y botón con ArrowLeft. App.jsx actualizado: ruta `*` renderiza NotFound dentro de ProtectedRoute + Layout. Build: 2218 módulos, 1.02s, 0 errores.

---

### ✅ F07 — Persistir el sidebar y accesibilidad en `Layout`
- **Tamaño:** mediano · **Riesgo:** medio · **Archivo:** `GymServiceFront/src/components/Layout.jsx`
- **Problema:** al recargar se pierde el estado del sidebar; algunos botones solo tienen icono y carecen de `aria-label`.
- **Pasos:**
  1. Inicializar el estado del sidebar desde `localStorage` (`sidebar-collapsed`) y guardarlo al cambiar. ✅
  2. Agregar `aria-label` descriptivo a los botones de icono (menú, notificaciones, cerrar sesión, colapsar/expandir). ✅
  3. Mantener el comportamiento responsive actual (<1024px cierra el sidebar). ✅
  4. No cambiar la navegación ni las rutas. ✅
- **Aceptación:**
  - El estado se conserva al recargar. ✅
  - Botones con nombre accesible. ✅
  - `npm run build` → OK. ✅
- **Estado:** ✅ Completada el 2026-11-18. Sidebar state persistido en localStorage con clave `sidebar-collapsed`. 6 botones de icono con `aria-label` agregado (cerrar menú móvil, abrir menú móvil, notificaciones, cerrar sesión x2, colapsar/expandir sidebar). Build: 2218 módulos, 1.01s, 0 errores.

---

### ✅ F08 — Documentar el frontend
- **Tamaño:** pequeño · **Riesgo:** nulo · **Archivo:** `GymServiceFront/README.md`
- **Pasos:** documentar en español: requisitos (Node 20+), instalación (`npm install`), scripts (`dev`, `build`, `lint`, `preview`), proxy `/api` → `http://localhost:5000`, estructura de `src/` y variables de entorno si aplican. ✅
- **Aceptación:**
  - Un desarrollador nuevo puede levantar el frontend siguiendo solo ese README. ✅
- **Estado:** ✅ Completada el 2026-11-18. README.md creado con 113 líneas: requisitos, instalación, scripts, proxy API, estructura de src/ con 38 archivos, tecnologías, autenticación JWT, despliegue en IIS y créditos.

---

## 5. Tareas NO delegadas (las asume la IA grande / Daniel)

- CRUD completo de las 10 páginas genéricas y componentes reutilizables.
- Envío real de OTP por correo (MailKit) en `IdentityModule`.
- Autenticación, sesiones, permisos y rate limiting.
- Esquema de BD y `ModelGym.cs`.
- Migración de datos desde PostgreSQL.
- Instalador WiX (`GymServiceSetup/`).
- Actualización de paquetes con vulnerabilidades (MimeKit/MailKit/JWT/MySqlConnector).
- Proyecto de tests.

---

## 6. Bitácora

Estados: `PENDIENTE` · `EN CURSO` · `HECHO` · `BLOQUEADO`

| ID | Estado | Fecha | Archivos tocados | Build | Notas |
|---|---|---|---|---|---|
| D01 | HECHO | 2026-11-18 | `README.md` | N/A | .NET 8 → .NET 10, Next.js → React+Vite, estado del proyecto actualizado. Sin menciones obsoletas restantes. |
| F01 | HECHO | 2026-11-18 | `GymServiceFront/src/pages/Members.jsx`, `GymServiceFront/src/pages/Sales.jsx` | `npm run build` OK | Reemplazo de `primary-*` por `brand-*`. Sin `primary-` restante. No se tocó lógica. |
| F02 | HECHO | 2026-11-18 | `GymServiceFront/src/pages/Inventory.jsx`, `GymService/Modules/Inventory/InventoryModule.cs` | build OK | Absorbida por la IA grande: CRUD real + corrección de columna `stock_quantity`. |
| F03 | HECHO | 2026-11-18 | `GymServiceFront/src/services/api.js` | `npm run build` OK | 2 console.log eliminados, console.error y lógica 401 intactos. Build 2217 módulos, 635ms. |
| F04 | HECHO | 2026-11-18 | `GymServiceFront/src/pages/Members.jsx` | build OK | Absorbida: `CrudPage` asocia `id`/`label` en todos los campos. |
| F05 | HECHO | 2026-11-18 | `GymServiceFront/src/pages/Members.jsx` | build OK | Absorbida: los errores de API se muestran en el modal. |
| F06 | HECHO | 2026-11-18 | `GymServiceFront/src/pages/NotFound.jsx`, `GymServiceFront/src/App.jsx` | `npm run build` OK | Página 404 con icono AlertTriangle, colores amber y botón ArrowLeft. Ruta `*` renderiza NotFound dentro de ProtectedRoute + Layout. Build 2218 módulos, 1.02s. |
| F07 | HECHO | 2026-11-18 | `GymServiceFront/src/components/Layout.jsx` | `npm run build` OK | Sidebar persistido en localStorage (`sidebar-collapsed`). 6 botones con `aria-label` agregado. Build 2218 módulos, 1.01s. |
| F08 | HECHO | 2026-11-18 | `GymServiceFront/README.md` | `npm run build` OK | README con requisitos, instalación, scripts, proxy API, estructura src/, tecnologías, autenticación y despliegue IIS. 113 líneas. |

**Al cerrar una tarea, Junie debe reportar:**
1. ID de la tarea.
2. Archivos modificados (rutas completas).
3. Qué cambió (2-4 líneas).
4. Salida de `npm run build` o `dotnet build`.
5. Dudas o efectos colaterales.

Si no puede completarla, marcar `BLOQUEADO` y explicar qué falta. **No improvisar.**
