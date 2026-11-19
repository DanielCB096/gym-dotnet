# GymService - Registro de Cambios y Correcciones

> Documento vivo que registra todos los bugs, errores y correcciones encontrados durante el desarrollo de GymService.

---

## Backend (.NET)

### Membresías y control de acceso (2026-11-19)
- **Estado del socio:** la lista devuelve `membershipStatus` (`active`/`frozen`/`expired`/`none`) y `hasActiveMembership`; el panel muestra “Sin membresía” o “Vencida” en lugar de “Activo” cuando no hay una membresía vigente. El estado de la cuenta (`status`) se mantiene separado.
- **Accesos:** no se genera código para socios sin membresía activa, con membresía congelada/vencida o cuentas inactivas; la validación vuelve a comprobar la membresía al usar el código y registra el intento denegado en la bitácora.
- **Pruebas:** 97 automatizadas, incluyendo socio con membresía, socio sin membresía y socio sin plan reportado correctamente.

### Pruebas amplias y correcciones (2026-11-19)
- **Socios:** se rechaza correo duplicado, nombre/apellido vacío y planes inexistentes; al arrancar se crean los planes base (Mensual/Trimestral/Anual) si la tabla está vacía.
- **Permisos:** la resolución de rutas usa la coincidencia más específica, por lo que subrutas como `/api/finance/accounts`, `/api/billing/invoices`, `/api/reports/*`, `/api/import/*` o `/api/kiosk/*` ya no se saltan el RBAC. Los roles ADMIN/STAFF/CASHIER reciben los permisos por defecto de la matriz aunque la tabla `gym_role_permissions` solo tenga al owner.
- **Finanzas:** cada movimiento actualiza el saldo de la cuenta, se rechazan montos negativos/cero, cuentas inexistentes y retiros sin saldo; no se eliminan cuentas con movimientos.
- **Seguridad:** validación de códigos de acceso con rate limiting estricto.
- **Pruebas:** 95 automatizadas (22 nuevas de permisos, socios y finanzas) y barrido HTTP completo sobre BD temporal: auth, socios, accesos, ventas, clases, rutinas, fisio, mantenimiento, facturación, personal, finanzas, reportes, kiosco e importación.

### Personal y facturación: correcciones y pruebas (2026-11-19)
- **Personal:** alta de usuarios del sistema (usuario, correo, contraseña, rol ADMIN/STAFF/CASHIER) desde el módulo; el alta de empleado valida usuario existente/activo, evita vincular dos veces al mismo usuario, guarda fecha de alta y devuelve `staffId`.
- **Personal (UI):** pestañas “Empleados” y “Usuarios”; alta de usuarios, selección de usuario disponible al crear empleado, estado activo/inactivo, fecha de alta y listado con rol/email.
- **Facturación:** validación de socio, importe > 0, IVA dentro del importe, estados `pending`/`paid`/`cancelled`, número de factura único y generación automática. Listado con nombre del socio, IVA, subtotal y filtros por estado/búsqueda; no se eliminan facturas pagadas.
- **Facturación (UI):** selección de socio por nombre, importe/IVA, filtro por estado, etiquetas corregidas (“Socio”, “Importe”, “No. de factura”) y búsqueda funcional.
- **Textos:** se corrigieron cadenas con doble codificación UTF-8 (acentos y signos) en módulos, servicios y en el panel de clases.
- **Pruebas:** 69 (unitarias + integración), con flujos de usuarios/empleados y validaciones/filtros de facturas.

### Despliegue y panel integrado (2026-11-19)
- **Panel web servido por el servicio:** Kestrel publica `wwwroot` con fallback SPA; las rutas `/api`, `/swagger`, `/health` y archivos inexistentes conservan 404. El contenido se resuelve desde el directorio del ejecutable (`UseContentRoot`), necesario al correr como servicio Windows.
- **Publicación:** `publish.ps1` compila el frontend, publica self-contained `win-x64`, copia el panel a `wwwroot`, genera `VERSION.txt` y un ZIP con instalador; nunca incluye `appsettings.json` con credenciales.
- **Instalador:** `GymServiceSetup/install.ps1` (Administrador) copia a `Program Files`, crea la configuración desde la plantilla solo si no existe, restringe permisos de `appsettings.json`, registra el servicio, configura recuperación, firewall opcional y arranque. `uninstall.ps1` desinstala conservando o borrando datos según `-RemoveData`. La actualización detiene/desregistra el servicio con reintentos y limpia el registro si queda colgado.
- **CI:** workflow `Release` publica el ZIP al crear un tag `v*`.
- **Seguridad de dependencias:** MailKit 5.0.0 (adaptado a `SmtpSendRequest`) y `System.IdentityModel.Tokens.Jwt` 8.23.0. Queda el aviso moderado de `MySqlConnector` 2.4.0 heredado de las librerías ANXO (no se usa MySQL en este despliegue).
- **Documentación:** guía de despliegue, configuración mínima de producción, operación, actualización y checklist en `README.md`.

### Inventario y mantenimiento completos (2026-11-19)
- **Inventario:** productos con SKU, categoría, costo, stock y mínimo; búsqueda por nombre/categoría/SKU, filtro de stock bajo, ajuste de stock, edición de datos, activación/desactivación y borrado protegido si hay ventas.
- **Mantenimiento:** CRUD de equipos (categoría, ubicación, estado, compra y garantía) y tickets con prioridad, estados `open`/`in_progress`/`resolved`/`cancelled`, resolución con usuario y fecha, y bloqueo de tickets duplicados abiertos.
- **Sincronización:** al abrir un ticket el equipo pasa a `maintenance`; al resolver/cancelar el último ticket vuelve a `operational`. Equipos con historial no se eliminan.
- **API:** `/api/inventory` (GET/POST/PUT/DELETE y `PUT /{id}/stock`), `/api/maintenance/equipment` y `/api/maintenance/tickets` con permisos `inventory.*` / `maintenance.*`.
- **UI:** Inventario con filtro de stock bajo, edición y estado; Mantenimiento con pestañas “Equipos” y “Tickets”, filtros por equipo/estado/prioridad y acciones de iniciar, resolver, cancelar y eliminar.
- **Pruebas:** 67 (unitarias + integración) con flujos de equipos/tickets y de productos/stock.

### Fisioterapia completa (2026-11-19)
- **Backend:** paquetes (generales o por socio) con sesiones, vigencia, precio y estado; citas con validación de socio activo, paquete vigente y sesiones disponibles; consumo de sesión al completar y liberación al cancelar/deshacer.
- **Reglas:** un paquete general queda asignado al socio en su primera cita; un paquete agotado o inactivo no permite agendar; las citas completadas no se editan, cancelan ni eliminan; un paquete con historial no se elimina (se desactiva).
- **API:** `/api/fisio/packages` (GET/POST/PUT/DELETE) y `/api/fisio/appointments` (GET/POST/PUT/DELETE, `PUT /{id}/status`) con permisos `fisio.view` / `fisio.manage`.
- **UI:** página de Fisioterapia con pestañas “Paquetes” y “Citas”: ocupación de sesiones, vigencia, filtros por socio/estado/fechas, agendar, reprogramar, completar, no asistió, cancelar y eliminar.
- **Pruebas:** 65 (unitarias + integración), incluyendo consumo/agotado, asignación de paquete general, reprogramación y protección del historial.

### Rutinas completas (2026-11-19)
- **Modelo:** nueva tabla puente `GYM_ROUTINE_PLAN_EXERCISES` (plan, ejercicio, día, orden, series, reps y notas) con FK a planes y ejercicios; el generador Anxo produce `GYM_ROUTINE_PLAN_EXERCISES.cs`.
- **Backend:** CRUD de ejercicios, composición de planes por días, asignación de rutina a socios (desactiva la anterior), registro y borrado de entrenamientos y captura de medidas corporales.
- **API:** `/api/routines/exercises`, `/api/routines/{planId}/exercises`, `/api/routines/plan-exercises/{id}`, `/api/routines/assignments`, `/api/routines/workouts` y `/api/routines/measurements` con permisos `routines.view` / `routines.manage`.
- **Reglas:** no se puede borrar un ejercicio en uso; una rutina con ejercicios o asignaciones no se elimina para conservar el historial; borrar/actualizar un plan con referencias devuelve un mensaje claro.
- **Esquema:** `InitializeSchema` ahora crea primero todas las tablas y después aplica índices y FK, evitando fallos por orden de dependencias.
- **UI:** página de Rutinas con pestañas “Planes”, “Ejercicios”, “Asignaciones”, “Entrenamientos” y “Medidas”, con editor de ejercicios por plan (día, orden, series, reps y notas).
- **Pruebas:** 64 (unitarias + integración), incluyendo el flujo completo plan → ejercicio → asignación → entrenamiento → medida.

### Clases y reservas (2026-11-19)
- **Backend:** CRUD de sesiones de clase (tipo, instructor, salón, horario, estado), reservas con control de cupo, lista de espera con promoción automática al liberarse un lugar, asistencia y cancelación de sesión con sus reservas.
- **API:** `/api/classes/sessions`, `/api/classes/sessions/{id}/reservations` y `/api/classes/reservations/{id}` con permisos `classes.view` / `classes.manage`.
- **UI:** página de Clases con pestañas “Tipos” y “Sesiones y reservas”: filtros por fecha/tipo, alta/edición/cancelación de sesiones y modal de reservas con asistencia.
- **Config:** soporte de variables de entorno (`Database__Server`, `WebServer__Port`, …).
- **Pruebas:** 63 (unitarias + integración), incluyendo cupo, lista de espera, promoción, asistencia y cancelación.


### Operación y seguridad (2026-11-19)
- **Bloqueo de login** tras `Authentication:MaxLoginAttempts` fallos con `LockoutMinutes`; nuevas columnas `FAILED_LOGIN_ATTEMPTS` y `LOCKED_UNTIL` (migración aditiva automática).
- **Rate limiting por IP** aplicado a auth (`auth`) y OTP/kiosco (`strict`); antes las políticas existían pero ningún endpoint las usaba.
- **OTP por correo real** con MailKit y plantilla HTML; en desarrollo se puede devolver el código con `Email:ShowCodeInResponse`.
- **Limpieza automática** cada hora: sesiones/OTP caducados, códigos de acceso no usados, retención de bitácora y membresías vencidas.
- **36 índices no únicos** para las consultas frecuentes.
- **Migración aditiva de columnas** en `DatabaseService` (sin `MatchTableStructure` destructivo).
- **Solución y CI**: `GymService.slnx`, `build.ps1` y workflow de GitHub Actions para runner self-hosted; `AnxoRoot` configurable.
- Pruebas: 60 (unitarias + integración), incluyendo lockout, limpieza y OTP en modo desarrollo.


### Correcciones posteriores a la migración (2026-11-19)
- **Bug:** `VerifyTotpCode` decodificaba el secreto (Base64 URL-safe sin padding) con `Convert.FromBase64String`, que lanzaba excepción; MFA nunca verificaba. Se normaliza el padding antes de decodificar.
- **Mejora:** el alta de socios registra `CREATED_BY` con el usuario autenticado (antes `-1`).
- **Mejora:** auditoría unificada: `CurrentUser` (AsyncLocal) se fija al validar la sesión y todas las `Session` usan el usuario real para `CREATED_BY`/`LAST_UPDATED_BY`.
- **Mejora:** `GET /api/kiosk/equipment/unavailable` exige token de dispositivo (`X-Device-Token` o `deviceToken`).
- **Mejora:** nuevo `GET /api/finance/transactions` (paginado y filtrable por `accountId`).
- **Mejora:** `VerifyMfa` migra de forma transparente secreto legacy (Base64) a AES-256 y códigos de respaldo en texto plano a SHA-256.
- **Pruebas:** proyecto `GymService.Tests` (xUnit) con 51 tests: 33 unitarios (hashing/tokens/OTP, `CurrentUser`, convertidor JSON, modelo y entidades Anxo) y 18 de integración contra una BD temporal (identidad, socios, ventas, accesos, finanzas y catálogos).

### Migración al modelo de objetos Anxo (2026-11-19)
- **Cambio:** El esquema pasó a estar definido por `GymService/Models/_ModelGym.xml` y las entidades generadas (`Models/Generated/GYM_*.cs`). Las PK ya no son `IDENTITY`: las asigna Anxo con secuencias (`ANXO_SEQUENCES`).
- **Cambio:** Todas las escrituras de los módulos usan `Session.InsertObject` / `InsertObjectWithID` / `UpdateObjectChanges` / `DeleteObject` (una transacción por operación). Los SELECT de listados siguen en SQL parametrizado con `?`.
- **Cambio:** Auditoría automática en todas las tablas (`CREATED_AT`, `UPDATED_AT`, `CREATED_BY`, `LAST_UPDATED_BY`, `LAST_UPDATE_LOG`, `OBJECT_VERSION`).
- **Bug corregido:** las FK nunca se aplicaban porque se llamaba `CreateTable` sobre tablas existentes. Ahora se usa `ApplyForeignKeys` (26 FK creadas).
- **Bug corregido:** los índices UNIQUE de columnas (`username`, `email`, `token_hash`, `sale_number`, etc.) no se creaban; ahora se crean de forma idempotente.
- **Bug corregido:** `gym_role_permissions` no tenía primary key; se añadió `id` surrogate + UNIQUE `(role, permission_id)`.
- **Bug corregido:** `-install` registraba el servicio apuntando a `AnxoServiceTools.dll`; se definió `AssemblyName = "GymService.exe"`.
- **Bug corregido:** la ruta de logs relativa se resolvía contra `C:\Windows\System32` al correr como servicio; ahora se ancla al directorio del ejecutable.
- **Bug corregido:** NLS no resolvía mensajes porque el código de assembly es un hash del nombre; ahora se calcula con `NlsTools.GetPersistenceHashCode`.
- **Nuevo:** modo `-console -daemon` para ejecución headless (CI/contenedores).
- **Herramienta:** `Tools/AnxoModelGenerator` compila el generador de Anxo contra `net10.0` desde el repo del gimnasio, sin modificar `C:\repositorios\pilot`.

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

### Tarea F08 — Documentar el frontend
- **Bug:** El frontend no tenía README.md, lo que impedía que un desarrollador nuevo pudiera instalar y ejecutar el proyecto sin conocer la estructura interna.
- **Solución:** Creado `GymServiceFront/README.md` con 113 líneas que incluyen: requisitos (Node 20+), instalación, scripts disponibles (dev, build, lint, preview), proxy de API en Vite, estructura completa de `src/` con 38 archivos, tecnologías, autenticación JWT, instrucciones de despliegue en IIS y créditos.
- **Verificación:** README cubre todos los puntos solicitados en la tarea F08.

### Tarea F07 — Persistir sidebar y accesibilidad en Layout
- **Bug:** Al recargar la página, el estado del sidebar (abierto/cerrado) se perdía y volvía siempre a abierto. Además, 6 botones de solo icono carecían de `aria-label`, lo que afectaba la accesibilidad con lectores de pantalla.
- **Solución:** Estado del sidebar inicializado desde `localStorage` con clave `sidebar-collapsed` y guardado automáticamente al cambiar. Agregados `aria-label` a 6 botones: cerrar menú móvil, abrir menú móvil, notificaciones, cerrar sesión (expandido y colapsado), colapsar sidebar y expandir sidebar.
- **Verificación:** `npm run build` → 2218 módulos, 1.01s, 0 errores.

### Tarea F06 — Página 404 real
- **Bug:** Cualquier ruta desconocida redirigía silenciosamente al dashboard (`/`) sin informar al usuario.
- **Solución:** Creado `NotFound.jsx` con diseño TailAdmin (icono AlertTriangle, colores amber, botón "Volver al inicio" con ArrowLeft). En `App.jsx`, la ruta `*` ahora renderiza `<NotFound />` dentro de `<ProtectedRoute>` + `<Layout>` en lugar de redirigir a `/`.
- **Verificación:** `npm run build` → 2218 módulos, 1.02s, 0 errores.

### Tarea F03 — Logs de depuración en api.js
- **Bug:** api.js contenía 2 `console.log` que imprimían cada petición y respuesta en la consola del navegador, incluso en producción.
- **Solución:** Eliminados los 2 `console.log` (interceptor de request y response). Se mantuvo `console.error` para errores reales y toda la lógica del interceptor 401.
- **Verificación:** `grep "console.log" api.js` → 0 resultados. `npm run build` → 2217 módulos, 635ms, 0 errores.

### Tarea F01 — Colores `primary-*` residuales en Members y Sales
- **Bug:** Members.jsx y Sales.jsx aún usaban clases `primary-*` (spinner, botón, inputs, avatar, badge) que no generan CSS con la paleta `brand-*` de TailAdmin.
- **Solución:** Reemplazadas 10 ocurrencias de `primary-*` por `brand-*` conservando el tono numérico. 8 en Members.jsx (spinner, botón gradiente, 5 inputs, avatar) y 2 en Sales.jsx (spinner, badge).
- **Verificación:** `npm run build` → 2210 módulos, 815ms, 0 errores.

---

## Correcciones críticas de backend (19-nov-2026)

### El arranque del servicio ya no borra la base de datos
- **Bug:** `DatabaseService.CreateOrUpdateTable()` hacía `DROP TABLE` de cada tabla existente y la
  recreaba en **cada arranque** del servicio. Al reiniciar con el build nuevo se perdieron usuarios,
  socios y planes.
- **Solución:** ahora solo crea las tablas que faltan (`CreateTableIfMissing`). Si el esquema ya existe,
  se conserva tal cual y no se recrean las foreign keys. Las foreign keys solo se agregan cuando la
  base se crea desde cero.
- **Verificación:** se sembraron 3 planes y se reinició el servicio; los datos siguieron intactos.

### `SCOPE_IDENTITY()` no funciona entre conexiones
- **Bug:** `MembersModule.CreateMember()` y `SalesModule.CreateSale()` insertaban la fila y luego
  consultaban `SELECT SCOPE_IDENTITY()` en **otra conexión**, por lo que siempre devolvía 0.
  - El socio se creaba sin membresía y la respuesta traía `memberId: 0`.
  - La venta se creaba sin items y la respuesta traía `saleId: 0`.
- **Solución:** se busca la fila recién insertada por sus datos (`MAX(id)` filtrando por
  nombre/apellido/email del socio o por `sale_number`, que es único).
- **Verificación:** socio creado con plan Premium y membresía de 30 días; venta con items guardados
  en `gym_sale_items`.

### `membershipType` vacío en el listado de socios
- **Bug:** la consulta devolvía `mp.plan_name AS membership_type`, pero el módulo leía la columna
  `plan_name`; el plan siempre aparecía vacío.
- **Solución:** leer `membership_type`.
- **Verificación:** `GET /api/members` devuelve `membershipType: "Premium"` y `membershipEnd`.

### Prueba funcional completa (instancia de prueba en `:5001`)
- `GET /api/members/plans` → 3 planes.
- `POST /api/inventory` + `GET /api/inventory` → producto con `stock_quantity`.
- `POST /api/members` con plan → socio + membresía; `GET /api/members` con plan y vencimiento.
- `POST /api/sales` + `GET /api/sales` → venta con número de 19 caracteres, IVA y items.
- Los datos de prueba se eliminaron al terminar; quedaron el usuario admin y los 3 planes.

### Incidente de datos
- El reinicio con el build anterior vació la base. Se volvió a crear el usuario dueño con una
  contraseña temporal (cambiar en el primer inicio) y se sembraron los planes Básico, Premium y VIP.
  Los socios de prueba no se recrearon.

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
  - **JSON de entrada:** los endpoints que reciben `Dictionary<string, object>` obtenían
    `JsonElement` y fallaban en `Convert.ToInt64/ToDecimal/ToBoolean`. Se añadió
    `Tools/ObjectToInferredTypesConverter.cs` (registrado en `AddControllers()`), por lo que
    los valores llegan como `long`, `decimal`, `bool`, `string`, `List<object>` o
    `Dictionary<string, object>`. Esto repara los POST de ventas, inventario, clases, fisio,
    mantenimiento y facturación.
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
