# GymService - Registro de Cambios y Correcciones

> Documento vivo que registra todos los bugs, errores y correcciones encontrados durante el desarrollo de GymService.

---

## Backend (.NET)

### Program.cs
- **Bug:** El servicio se "trababa" al iniciar con `-console` porque `ServiceConsole` abre un shell interactivo que espera que escribas `start` manualmente y no llamaba automáticamente al método `Start()`.
- **Solución:** Modificado para que en modo consola se inicie automáticamente sin esperar comandos interactivos.

### appsettings.json
- **Bug 1:** Comilla de cierre faltante en la contraseña de la base de datos (faltaba el `"` final del valor).
- **Solución:** Corregida la comilla de cierre.
- **Bug 2:** La contraseña configurada no coincidía con la del servidor SQL.
- **Solución:** Actualizada la contraseña después de reestablecerla en SQL Server con `ALTER LOGIN sa WITH PASSWORD`.

### ModelGym.cs
- **Bug:** 12 `DefaultValue` corruptos donde comillas simples dentro de strings causaban error SQL `"Incorrect syntax near 'ACTIVE'"`. Ejemplo: `"'active'"` en lugar de `"active"`.
- **Solución:** Eliminadas las comillas internas porque `SQLServerInterface.cs` ya agrega las comillas en el formato `DEFAULT '{valor}'`.

### DatabaseService.cs
- **Bug:** `IDENTITY=1` no era reconocido por el switch case en `SQLServerInterface.cs`, impidiendo que las tablas se crearan con auto-increment correcto.
- **Solución:** Cambiado a `IDENTITY` (sin `=1`) para que coincida con el switch case.

### MembersModule.cs
- **Bug:** Consulta SQL usaba columna inexistente `membership_type` en lugar de `plan_id`.
- **Solución:** Corregida la consulta para usar `plan_id` con JOIN a `gym_membership_plans`.

### SalesModule.cs
- **Bug:** Consulta `GetSales` usaba columnas inexistentes (`cashier_id`, `status`, `created_at`). El `CreateSale` INSERT también usaba columnas incorrectas.
- **Solución:** Corregidas las consultas para usar las columnas reales del esquema (`sale_number`, `sale_date`, `tax_amount`, `is_offline`, `created_by`).

### MaintenanceModule.cs
- **Bug:** Consulta `GetEquipment` usaba `ORDER BY name` y leía columna `name`, que no existían.
- **Solución:** Corregido a `ORDER BY equipment_name` y lectura de `equipment_name`.

### ReportsModule.cs
- **Bug:** Referencia a tabla `gym_reports` que no existía.
- **Solución:** Corregida a `gym_scheduled_reports`.

### SQL Server - Problemas de conexión
- **Bug 1:** Windows App Control bloqueaba las DLL compiladas con error `0x800711C7`.
- **Solución:** Agregadas exclusiones con `Add-MpPreference -ExclusionPath` y desactivado Control de aplicaciones en Aislamiento del núcleo.
- **Bug 2:** SQL Server rechazaba el login con `Login failed for user 'sa'` aunque el usuario estaba habilitado y la contraseña era correcta.
- **Solución:** El problema era que la contraseña en `appsettings.json` no coincidía exactamente con la del servidor. Se reestableció con `ALTER LOGIN sa WITH PASSWORD` en SSMS.
- **Nota:** El puerto 1433 era correcto y accesible. La autenticación era mixta (SQL + Windows). El nombre de la instancia era `LETO`.

### Warnings menores
- Foreign keys duplicadas al iniciar (`gym_bank_transactions`, `gym_staff`, `gym_push_subscriptions`): El código intenta recrear la tabla para agregar la FK, pero ya existe. Inofensivo.

---

## Frontend (React + Vite)

### api.js
- **Bug 1:** Interceptor de errores 401 interceptaba las peticiones de `/auth/login`, haciendo `window.location.href = '/login'` que recargaba la página y borraba el mensaje de error antes de que el usuario pudiera verlo.
- **Solución:** El interceptor ahora no intercepta peticiones de `/auth/login`.
- **Bug 2:** Interceptores duplicados en el archivo.
- **Solución:** Eliminados los duplicados.
- **Bug 3:** CORS bloqueaba las peticiones al conectar directamente desde `localhost:5173` a `localhost:5000`.
- **Solución:** Cambiada `API_BASE` de `http://localhost:5000/api` a `/api` (ruta relativa) y configurado proxy en `vite.config.js`.

### AuthContext.jsx
- **Bug 1:** `JSON.parse(storedUser)` intentaba parsear `"undefined"` (cadena literal) como JSON al leer `localStorage`, causando un `SyntaxError` que rompía toda la aplicación.
- **Solución:** Envuelto en `try-catch`. Si el valor es corrupto, se limpian `token` y `user` de `localStorage`.
- **Bug 2:** El mapeo de la respuesta del login era incorrecto. El backend devuelve `{ token, userId, role, username, permissions }` directamente, pero el código esperaba `{ token, user: {...} }`. Esto hacía que `userData` fuera `undefined`, `isAuthenticated` fuera `false`, y `ProtectedRoute` redirigiera en silencio a `/login` sin mostrar error.
- **Solución:** Corregido el mapeo para leer las propiedades directamente del objeto de respuesta.

### GenericPage.jsx
- **Bug:** El backend devuelve `{ success, data: [...], pagination }`, pero el código leía `r.data?.items`.
- **Solución:** Corregido a `r.data?.data`. Además reemplazada la tabla inline por el componente `Table` con `overflow-x-auto` para evitar desbordamiento horizontal.

### Dashboard.jsx
- **Bug:** Las estadísticas usaban `pagination.totalCount` a nivel raíz de la respuesta, pero el backend lo devuelve dentro del objeto `pagination`.
- **Solución:** Corregido para usar `pagination.totalCount` de la respuesta del backend.

### Login.jsx
- **Bug:** Los `<label>` no estaban asociados a sus campos `<input>` (problema de accesibilidad WCAG).
- **Solución:** Agregado `htmlFor="username"` y `id="username"` al campo de usuario, y `htmlFor="password"` y `id="password"` al campo de contraseña.

### vite.config.js
- **Bug:** Sin proxy configurado, las peticiones CORS fallaban.
- **Solución:** Agregado proxy de servidor para redirigir `/api` a `http://localhost:5000` con `changeOrigin: true`.

---

## Rediseño TailAdmin (Frontend)

### Problemas de responsividad
- **Layout.jsx:**
  - **Bug:** El sidebar no se cerraba automáticamente en pantallas pequeñas, causando sobreposición con el contenido.
  - **Solución:** Agregado `useEffect` con `resize` listener que cierra el sidebar en pantallas < 1024px.
  - **Bug:** El header era demasiado alto en móvil (80px) y los elementos se sobreponían.
  - **Solución:** Header compacto en móvil (`h-16` vs `h-20`), breadcrumb truncado, búsqueda más pequeña en tablet, `flex-shrink-0` en elementos del header, padding reducido.
- **Dashboard.jsx:**
  - **Bug:** Las 5 tarjetas de métricas se apilaban mal en pantallas medianas.
  - **Solución:** Grid cambiado de `lg:grid-cols-5` a `xl:grid-cols-5` para dar más espacio. Los 3 paneles inferiores usan `lg:grid-cols-2 xl:grid-cols-3` con gaps adaptativos.
- **GenericPage.jsx:**
  - **Bug:** La tabla se desbordaba horizontalmente en pantallas pequeñas.
  - **Solución:** Reemplazada por componente `Table` con `overflow-x-auto` nativo.
- **Login.jsx:**
  - **Bug:** Colores obsoletos `primary-*` no existían después del cambio a `brand-*`.
  - **Solución:** Todos los colores cambiados a `brand-*`. Iconos SVG inline reemplazados por lucide-react. Tamaños de texto, padding y spacing ahora responsivos con breakpoints `lg:`.

### Colores y tipografía
- **Cambio:** Paleta `primary-*` (Tailwind #3b82f6) reemplazada por paleta `brand-*` de TailAdmin (#465FFF base, 11 tonos 50-950).
- **Cambio:** Agregados colores de estado: success (#10B981), danger (#F43F5E), warning (#F59E0B), info (#3AB3F2), purple (#8B5CF6).
- **Cambio:** Todos los SVG inline reemplazados por iconos de `lucide-react`.

### Tarea D01 — README desactualizado
- **Bug:** README.md mencionaba ".NET 8" (3 veces), "Next.js" y decía que los controllers estaban "pendientes" y los módulos eran "stubs", cuando el proyecto ya usa .NET 10, el frontend es React+Vite y los 15 controllers están operativos.
- **Solución:** 5 ediciones: .NET 8 → .NET 10 (3 ocurrencias), Next.js → React 19 + Vite + Tailwind CSS 4, sección Estado del Proyecto actualizada con 15 controllers, ~67 endpoints, frontend con 14 páginas y pendientes reales (tests, WiX, OTP por correo).
- **Verificación:** `grep "Next.js|\.NET 8" README.md` → 0 resultados.

### Tarea F01 — Colores `primary-*` residuales en Members y Sales
- **Bug:** Members.jsx y Sales.jsx aún usaban clases `primary-*` (spinner, botón, inputs, avatar, badge) que no generan CSS con la paleta `brand-*` de TailAdmin.
- **Solución:** Reemplazadas 10 ocurrencias de `primary-*` por `brand-*` conservando el tono numérico. 8 en Members.jsx (spinner, botón gradiente, 5 inputs, avatar) y 2 en Sales.jsx (spinner, badge).
- **Verificación:** `npm run build` → 2210 módulos, 815ms, 0 errores.

---

## Rediseño del frontend a CRUD real (18-nov-2026)

### Páginas con CRUD completo
- Se reemplazaron las 9 páginas genéricas de solo lectura por páginas reales con crear, editar y eliminar:
  `Billing`, `Classes`, `Finance`, `Fisio`, `Maintenance`, `Reports`, `Routines`, `Staff`, `Kiosk` y `AccessLogs`.
- Se corrigieron los nombres de campo que no coincidían con la respuesta real del backend:
  - **Classes:** `typeName`, `durationMinutes` (antes `name`, `instructor`).
  - **Finance:** `bankName`, `accountNumber`, `currency` (antes `accountName`, `accountType`).
  - **Fisio:** `name` (antes `packageName`).
  - **Routines:** `planName`, `durationMinutes` (antes `name`, `type`, `duration`).
  - **Reports:** `lastRun` (antes `format`, `isActive`).
  - **Staff:** `employeeName`, `username`, `position` (antes `name`, `role` como puesto).
  - **Billing:** `amount` (antes `totalAmount`) y `memberId` (no existe `memberName`).

### Infraestructura nueva
- `components/CrudPage.jsx`: página CRUD configurable (tabla, formulario modal, validación, paginación, confirmación de borrado, notificaciones y acciones por fila).
- `hooks/useApiList.js`: carga paginada con manejo de errores y protección contra respuestas obsoletas.
- `components/ui/Select.jsx`, `PageHeader.jsx`, `ConfirmDialog.jsx`.
- `utils/format.js`: moneda MXN, fechas y textos legibles.
- `StaffModule.GetAvailableUsers()` + `GET /api/staff/users`: selector de usuario al crear personal.
- `StaffModule.GetStaff()` ahora devuelve `department`.

### Decisiones
- **PDF:** se eliminó `QuestPDF` del `.csproj` porque no se usa y su versión instalada tiene una API incompatible. Cuando se implemente impresión de tickets se agregará la versión estable actual y se decidirá la licencia.
- **App settings:** `GymService/appsettings.json` queda ignorado por git; se agregó `appsettings.example.json` sin credenciales.
- **Git:** repositorio inicializado y publicado en https://github.com/DanielCB096/gym-dotnet. Se eliminó la contraseña de la base de datos del `CHANGELOG.md`.

### Cierre del frontend: Inventario, Ventas y Membresías (18-nov-2026)

- **Inventario:** página con CRUD real (crear, eliminar, búsqueda y ajuste de stock).
  El módulo backend usaba la columna `stock`, que no existe en la base; ahora usa
  `stock_quantity`, que es la que ya declaraba `ModelGym.cs`.
- **Ventas:** formulario de nueva venta con selección de socio, método de pago y varios
  productos; calcula subtotal, IVA (16%) y total. Se registra con `POST /api/sales`.
- **Miembros:** el plan se elige desde `GET /api/members/plans` (planes activos reales) y la
  membresía se crea con la duración del plan (`duration_days`) en lugar de un mes fijo.
- **Correcciones de backend:**
  - `SalesModule.CreateSale()` generaba `sale_number` con `NEWID()`, que no cabe en
    `nvarchar(20)`; ahora genera un código compacto de 19 caracteres.
  - `SalesController` enviaba `cashierId` pero el módulo leía `createdBy`; ahora usa `createdBy`.
  - `MembersModule.GetMembershipPlans()` + `GET /api/members/plans`, con regla `members.view`
    en `PermissionMatrix`.
- **Datos:** se sembraron los planes Básico, Premium y VIP en `gym_membership_plans`,
  que estaba vacía.

### Pendiente en frontend
- Página 404 real, accesibilidad y persistencia del sidebar (tareas F06/F07, delegadas a Junie).
- Documentación del frontend (F08) y actualización del README raíz (D01).

---

## Estado actual del sistema

| Componente | Estado |
|---|---|
| Backend API | ✅ ~69 endpoints de 15 controllers operativos |
| Base de datos | ✅ 35 tablas, 26 foreign keys en SQL Server |
| Autenticación JWT | ✅ Funcional con bootstrap y login |
| Frontend Login | ✅ Completo con validación y mensajes de error |
| Frontend Dashboard | ✅ Completo con datos reales de la API |
| Frontend Miembros | ✅ Tabla + formulario con selector real de planes |
| Frontend Layout | ✅ Sidebar + Header estilo TailAdmin, responsivo |
| Frontend 10 módulos | ✅ CRUD real (clases, rutinas, personal, facturación, finanzas, fisio, mantenimiento, reportes, kiosco, accesos) |
| Frontend Inventario | ✅ CRUD real + ajuste de stock |
| Frontend Ventas | ✅ Formulario de nueva venta con productos, IVA y total |
| Componentes UI | ✅ 9 componentes reutilizables + `CrudPage` + `useApiList` |

---

## Lecciones aprendidas

1. **Siempre verificar el esquema real de la base de datos** antes de escribir consultas SQL. La mayoría de los bugs del backend fueron referencias a columnas que no existían.
2. **Las contraseñas con caracteres especiales** (`;`) en `appsettings.json` pueden causar problemas si no se escapan correctamente.
3. **Windows App Control** bloquea binarios no firmados. Agregar exclusiones es necesario para desarrollo local.
4. **localStorage puede tener datos corruptos.** Siempre usar `try-catch` al parsear JSON de almacenamiento local.
5. **Los interceptores de Axios** pueden interferir con las peticiones de autenticación. Excluir `/auth/login` del interceptor 401.
6. **CORS en desarrollo** se resuelve mejor con proxy de Vite que configurando el backend para cada puerto.
7. **Las respuestas del backend** deben documentarse y verificarse antes de consumirlas en el frontend. La estructura `{ success, data, pagination }` no es obvia sin documentación.
8. **Los labels de formularios** deben tener `htmlFor`/`id` para accesibilidad WCAG.
9. **El diseño responsivo** requiere probar en múltiples tamaños de pantalla, no solo desktop.

---

*Última actualización: 2026-11-18*
