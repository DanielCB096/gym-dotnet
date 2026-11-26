# Centro Fit Gym Manager

Nueva versión del sistema de gestión de gimnasios **Centro Fit**, reescrita en **.NET 10** que corre como **servicio de Windows**, reutilizando las bibliotecas GymCore de PILOT TMS.

## Stack Técnico

- **Backend**: .NET 10 con ASP.NET Core Kestrel
- **Servicio Windows**: GymCore ServiceTools (ensamblado `AnxoServiceTools`)
- **Base de datos**: SQL Server con la capa de datos de GymCore (`AnxoDatabase`, `AnxoObjectDatabaseManager`)
- **Bibliotecas**: GymCore Libraries (ensamblados `AnxoLibrary`, `AnxoServiceTools`, `AnxoDatabase`, `AnxoObjectDatabaseManager`)
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
| **Members** | Socios, membresías, ciclo de vida (freeze/resume/cancel), pagos manuales, crédito, ficha completa y foto |
| **Access** | Códigos rotativos HMAC, check-in, bitácora, override manual |
| **Sales** | Punto de venta, devoluciones, ventas offline, tickets PDF |
| **Inventory** | Productos, stock, proveedores, órdenes de compra |
| **Classes** | Tipos, salones, sesiones, reservas, lista de espera, asistencia |
| **Routines** | Planes, ejercicios, asignaciones, entrenamientos, medidas, metas, impresión de rutina |
| **Fisio** | Paquetes, citas, historial |
| **Maintenance** | Equipos, tickets, bitácora técnica, rondas |
| **Billing** | Facturas, comprobante interno imprimible (CFDI 4.0 diferido) |
| **Finance** | Cuentas bancarias, importación CSV, conciliación |
| **Reports** | Reportes financieros y operativos, Excel programados |
| **Kiosk** | Dispositivos, tokens de 90 días, validación de códigos |
| **Staff** | Personal, roles, permisos, push notifications |

## Base de Datos

41 tablas GYM (42 con `ANXO_SEQUENCES`) gestionadas por el modelo de objetos de GymCore. La fuente de verdad es
`GymService/Models/_ModelGym.xml`; las entidades (`IDatabaseObject`) y el registro
`ModelGym.MappingObjects` se generan en `GymService/Models/Generated/`.

- Las PK son `BIGINT` **sin IDENTITY**: las asigna GymCore con secuencias (`ANXO_SEQUENCES`).
  **No reinicies secuencias a 1** si la tabla conserva filas: la siguiente inserción chocaría con
  una PK existente. Tras una limpieza manual, ejecuta
  `deploy/maintenance/Sync-Sequences.ps1 -Apply` (informa y sube cada secuencia por encima de
  `max(ID)`), y reinicia el servicio para que tome un bloque nuevo.
- Auditoría automática en todas las tablas: `CREATED_AT`, `UPDATED_AT`, `CREATED_BY`,
  `LAST_UPDATED_BY`, `LAST_UPDATE_LOG`, `OBJECT_VERSION`.
- El esquema se crea/valida al arrancar (`Services/DatabaseService.cs`) de forma aditiva:
  crea tablas, índices y FK que falten; nunca borra datos.

### SQL Server Express (instalación local)

Express es suficiente para una tienda/gimnasio pequeño y es la edición usada en la instalación local.

- **Límites de la edición:** 1 CPU física, 1 GB de memoria y 50 GB por base de datos (SQL Server 2025 Express).
- Si el negocio crece, se necesita varias sucursales o la base supera el límite, migrar a **Standard**.
- La aplicación no usa funciones exclusivas de Enterprise; los scripts de respaldo detectan Express
  (`EngineEdition=4`) y omiten `COMPRESSION`, que Express no admite.
- Instancia predeterminada `MSSQLSERVER` en el puerto 1433. Para producción, usar un **login SQL dedicado**
  (`gym_app`) con permisos mínimos sobre `GymManager`; no usar `sa`.
- **Permisos del login dedicado:** `db_owner` sobre `GymManager` (el arranque crea/valida tablas, índices
  y FK) y el permiso de servidor `CREATE ANY DATABASE`, que SQL Server exige para `RESTORE VERIFYONLY`,
  `RESTORE HEADERONLY`/`FILELISTONLY` y para restaurar la base temporal de prueba. No se le asigna
  `sysadmin` ni el rol `dbcreator`. Ejecutar en `master`:

  ```sql
  CREATE LOGIN [gym_app] WITH PASSWORD = N'<contraseña-fuerte>',
      CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [GymManager];
  GRANT CREATE ANY DATABASE TO [gym_app];

  USE [GymManager];
  CREATE USER [gym_app] FOR LOGIN [gym_app];
  ALTER ROLE [db_owner] ADD MEMBER [gym_app];
  ```

  La contraseña solo se guarda en `appsettings.json` (no versionado); no se documenta ni se sube al repositorio.

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
dotnet run --project "C:\proyectos\gimnasio-dotnet\Tools\GymModelGenerator\GymModelGenerator.csproj" -- ..\_ModelGym.xml
```

`Tools/GymModelGenerator` es un envoltorio **del repo del gimnasio**: compila los fuentes del
generador de GymCore contra `net10.0` sin modificar `C:\repositorios\pilot`.
El generador sobreescribe `GYM_*.cs` y `_ModelGym.cs` en el directorio actual.
No edites esos archivos a mano.

## API REST

### Autenticación

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login con username/password. Si MFA está activo devuelve `requiresMfa` y `mfaToken` (5 min) |
| `/api/auth/mfa/login` | POST | Verifica TOTP o código de respaldo y crea la sesión |
| `/api/auth/logout` | POST | Cerrar sesión |
| `/api/auth/change-password` | POST | Cambiar la contraseña propia; invalida todas las sesiones |
| `/api/auth/mfa/setup` | POST | Generar secreto TOTP, URI `otpauth` y códigos de respaldo |
| `/api/auth/mfa/verify` | POST | Activar MFA verificando el primer código |
| `/api/auth/mfa/status` | GET | Estado de MFA y códigos de respaldo restantes |
| `/api/auth/mfa/disable` | POST | Desactivar MFA con un código válido |
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
    "User": "gym_app",
    "Password": "tu_password",
    "Port": 1433,
    "PoolSize": 10
  },
  "WebServer": {
    "Port": 5000,
    "Urls": "http://localhost:5000",
    "EnableSwagger": false
  },
  "Authentication": {
    "MfaEncryptionKey": "clave_aleatoria_de_32_bytes_en_base64",
    "MfaChallengeMinutes": 5,
    "MfaMaxAttempts": 5,
    "AdminResetToken": ""
  }
}
```

> `appsettings.json` está en `.gitignore`: usa `appsettings.example.json` como plantilla.
> `MfaEncryptionKey` es obligatoria para MFA (sin ella el setup/verify devuelve error controlado).
> Si `AdminResetToken` está vacío, `/api/auth/reset-admin-password` queda deshabilitado (404); para usarlo, define un token y envíalo en el header `X-Admin-Reset-Token`.
> En producción no uses `sa`: crea el login `gym_app` con los permisos de la sección
> [SQL Server Express](#sql-server-express-instalación-local) y configura aquí sus credenciales.

Cualquier clave de `appsettings.json` se puede sobrescribir con variables de entorno usando
doble guion bajo (`Database__Server`, `WebServer__Port`, `Email__SmtpPassword`…), útil para
producción y CI.

### Seguridad y operación

- **Bloqueo de cuenta**: tras `Authentication:MaxLoginAttempts` fallos, la cuenta queda bloqueada
  `Authentication:LockoutMinutes` minutos (`FAILED_LOGIN_ATTEMPTS` / `LOCKED_UNTIL` en `GYM_USERS`).
- **MFA TOTP (opcional por usuario)**: la contraseña correcta no crea sesión si MFA está activo;
  devuelve un desafío firmado (HMAC, `MfaChallengeMinutes`, un solo uso) y el segundo factor se
  valida en `/api/auth/mfa/login`. Los códigos de respaldo son de un solo uso. Se configura desde
  **Seguridad** en el panel, con QR, clave manual y 10 códigos de respaldo.
  `MfaMaxAttempts` limita los intentos por desafío. Los secretos se guardan cifrados con AES-256
  usando `MfaEncryptionKey`; sin esa clave MFA no se puede activar ni verificar.
  **La clave debe guardarse fuera del servidor**: si se restaura la base en otro servidor con una
  clave distinta, el TOTP falla, pero los códigos de respaldo siguen funcionando para recuperar la
  cuenta. El respaldo offsite incluye `mfa-key-*.7z`, cifrado con la misma contraseña AES-256.
  **Recuperación:** si se pierden el teléfono y los códigos de respaldo, un administrador puede
  usar **Personal → Usuarios → Resetear MFA** (solo owner para otro owner), que borra el secreto y
  revoca las sesiones. Como último recurso con acceso al servidor:
  `UPDATE gym_users SET mfa_enabled=0, mfa_secret_encrypted=NULL, mfa_backup_codes=NULL WHERE username='...';`
  y el usuario vuelve a configurarlo desde **Seguridad**. Guarda los códigos de respaldo fuera
  del servidor.
- **Gestión de usuarios:** cada usuario puede cambiar su contraseña desde **Seguridad** (se
  invalidan todas sus sesiones). Un ADMIN/OWNER puede editar correo, nombre, rol y estado, y
  resetear contraseñas desde **Personal → Usuarios**. No se puede desactivar al owner ni a uno
  mismo, ni cambiar el propio rol.
- **Superficie HTTP:** Swagger está apagado por defecto (`WebServer:EnableSwagger=false`) y no se
  usa `DeveloperExceptionPage`. Para diagnóstico, habilita Swagger explícitamente y solo en un
  entorno controlado; en producción `/swagger` responde 404.
- **Cuenta de servicio:** el servicio corre como la cuenta virtual
  `NT SERVICE\centrofitgymservice`, no como `LocalSystem`. Solo tiene lectura/ejecución sobre la
  aplicación, escritura sobre `logs` y lectura de `appsettings.json` y del PFX de HTTPS.
  `deploy/security/Set-ServiceAccount.ps1` aplica los permisos y la cuenta (con `-Rollback`
  vuelve a `LocalSystem`). El instalador y el MSI aplican la misma configuración. La conexión a
  SQL sigue usando el login `gym_app`; la cuenta de Windows no cambia permisos de base de datos.
  Si se renueva el certificado, `New-GymServerCertificate.ps1` conserva el permiso de lectura
  para esa cuenta.
- **SQL Server:** el login `sa` está deshabilitado. La aplicación usa el login dedicado `gym_app`;
  la administración del servidor se hace con autenticación Windows desde el propio servidor.
  Los respaldos completo, log y offsite siguen funcionando con `gym_app`.
- **Rate limiting** por IP: `auth` (20/min), `strict` (5/min en OTP y validación de kiosco) y
  `general` (100/min) en `WebServerService`.
- **OTP por correo real** con MailKit (`Email:SmtpServer/Port/User/Password/FromAddress/EnableSsl`).
  Si no hay SMTP y `Email:ShowCodeInResponse=true` (solo desarrollo), el código se devuelve en la
  respuesta; si no, el alta de OTP falla y no deja códigos huérfanos.
- **Correos transaccionales:** bienvenida al alta de socio, recibo al asignar/renovar con cobro y
  aviso de vencimiento (`Notifications:ExpiryNoticeDays`, por defecto 7 días) con la marca
  `EXPIRY_NOTICE_SENT_AT` para no repetirlo. Si no hay SMTP configurado,
  `Email:OutboxDirectory` guarda los correos como HTML (útil para desarrollo y pruebas).
- **Prueba de SMTP:** `GymService.exe -test-email correo@dominio` envía un mensaje de prueba sin
  arrancar el servicio ni crear datos (también `dotnet run --project GymService -- -test-email ...`).
  El puerto 465 usa SSL directo y el 587 STARTTLS.
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

### GymCore Libraries (solo para compilar)

El código fuente de las GymCore Libraries **no está en este repositorio** (es propietario).
La ruta por defecto es `..\third_party\gym-core` (en este equipo,
`third_party\gym-core`). En otro equipo, copia esa carpeta o define la ruta:

> Los ensamblados y proyectos de la librería conservan sus nombres `Anxo*` y sus namespaces
> `Ax*`; renombrarlos es la **fase C** (white-label completo) descrita en `docs/pendientes.md`.

```powershell
$env:GYM_LIBS_ROOT = "D:\libs\GymCore Libraries"   # tiene prioridad sobre la ruta por defecto
dotnet build GymService.slnx -c Release

.\build.ps1 -GymLibsRoot "D:\libs\GymCore Libraries"
.\publish.ps1 -GymLibsRoot "D:\libs\GymCore Libraries"
.\build-msi.ps1 -GymLibsRoot "D:\libs\GymCore Libraries"
```

Si la ruta no existe, la compilación se detiene con un mensaje claro. El **MSI y la
publicación self-contained ya incluyen las DLLs**: el servidor no necesita las librerías.

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

Se versionan `graphify-out/graph.json` y `graphify-out/GRAPH_REPORT.md`; el HTML
(`graph.html`), las cachés y el estado incremental están en `.gitignore`.

### Hook de commit

Actívalo una vez por clon:

```powershell
git config core.hooksPath .githooks
```

Desde entonces, cada `git commit` ejecuta `graphify update .` y añade el grafo actualizado
al propio commit (si falla la actualización, el commit se detiene; usa `--no-verify` para
saltarlo). El skill `.opencode/skills/graphify-commit/` documenta el mismo flujo para los
agentes.

## Pruebas

```powershell
cd C:\proyectos\gimnasio-dotnet
dotnet test GymService.Tests\GymService.Tests.csproj
```

> **Regla obligatoria:** toda funcionalidad debe probarse **en vivo** (API real y
> navegador cuando aplique) antes de considerarse terminada. Ver
> [`docs/pruebas-en-vivo.md`](docs/pruebas-en-vivo.md).

- **Unitarias (siempre):** hashing de tokens/OTP, contexto de auditoría (`CurrentUser`),
  convertidor JSON y consistencia del modelo/entidades de GymCore.
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
.\publish.ps1 -GymLibsRoot "D:\libs\GymCore Libraries"
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
  -OpenFirewall `
  -FirewallRemote "192.168.100.0/24"
```

El instalador:

- Copia los binarios a `C:\Program Files\Centro Fit\GymService`.
- Crea `appsettings.json` desde la plantilla **solo si no existe** (las actualizaciones conservan la configuración).
- Restringe los permisos de `appsettings.json` a SYSTEM y Administradores.
- Registra el servicio `centrofitgymservice`, configura recuperación ante fallos y lo inicia.
- Opcionalmente abre el puerto en el firewall (`-OpenFirewall`), limitado a `-FirewallRemote` (`localsubnet` por defecto).

Panel web y API quedan en `http://<servidor>:<puerto>` (por defecto `http://0.0.0.0:5000`). Para producción en red local se recomienda HTTPS directo en Kestrel; ver [3b](#3b-https-en-red-local-solo-ip).

### 2b. Instalador MSI (WiX)

Alternativa al ZIP con PowerShell. Genera un MSI per-machine que instala en
`C:\Program Files\Centro Fit\GymService` y registra el servicio `centrofitgymservice`.

```powershell
dotnet tool install -g wix --version 5.0.2   # WiX 6+ exige aceptar el EULA OSMF
.\build-msi.ps1                              # publica y genera artifacts\GymService-<version>-win-x64.msi
msiexec /i "artifacts\GymService-1.6.0-win-x64.msi" OPEN_FIREWALL=1 WEB_PORT=5055 FIREWALL_REMOTE=192.168.100.0/24   # Administrador
```

- El MSI instala `appsettings.json` desde la plantilla **solo si no existe** y lo conserva al
  desinstalar (`NeverOverwrite` + `Permanent`).
- El servicio queda instalado en automático **sin arrancar**: edita `appsettings.json` y ejecuta
  `Start-Service centrofitgymservice`.
- `OPEN_FIREWALL=1` abre `WEB_PORT` solo para `FIREWALL_REMOTE` (`localsubnet` por defecto; por ejemplo `192.168.100.0/24`). La regla `GymService HTTPS` se elimina al desinstalar.
- Configura recuperación ante fallos (`sc failure`) automáticamente.
- Si ya instalaste con el ZIP/PowerShell, desinstala esa versión antes para no duplicar el servicio.
- `build-msi.ps1` valida el MSI con `wix msi validate` y `-FrameworkDependent` reduce el tamaño.

### 3. Configuración mínima de producción

Editar `C:\Program Files\Centro Fit\GymService\appsettings.json`:

- `Database`: servidor, base de datos y **login SQL con permisos mínimos** (no `sa`).
- `Authentication:MfaEncryptionKey`: clave aleatoria de 32 bytes en Base64.
- `Authentication:AdminResetToken`: token largo y secreto (o vacío para deshabilitarlo).
- `Email`: SMTP real y `ShowCodeInResponse: false`.
- `WebServer:Urls`: `http://0.0.0.0:5000` en desarrollo o `https://0.0.0.0:5055` con TLS directo (ver 3b). No se recomienda IIS/Nginx delante: el rate limiting y los bloqueos usan `RemoteIpAddress`, y un proxy haría que todos los clientes compartieran `127.0.0.1` (el límite estricto de 5/min del kiosco pasaría a ser global).
- `WebServer:CorsOrigins`: solo los orígenes necesarios si el panel se sirve aparte.
- `Logging:LogPath` y retención según política del cliente.

Cualquier valor se puede sobrescribir por variables de entorno con doble guion bajo:
`Database__Password`, `WebServer__Port`, `Email__SmtpPassword`, etc.

### 3b. HTTPS en red local (solo IP)

Para acceso por IP dentro de la LAN, GymService termina TLS en Kestrel con un certificado
emitido por una CA local; no hace falta dominio ni proxy inverso.

```powershell
# Administrador
.\deploy\https\New-GymServerCertificate.ps1 -IpAddress 192.168.100.53
```

El script crea la CA `GymService LAN Root CA` (si no existe), emite el certificado con
SAN de IP (`192.168.100.53` y `127.0.0.1`), guarda el PFX en
`C:\ProgramData\GymService\certs`, actualiza `Kestrel` y `WebServer:Urls` en
`appsettings.json`, restringe las ACL y reinicia el servicio.

- Instala `C:\ProgramData\GymService\certs\gymservice-root-ca.cer` en
  **Entidades de certificación raíz de confianza** de cada equipo/tablet que use el panel.
- Abre `https://192.168.100.53:5055` (HTTP plano en ese puerto ya no responde).
- Firewall: regla `GymService HTTPS`, TCP 5055, solo `192.168.100.0/24`.
- En el kiosco, configura `apiUrl` como `https://192.168.100.53:5055`.
- Renovación o cambio de IP: vuelve a ejecutar el script con la IP nueva; la CA raíz no cambia.

Detalles por sistema operativo, diagnóstico y renovación: [`docs/https-red-local.md`](docs/https-red-local.md).

### 4. Operación

```powershell
Get-Service centrofitgymservice
Restart-Service centrofitgymservice
Get-Content "C:\Program Files\Centro Fit\GymService\logs\*.log" -Tail 50

# Desinstalar (conserva configuración y logs)
.\uninstall.ps1

# Desinstalar y borrar configuración/logs
.\uninstall.ps1 -RemoveData
```

Actualizar = ejecutar de nuevo `install.ps1` con el ZIP nuevo (detiene, reemplaza binarios y arranca; conserva `appsettings.json` y `logs`).

### 5. Checklist antes de entregar

- [ ] Login SQL dedicado `gym_app` y contraseña fuerte (sin `sa`), probado con `/health` y respaldos.
- [ ] `MfaEncryptionKey` generada para ese servidor.
- [ ] MFA activado para el owner y códigos de respaldo guardados fuera del servidor.
- [ ] Servicio con cuenta virtual (`NT SERVICE\centrofitgymservice`) y ACLs mínimas, no `LocalSystem`.
- [ ] SMTP probado (código OTP real).
- [ ] Backup programado de la base de datos.
- [ ] HTTPS en la LAN con CA local instalada en los equipos cliente (o reverse proxy con `ForwardedHeaders` si se publica fuera).
- [ ] Firewall limitado a la red del gimnasio.
- [ ] `GET /health` responde `healthy`.
- [ ] Alta del owner y prueba de login/panel.

## Respaldos y restauración

`backup.ps1` crea un respaldo completo con verificación (`RESTORE VERIFYONLY`) y
`verify-backup.ps1` lo restaura en una base temporal para comprobar que es recuperable.
El login usado por los scripts necesita `CREATE ANY DATABASE` en el servidor (además de `db_owner`
sobre `GymManager`); sin ese permiso, `RESTORE VERIFYONLY` y la restauración de prueba fallan con
"CREATE DATABASE permission denied in database 'master'".

```powershell
# Respaldo (carpeta por defecto del servidor SQL, base de appsettings.json)
.\backup.ps1

# Base o carpeta concretas; copia opcional a una ruta accesible desde este equipo
.\backup.ps1 -Database GymManager -BackupDir "D:\SQLBackups" -CopyTo "\\nas\gym\backups"

# Verificación: restaura en GymService_Verify_<fecha>, cuenta tablas y elimina la copia
.\verify-backup.ps1 -BackupFile "D:\SQLBackups\GymManager_20261120_170000.bak"

# Respaldo de log (requiere modelo FULL y un respaldo completo previo)
.\backup-log.ps1

# Programar: completo diario a las 02:00 + log cada 15 minutos
.\register-backup-tasks.ps1
.\register-backup-tasks.ps1 -RunAsSystem     # en un servidor, consola de Administrador
.\register-backup-tasks.ps1 -Unregister
```

- Los respaldos de log usan `backup-log.ps1`; falla con un mensaje claro si la base está en
  modelo SIMPLE o si aún no existe un respaldo completo. Para RPO de 15 minutos: modelo FULL,
  completo diario y log cada 15 minutos (`register-backup-tasks.ps1` lo deja configurado).
- En **SQL Server Express** los respaldos se crean sin `COMPRESSION` automáticamente; en Standard o
  superior se conserva la compresión. Los `.bak` de Express ocupan más espacio.
- En producción, copia `backup.ps1`, `backup-log.ps1`, `verify-backup.ps1` y
  `register-backup-tasks.ps1` junto al servicio (`C:\Program Files\Centro Fit\GymService`) y registra las
  tareas con `-ConfigPath` absoluto. Así no dependen del repositorio. Usa `-CopyTo` para una copia
  secundaria en USB, NAS u otra unidad.
- `register-backup-tasks.ps1` crea las tareas `GymService-Full` y `GymService-Log`. Sin
  `-RunAsSystem` corren con el usuario actual y solo con sesión iniciada; en un servidor usa
  `-RunAsSystem` desde una consola de Administrador.
- El `.bak` se escribe **en el servidor SQL**. `-CopyTo` solo funciona si ejecutas el script
  en ese servidor o si la ruta es un recurso compartido accesible por ambos equipos.
- `-RetentionDays` (14 por defecto) solo purga las copias hechas con `-CopyTo`; la retención del
  servidor debe gestionarla la política de respaldos.
- Objetivo recomendado (ADR-086 del proyecto original): **RPO ≤ 15 min y RTO ≤ 4 h**. Este script
  hace respaldos completos; para RPO de 15 minutos programa respaldos completos diarios y de
  log cada 15 minutos en modelo FULL.
- Para una recuperación real: detén el servicio, restaura a una base nueva y cambia
  `Database:Database`, o restaura sobre la base actual con `WITH REPLACE` con el servicio detenido.
  Nunca ejecutes una restauración destructiva sin autorización.
- `verify-backup.ps1` nunca usa como destino la base configurada en `appsettings.json`.

### Respaldo offsite cifrado

`deploy/offsite/Sync-OffsiteBackups.ps1` copia los `.bak`/`.trn` de `D:\GymBackups`
a una carpeta dentro de OneDrive, **comprimidos y cifrados con 7-Zip AES-256**
(`-mhe=on`, cabeceras incluidas). La tarea `GymService-Offsite` se ejecuta como
`SYSTEM` cada 15 minutos, omite los archivos ya cifrados y aplica retención de
30 días para completos y 7 días para logs.

- Destino actual: `C:\Users\PC\OneDrive\GymServiceBackups`.
- Contraseña: archivo protegido del servidor y copia para el cliente; **no está en
  Git**. Sin esa contraseña no se pueden restaurar los `.7z`.
- Log: `C:\ProgramData\GymService\logs\offsite-backup.log`.
- Si OneDrive no está en ejecución, la tarea falla para que se detecte.
- El script resetea la herencia del `.7z` después de moverlo (`icacls /reset`) para que el
  usuario de OneDrive pueda sincronizarlo; sin eso queda en “Sincronización pendiente”.
- Restauración, contraseña y reinstalación: [`deploy/offsite/README.md`](deploy/offsite/README.md).

### Monitoreo de respaldos

`deploy/monitoring/Test-GymBackupHealth.ps1` revisa tareas Full/Log/Offsite, espacio en
`D:\GymBackups`, OneDrive, antigüedad de los `.7z` y la copia de la clave MFA. Si algo falla,
envía un correo al owner con el detalle.

- `GymService-BackupMonitor`: diario 08:00.
- `GymService-RestoreVerify`: domingos 03:00; descifra el último `.bak.7z` y prueba una
  restauración real en una base temporal.
- Log: `C:\ProgramData\GymService\logs\backup-monitor.log`.
- El respaldo completo tiene `WakeToRun` para despertar el equipo a las 02:00.
- Detalle y comandos: [`deploy/monitoring/README.md`](deploy/monitoring/README.md).

### Reportes programados

En **Reportes → Programar reporte** se elige tipo (ventas, miembros, asistencia, inventario,
caja, membresías o kardex), frecuencia (diario/semanal/mensual) y destinatarios separados por
coma. El servicio revisa cada `Reports:CheckIntervalMinutes` (5 min por defecto), ejecuta los
vencidos a la hora `Reports:ScheduledHour` (8 por defecto) y envía un Excel adjunto por SMTP.
La tabla muestra última y próxima ejecución.

### Impresión de rutinas

En **Rutinas → Asignaciones → Imprimir** se abre una ficha lista para imprimir o guardar
como PDF: datos del socio, rutina, dificultad, duración, descripción y ejercicios agrupados
por día con series, reps, equipo y notas. No requiere conexión adicional ni cambios de base.

### Ficha completa del socio

En **Miembros → Ficha** se abre una hoja lista para imprimir o guardar como PDF con:

- Nombre, estado, email y teléfono.
- CURP, RFC y dirección.
- Contacto y teléfono de emergencia.
- Membresía actual: plan, estado, inicio, vencimiento y crédito.
- Notas del socio.

Los campos se capturan en el alta/edición del socio. La ficha omite la membresía si el socio
no tiene una asignada.

### Foto del socio

En **Miembros → Foto** se sube una imagen JPG, PNG o WebP de hasta 5 MB. Se guarda en
`wwwroot/uploads/members` con un nombre GUID y se muestra en la tabla, en el modal y en la
ficha imprimible. Los archivos son estáticos dentro de la LAN; no se indexan ni se listan.

### Impresión de facturas

En **Facturación → Imprimir** se abre un comprobante interno A4 con número, fecha, socio,
estado, subtotal, IVA y total. El botón **Imprimir / Guardar PDF** usa el diálogo del navegador.
El documento indica que **no sustituye un CFDI**; la facturación fiscal sigue diferida.

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
- ✅ Servicio Windows con GymCoreServiceTools (`AssemblyName` correcto para `-install`)
- ✅ Conexión a SQL Server con AnxoDatabase
- ✅ Servidor Kestrel con ASP.NET Core
- ✅ 41 tablas SQL Server definidas, con índices UNIQUE y foreign keys aplicadas
- ✅ 18 controllers REST con ~67 endpoints operativos
- ✅ Módulo Identity completo (login, MFA, OTP, RBAC)
- ✅ AuthController con endpoints REST
- ✅ Seguridad: PBKDF2-SHA256, MFA cifrado AES-256, SQL parametrizado, endpoints de personal autenticados
- ✅ Frontend React 19 + Vite + Tailwind CSS 4 con 20 páginas
- ✅ Importación masiva de socios e inventario desde el panel (CSV/Excel, UTF-8 o Latin-1), con validación de correos duplicados
- ✅ Exportación a Excel, respaldos verificados, tareas programadas, correos SMTP y crédito proporcional al cancelar
- ✅ 130 pruebas automatizadas (unitarias + integración)
- ✅ Instalador MSI con WiX 5 (build y validación; la instalación per-machine requiere administrador)

## Migración desde PostgreSQL

El gimnasio actual usa PostgreSQL. Para migrar a SQL Server:

1. Exportar datos desde PostgreSQL con `pg_dump`
2. Transformar el schema a SQL Server
3. Importar datos con scripts de migración
4. Validar integridad de datos

## Licencia

Propietario - Centro Fit
