# GymService - Contexto Completo del Proyecto

> **Última actualización:** 20 de noviembre de 2026
> **Estado:** .NET 10 (`net10.0-windows7.0`), 18 controllers, 15 módulos de negocio, 40 tablas gestionadas por el modelo de objetos de Anxo y 113 pruebas automatizadas. Frontend React 19 + Vite 8 + Tailwind CSS 4 con 20 páginas.
> **Framework:** .NET 10 Windows + Anxo Libraries (framework propietario)
>
> **Documentos vigentes:** `README.md` (estado y operación), `CHANGELOG.md` (últimos cambios),
> `docs/reglas-negocio.md` (reglas) y `Models/_ModelGym.xml` (esquema). Este archivo conserva
> el contexto y los patrones de la API de Anxo; si hay conflicto, mandan los documentos anteriores.

---

## INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs

**ANTES de escribir cualquier código en GymService, LEA estos documentos:**
1. `OBSTACLES_AND_API.md` — Documenta ~458 errores de API inventada y la API real del framework.
2. `PROJECT_CONTEXT.md` (este archivo) — Contexto completo del proyecto, arquitectura y patrones.

**Reglas obligatorias:**
- Siempre usar `DatabaseTools.ExecuteQueryResultSet()` para SELECT (SQL parametrizado con `?`).
- Escrituras con el modelo de objetos: `Session.InsertObject` / `InsertObjectWithID` /
  `UpdateObjectChanges` / `DeleteObject` y `TryExecute()`. `ExecuteTransaction(sql, params)`
  queda para operaciones masivas o DDL en `Services/`.
- Siempre usar `Logger.AddErrorSubModule()` para logging de errores.
- Los controllers usan `[ApiController]` con `SecurityValidation.ValidateAuthentication()` para endpoints protegidos.
- Los módulos reciben `DatabaseTools` en el constructor, NO `OODatabaseManager`.

---

## 1. ¿Qué es GymService?

GymService es un **servicio de Windows** que proporciona una API REST para gestionar un gimnasio ("Centro Fit"). Maneja:

- **Autenticación y autorización** de usuarios con JWT, MFA (TOTP), y RBAC
- **Gestión de socios**: CRUD de miembros, membresías, congelaciones y pagos
- **Control de acceso**: Códigos OTP para entrada al gimnasio
- **Ventas y punto de venta**: Transacciones de caja
- **Inventario**: Productos y stock
- **Clases grupales**: Tipos de clases y horarios
- **Rutinas**: Planes de entrenamiento
- **Fisioterapia**: Paquetes y sesiones
- **Mantenimiento**: Registro de equipos
- **Facturación**: Generación de facturas
- **Finanzas**: Cuentas bancarias y transacciones
- **Reportes**: Reportes programados
- **Kiosco**: Dispositivos de autoservicio
- **Personal**: Gestión de staff y notificaciones push

### Arquitectura

```
GymService (Windows Service)
├── Program.cs              → Entry point, Logger y ServiceConsole
├── ServiceImplementation.cs → Lifecycle del servicio (Start/Stop)
├── DataCenter.cs           → Estado global: BD, configuración y módulos
│
├── Controllers/            → API REST (18 controllers)
│   ├── Auth, Members, Access y Kiosk
│   ├── Sales, Cash, Inventory y Procurement
│   ├── Classes, Routines, Fisio y Maintenance
│   ├── Billing, Finance y Reports
│   ├── Staff y Dashboard
│   └── Import
│
├── Modules/                → Lógica de negocio (15 módulos)
│   ├── Identity, Members y Access
│   ├── Sales, Cash e Inventory
│   ├── Classes, Routines y Fisio
│   ├── Maintenance, Billing y Finance
│   ├── Reports, Kiosk y Staff
│
├── Services/
│   ├── DatabaseService.cs     → Esquema aditivo desde MappingObject
│   ├── WebServerService.cs    → Kestrel, CORS, rate limiting, panel y Swagger
│   ├── RolePermissionMiddleware.cs → RBAC por ruta y método
│   ├── CleanupHostedService.cs → Limpieza periódica de sesiones/OTP/bitácora
│   ├── MaintenanceService.cs   → Retención y membresías vencidas
│   ├── EmailService.cs         → SMTP con MailKit, plantillas y outbox de desarrollo
│   ├── BulkImportService.cs    → Importación CSV/Excel (CsvHelper/ClosedXML)
│   ├── ReportExportService.cs  → Exportación a Excel (ClosedXML)
│   └── GymHealthCheck.cs       → Health checks
│
├── Models/
│   ├── _ModelGym.xml          → Esquema declarativo (fuente de verdad)
│   └── Generated/             → Entidades GYM_*.cs y MappingObjects
│
├── Tools/
│   ├── SecurityValidation.cs  → Sesiones, hashing y utilidades de seguridad
│   ├── CurrentUser.cs         → Usuario de auditoría (AsyncLocal)
│   └── ObjectToInferredTypesConverter.cs → JSON a primitivos CLR
│
└── appsettings.json        → Configuración (ignorado por git)
```

---

## 2. Framework Anxo - Bibliotecas Usadas

El proyecto depende de **Anxo Libraries**, un framework propietario de .NET ubicado en:
`C:\repositorios\pilot\ANXO LIBRARIES\`

### Bibliotecas del framework:

| Biblioteca | Propósito |
|---|---|
| **AnxoServiceTools** | Infraestructura de Windows Service (ServiceConsole, ServiceDefinition) |
| **AnxoLibrary** | Utilidades generales (Logger, ProcessResult) |
| **AnxoObjectDatabaseManager** | Acceso a BD orientado a objetos (OODatabaseManager, DatabaseTools, Session) |
| **AnxoDatabase** | Tipos de datos de BD (ResultSet, Row, DataType, Query) |
| **AnxoDatabaseGenerator** | Generador de código de BD |
| **AnxoDesktopEngineApp** | Motor de aplicaciones de escritorio |
| **AnxoDesktopEngineCommon** | Comunes de escritorio |
| **AnxoGrammar** | Gramática/parseo |
| **AnxoNLSGenerator** | Generador de localización |

### Referencias del proyecto (GymService.csproj):
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\repositorios\pilot\ANXO LIBRARIES\AnxoServiceTools\AnxoServiceTools.csproj" />
  <ProjectReference Include="..\..\..\repositorios\pilot\ANXO LIBRARIES\AnxoLibrary\AnxoLibrary.csproj" />
  <ProjectReference Include="..\..\..\repositorios\pilot\ANXO LIBRARIES\AnxoObjectDatabaseManager\AnxoObjectDatabaseManager.csproj" />
  <ProjectReference Include="..\..\..\repositorios\pilot\ANXO LIBRARIES\AnxoDatabase\AnxoDatabase.csproj" />
</ItemGroup>
```

---

## 3. API REAL del Framework Anxo

### ⚠️ ADVERTENCIA CRÍTICA
**El código original del proyecto usaba una API inventada que NO existe en el framework.**
**~458 errores de compilación se corrigieron usando la API real documentada aquí.**

### 3.1. Conexión a Base de Datos

```csharp
// Crear datos de conexión
var connData = new ConnectionData
{
    Server = "localhost",
    Database = "GymManager",
    User = "sa",
    Password = "",
    Port = 1433,
    DatabaseType = DatabaseTypes.SQLServer,
    PoolSize = 10
};

// Crear y conectar el manager
var dbManager = new OODatabaseManager();
dbManager.Connect(connData);

// Acceder a DatabaseTools (la API principal)
var tools = dbManager.Tools;
```

### 3.2. Consultas SELECT (DatabaseTools)

```csharp
// Sin parámetros
var result = tools.ExecuteQueryResultSet("SELECT * FROM users");

// Con parámetros (usando ? como placeholder)
var result = tools.ExecuteQueryResultSet(
    "SELECT * FROM users WHERE status = ? AND role = ?",
    "active", "admin"
);

// Leer los resultados
if (result != null && result.Rows.Count > 0)
{
    foreach (Row row in result.Rows)
    {
        // Row NO tiene indexador [] - usa GetObject(index)
        var id = row.GetInt64(0);
        var name = row.GetString(1);
        
        // O usa un helper para acceder por nombre de columna:
        // var value = GetRowValue(row, result, "column_name");
    }
}
```

### 3.3. INSERT / UPDATE / DELETE (modelo de objetos)

```csharp
// Crear una sesión de transacción con el usuario de auditoría
var session = tools.CreateSession("MODULE_NAME", CurrentUser.ForAudit);

// Insertar una entidad generada (la PK la asigna Anxo con secuencias)
session.InsertObject(new GYM_MEMBERS
{
    FIRST_NAME = "John",
    LAST_NAME = "Doe",
    EMAIL = "john@example.com",
    STATUS = "active"
});

// Actualizar solo los cambios de una entidad existente
session.UpdateObjectChanges(member);   // member debe tener ID cargado
session.DeleteObject(member);

// Ejecutar todas las operaciones de la sesión
var processResult = session.TryExecute();

if (processResult.IsError)
{
    // Manejar error (processResult.Message)
}
```

`InsertObjectWithID` se usa cuando la PK viene de otra operación de la misma sesión
(cabecera + detalle). `Session.ExecuteTransaction(sql, params)` sigue disponible para
operaciones masivas o DDL en `Services/` (p. ej. limpieza por fechas).

### 3.4. Lectura de Row (sin indexador [])

```csharp
// Row SOLO tiene métodos tipados por índice:
row.GetObject(int index);
row.GetString(int index);
row.GetInt64(int index);
row.GetInt32(int index);
row.GetBoolean(int index);
row.GetDateTime(int index);
row.GetDouble(int index);
row.GetDecimal(int index);

// Helper recomendado para acceder por nombre de columna:
private int GetColumnIndex(ResultSet result, string columnName)
{
    for (int i = 0; i < result.Columns.Count; i++)
    {
        if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            return i;
    }
    return -1;
}

private object GetRowValue(Row row, ResultSet result, string columnName)
{
    int idx = GetColumnIndex(result, columnName);
    if (idx < 0) return null;
    return row.GetObject(idx);
}
```

### 3.5. Logger

```csharp
// Configuración (6 parámetros obligatorios)
Logger.ConfigLogger(
    "GymService",           // name
    LogLevel.Debug,         // level
    true,                   // localtime
    true,                   // logToConsole
    true,                   // logToFile
    "logs"                  // logDirectory
);

// Métodos de logging disponibles:
Logger.AddInfo("message");
Logger.AddWarning("message");
Logger.AddError("message");
Logger.AddDebug("message");

// Con sub-módulo (recomendado para módulos de negocio):
Logger.AddInfoSubModule("MODULE_NAME", "message");
Logger.AddWarningSubModule("MODULE_NAME", "message");
Logger.AddErrorSubModule("MODULE_NAME", "message");
Logger.AddErrorDevExceptionSubModule("MODULE_NAME", "message");

// ⚠️ NO EXISTE: Logger.AddErrorDevException(module, msg, exception) con 3 args
// ⚠️ NO EXISTE: Logger.AddWarn() (se llama AddWarning)
```

### 3.6. ServiceDefinition

```csharp
var serviceDef = new ServiceDefinition
{
    ServiceName = "anxogymmservice",
    ServiceDisplayName = "Anxo Gym Manager Service",
    StartType = "auto",           // String, NO enum
    ServiceAccount = "localsystem" // String, NO enum
};

// ⚠️ StartType y ServiceAccount son STRINGS, no enums
```

### 3.7. ServiceConsole.Execute()

```csharp
// En Program.cs:
ServiceConsole.Execute(
    args,                    // string[] arguments
    DataCenter.SERVICE_DATA, // ServiceDefinition
    serviceImplementation    // IServiceConsole
);
```

### 3.8. DataType Enum

```csharp
// Valores válidos de DataType:
DataType.Int16    // 1
DataType.Int32    // 2
DataType.Int64    // 3
DataType.String   // 4
DataType.Boolean  // 5
DataType.DateTime // 6
DataType.Double   // 7
DataType.Float    // 8
DataType.Decimal  // 9
// ... hasta DateTime2 (18)

// ⚠️ NO EXISTE: DataType.BigInt → usa DataType.Int64
// ⚠️ NO EXISTE: DataType.NVarChar → usa DataType.String
// ⚠️ NO EXISTE: DataType.Bit → usa DataType.Boolean
// ⚠️ NO EXISTE: DataType.Int → usa DataType.Int32
```

### 3.9. ProcessResult

```csharp
var result = new ProcessResult();
result.Code = ProcessResult.GenericError;  // Para errores
result.ErrorMessage("mensaje de error");

// Verificar errores:
if (result.IsError) { /* manejar error */ }
```

---

## 4. Estado Actual del Proyecto

### ✅ Completado
- **Compilación:** .NET 10, 0 errores. Los warnings restantes son NU1902/NU1510 de dependencias heredadas de Anxo.
- **Esquema:** 40 tablas, índices UNIQUE y 26 FK desde `Models/_ModelGym.xml`; creación y migración aditiva al arrancar.
- **Módulos (15):** Identity, Members, Access, Sales, Cash, Inventory (productos, proveedores, órdenes y kardex), Classes (sesiones, reservas y lista de espera), Routines, Fisio, Maintenance, Billing, Finance (cuentas, movimientos y conciliación), Reports, Kiosk y Staff (turnos y asistencia).
- **Frontend:** 20 páginas React 19 + Vite 8 + Tailwind 4, `CrudPage`, paneles por módulo, recibos en el panel, PWA de kiosco, importación CSV/Excel y exportación de reportes.
- **Seguridad:** PBKDF2-SHA256, MFA AES-256, sesiones Bearer opacas, RBAC por ruta, rate limiting, bloqueo por intentos y OTP real por MailKit.
- **Operación:** servicio Windows (`-install`), `publish.ps1`, instalador PowerShell, health checks y CI de release por tag.
- **Pruebas:** 113 automatizadas (unitarias + integración contra BD temporal); la prueba en vivo es obligatoria (`docs/pruebas-en-vivo.md`).
- **Documentación:** `README.md`, `CHANGELOG.md`, `docs/reglas-negocio.md`, `docs/pruebas-en-vivo.md` y `OBSTACLES_AND_API.md`.

### 🚧 Pendientes conocidos
- Pasarela de pago, tokenización y renovación automática.
- Bloqueo por adeudo, horarios por plan, aforo y antipassback.
- Instalador WiX (hoy se usa el instalador PowerShell).
- Vulnerabilidad moderada de `MySqlConnector` heredada de Anxo (MySQL no se usa en este despliegue).

---

## 5. Patrones de Código Obligatorios

### 5.1. Patrón de Módulo de Negocio

Todos los módulos de negocio DEBEN seguir este patrón:

```csharp
using System;
using System.Collections.Generic;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.ModuleName
{
    public class ModuleNameModule
    {
        private const string SubModule = "MODULE_NAME";
        private DatabaseTools _tools;

        public ModuleNameModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        // Helper para leer columnas por nombre
        private int GetColumnIndex(ResultSet result, string columnName) { ... }
        private object GetRowValue(Row row, ResultSet result, string columnName) { ... }

        // Método de ejemplo - SELECT
        public bool GetData(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();
            try
            {
                var result = _tools.ExecuteQueryResultSet("SELECT * FROM table WHERE status = ?", "active");
                var data = new List<Dictionary<string, object>>();
                
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        data.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "name")) ?? ""
                        });
                    }
                }
                
                response["success"] = true;
                response["data"] = data;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // Método de ejemplo - INSERT/UPDATE/DELETE
        public bool CreateItem(Dictionary<string, object> itemData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();
            try
            {
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "INSERT INTO table (name, status) VALUES (?, ?)",
                    itemData["name"], "active"
                );
                
                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear");
                    response["success"] = false;
                    response["message"] = "Error al crear";
                    return false;
                }
                
                response["success"] = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
```

### 5.2. Reglas Obligatorias

1. **DatabaseTools** es la API principal para acceso a BD. NO uses `OODatabaseManager` directamente en módulos.
2. **ExecuteQueryResultSet()** para SELECT. NO existe `ExecuteQuery()`.
3. **Escrituras con el modelo de objetos:** `Session.InsertObject` / `InsertObjectWithID` /
   `UpdateObjectChanges` / `DeleteObject` + `TryExecute()`. `CreateSession()` +
   `ExecuteTransaction()` queda para operaciones masivas o DDL en `Services/`.
4. **Row.GetObject(index)** para leer valores. NO existe `row["column"]`.
5. **Logger.AddErrorSubModule()** para logging. NO existe `Logger.AddErrorDevException()` con 3 args.
6. **Session NO implementa IDisposable**. NO uses `using` con Session.
7. **ServiceDefinition** usa strings para StartType y ServiceAccount. NO uses enums.
8. **DataType** usa Int64, String, Boolean, Int32. NO usa BigInt, NVarChar, Bit, Int.
9. **Logger.ConfigLogger()** requiere 6 parámetros.
10. **ServiceConsole.Execute()** requiere 3 parámetros.

---

## 6. Esquema de Base de Datos

El esquema se declara en `Models/_ModelGym.xml` y se genera en `Models/Generated/`
(`GYM_*.cs` y `ModelGym.MappingObjects`). No edites esos archivos a mano: usa
`Tools/AnxoModelGenerator` (ver `README.md`). Son 40 tablas; por grupo:

| Grupo | Tablas |
|---|---|
| Identidad | `GYM_USERS`, `GYM_PERMISSIONS`, `GYM_ROLE_PERMISSIONS`, `GYM_SESSIONS`, `GYM_OTP_CODES` |
| Socios | `GYM_MEMBERS`, `GYM_MEMBERSHIP_PLANS`, `GYM_MEMBERSHIPS`, `GYM_PAYMENTS` |
| Acceso | `GYM_ACCESS_CODES`, `GYM_ACCESS_LOG`, `GYM_KIOSK_DEVICES` |
| Ventas e inventario | `GYM_SALES`, `GYM_SALE_ITEMS`, `GYM_CASH_SESSIONS`, `GYM_INVENTORY`, `GYM_INVENTORY_MOVEMENTS`, `GYM_SUPPLIERS`, `GYM_PURCHASE_ORDERS`, `GYM_PURCHASE_ORDER_ITEMS` |
| Clases y rutinas | `GYM_CLASS_TYPES`, `GYM_CLASS_SESSIONS`, `GYM_CLASS_RESERVATIONS`, `GYM_ROUTINE_PLANS`, `GYM_ROUTINE_PLAN_EXERCISES`, `GYM_EXERCISES`, `GYM_ROUTINE_ASSIGNMENTS`, `GYM_WORKOUTS`, `GYM_MEASUREMENTS` |
| Fisio y mantenimiento | `GYM_FISIO_PACKAGES`, `GYM_FISIO_APPOINTMENTS`, `GYM_EQUIPMENT`, `GYM_MAINTENANCE_TICKETS` |
| Finanzas y facturación | `GYM_INVOICES`, `GYM_BANK_ACCOUNTS`, `GYM_BANK_TRANSACTIONS` |
| Personal y reportes | `GYM_STAFF`, `GYM_STAFF_SHIFTS`, `GYM_PUSH_SUBSCRIPTIONS`, `GYM_SCHEDULED_REPORTS` |

---

## 7. Cómo Continuar el Trabajo

### Patrón de controller (usar siempre):

Todos los controllers siguen este patrón. Ejemplo:

```csharp
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class MyController : ControllerBase
    {
        // Endpoint público (sin auth):
        [HttpGet]
        [Route("api/myendpoint")]
        public ActionResult<Dictionary<string, object>> GetPublic()
        {
            if (DataCenter.BusinessProcess.MyModule == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.MyModule.MyMethod(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // Endpoint protegido (con auth JWT):
        [HttpPost]
        [Route("api/myendpoint")]
        public ActionResult<Dictionary<string, object>> DoSomething([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.MyModule == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.MyModule.MyMethod(data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
```

### Para agregar un controller nuevo:

1. Crea el archivo en `Controllers/ModuleNameController.cs`
2. Usa `[ApiController]` heredando de `ControllerBase`
3. Accede a los módulos vía `DataCenter.BusinessProcess.ModuleName`
4. Usa `SecurityValidation.ValidateAuthentication()` para proteger endpoints
5. El WebServerService ya tiene `MapControllers()` configurado — no necesitas registrar el controller manualmente

### Para agregar métodos a un módulo existente:

1. Abre el archivo del módulo en `Modules/ModuleName/ModuleNameModule.cs`
2. Usa `ExecuteQueryResultSet()` para SELECT
3. Usa el modelo de objetos (`InsertObject`, `InsertObjectWithID`, `UpdateObjectChanges`,
   `DeleteObject`) y `TryExecute()` para INSERT/UPDATE/DELETE. `ExecuteTransaction` queda
   para operaciones masivas o DDL.
4. Usa `Logger.AddErrorSubModule()` para logging
5. Agrega el endpoint correspondiente en el controller
6. Compila con `dotnet build` para verificar

### Para verificar la compilación:

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService
dotnet build
```

### Para decompilar las DLLs del framework:

```powershell
# Instalar ILSpy command-line tool
dotnet tool install --global ilspycmd

# Decompilar una clase específica
ilspycmd --type ClassName "C:\repositorios\pilot\ANXO LIBRARIES\LibraryName\bin\Debug\net8.0\LibraryName.dll"
```

---

## 8. Obstáculos Conocidos

Ver `OBSTACLES_AND_API.md` para la lista completa de obstáculos técnicos.

### Obstáculos principales:
1. **~458 errores de API inventada** - Resueltos reescribiendo con la API real
2. **DatabaseService** - ✓ Resuelto usando `OODatabaseManager.DatabaseAdministrator` con CreateTable/ExistTable/DropTable
3. **Logger API diferente** - Los métodos tienen nombres y firmas diferentes a lo esperado
4. **Row sin indexador** - Requiere helper para acceder por nombre de columna
5. **Session sin IDisposable** - No se puede usar `using`
6. **DataType enums diferentes** - BigInt→Int64, NVarChar→String, Bit→Boolean, Int→Int32
7. **ServiceDefinition con strings** - StartType y ServiceAccount son strings, no enums

---

## 9. Referencias Útiles

- **Código de referencia del framework:** `C:\repositorios\pilot\TMS\CombuExpressBusinessProcess\Modules\`
- **AuthorizationModule de referencia:** `CombuExpressBusinessProcess\Modules\Auth\AuthorizationModule.cs`
- **FuelRequestsModule de referencia:** `CombuExpressBusinessProcess\Modules\Fuel\FuelRequestsModule.cs`
- **DatabaseTools API:** `C:\repositorios\pilot\ANXO LIBRARIES\AnxoObjectDatabaseManager\Tools\DatabaseTools.cs`
- **Logger API:** `C:\repositorios\pilot\ANXO LIBRARIES\AnxoLibrary\Logger\Logger.cs`

---

## 10. API REST - Endpoints

La fuente viva es Swagger (`/swagger`) y los controllers. Grupos principales:

| Grupo | Rutas |
|---|---|
| Autenticación | `/api/auth/*` (login, logout, MFA, OTP, bootstrap) |
| Socios y planes | `/api/members`, `/api/members/plans`, `/api/members/{id}/membership`, `/api/members/{id}/payments` |
| Acceso y kiosco | `/api/access/*`, `/api/kiosk/*`, `/api/dashboard/summary` |
| Ventas y caja | `/api/sales`, `/api/cash/*` |
| Inventario y compras | `/api/inventory/*`, `/api/procurement/*` |
| Clases, rutinas y fisio | `/api/classes/*`, `/api/routines/*`, `/api/fisio/*` |
| Mantenimiento y personal | `/api/maintenance/*`, `/api/staff/*` |
| Facturación y finanzas | `/api/billing/*`, `/api/finance/*` |
| Reportes e importación | `/api/reports/*`, `/api/import/*` |

Los permisos por ruta y método están en `Services/RolePermissionMiddleware.cs`.

---
## 11. Historial de Sesiones

### Sesión 1: Corrección de ~458 errores
- Se identificó que el código usaba una API inventada
- Se documentó la API real del framework Anxo
- Se corrigió IdentityModule, ServiceImplementation, DataCenter, Program.cs, SecurityValidation, ModelGym
- Se creó OBSTACLES_AND_API.md

### Sesión 2: Compilación exitosa
- Se lograron 0 errores de compilación
- Los 13 módulos de negocio se convirtieron a stubs vacíos para compilar
- Se verificó que el proyecto compila correctamente

### Sesión 3: Implementación de módulos
- Se unificó el patrón: todos los módulos usan `DatabaseTools` (no `OODatabaseManager`)
- Se implementó MembersModule completo (5 métodos)
- Se implementó AccessModule completo (2 métodos)
- Se implementaron 11 módulos restantes: Sales, Billing, Classes, Finance, Fisio, Inventory, Kiosk, Maintenance, Reports, Routines, Staff
- Se creó PROJECT_CONTEXT.md para documentar el contexto completo
- El proyecto compila con 0 errores y 26 warnings de nullable references

### Sesión 4: Documentación comprehensiva
- Se actualizó PROJECT_CONTEXT.md con el estado final del proyecto
- Se documentaron todos los módulos implementados
- Se identificaron los siguientes pasos: controllers REST, DatabaseService, base de datos, pruebas, despliegue

### Sesión 5: Controllers REST completos
- Se crearon 13 controllers REST: Members, Access, Sales, Inventory, Classes, Routines, Fisio, Maintenance, Billing, Finance, Reports, Kiosk, Staff
- Se corrigió SalesController para usar la firma real de SalesModule.CreateSale(Dictionary)
- Todos los controllers usan `[ApiController]` con `SecurityValidation.ValidateAuthentication()` para endpoints protegidos
- WebServerService ya tiene `MapControllers()` configurado — los controllers se registran automáticamente
- El proyecto compila con 0 errores y 27 warnings de nullable references
- Se actualizó PROJECT_CONTEXT.md con tabla de endpoints, patrón de controller, e instrucciones para futuras IAs

### Sesión 7: CRUD completo para todos los módulos
- Se identificaron 8 controllers con solo métodos GET: Billing, Classes, Finance, Fisio, Inventory, Maintenance, Reports, Routines, Staff
- Se implementaron métodos POST (crear), PUT (actualizar) y DELETE (eliminar) para cada módulo
- Patrón correcto de transacciones: `session.ExecuteTransaction(sql, params)` seguido de `session.TryExecute().IsError`
- Error encontrado: `TryExecute()` no acepta parámetros SQL; usa `ExecuteTransaction()` para agregar operaciones y `TryExecute()` para ejecutar
- Los 14 controllers ahora tienen CRUD completo con ~45 endpoints totales
- El proyecto compila con 0 errores y 27 warnings de nullable references
- Se actualizó PROJECT_CONTEXT.md con el estado actual

### Sesión 8: CRUD completo para Access, Sales y Kiosk
- Se identificaron 3 controllers con CRUD incompleto: Access (solo POST), Sales (solo POST), Kiosk (sin GET devices)
- **AccessModule:** Se agregaron `GetAccessLogs()` con filtro por rango de fechas y `CleanOldAccessCodes()` para eliminar registros antiguos
- **SalesModule:** Se agregaron `GetSales()` con filtro por rango de fechas y `DeleteSale()` para eliminar ventas con sus items
- **KioskModule:** Se agregaron `GetDevices()` para listar dispositivos registrados y `DeactivateDevice()` para desactivar dispositivos
- **AccessController:** Se agregaron `GET /api/access/logs` y `DELETE /api/access/clean/{daysOld}`
- **SalesController:** Se agregaron `GET /api/sales` y `DELETE /api/sales/{saleId}`
- **KioskController:** Se agregaron `GET /api/kiosk/devices` y `PUT /api/kiosk/devices/{deviceId}/deactivate`
- El proyecto compila con 0 errores y 27 warnings de nullable references
- Total de endpoints: ~55 endpoints en 14 controllers

### API de transacciones (hallazgo clave)
- `Session.ExecuteTransaction(string command, params Object[] binding_variables)` — agrega una operación a la sesión
- `Session.TryExecute()` — ejecuta todas las operaciones y retorna `ProcessResult`
- `ProcessResult.IsError` — verifica si hubo error en la ejecución
- NO usar `TryExecute(sql, params)` — no existe esa sobrecarga
- Patrón correcto:
```csharp
var session = _tools.CreateSession();
session.ExecuteTransaction("INSERT INTO table (col) VALUES (?)", value);
if (!session.TryExecute().IsError)
{
    // éxito
}
```

### Sesión 6: DatabaseService implementado
- Se investigó la API del framework Anxo para encontrar los métodos de creación de tablas
- Se descubrió `OODatabaseManager.DatabaseAdministrator` que expone `CreateTable()`, `ExistTable()`, `DropTable()`, `GetTableStructure()`
- Se implementó DatabaseService completo que convierte `TableDefinition` de ModelGym a `DatabaseTable` del framework
- Soporta: columnas con tipos, primary keys, identity, unique, default values, composite PKs, foreign keys
- Las 32 tablas de ModelGym se crean automáticamente al iniciar el servicio
- Las 25 foreign keys se agregan después de crear las tablas
- Se agregó método `DropAllTables()` para desarrollo/testing
- El proyecto compila con 0 errores y 27 warnings de nullable references

### API de DatabaseAdministrator (hallazgo clave)
- `OODatabaseManager.DatabaseAdministrator` retorna un `DatabaseAdministrator` listo para usar
- `DatabaseAdministrator.CreateTable(DatabaseTable table)` — crea una tabla
- `DatabaseAdministrator.ExistTable(string tableName)` — verifica si existe
- `DatabaseAdministrator.DropTable(string tableName)` — elimina una tabla
- `DatabaseAdministrator.GetTableStructure(string tableName)` — obtiene la estructura actual
- `DatabaseTable` contiene `Columns`, `Indexes`, `ForeignKeys`
- `DatabaseColumn` usa `ColumnType` enum (NotNull, Null, PrimaryKey)
- `DatabaseColumn.Properties` acepta `IDENTITY=1;UNIQUE=1` para propiedades especiales
- `DatabaseIndex` requiere `(name, IndexTypes)` en el constructor
- `IndexTypes` solo tiene `Unique` y `NonUnique` (no tiene PrimaryKey)
- `DatabaseForeignKey` se construye con el nombre de la tabla externa y una lista de `DatabaseForeignKeyColumn`

### Sesión 9: Swagger/OpenAPI con JWT Bearer auth
- Se descubrió que Swagger ya estaba configurado en `WebServerService.cs` con Swashbuckle.AspNetCore 6.5.0
- Se mejoró la configuración de Swagger agregando:
  - Metadata completa: título, versión, descripción, contacto y licencia
  - Autenticación JWT Bearer: botón "Authorize" en Swagger UI para ingresar token JWT
  - `AddSecurityDefinition("Bearer")` con esquema HTTP Bearer
  - `AddSecurityRequirement()` para aplicar autenticación a todos los endpoints
- Swagger UI disponible en `http://localhost:{port}/swagger` al iniciar el servicio
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 10: Paginación y filtros en endpoints GET
- Se implementó paginación con SQL Server `OFFSET/FETCH NEXT` en 7 módulos principales
- **Patrón de paginación:** parámetros `page` (default 1), `limit` (default 50, max 500), con metadata de paginación en la respuesta
- **Metadata de respuesta:** `pagination` object con `page`, `limit`, `totalCount`, `totalPages`, `hasNext`, `hasPrev`
- **Módulos actualizados:**
  - **MembersModule:** `GetMembers()` con búsqueda por nombre/email y paginación
  - **SalesModule:** `GetSales()` con filtro por fechas y paginación
  - **BillingModule:** `GetInvoices()` con filtro por memberId y paginación
  - **InventoryModule:** `GetProducts()` con búsqueda por nombre/categoría y paginación
  - **StaffModule:** `GetStaff()` con búsqueda por nombre/email y paginación; `GetPushSubscriptions()` con paginación
  - **AccessModule:** `GetAccessLogs()` con filtro por fechas y paginación
- **Controllers actualizados:** Members, Sales, Billing, Inventory, Staff, Access con parámetros `[FromQuery] page` y `limit`
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 11: Health checks para monitoreo del servicio
- Se implementaron health checks usando `Microsoft.Extensions.Diagnostics.HealthChecks` (incluido en ASP.NET Core, sin paquete adicional)
- **GymHealthCheck.cs:** Clase personalizada que verifica 4 componentes:
  - **Base de datos:** Verifica DatabaseManager, ConnectionData, y conectividad con query a `gym_members`
  - **Módulos de negocio:** Verifica los 14 módulos (Identity, Members, Access, Sales, Inventory, Classes, Routines, Fisio, Maintenance, Billing, Finance, Reports, Kiosk, Staff)
  - **Recursos del sistema:** Memoria RAM, núcleos CPU, uptime del proceso, PID
  - **Servidor web:** Confirma que Kestrel está corriendo
- **GymReadinessCheck:** Verificación ligera para load balancers — solo verifica que DatabaseManager y WebServer están inicializados
- **3 endpoints configurados en WebServerService.cs:**
  - `GET /health` — Health check completo con detalle de todos los componentes
  - `GET /ready` — Readiness check ligero (solo gym-readiness)
  - `GET /health/live` — Liveness probe (siempre healthy si el proceso corre)
- **WriteHealthCheckResponse:** Método personalizado que retorna JSON detallado con status, timestamp, duration, components y data de cada componente
- **Códigos HTTP:** 200 OK para healthy/degraded, 503 Service Unavailable para unhealthy
- **Hallazgo:** `GCMemoryInfo.WorkingSet64` no existe — usar `Process.GetCurrentProcess().WorkingSet64` directamente
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 12: Validación de roles y permisos por endpoint
- Se descubrió que el sistema ya tenía RBAC completo en IdentityModule con 27 permisos definidos y roles (OWNER, ADMIN, STAFF, CASHIER)
- **RolePermissionMiddleware.cs:** Middleware que intercepta cada request y verifica el permiso requerido
- **PermissionMatrix:** Matriz estática que mapea ~55 rutas API a permisos específicos (members.view, sales.create, etc.)
- **4 roles definidos:**
  - **OWNER:** Acceso total (`*` wildcard)
  - **ADMIN:** Todos los permisos (27 permisos)
  - **STAFF:** 17 permisos (members, access, classes, routines, fisio, maintenance, reports)
  - **CASHIER:** 7 permisos (members.view, sales, inventory.view, billing.view)
- **Flujo del middleware:**
  1. Skip health checks, Swagger, y /api/auth
  2. Determinar permiso requerido desde PermissionMatrix por ruta + método HTTP
  3. Validar token Bearer con IdentityModule.ValidateSessionToken()
  4. Verificar permiso con SecurityValidation.HasPermission()
  5. Retornar 401 Unauthorized si no hay token, 403 Forbidden si no tiene permiso
- **Endpoints excluidos:** /health, /ready, /swagger, /api/auth (no requieren permisos)
- **Logging:** Los accesos denegados se registran con Logger.AddWarningSubModule para auditoría
- **Integración:** Agregado en WebServerService.cs antes de MapControllers()
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 13: Windows Service deployment
- Se descubrió que AnxoServiceTools ya soporta instalación de Windows Service con comandos integrados
- **ServiceConsole.Execute()** soporta: `-install`, `-uninstall`, `-start`, `-stop`, `-console`, `-help`
- **ServiceDefinition** soporta: `ServiceName`, `ServiceDisplayName`, `ServiceAccount` (localsystem, localservice, networkservice), `StartType` (auto, manual, disable, delayed)
- **install-service.ps1:** Script PowerShell que automatiza:
  1. Instalación del servicio vía `-install`
  2. Configuración de startup type (Automatic)
  3. Recovery options: restart after 60s/120s/180s, reset counter 1 day
  4. Descripción del servicio
  5. Inicio automático del servicio
- **uninstall-service.ps1:** Script que detiene y desinstala el servicio con confirmación
- **DEPLOYMENT.md:** Guía completa de deployment con:
  - Prerrequisitos (.NET 8, SQL Server, Windows)
  - Build y configuración
  - Instalación automática y manual
  - Gestión del servicio (comandos, services.msc, PowerShell)
  - Health checks y Swagger UI
  - Troubleshooting
  - Configuración de service account
  - Bootstrap del primer usuario admin
- **Hallazgo:** El framework Anxo ya tiene toda la infraestructura para Windows Service — no se necesita TopShelf ni InstallService manual
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 14: Rate limiting para protección contra abuso
- Se implementó rate limiting nativo de ASP.NET Core 8 con `Microsoft.AspNetCore.RateLimiting`
- **3 políticas configuradas en WebServerService.cs:**
  - **"general":** 100 requests por minuto, cola de 10, ventana de 1 minuto
  - **"auth":** 20 requests por minuto, cola de 5, ventana de 1 minuto (para login, bootstrap, etc.)
  - **"strict":** 5 requests por minuto, sin cola, ventana de 1 minuto (para OTP y endpoints sensibles)
- **OnRejected handler personalizado:** Retorna 429 Too Many Requests con JSON detallado:
  ```json
  { "success": false, "message": "Demasiadas solicitudes...", "retryAfter": "45" }
  ```
- **Header RetryAfter:** Se incluye automáticamente para que el cliente sepa cuándo reintentar
- **Logging:** Los excedidos se registran con Logger.AddWarningSubModule con la IP del cliente
- **Uso:** `app.UseRateLimiter()` en el pipeline antes del middleware de roles
- **Aplicar políticas:** Usar `[EnableRateLimiting("policy-name")]` en controllers o endpoints específicos
- **Hallazgo:** `AddPartitionedRateLimiter` no está disponible en esta configuración — usar `AddFixedWindowLimiter` con políticas nombradas
- El proyecto compila con 0 errores y 27 warnings de nullable references

### Sesión 15: Ingreso masivo de datos (CSV/Excel)
- Se implementó importación masiva de datos usando **CsvHelper 33.0.1** (CSV) y **ClosedXML 0.104.2** (Excel .xlsx)
- **BulkImportService.cs:** Servicio sin estado con 4 métodos de importación y 4 métodos de generación de plantillas:
  - `ImportMembersFromCsv()` — Importa miembros desde CSV con columnas: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
  - `ImportMembersFromExcel()` — Importa miembros desde Excel (.xlsx) con las mismas columnas
  - `ImportInventoryFromCsv()` — Importa inventario desde CSV con columnas: ProductName, Description, SKU, Category, Price, Cost, StockQuantity, MinStock
  - `ImportInventoryFromExcel()` — Importa inventario desde Excel (.xlsx) con las mismas columnas
  - `GenerateMembersCsvTemplate()` / `GenerateMembersExcelTemplate()` — Generan plantillas de ejemplo
  - `GenerateInventoryCsvTemplate()` / `GenerateInventoryExcelTemplate()` — Generan plantillas de ejemplo
- **BulkImportResult:** Clase de resultado con `TotalRows`, `ImportedRows`, `SkippedRows`, `ErrorRows` y lista de `BulkImportError`
- **BulkImportError:** Error por fila con `RowNumber`, `RowData` y `ErrorMessage`
- **ImportController.cs:** 8 endpoints REST nuevos:
  - `POST /api/import/members/csv` — Importar miembros desde CSV (con parámetro opcional `encoding`)
  - `POST /api/import/members/excel` — Importar miembros desde Excel
  - `POST /api/import/inventory/csv` — Importar inventario desde CSV
  - `POST /api/import/inventory/excel` — Importar inventario desde Excel
  - `GET /api/import/templates/members/csv` — Descargar plantilla CSV de miembros
  - `GET /api/import/templates/members/excel` — Descargar plantilla Excel de miembros
  - `GET /api/import/templates/inventory/csv` — Descargar plantilla CSV de inventario
  - `GET /api/import/templates/inventory/excel` — Descargar plantilla Excel de inventario
- **Validación:** Campos obligatorios verificados antes de insertar; errores por fila con mensaje descriptivo
- **Autenticación:** Los endpoints de importación requieren token JWT; los de plantillas son públicos
- **Hallazgo:** `InventoryModule.CreateProduct()` usa la clave `stock` (no `stockQuantity`) — el BulkImportService debe usar `stock`
- **Paquetes agregados:** CsvHelper 33.0.1, ClosedXML 0.104.2 (con dependencias DocumentFormat.OpenXml, SixLabors.Fonts, RBush)
- El proyecto compila con 0 errores y 29 warnings de nullable references

### Sesión 16: Exportación de reportes a Excel
- Se implementó exportación de reportes a Excel usando **ClosedXML 0.104.2** (ya instalado en Sesión 15)
- **ReportExportService.cs:** Servicio con 4 métodos de exportación:
  - `ExportSalesToExcel()` — Exporta ventas con filtro por fechas, incluye fila de totales con fórmulas
  - `ExportMembersToExcel()` — Exporta lista de miembros con membresía activa, opcional filtro por estado
  - `ExportAttendanceToExcel()` — Exporta historial de asistencia con filtro por fechas
  - `ExportInventoryToExcel()` — Exporta inventario con resaltado naranja en stock bajo
- **ReportsController.cs:** 4 nuevos endpoints REST:
  - `GET /api/reports/sales/excel?startDate=&endDate=` — Exportar ventas a Excel
  - `GET /api/reports/members/excel?status=` — Exportar miembros a Excel
  - `GET /api/reports/attendance/excel?startDate=&endDate=` — Exportar asistencia a Excel
  - `GET /api/reports/inventory/excel` — Exportar inventario a Excel
- **Formato Excel:** Encabezados con colores por tipo de reporte (azul=ventas, verde=miembros, naranja=asistencia, púrpura=inventario), columnas auto-ajustadas
- **Hallazgo:** QuestPDF 2024.7.1 tiene API incompatible con la documentación actual — se descartó PDF por ahora
- **Paquete QuestPDF:** Instalado pero no usado (versión 2024.7.1 con API diferente a la documentada). Eliminado del `.csproj` el 18-nov-2026.
- El proyecto compila con 0 errores y 30 warnings de nullable references

### Sesión 17: Frontend con CRUD real
- Se reemplazaron las páginas genéricas de solo lectura por CRUD real en: Billing, Classes, Finance, Fisio, Maintenance, Reports, Routines, Staff, Kiosk y AccessLogs.
- **Componentes nuevos:**
  - `GymServiceFront/src/components/CrudPage.jsx` — CRUD configurable: tabla, formulario modal, validación, paginación, borrado con confirmación, acciones por fila y avisos.
  - `GymServiceFront/src/hooks/useApiList.js` — listado paginado con manejo de errores.
  - `Select.jsx`, `PageHeader.jsx`, `ConfirmDialog.jsx` y `src/utils/format.js`.
- **Contratos corregidos:** varias páginas mostraban columnas vacías porque usaban nombres inventados (`name`, `instructor`, `accountName`, `packageName`, `totalAmount`). Ahora usan los campos reales del backend (`typeName`, `durationMinutes`, `bankName`, `accountNumber`, `name`, `planName`, `amount`, etc.).
- **Backend:** `StaffModule.GetAvailableUsers()` + `GET /api/staff/users` para elegir el usuario al crear personal; `GetStaff()` ahora devuelve `department`; nueva regla en `PermissionMatrix` para `/api/staff/users`.
- **Seguridad:** `appsettings.json` ignorado por git y reemplazado por `appsettings.example.json`; contraseña eliminada del `CHANGELOG.md`.
- **PDF:** se eliminó el paquete `QuestPDF` (sin uso). Se reevaluará al implementar tickets imprimibles.
- **Pendiente frontend:** CRUD de Inventario, formulario de nueva venta y selector de planes reales en Miembros.
- **Nota operativa:** el servicio `GymService.exe` (PID 7276) estaba en ejecución durante el build; usar `dotnet build -t:Compile` para validar sin bloquear el ejecutable.

### Sesión 18: Cierre de Inventario, Ventas y Membresías
- **Inventario:** `Inventory.jsx` ahora usa `CrudPage` (crear, eliminar, búsqueda y ajuste de stock).
  - **Bug corregido:** `InventoryModule` usaba la columna `stock`, pero la tabla real tiene
    `stock_quantity` (como ya declaraba `ModelGym.cs`). El listado de inventario fallaba en SQL.
- **Ventas:** formulario de nueva venta con socio opcional, método de pago y varios productos;
  calcula subtotal, IVA 16% y total. Envía `POST /api/sales`.
  - **Bug corregido:** `sale_number` se generaba con `NEWID()` (36 caracteres) en una columna
    `nvarchar(20)`. Ahora se genera un código de 19 caracteres: `V` + `yyMMddHHmmssfff` + 3 dígitos.
  - **Bug corregido:** `SalesController` enviaba `cashierId` y el módulo leía `createdBy`.
- **Miembros:** `GET /api/members/plans` (`MembersModule.GetMembershipPlans()`) alimenta el selector
  de planes; la membresía usa `duration_days` del plan. Regla `members.view` añadida a `PermissionMatrix`.
- **Datos:** `gym_membership_plans` estaba vacía; se sembraron Básico ($399), Premium ($599) y VIP ($899),
  todos de 30 días.
- **Corrección sistémica de JSON:** `Tools/ObjectToInferredTypesConverter.cs` convierte los valores de
  `Dictionary<string, object>` a primitivos CLR (`long`, `decimal`, `bool`, `string`, `List<object>`,
  `Dictionary<string, object>`) en lugar de `JsonElement`. Registrado en `AddControllers()`.
  - Sin esto, cualquier `Convert.ToInt64/ToDecimal/ToBoolean` sobre un body JSON lanzaba
    `InvalidCastException`, afectando a ventas, inventario, clases, fisio, mantenimiento y facturación.
  - `SalesModule.CreateSale()` ahora recorre los items como `IEnumerable<object?>`.
- **Validación:** frontend `npm run build` OK y sin warnings nuevos; backend `dotnet build -t:Compile` con 0 errores.
  - Se validó el esquema real con una transacción SQL de prueba (venta + items + membresía) y `ROLLBACK`.

### Sesión 19: Correcciones críticas de arranque y de IDs
- **El arranque ya no borra la base:** `DatabaseService.CreateOrUpdateTable()` hacía `DROP TABLE` de cada
  tabla existente en cada arranque. Se reemplazó por `CreateTableIfMissing()`; si el esquema existe, se
  conserva. Las foreign keys solo se recrean en una base nueva.
  - Esto provocó una pérdida real de datos al reiniciar con el build nuevo; se recreó el usuario dueño
    con contraseña temporal y se sembraron de nuevo los 3 planes.
- **`SCOPE_IDENTITY()` entre conexiones:** `MembersModule.CreateMember()` y `SalesModule.CreateSale()`
  ejecutaban la consulta en otra conexión y siempre recibían 0. Ahora buscan la fila insertada por
  `MAX(id)` usando los datos del socio o el `sale_number` único.
- **`membershipType` vacío:** la consulta devolvía `membership_type`, pero el módulo leía `plan_name`.
- **Prueba funcional (instancia en `:5001`):** planes, producto de inventario, socio con membresía de
  30 días y venta con items. Todo verificado y luego eliminado.
- **Pendiente operativo:** `GymService.exe` (PID 7276) sigue con el build anterior porque esta sesión no
  puede terminarlo; el usuario debe reiniciarlo para aplicar los cambios.
- **Contraseña temporal del admin:** se comunicó fuera del repositorio; cambiarla en el primer inicio.
- **Recuperación de acceso:** `POST /api/auth/reset-admin-password` resetea la contraseña del dueño.
  Solo acepta peticiones desde la misma máquina (loopback); si se expone la API, sigue siendo un
  endpoint sensible y debe deshabilitarse en producción.
