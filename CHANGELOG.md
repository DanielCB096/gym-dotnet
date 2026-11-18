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

---

## Estado actual del sistema

| Componente | Estado |
|---|---|
| Backend API | ✅ ~67 endpoints de 14 controllers operativos |
| Base de datos | ✅ 35 tablas, 26 foreign keys en SQL Server |
| Autenticación JWT | ✅ Funcional con bootstrap y login |
| Frontend Login | ✅ Completo con validación y mensajes de error |
| Frontend Dashboard | ✅ Completo con datos reales de la API |
| Frontend Layout | ✅ Sidebar + Header estilo TailAdmin, responsivo |
| Frontend Miembros | ✅ Tabla + formulario de creación |
| Frontend 9 módulos | ⚠️ Solo lectura (usan GenericPage) |
| Componentes UI | ✅ 6 componentes reutilizables (Button, Card, Table, Badge, Input, Modal) |

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
