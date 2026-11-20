# GymService - Centro Fit

Nueva versión del sistema de gestión de gimnasios **Centro Fit**, reescrita en **.NET 10** que corre como **servicio de Windows**, reutilizando las bibliotecas ANXO de PILOT TMS.

## Stack Técnico

- **Backend**: .NET 10 con ASP.NET Core Kestrel
- **Servicio Windows**: AnxoServiceTools
- **Base de datos**: SQL Server con AnxoDatabase y AnxoObjectDatabaseManager
- **Bibliotecas**: AnxoLibrary, AnxoServiceTools, AnxoDatabase, AnxoObjectDatabaseManager
- **API REST**: Controllers con DTOs, Swagger/OpenAPI
- **Autenticación**: Sesiones Bearer opacas, MFA TOTP, OTP por correo
- **Frontend**: Panel web React 19 + Vite + Tailwind CSS 4

## Estructura del Proyecto

```
gimnasio-dotnet/
├── GymService/
│   ├── GymService.csproj          # Proyecto .NET 10
│   ├── Program.cs                 # Entry point
│   ├── DataCenter.cs              # Estado global
│   ├── ServiceImplementation.cs   # IServiceConsole
│   ├── appsettings.json           # Configuración
│   ├── nls.xml                    # Localización
│   ├── Controllers/               # API REST
│   │   └── AuthController.cs
│   ├── DTOs/                      # Request/Response DTOs
│   ├── Modules/                   # Lógica de negocio
│   │   ├── Identity/              # Autenticación, MFA, OTP, RBAC
│   │   ├── Members/               # Socios y membresías
│   │   ├── Access/                # Códigos rotativos, check-in
│   │   ├── Sales/                 # Punto de venta
│   │   ├── Inventory/             # Productos y stock
│   │   ├── Classes/               # Clases y reservas
│   │   ├── Routines/              # Rutinas y ejercicios
│   │   ├── Fisio/                 # Fisioterapia
│   │   ├── Maintenance/           # Equipos y tickets
│   │   ├── Billing/               # Facturación CFDI
│   │   ├── Finance/               # Cuentas bancarias
│   │   ├── Reports/               # Reportes
│   │   ├── Kiosk/                 # Dispositivos kiosco
│   │   └── Staff/                 # Personal y push
│   ├── Models/
│   │   └── ModelGym.cs            # 36 tablas SQL Server
│   ├── Services/
│   │   ├── WebServerService.cs    # Kestrel + panel web
│   │   └── DatabaseService.cs     # Esquema de BD
│   └── Tools/
│       └── SecurityValidation.cs  # Validación JWT/sesiones
├── GymServiceSetup/               # Instalador/desinstalador PowerShell
└── publish.ps1                    # Publicación (backend + frontend + ZIP)
```

## Módulos

| Módulo | Descripción |
|--------|-------------|
| **Identity** | Autenticación, sesiones Bearer, MFA TOTP, OTP por correo, RBAC, bootstrap de dueño |
| **Members** | Socios, membresías, ciclo de vida (freeze/resume/cancel), pagos manuales |
| **Access** | Códigos rotativos HMAC, check-in, bitácora, override manual |
| **Sales** | Punto de venta, devoluciones, ventas offline, tickets PDF |
| **Inventory** | Productos, stock, proveedores, órdenes de compra |
| **Classes** | Tipos, salones, sesiones, reservas, lista de espera, asistencia |
| **Routines** | Planes, ejercicios, asignaciones, entrenamientos, medidas, metas |
| **Fisio** | Paquetes, citas, historial |
| **Maintenance** | Equipos, tickets, bitácora técnica, rondas |
| **Billing** | Facturas CFDI 4.0 (diferido), complementos de pago PPD |
| **Finance** | Cuentas bancarias, importación CSV, conciliación |
| **Reports** | Reportes financieros y operativos, CSV programados |
| **Kiosk** | Dispositivos, tokens de 90 días, validación de códigos |
| **Staff** | Personal, roles, permisos, push notifications |

## Base de Datos

35 tablas SQL Server gestionadas por el modelo de objetos de Anxo. La fuente de verdad es
`GymService/Models/_ModelGym.xml`; las entidades (`IDatabaseObject`) y el registro
`ModelGym.MappingObjects` se generan en `GymService/Models/Generated/`.

- Las PK son `BIGINT` **sin IDENTITY**: las asigna Anxo con secuencias (`ANXO_SEQUENCES`).
- Auditoría automática en todas las tablas: `CREATED_AT`, `UPDATED_AT`, `CREATED_BY`,
  `LAST_UPDATED_BY`, `LAST_UPDATE_LOG`, `OBJECT_VERSION`.
- El esquema se crea/valida al arrancar (`Services/DatabaseService.cs`) de forma aditiva:
  crea tablas, índices y FK que falten; nunca borra datos.

Tablas:

- `gym_users` - Usuarios del sistema
- `gym_permissions` - Permisos RBAC
- `gym_role_permissions` - Asignación de permisos por rol
- `gym_sessions` - Sesiones Bearer
- `gym_otp_codes` - Códigos OTP por correo
- `gym_members` - Socios
- `gym_membership_plans` - Planes de membresía
- `gym_memberships` - Membresías activas
- `gym_payments` - Pagos manuales
- `gym_access_codes` - Códigos rotativos
- `gym_access_log` - Bitácora de accesos
- `gym_kiosk_devices` - Dispositivos kiosco
- `gym_inventory` - Productos
- `gym_sales` - Ventas
- `gym_sale_items` - Items de venta
- `gym_suppliers` - Proveedores
- `gym_purchase_orders` - Órdenes de compra
- `gym_class_types` - Tipos de clase
- `gym_class_sessions` - Sesiones de clase
- `gym_class_reservations` - Reservas
- `gym_routine_plans` - Planes de rutina
- `gym_routine_plan_exercises` - Ejercicios de cada plan (día, orden, series y reps)
- `gym_exercises` - Ejercicios
- `gym_routine_assignments` - Asignaciones
- `gym_workouts` - Entrenamientos
- `gym_measurements` - Medidas corporales
- `gym_fisio_packages` - Paquetes de fisioterapia
- `gym_fisio_appointments` - Citas de fisioterapia
- `gym_equipment` - Equipos
- `gym_maintenance_tickets` - Tickets de mantenimiento
- `gym_invoices` - Facturas
- `gym_bank_accounts` - Cuentas bancarias
- `gym_bank_transactions` - Transacciones bancarias
- `gym_scheduled_reports` - Reportes programados
- `gym_staff` - Personal
- `gym_push_subscriptions` - Suscripciones Web Push

### Regenerar el modelo y las entidades

Tras editar `Models/_ModelGym.xml`:

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService\Models\Generated
dotnet run --project "C:\proyectos\gimnasio-dotnet\Tools\AnxoModelGenerator\AnxoModelGenerator.csproj" -- ..\_ModelGym.xml
```

`Tools/AnxoModelGenerator` es un envoltorio **del repo del gimnasio**: compila los fuentes del
generador de Anxo contra `net10.0` sin modificar `C:\repositorios\pilot`.
El generador sobreescribe `GYM_*.cs` y `_ModelGym.cs` en el directorio actual.
No edites esos archivos a mano.

## API REST

### Autenticación

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login con username/password |
| `/api/auth/logout` | POST | Cerrar sesión |
| `/api/auth/mfa/setup` | POST | Configurar MFA TOTP |
| `/api/auth/mfa/verify` | POST | Verificar código MFA |
| `/api/auth/otp/request` | POST | Solicitar OTP por correo |
| `/api/auth/otp/verify` | POST | Verificar OTP |
| `/api/auth/bootstrap` | POST | Crear primer usuario dueño |

## Configuración

Edita `appsettings.json`:

```json
{
  "Database": {
    "Server": "localhost",
    "Database": "GymManager",
    "User": "sa",
    "Password": "tu_password",
    "Port": 1433,
    "PoolSize": 10
  },
  "WebServer": {
    "Port": 5000,
    "Urls": "http://localhost:5000"
  },
  "Authentication": {
    "MfaEncryptionKey": "clave_aleatoria_de_32_bytes_en_base64",
    "AdminResetToken": ""
  }
}
```

> `appsettings.json` está en `.gitignore`: usa `appsettings.example.json` como plantilla.
> `MfaEncryptionKey` es obligatoria para MFA (sin ella el setup/verify devuelve error controlado).
> Si `AdminResetToken` está vacío, `/api/auth/reset-admin-password` queda deshabilitado (404); para usarlo, define un token y envíalo en el header `X-Admin-Reset-Token`.
> En producción no uses `sa`: crea un login SQL con permisos mínimos sobre la base de datos del gimnasio.

Cualquier clave de `appsettings.json` se puede sobrescribir con variables de entorno usando
doble guion bajo (`Database__Server`, `WebServer__Port`, `Email__SmtpPassword`…), útil para
producción y CI.

### Seguridad y operación

- **Bloqueo de cuenta**: tras `Authentication:MaxLoginAttempts` fallos, la cuenta queda bloqueada
  `Authentication:LockoutMinutes` minutos (`FAILED_LOGIN_ATTEMPTS` / `LOCKED_UNTIL` en `GYM_USERS`).
- **Rate limiting** por IP: `auth` (20/min), `strict` (5/min en OTP y validación de kiosco) y
  `general` (100/min) en `WebServerService`.
- **OTP por correo real** con MailKit (`Email:SmtpServer/Port/User/Password/FromAddress/EnableSsl`).
  Si no hay SMTP y `Email:ShowCodeInResponse=true` (solo desarrollo), el código se devuelve en la
  respuesta; si no, el alta de OTP falla y no deja códigos huérfanos.
- **Correos transaccionales:** bienvenida al alta de socio, recibo al asignar/renovar con cobro y
  aviso de vencimiento (`Notifications:ExpiryNoticeDays`, por defecto 7 días) con la marca
  `EXPIRY_NOTICE_SENT_AT` para no repetirlo. Si no hay SMTP configurado,
  `Email:OutboxDirectory` guarda los correos como HTML (útil para desarrollo y pruebas).
- **Limpieza automática** (`CleanupHostedService` cada `Maintenance:CleanupIntervalMinutes`):
  sesiones y OTP caducados, códigos de acceso no usados, retención de bitácora y membresías vencidas.
- **Índices no únicos** para las consultas más frecuentes (`member_id`, `status`, fechas, `sale_id`…),
  creados de forma aditiva al arrancar.
- **Migración aditiva de columnas**: si el modelo agrega columnas, se crean con `ALTER TABLE`
  (sin borrar datos); `Database:MatchStructure=true` sigue siendo la opción destructiva explícita.

## Compilación

```powershell
cd C:\proyectos\gimnasio-dotnet
.\build.ps1
```

`build.ps1` compila `GymService.slnx` (Release) y ejecuta las pruebas unitarias.
Para integración: `.\build.ps1 -Integration` con las variables `GYM_TEST_DB_*` definidas.
La ruta de las ANXO Libraries se puede sobrescribir con `-AnxoRoot` o `-p:AnxoRoot=...`.

## Grafo del código (graphify)

El repositorio incluye la skill y el plugin de
[graphify](https://github.com/Graphify-Labs/graphify) en `.opencode/`, con las instrucciones
en `AGENTS.md`. El grafo se genera en local con el AST de tree-sitter (sin LLM ni API):

```powershell
python -m pip install --user graphifyy   # o: uv tool install graphifyy
graphify extract . --code-only           # construye graphify-out/
graphify update .                        # tras cambiar código
graphify explain "GymService/Modules/Members/MembersModule.cs::MembersModule"
graphify query "como se valida el acceso de un socio"
```

`graphify-out/` (grafo, reporte y HTML) es generado y **no se versiona**.

## Pruebas

```powershell
cd C:\proyectos\gimnasio-dotnet
dotnet test GymService.Tests\GymService.Tests.csproj
```

> **Regla obligatoria:** toda funcionalidad debe probarse **en vivo** (API real y
> navegador cuando aplique) antes de considerarse terminada. Ver
> [`docs/pruebas-en-vivo.md`](docs/pruebas-en-vivo.md).

- **Unitarias (siempre):** hashing de tokens/OTP, contexto de auditoría (`CurrentUser`),
  convertidor JSON y consistencia del modelo/entidades de Anxo.
- **Integración:** crean una base de datos temporal en el servidor configurado, aplican el
  esquema del modelo, ejecutan los flujos reales (identidad, socios, ventas, accesos,
  finanzas, catálogos) y **borran la BD al terminar**. Se omiten si no hay credenciales:

```powershell
$env:GYM_TEST_DB_SERVER   = "localhost,1433"
$env:GYM_TEST_DB_USER     = "sa"
$env:GYM_TEST_DB_PASSWORD = "********"
dotnet test GymService.Tests\GymService.Tests.csproj
```

## Ejecución (modo consola)

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService
dotnet run
```

Modo headless (CI/contenedor, sin esperar Enter):

```powershell
.\GymService.exe -console -daemon
```

## Despliegue (producción)

### 1. Publicar

```powershell
cd C:\proyectos\gimnasio-dotnet
.\publish.ps1
```

- Compila el panel React y lo copia a `wwwroot` del servicio (se sirve en la misma URL).
- Publica el backend **self-contained** para `win-x64` (no requiere instalar .NET en el servidor).
- Genera `artifacts\GymService-<versión>-win-x64.zip` con `install.ps1` y `uninstall.ps1`.

Opciones útiles:

```powershell
.\publish.ps1 -Version 1.2.0                 # versión del paquete
.\publish.ps1 -FrameworkDependent            # requiere .NET 10 en el servidor
.\publish.ps1 -SkipFrontend -NoZip           # solo backend, sin ZIP
.\publish.ps1 -AnxoRoot "D:\libs\ANXO LIBRARIES"
```

En CI, el workflow `Release` publica el ZIP al crear un tag `v*`.

### 2. Instalar

Copiar el ZIP al servidor y ejecutar **como Administrador**:

```powershell
.\install.ps1 `
  -Source .\GymService-1.0.0-win-x64.zip `
  -Port 5000 `
  -DatabaseServer "sql.empresa.local,1433" `
  -DatabaseName "GymManager" `
  -DatabaseUser "gym_app" `
  -DatabasePassword "********" `
  -OpenFirewall
```

El instalador:

- Copia los binarios a `C:\Program Files\Anxo\GymService`.
- Crea `appsettings.json` desde la plantilla **solo si no existe** (las actualizaciones conservan la configuración).
- Restringe los permisos de `appsettings.json` a SYSTEM y Administradores.
- Registra el servicio `anxogymmservice`, configura recuperación ante fallos y lo inicia.
- Opcionalmente abre el puerto en el firewall (`-OpenFirewall`).

Panel web y API quedan en `http://<servidor>:<puerto>` (por defecto `http://0.0.0.0:5000`).

### 3. Configuración mínima de producción

Editar `C:\Program Files\Anxo\GymService\appsettings.json`:

- `Database`: servidor, base de datos y **login SQL con permisos mínimos** (no `sa`).
- `Authentication:MfaEncryptionKey`: clave aleatoria de 32 bytes en Base64.
- `Authentication:AdminResetToken`: token largo y secreto (o vacío para deshabilitarlo).
- `Email`: SMTP real y `ShowCodeInResponse: false`.
- `WebServer:Urls`: por ejemplo `http://0.0.0.0:5000`; detrás de IIS/Nginx usar `http://localhost:5000`.
- `WebServer:CorsOrigins`: solo los orígenes necesarios si el panel se sirve aparte.
- `Logging:LogPath` y retención según política del cliente.

Cualquier valor se puede sobrescribir por variables de entorno con doble guion bajo:
`Database__Password`, `WebServer__Port`, `Email__SmtpPassword`, etc.

### 4. Operación

```powershell
Get-Service anxogymmservice
Restart-Service anxogymmservice
Get-Content "C:\Program Files\Anxo\GymService\logs\*.log" -Tail 50

# Desinstalar (conserva configuración y logs)
.\uninstall.ps1

# Desinstalar y borrar configuración/logs
.\uninstall.ps1 -RemoveData
```

Actualizar = ejecutar de nuevo `install.ps1` con el ZIP nuevo (detiene, reemplaza binarios y arranca; conserva `appsettings.json` y `logs`).

### 5. Checklist antes de entregar

- [ ] Login SQL dedicado y contraseña fuerte (sin `sa`).
- [ ] `MfaEncryptionKey` generada para ese servidor.
- [ ] SMTP probado (código OTP real).
- [ ] Backup programado de la base de datos.
- [ ] HTTPS por reverse proxy si se publica fuera de la red local.
- [ ] Firewall limitado a la red del gimnasio.
- [ ] `GET /health` responde `healthy`.
- [ ] Alta del owner y prueba de login/panel.

## Reglas de negocio

Las reglas de membresías, renovaciones y control de acceso se documentan en
[`docs/reglas-negocio.md`](docs/reglas-negocio.md), portadas del proyecto original
(`C:\proyectos\gimnasio`). Incluyen: sin acceso sin membresía vigente, sin periodo
de gracia, renovación desde el vencimiento actual, congeladas sin acceso, cortesías
auditadas, cobro manual y límite diario de entradas por plan.

## Roles

| Rol | Descripción |
|-----|-------------|
| **OWNER** | Dueño, acceso total, MFA TOTP opcional |
| **MANAGER** | Gerente, gestión operativa |
| **RECEPTION** | Recepción, acceso, ventas, clases |
| **ACCOUNTING** | Contabilidad, facturación y finanzas |
| **MEMBER** | Socio, app móvil con OTP |
| **EMPLOYEE** | Personal del gimnasio |

## Estado del Proyecto

- ✅ Estructura del proyecto .NET 10
- ✅ Servicio Windows con AnxoServiceTools (`AssemblyName` correcto para `-install`)
- ✅ Conexión a SQL Server con AnxoDatabase
- ✅ Servidor Kestrel con ASP.NET Core
- ✅ 35 tablas SQL Server definidas, con índices UNIQUE y foreign keys aplicadas
- ✅ 18 controllers REST con ~67 endpoints operativos
- ✅ Módulo Identity completo (login, MFA, OTP, RBAC)
- ✅ AuthController con endpoints REST
- ✅ Seguridad: PBKDF2-SHA256, MFA cifrado AES-256, SQL parametrizado, endpoints de personal autenticados
- ✅ Frontend React 19 + Vite + Tailwind CSS 4 con 20 páginas
- ✅ Importación masiva de socios e inventario desde el panel (CSV/Excel, UTF-8 o Latin-1)
- ✅ 106 pruebas automatizadas (unitarias + integración)
- ⏳ Instalador WiX

## Migración desde PostgreSQL

El gimnasio actual usa PostgreSQL. Para migrar a SQL Server:

1. Exportar datos desde PostgreSQL con `pg_dump`
2. Transformar el schema a SQL Server
3. Importar datos con scripts de migración
4. Validar integridad de datos

## Licencia

Propietario - ANXO / Centro Fit
