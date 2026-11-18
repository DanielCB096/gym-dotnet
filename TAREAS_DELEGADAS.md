# Tareas delegadas — GymService / Centro Fit

> **Para la IA pequeña:** este archivo es tu única fuente de trabajo.
> Haz **UNA sola tarea a la vez**, la que te indiquen. No adelantes otras tareas.
> No reescribas archivos completos: usa cambios mínimos y localizados.
>
> **Para el usuario:** pídele a la IA pequeña algo como:
> *"Lee `TAREAS_DELEGADAS.md` y ejecuta SOLO la tarea F01. No toques otros archivos. Al terminar, actualiza la bitácora y pega la salida de `npm run build`."*

---

## 1. Contexto mínimo (leer antes de tocar código)

**Stack real:**
- Backend: `GymService/` — .NET 10 (`net10.0-windows7.0`), Windows Service, Kestrel, SQL Server vía framework propietario **Anxo**.
- Frontend: `GymServiceFront/` — React 19 + Vite 8 + Tailwind CSS 4 + axios + lucide-react. **No es Next.js.**
- Base de datos: 35+ tablas en `GymService/Models/ModelGym.cs`.

**Reglas del backend (obligatorias, no inventar API):**
1. SELECT → `tools.ExecuteQueryResultSet(sql, params)`.
2. INSERT/UPDATE/DELETE → `tools.CreateSession("MODULO")` + `session.ExecuteTransaction(sql, params)` + `session.TryExecute().IsError`.
3. `Row` no tiene indexador. Se lee con `row.GetObject(i)` + helper de columnas.
4. `Session` **no** implementa `IDisposable`: nunca usar `using`.
5. Logs → `Logger.AddErrorSubModule("MODULO", "mensaje")`, `Logger.AddWarningSubModule(...)`. No existe `AddWarn`.
6. DataType: `Int64`, `String`, `Boolean`, `Int32`. No existen `BigInt`, `NVarChar`, `Bit`, `Int`.
7. Los módulos reciben `DatabaseTools`, no `OODatabaseManager`.

**Reglas del frontend:**
1. Tailwind 4 con tema en `src/index.css`. La paleta válida es **`brand-*`** (NO `primary-*`).
2. Iconos con `lucide-react`. No agregar SVGs inline nuevos.
3. No instalar dependencias nuevas.
4. Respuesta estándar del backend:
   ```json
   { "success": true, "data": [], "pagination": { "page": 1, "limit": 50, "totalCount": 0, "totalPages": 0, "hasNext": false, "hasPrev": false } }
   ```
   Leer con: `r.data?.data || r.data?.items || r.data || []`.
5. UI en español. Componentes funcionales con hooks.

**Comandos de verificación:**
```powershell
# Frontend
cd C:\proyectos\gimnasio-dotnet\GymServiceFront
npm run build
npm run lint

# Backend
cd C:\proyectos\gimnasio-dotnet\GymService
dotnet build
```

**Baseline actual (18-nov-2026):** frontend build ✅, backend build ✅ 0 errores, lint con 8 warnings preexistentes (no son errores). No debe aparecer ningún error nuevo.

---

## 2. Zonas prohibidas (no tocar sin autorización explícita)

- `GymService/Modules/Identity/IdentityModule.cs` (autenticación, MFA, OTP)
- `GymService/Tools/SecurityValidation.cs`
- `GymService/Services/DatabaseService.cs`
- `GymService/Models/ModelGym.cs` (esquema de BD)
- `GymService/Program.cs`, `DataCenter.cs`, `ServiceImplementation.cs`
- `GymService/appsettings.json` (contiene credenciales)
- Cualquier archivo en `C:\repositorios\pilot\ANXO LIBRARIES\` (framework compartido)
- Actualizar/eliminar paquetes NuGet
- Refactors masivos, renombrados o borrados de archivos

Si una tarea parece requerir tocar algo de esta lista, **detente y repórtalo**.

---

## 3. Cola de tareas

Regla: respetar el orden y las dependencias. No ejecutar dos tareas que toquen el mismo archivo.

### D01 — Sincronizar README con la realidad
- **Tamaño:** muy pequeño · **Riesgo:** nulo · **Archivo:** `README.md` (raíz)
- **Problema:** el README dice ".NET 8", "Next.js", "controllers pendientes" y "14 módulos stubs", pero el proyecto ya está en .NET 10, el frontend es React+Vite y hay 15 controllers.
- **Pasos:**
  1. Cambiar ".NET 8" por ".NET 10" en las referencias al backend.
  2. Cambiar "Panel web Next.js" por "Panel web React 19 + Vite + Tailwind CSS 4".
  3. En "Estado del Proyecto", marcar controllers y módulos como completados, y dejar como pendientes solo lo que sigue pendiente (tests, instalador WiX, correo OTP real).
  4. No inventar endpoints ni módulos nuevos.
- **Aceptación:** `grep -n "Next.js\|\.NET 8" README.md` no devuelve nada; el resto del README queda coherente.

---

### F01 — Reemplazar colores `primary-*` por `brand-*`
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivos:** `GymServiceFront/src/pages/Members.jsx`, `GymServiceFront/src/pages/Sales.jsx`
- **Problema:** el tema de Tailwind 4 solo define `brand-*`. Las clases `primary-*` no generan CSS, así que spinners, botones y badges se ven sin color.
- **Pasos:**
  1. En ambos archivos, sustituir cada `primary-` por `brand-` respetando el tono (`primary-600` → `brand-600`, etc.).
  2. No cambiar estructura, textos ni lógica.
- **Aceptación:**
  - `grep -rn "primary-" src/pages/Members.jsx src/pages/Sales.jsx` → sin resultados.
  - `npm run build` → OK.

---

### F02 — Corregir lectura de datos en Inventario
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/pages/Inventory.jsx`
- **Problema:** usa `r.data?.items`, pero el backend responde `{ success, data: [...] }`; la tabla siempre sale vacía.
- **Pasos:**
  1. Cambiar a `r.data?.data || r.data?.items || []` y validar con `Array.isArray`.
  2. Agregar estado `error` y mostrar un mensaje visible si la petición falla (hoy el `catch` está vacío).
  3. Mantener el resto del diseño igual.
- **Aceptación:**
  - La página muestra productos cuando la API devuelve `data`.
  - Si la API falla, se ve un mensaje de error y no una tabla vacía sin explicación.
  - `npm run build` → OK.

---

### F03 — Quitar logs de depuración de `api.js`
- **Tamaño:** muy pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/services/api.js`
- **Problema:** imprime cada petición y respuesta en consola de producción.
- **Pasos:**
  1. Eliminar los dos `console.log` (`[API] Petición` y `[API] Respuesta`).
  2. Conservar el `console.error` de errores y toda la lógica del interceptor 401.
- **Aceptación:** `grep -n "console.log" src/services/api.js` → sin resultados; `npm run build` → OK.

---

### F04 — Accesibilidad de etiquetas en formulario de Miembros
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/pages/Members.jsx`
- **Dependencia:** ejecutar después de F01.
- **Problema:** los `<label>` del formulario no tienen `htmlFor` y los `<input>`/`<select>` no tienen `id` (fallo WCAG).
- **Pasos:**
  1. Agregar `htmlFor` a cada label: `firstName`, `lastName`, `email`, `phone`, `membershipType`.
  2. Agregar el `id` correspondiente a cada campo.
  3. No cambiar nombres de estado ni el payload.
- **Aceptación:** los 5 labels están asociados; `npm run lint` no agrega errores nuevos.

---

### F05 — Mostrar errores al crear miembro
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/pages/Members.jsx`
- **Dependencia:** ejecutar después de F04.
- **Problema:** si el POST falla solo se hace `console.error`; el usuario no ve nada.
- **Pasos:**
  1. Agregar estado `error`.
  2. En el `catch` del submit, mostrar el `message` que devuelve el backend o un texto genérico.
  3. Renderizar un bloque de error similar al bloque de éxito existente (colores `danger-*` o `rose-*`).
  4. Limpiar el error al abrir/cerrar el formulario y al enviar de nuevo.
- **Aceptación:** un fallo de API produce un mensaje visible; `npm run build` → OK.

---

### F06 — Paginación opcional en `GenericPage`
- **Tamaño:** mediano · **Riesgo:** medio · **Archivo:** `GymServiceFront/src/pages/GenericPage.jsx`
- **Problema:** las 9 páginas genéricas solo muestran la primera página (límite por defecto del backend).
- **Pasos:**
  1. Enviar `page` y `limit` (50 por defecto) como query params.
  2. Leer `r.data.pagination`.
  3. Si existe `pagination`, mostrar botones "Anterior"/"Siguiente", página actual y total. Si no existe, no mostrar controles.
  4. Al cambiar de página, volver a cargar.
  5. No tocar los archivos de página que usan `GenericPage`; la mejora debe ser centralizada.
- **Aceptación:**
  - `/members`, `/sales`, `/billing`, etc. paginan sin errores.
  - Endpoints sin paginación siguen funcionando y no muestran controles.
  - `npm run build` → OK.

---

### F07 — Estado de error en páginas genéricas
- **Tamaño:** pequeño · **Riesgo:** bajo · **Archivo:** `GymServiceFront/src/pages/GenericPage.jsx`
- **Dependencia:** ejecutar después de F06.
- **Problema:** si la API falla, se muestra "0 registros" como si no hubiera datos.
- **Pasos:**
  1. Agregar estado `error`.
  2. En el `catch`, guardar el mensaje y mostrar un aviso visible.
  3. Mantener los datos anteriores si ya había una carga exitosa (no borrar la tabla).
- **Aceptación:** un fallo de red muestra un aviso; `npm run build` → OK.

---

## 4. Tareas explícitamente NO delegadas (las asume la IA grande)

- Implementar envío real de OTP por correo (MailKit) en `IdentityModule`.
- Cambios de autenticación, sesiones, permisos o rate limiting.
- Cambios al esquema de BD o a `ModelGym.cs`.
- Migración de datos desde PostgreSQL.
- Instalador WiX en `GymServiceSetup/`.
- Actualización de paquetes con vulnerabilidades (MimeKit/MailKit/JWT/MySqlConnector).
- Creación de proyecto de tests.

---

## 5. Bitácora (actualizar al terminar cada tarea)

Estados: `PENDIENTE` · `EN CURSO` · `HECHO` · `BLOQUEADO`

| ID | Estado | Fecha | Archivos tocados | Build | Notas |
|---|---|---|---|---|---|
| D01 | PENDIENTE | — | — | — | — |
| F01 | PENDIENTE | — | — | — | — |
| F02 | PENDIENTE | — | — | — | — |
| F03 | PENDIENTE | — | — | — | — |
| F04 | PENDIENTE | — | — | — | — |
| F05 | PENDIENTE | — | — | — | — |
| F06 | PENDIENTE | — | — | — | — |
| F07 | PENDIENTE | — | — | — | — |

**Al cerrar una tarea, la IA debe reportar exactamente:**
1. ID de la tarea.
2. Archivos modificados (rutas completas).
3. Qué cambió (2-4 líneas).
4. Salida de `npm run build` o `dotnet build`.
5. Cualquier duda o efecto colateral.

Si no puede completar la tarea, marcar `BLOQUEADO` y explicar qué falta. **No improvisar.**
