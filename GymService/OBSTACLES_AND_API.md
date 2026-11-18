# Obstáculos y API del Framework Anxo - GymService

## INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs

**ANTES de escribir cualquier código en GymService, LEA este documento completo.**
Todos los errores de compilación encontrados se deben a que el código original usaba una API inventada que NO existe en el framework Anxo. Las secciones abajo documentan la API REAL que debe usarse.

---

## Resumen de Obstáculos Encontrados

### Obstáculo 1: Propiedad mal nombrada en DataCenter.cs

**Error:** `DataCenter.BusinessModules` no existe.
**Causa:** Los controllers referenciaban `DataCenter.BusinessProcess` pero DataCenter definía `BusinessModules`.
**Solución:** La propiedad y clase se llaman `BusinessProcess`.

### Obstáculo 2: Target Framework incompatible

**Error:** `GymService.csproj` usaba `net8.0` pero `AnxoServiceTools` requiere `net8.0-windows7.0`.
**Solución:** `<TargetFramework>net8.0-windows7.0</TargetFramework>`

### Obstáculo 3: API de Query inventada (~458 errores)

**Error:** `new Query("SELECT...")`, `query.AddParameter()`, `_dbManager.ExecuteQuery(query)` NO EXISTEN.

**API REAL:**

```csharp
// SELECT con ResultSet (sin objetos mapeados):
var result = tools.ExecuteQueryResultSet(
    "SELECT id, name FROM users WHERE status = ?",
    statusValue
);

// COUNT:
long count = tools.ExecuteQueryObjectCount(
    "SELECT COUNT(*) FROM users WHERE status = ?",
    statusValue
);

// INSERT/UPDATE/DELETE (usando Session):
var session = tools.CreateSession("MODULE_NAME");
session.ExecuteTransaction(
    "INSERT INTO users (name, email) VALUES (?, ?)",
    name, email
);
session.ExecuteTransaction(
    "UPDATE users SET name = ? WHERE id = ?",
    newName, userId
);
var processResult = session.TryExecute();
if (processResult.IsError) { /* manejar error */ }
```

### Obstáculo 4: Row no soporta indexador []

**Error:** `row["column_name"]` NO EXISTE en la clase `Row`.

**API REAL:** `Row` solo tiene métodos tipados por índice:

```csharp
// Row usa métodos tipados con índice de columna:
row.GetObject(int index);
row.GetString(int index);
row.GetInt64(int index);
row.GetInt32(int index);
row.GetBoolean(int index);
row.GetDateTime(int index);
row.GetDouble(int index);
row.GetDecimal(int index);

// Para acceder por nombre de columna, usa ResultSet.Columns:
// ResultSet.Columns[i].ColumnName → nombre de columna
// ResultSet.Columns[i].ColumnType → tipo de dato

// Helper recomendado:
private object GetRowValue(Row row, ResultSet result, string columnName)
{
    for (int i = 0; i < result.Columns.Count; i++)
    {
        if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            return row.GetObject(i);
    }
    return null;
}
```

### Obstáculo 5: Session no implementa IDisposable

**Error:** `using (var session = tools.CreateSession())` NO COMPILA.

**API REAL:** Session NO implementa IDisposable. No uses `using`:

```csharp
// INCORRECTO:
// using (var session = tools.CreateSession()) { ... }

// CORRECTO:
var session = tools.CreateSession("MODULE_NAME");
session.ExecuteTransaction("INSERT INTO ...", param1, param2);
var result = session.TryExecute();
```

### Obstáculo 6: Logger API incorrecta

**Errores encontrados:**
- `Logger.AddErrorDevException(module, msg, ex)` con 3 args → NO EXISTE
- `Logger.AddError(module, msg)` con 2 args → NO EXISTE
- `Logger.AddWarn(module, msg)` → NO EXISTE (se llama `AddWarning`)
- `Logger.Initialize(...)` → NO EXISTE

**API REAL de Logger:**

```csharp
// Inicialización (en Program.cs):
Logger.ConfigLogger("AppName", AxLogger.LogLevel.Debug, true, true, false, logPath);
// Parámetros: name, level, localtime, logToConsole, filePerSubModule, defaultPath

// Logging simple:
Logger.AddInfo("mensaje");
Logger.AddError("mensaje");
Logger.AddWarning("mensaje");       // NO es AddWarn
Logger.AddDebug("mensaje");

// Logging con submódulo:
Logger.AddErrorSubModule("MODULE_NAME", "mensaje");
Logger.AddWarningSubModule("MODULE_NAME", "mensaje");
Logger.AddInfoSubModule("MODULE_NAME", "mensaje");

// Excepciones:
Logger.AddErrorDevException("mensaje con exception");
Logger.AddErrorDevExceptionSubModule("MODULE_NAME", "mensaje con exception");

// NO existe: Logger.AddErrorDevException(module, msg, exception) con 3 args
// NO existe: Logger.AddError(module, msg) con 2 args
// NO existe: Logger.AddWarn()
```

### Obstáculo 7: ServiceDefinition API incorrecta

**Error:** `ServiceStartType.Automatic`, `ServiceAccount.LocalSystem`, `ServiceDescription` → NO EXISTEN.

**API REAL:**

```csharp
var serviceDef = new ServiceDefinition
{
    ServiceName = "myservice",
    ServiceDisplayName = "My Service",
    StartType = "auto",           // String, no enum
    ServiceAccount = "localsystem" // String, no enum
};
// NO hay ServiceDescription
```

### Obstáculo 8: ServiceConsole.Execute() requiere ServiceDefinition

**Error:** `ServiceConsole.Execute(args, serviceImpl)` → Faltan argumentos.

**API REAL:**

```csharp
ServiceConsole.Execute(args, serviceDefinition, serviceImplementation);
```

### Obstáculo 9: DataType enum incorrecto en ModelGym

**Errores:** `DataType.BigInt`, `DataType.NVarChar`, `DataType.Bit`, `DataType.Int` → NO EXISTEN.

**API REAL del enum DataType:**

```csharp
// Valores disponibles:
DataType.Int16     // 1
DataType.Int32     // 2  (usa Int32, NO "Int")
DataType.Int64     // 3  (usa Int64, NO "BigInt")
DataType.Float     // 4
DataType.Double    // 5
DataType.String    // 6  (usa String, NO "NVarChar")
DataType.Block     // 7
DataType.DateTime  // 8
DataType.Boolean   // 9  (usa Boolean, NO "Bit")
DataType.Decimal   // 10
DataType.Date      // 11
DataType.Byte      // 12
DataType.Guid      // 13
DataType.Xml       // 14
DataType.Time      // 15
DataType.DateTimeOffset // 16
DataType.Geography // 17
DataType.DateTime2 // 18
```

### Obstáculo 10: DatabaseAdministrator API inventada → ✓ RESUELTO

**Error:** `admin.TableExists()`, `admin.CreateTableFromMappingObject()`, `admin.UpdateTableStructureFromMapping()` → NO EXISTEN.

**Solución encontrada:** La API correcta es `OODatabaseManager.DatabaseAdministrator` que expone:

```csharp
// Acceder a DatabaseAdministrator desde OODatabaseManager:
var dba = dbManager.DatabaseAdministrator;

// Crear tabla:
dba.CreateTable(databaseTable);

// Verificar existencia:
bool exists = dba.ExistTable("table_name");

// Eliminar tabla:
dba.DropTable("table_name");

// Obtener estructura actual:
var structure = dba.GetTableStructure("table_name");
```

**Tipos clave del framework (AxDatabase.Administration):**

```csharp
// DatabaseTable: contenedor de columnas, indexes y foreign keys
var table = new DatabaseTable("table_name");
table.Columns.Add(column);
table.Indexes.Add(index);
table.ForeignKeys.Add(foreignKey);

// DatabaseColumn: columna con tipo, tamaño y propiedades
var column = new DatabaseColumn("col_name", DataType.String, 100, ColumnType.NotNull);
column.DefaultValue = "GETDATE()";
column.Properties = "IDENTITY=1;UNIQUE=1"; // Para identity y unique

// ColumnType enum: NotNull, Null, PrimaryKey

// DatabaseIndex: índice compuesto
var index = new DatabaseIndex("index_name", IndexTypes.Unique);
index.IndexColumns.Add(new DatabaseIndexColumn("col_name", 1));

// IndexTypes enum: Unique, NonUnique (NO tiene PrimaryKey)

// DatabaseForeignKey: clave foránea
var fk = new DatabaseForeignKey("referenced_table");
fk.ForeignKeyName = "FK_name";
fk.ForeignKeyColumns.Add(new DatabaseForeignKeyColumn("local_col", "external_col"));
```

**Implementación en DatabaseService.cs:**
- Convierte `TableDefinition` de ModelGym a `DatabaseTable` del framework
- Convierte `ColumnDefinition` a `DatabaseColumn` con ColumnType correcto
- Las 32 tablas se crean en orden, recreando si ya existen
- Las 25 foreign keys se agregan después de crear todas las tablas
- `DropAllTables()` elimina en orden inverso para respetar dependencias

### Obstáculo 11: LogLevel ambiguo

**Error:** `LogLevel.Debug` es ambiguo entre `AxLogger.LogLevel` y `Microsoft.Extensions.Logging.LogLevel`.

**Solución:** Usa el namespace completo: `AxLogger.LogLevel.Debug` o `Microsoft.Extensions.Logging.LogLevel.Debug`.

---

## Estructura del Framework Anxo

### Bibliotecas principales

| Biblioteca | Namespace | Descripción |
|---|---|---|
| AnxoLibrary | AxLibraries | ProcessResult, Logger, utilidades generales |
| AnxoDatabase | AxDatabase | Query, ResultSet, Row, DataType, DatabasePool |
| AnxoObjectDatabaseManager | AxObjectOrientedDatabase | OODatabaseManager, DatabaseTools, Session |
| AnxoServiceTools | AxServiceTools | IServiceConsole, ServiceDefinition, ServiceConsole |

### Patrón de acceso a base de datos

```
OODatabaseManager (DataCenter.DatabaseManager)
    └── .Tools → DatabaseTools (LocalDatabaseTools)
            ├── ExecuteQueryResultSet(sql, params) → ResultSet
            ├── ExecuteQueryObjectCount(sql, params) → long
            ├── CreateSession(string logstring) → Session
            │       ├── ExecuteTransaction(string sql, params)
            │       └── TryExecute() → ProcessResult
            └── GenerateUniqueID(mappingObject, qty)
```

### Patrón de servicio (Windows Service)

```
ServiceImplementation : IServiceConsole
    ├── Start() → ProcessResult
    └── Stop() → void

DataCenter (clase estática)
    ├── Configuration
    ├── DatabaseManager (OODatabaseManager)
    ├── ConnectionData
    ├── WebServer
    └── BusinessProcess (contiene módulos de negocio)
```

### Patrón de módulos de negocio (referencia: CombuExpressBusinessProcess)

```csharp
// Los módulos reciben DatabaseTools, no OODatabaseManager:
public class MyModule(DatabaseTools tools)
{
    // SELECT:
    var result = tools.ExecuteQueryResultSet("SELECT ...", param1, param2);
    
    // INSERT/UPDATE/DELETE:
    var session = tools.CreateSession("MODULE_NAME");
    session.ExecuteTransaction("INSERT INTO ...", param1);
    session.TryExecute();
}
```

---

## Reglas OBLIGATORIAS para código en GymService

1. **NUNCA** usar `new Query("SELECT...")` — usa `tools.ExecuteQueryResultSet(sql, params)`.
2. **NUNCA** usar `query.AddParameter()` — los parámetros se pasan directamente como args.
3. **NUNCA** usar `db.ExecuteQuery()` — usa `db.Tools.ExecuteQueryResultSet()`.
4. **NUNCA** usar `Logger.AddErrorDevException(module, msg, ex)` con 3 args — usa `Logger.AddErrorDevExceptionSubModule()`.
5. **NUNCA** usar `Logger.AddError(module, msg)` con 2 args — usa `Logger.AddErrorSubModule()`.
6. **NUNCA** usar `Logger.AddWarn()` — usa `Logger.AddWarning()` o `Logger.AddWarningSubModule()`.
7. **NUNCA** usar `using (var session = ...)` con Session — no implementa IDisposable.
8. **NUNCA** usar `row["column"]` — usa `row.GetObject(index)` con el índice de columna.
9. **NUNCA** usar `DataType.BigInt`, `DataType.NVarChar`, `DataType.Bit`, `DataType.Int` — usa `DataType.Int64`, `DataType.String`, `DataType.Boolean`, `DataType.Int32`.
10. Los parámetros SQL usan `?` como placeholder y se pasan como argumentos adicionales.
11. Para INSERT/UPDATE/DELETE, usa `tools.CreateSession()` + `session.ExecuteTransaction()` + `session.TryExecute()`.
12. ServiceDefinition usa strings simples: `StartType = "auto"`, `ServiceAccount = "localsystem"`.
13. ServiceConsole.Execute requiere 3 args: `(args, serviceDefinition, serviceImplementation)`.
14. Logger.ConfigLogger requiere: `(name, level, localtime, logToConsole, filePerSubModule, defaultPath)`.
15. Los módulos de negocio reciben `DatabaseTools`, no `OODatabaseManager`.
18. Para crear tablas, usa `OODatabaseManager.DatabaseAdministrator` con `CreateTable(DatabaseTable)`. No uses `TableExists()` — usa `ExistTable(string)`.
19. `DatabaseColumn.Properties` acepta `IDENTITY=1` y `UNIQUE=1` separados por `;`.
20. `IndexTypes` solo tiene `Unique` y `NonUnique`. Para composite PKs, usa `IndexTypes.Unique`.
21. Para transacciones, usa `session.ExecuteTransaction(sql, params)` para agregar operaciones, luego `session.TryExecute()` para ejecutar. Verifica con `.IsError`.
22. `TryExecute()` no acepta parámetros SQL directamente. Solo existe `TryExecute()` sin args o `TryExecute(string SubModule, ErrorInfo errorinfo)`.

---

## Archivos corregidos en esta sesión

| Archivo | Cambios principales |
|---|---|
| `IdentityModule.cs` | Reescrito completo: DatabaseTools, ExecuteQueryResultSet, Session.ExecuteTransaction, Logger correcto, helper GetRowValue |
| `ServiceImplementation.cs` | Logger.AddErrorSubModule, BusinessProcess, DatabaseTools para IdentityModule |
| `DataCenter.cs` | ServiceDefinition con strings, BusinessProcess |
| `Program.cs` | Logger.ConfigLogger correcto, ServiceConsole.Execute con 3 args |
| `SecurityValidation.cs` | Logger.AddWarningSubModule, Logger.AddErrorSubModule, Logger.AddErrorDevExceptionSubModule |
| `DatabaseService.cs` | ✓ Implementado: convierte ModelGym → DatabaseTable, crea 32 tablas + 25 FK |
| `Controllers CRUD` | ✓ CRUD completo: 11 módulos actualizados con POST, PUT, DELETE usando ExecuteTransaction + TryExecute (~55 endpoints) |
| `WebServerService.cs` | Logger.AddErrorDevExceptionSubModule, LogLevel cualificado, Swagger con JWT Bearer auth |
| `ModelGym.cs` | DataType.BigInt→Int64, DataType.NVarChar→String, DataType.Bit→Boolean, DataType.Int→Int32 |
| Módulos restantes | Stubs vacíos para compilar |

---

## Proyectos de referencia

- **PilotTmsService**: `C:\repositorios\pilot\TMS\PilotTmsService\`
- **CombuExpressBusinessProcess**: `C:\repositorios\pilot\TMS\CombuExpressBusinessProcess\`
- **Anxo Libraries**: `C:\repositorios\pilot\ANXO LIBRARIES\`

Para ver ejemplos reales de uso de DatabaseTools, consulta:
- `CombuExpressBusinessProcess\Modules\Fuel\FuelRequestsModule.cs`

---

## Cómo decompilar DLLs del framework

Si necesitas ver la API real de una clase del framework:

```powershell
# Instalar ilspycmd (una vez):
dotnet tool install --global ilspycmd

# Ver una clase específica:
ilspycmd --type ClassName "C:\repositorios\pilot\ANXO LIBRARIES\AnxoLibrary\bin\Debug\net8.0\AnxoLibrary.dll"

# Ver métodos públicos:
ilspycmd --type ClassName "path\to\assembly.dll" | Select-String -Pattern 'public '
```

---

## Obstáculo 12: Controllers REST — Patrón correcto

**Error:** Los controllers no se registran manualmente. El WebServerService ya tiene `MapControllers()` configurado.

**API REAL:**

```csharp
// En WebServerService.ConfigureApp():
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Registra automáticamente todos los [ApiController]
});

// Los controllers deben:
// 1. Heredar de ControllerBase
// 2. Usar [ApiController] attribute
// 3. Usar [HttpGet], [HttpPost], etc. con [Route("...")]
// 4. Usar SecurityValidation.ValidateAuthentication() para endpoints protegidos
```

**Regla adicional:**
16. Los controllers REST usan `[ApiController]` heredando de `ControllerBase`. No se registran manualmente — `MapControllers()` los detecta automáticamente.
17. Los métodos de módulo tienen firmas específicas. Verificar la firma exacta antes de llamarlos desde un controller (ej: `SalesModule.CreateSale(Dictionary<string, object>)` no es `CreateSale(memberId, items, paymentMethod, createdBy)`).

---

## Estado actual del proyecto (actualizado 18/nov/2026)

- **Compilación:** ✓ Exitosa (0 errores, 30 warnings nullable)
- **13 módulos de negocio:** ✓ Todos implementados con API correcta
- **15 controllers REST:** ✓ CRUD completo + importación + exportación — ~67 endpoints
- **DatabaseService:** ✓ Implementado — crea 32 tablas + 25 foreign keys automáticamente
- **Swagger/OpenAPI:** ✓ Documentación interactiva con JWT Bearer auth en /swagger
- **Paginación:** ✓ OFFSET/FETCH en 7 módulos principales (Members, Sales, Billing, Inventory, Staff, Access)
- **Health checks:** ✓ 3 endpoints (/health, /ready, /health/live) con verificación de BD, módulos y recursos
- **Validación de roles:** ✓ Middleware con 27 permisos y 4 roles (OWNER, ADMIN, STAFF, CASHIER)
- **Windows Service:** ✓ Scripts de instalación/desinstalación con recovery options
- **Rate limiting:** ✓ 3 políticas (general 100/min, auth 20/min, strict 5/min)
- **Ingreso masivo:** ✓ Importación CSV/Excel para miembros e inventario con plantillas descargables
- **Exportación de reportes:** ✓ 4 reportes a Excel (ventas, miembros, asistencia, inventario) con formato profesional
- **WebServerService:** ✓ Corregido con MapControllers() y Swagger UI
- **Program.cs:** ✓ Corregido
- **DataCenter.cs:** ✓ Corregido
- **Documentación:** ✓ PROJECT_CONTEXT.md y OBSTACLES_AND_API.md actualizados

### Próximos pasos pendientes:
1. **Pruebas de integración:** Bloqueado — requiere SQL Server instalado (necesita privilegios de administrador)
2. **Despliegue:** ✓ Scripts de instalación PowerShell con recovery options y guía DEPLOYMENT.md
3. **Tests unitarios:** No hay tests unitarios aún

---

## Obstáculo 13: Swagger/OpenAPI — Configuración correcta

**Hallazgo:** Swagger ya estaba configurado en `WebServerService.cs` con Swashbuckle.AspNetCore 6.5.0, pero sin soporte para autenticación JWT.

**Mejora aplicada:**

```csharp
// En ConfigureServices():
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GymService API",
        Version = "v1",
        Description = "API REST para el sistema de gestión del gimnasio Centro Fit",
        Contact = new OpenApiContact
        {
            Name = "GymService Support",
            Email = "support@centrofit.com"
        }
    });

    // JWT Bearer authentication:
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa 'Bearer' seguido de un espacio y tu token JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// En ConfigureApp():
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GymService API v1");
    c.RoutePrefix = "swagger";
});
```

**Acceso:** `http://localhost:{port}/swagger` al iniciar el servicio.
**Uso:** Hacer clic en "Authorize" → ingresar token JWT → cerrar → probar endpoints protegidos.

---

## Paginación en endpoints GET (Sesión 10)

**Patrón implementado:** SQL Server `OFFSET/FETCH NEXT` con metadata de paginación en la respuesta.

**Parámetros de query:**
- `page` (int, default 1) — número de página
- `limit` (int, default 50, max 500) — registros por página
- `query` (string, opcional) — búsqueda por texto (LIKE)

**Ejemplo de respuesta con paginación:**
```json
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 50,
    "totalCount": 150,
    "totalPages": 3,
    "hasNext": true,
    "hasPrev": false
  }
}
```

**Módulos con paginación:** Members, Sales, Billing, Inventory, Staff, Access.

**Regla adicional:**
23. Para paginación, usa `OFFSET ? ROWS FETCH NEXT ? ROWS ONLY` en SQL Server. Calcula `offset = (page - 1) * limit`. Usa `ExecuteQueryObjectCount()` para obtener el total de registros.
24. Los health checks usan `Microsoft.Extensions.Diagnostics.HealthChecks` (incluido en ASP.NET Core). Registra con `services.AddHealthChecks().AddCheck<T>("name")` y mapea con `endpoints.MapHealthChecks("/path", options)`.

---

## Health Checks (Sesión 11)

**Implementación:** Health checks personalizados para monitoreo del servicio sin paquete adicional (incluido en ASP.NET Core).

**Archivos creados:**
- `Services/GymHealthCheck.cs` — Contiene `GymHealthCheck` y `GymReadinessCheck`

**Endpoints disponibles:**

| Endpoint | Propósito | HTTP Status |
|---|---|---|
| `GET /health` | Health check completo (BD, módulos, sistema, web) | 200/503 |
| `GET /ready` | Readiness check ligero (solo servicios críticos) | 200/503 |
| `GET /health/live` | Liveness probe (siempre healthy si corre) | 200 |

**GymHealthCheck verifica:**
1. **Base de datos:** DatabaseManager inicializado, ConnectionData configurado, conectividad con query a `gym_members`
2. **Módulos de negocio:** Los 14 módulos cargados (Identity, Members, Access, Sales, Inventory, Classes, Routines, Fisio, Maintenance, Billing, Finance, Reports, Kiosk, Staff)
3. **Recursos del sistema:** Memoria RAM (`Process.GetCurrentProcess().WorkingSet64`), núcleos CPU, uptime, PID
4. **Servidor web:** Kestrel corriendo

**GymReadinessCheck verifica:**
- DatabaseManager != null
- WebServer != null

**Formato de respuesta JSON:**
```json
{
  "status": "healthy",
  "timestamp": "2026-11-18T11:00:00Z",
  "duration": "5.23ms",
  "components": {
    "gym-health": {
      "status": "healthy",
      "description": "All systems operational",
      "duration": "5.23ms",
      "data": {
        "database": { "status": "healthy", "message": "Database connected and schema ready" },
        "modules": { "status": "healthy", "available": 14, "total": 14, "missing": [] },
        "system": { "memoryMB": 128, "cpuCores": 8, "uptime": "00.00:15:30", "processId": 1234 },
        "webServer": { "status": "healthy", "message": "Kestrel is running" }
      }
    }
  }
}
```

**Hallazgo:** `GCMemoryInfo.WorkingSet64` no existe en .NET 8 — usar `Process.GetCurrentProcess().WorkingSet64` directamente.

**Uso con Windows Service:**
- `/health/live` para el Service Control Manager (SCM) — verifica que el proceso está vivo
- `/ready` para load balancers — verifica que los servicios críticos están listos
- `/health` para monitoreo completo — verifica todos los componentes del sistema

---

## Validación de Roles y Permisos (Sesión 12)

**Implementación:** Middleware de autorización por rol que verifica permisos granulares por endpoint.

**Archivos creados:**
- `Services/RolePermissionMiddleware.cs` — Middleware + PermissionMatrix

**Roles y permisos:**

| Rol | Permisos | Descripción |
|---|---|---|
| **OWNER** | `*` (wildcard) | Acceso total a todo |
| **ADMIN** | 27 permisos | Todos los módulos |
| **STAFF** | 17 permisos | Members, access, classes, routines, fisio, maintenance, reports |
| **CASHIER** | 7 permisos | Members.view, sales, inventory.view, billing.view |

**27 permisos definidos:**
- `members.view`, `members.create`, `members.edit`, `members.delete`
- `access.view`, `access.override`
- `sales.view`, `sales.create`, `sales.return`
- `inventory.view`, `inventory.edit`
- `classes.view`, `classes.manage`
- `routines.view`, `routines.manage`
- `fisio.view`, `fisio.manage`
- `maintenance.view`, `maintenance.manage`
- `billing.view`, `billing.create`
- `finance.view`, `finance.reconcile`
- `reports.view`
- `staff.view`, `staff.manage`
- `admin.settings`

**Flujo del middleware:**
1. Skip /health, /ready, /swagger, /api/auth
2. Buscar permiso requerido en PermissionMatrix por ruta + método HTTP
3. Validar token Bearer con IdentityModule
4. Verificar permiso con SecurityValidation.HasPermission()
5. Retornar 401/403 si falla

**Respuesta 403 Forbidden:**
```json
{
  "success": false,
  "message": "No tienes permiso para realizar esta acción",
  "requiredPermission": "members.delete",
  "userRole": "CASHIER"
}
```

**Regla adicional:**
25. El middleware de roles se ejecuta antes de los controllers. Los endpoints /health, /ready, /swagger y /api/auth están excluidos de la verificación de permisos.

---

## Windows Service Deployment (Sesión 13)

**Hallazgo:** AnxoServiceTools ya soporta instalación de Windows Service con comandos integrados.

**Comandos disponibles:**
- `GymService.exe -install` — Instalar servicio
- `GymService.exe -uninstall` — Desinstalar servicio
- `GymService.exe -start` — Iniciar servicio
- `GymService.exe -stop` — Detener servicio
- `GymService.exe -console` — Ejecutar en modo consola (debug)
- `GymService.exe -help` — Mostrar ayuda

**ServiceDefinition opciones:**
- `ServiceName`: "anxogymmservice"
- `ServiceDisplayName`: "Anxo Gym Manager Service"
- `ServiceAccount`: "localsystem" (también localservice, networkservice, o cuenta personalizada)
- `StartType`: "auto" (también manual, disable, delayed)

**Scripts creados:**
- `install-service.ps1` — Instalación automática con recovery options
- `uninstall-service.ps1` — Desinstalación con confirmación
- `DEPLOYMENT.md` — Guía completa de deployment

**Recovery options configuradas:**
- 1er fallo: Restart después de 60 segundos
- 2do fallo: Restart después de 120 segundos
- 3er fallo: Restart después de 180 segundos
- Reset counter: 1 día

**Regla adicional:**
26. Para instalar el servicio, usar `GymService.exe -install` (requiere administrador). Para debugging, usar `GymService.exe -console` que permite start/stop interactivo.

---

## Rate Limiting (Sesión 14)

**Implementación:** Rate limiting nativo de ASP.NET Core 8 con `Microsoft.AspNetCore.RateLimiting`.

**3 políticas configuradas:**

| Política | Requests/min | Cola | Uso |
|---|---|---|---|
| **general** | 100 | 10 | API general |
| **auth** | 20 | 5 | Login, bootstrap, MFA |
| **strict** | 5 | 0 | OTP, endpoints sensibles |

**Configuración en WebServerService.cs:**
- `services.AddRateLimiter()` con 3 `AddFixedWindowLimiter`
- `app.UseRateLimiter()` en el pipeline antes del middleware de roles
- `OnRejected` handler personalizado que retorna JSON con 429 Too Many Requests

**Respuesta 429:**
```json
{
  "success": false,
  "message": "Demasiadas solicitudes. Por favor, intenta más tarde.",
  "retryAfter": "45",
  "retryAfterSeconds": 45
}
```

**Header RetryAfter:** Incluido automáticamente para que el cliente sepa cuándo reintentar.

**Aplicar políticas:** Usar `[EnableRateLimiting("policy-name")]` en controllers o endpoints.

**Hallazgo:** `AddPartitionedRateLimiter` no está disponible — usar `AddFixedWindowLimiter` con políticas nombradas.

**Regla adicional:**
27. El rate limiting se configura con `AddFixedWindowLimiter` en ConfigureServices y se activa con `UseRateLimiter()` en el pipeline. Las políticas se aplican con `[EnableRateLimiting("name")]` en controllers.

---

## Ingreso Masivo de Datos (Sesión 15)

**Implementación:** Importación masiva de datos desde CSV y Excel (.xlsx) usando CsvHelper y ClosedXML.

**Paquetes agregados:**
- `CsvHelper 33.0.1` — Parser CSV rápido con soporte RFC 4180
- `ClosedXML 0.104.2` — Lectura/escritura de Excel .xlsx sin Office

**Archivos creados:**
- `Services/BulkImportService.cs` — Servicio de importación (722 líneas)
- `Controllers/ImportController.cs` — 8 endpoints REST (203 líneas)

**Endpoints disponibles:**

| Endpoint | Método | Auth | Descripción |
|---|---|---|---|
| `/api/import/members/csv` | POST | Sí | Importar miembros desde CSV |
| `/api/import/members/excel` | POST | Sí | Importar miembros desde Excel |
| `/api/import/inventory/csv` | POST | Sí | Importar inventario desde CSV |
| `/api/import/inventory/excel` | POST | Sí | Importar inventario desde Excel |
| `/api/import/templates/members/csv` | GET | No | Descargar plantilla CSV miembros |
| `/api/import/templates/members/excel` | GET | No | Descargar plantilla Excel miembros |
| `/api/import/templates/inventory/csv` | GET | No | Descargar plantilla CSV inventario |
| `/api/import/templates/inventory/excel` | GET | No | Descargar plantilla Excel inventario |

**Formato CSV de miembros:**
```
FirstName,LastName,Email,Phone,CURP,RFC,Address,EmergencyContact,EmergencyPhone
Juan,Perez,juan@example.com,5551234567,...
```

**Formato CSV de inventario:**
```
ProductName,Description,SKU,Category,Price,Cost,StockQuantity,MinStock
Proteína Whey,Proteína de suero 1kg,PRO-001,Suplementos,850.00,600.00,50,10
```

**Respuesta de importación:**
```json
{
  "success": true,
  "message": "Importación completada: 50 importados, 2 errores, 0 omitidos",
  "tableName": "gym_members",
  "totalRows": 52,
  "importedRows": 50,
  "skippedRows": 0,
  "errorRows": 2,
  "errors": [
    { "rowNumber": 5, "rowData": "...", "errorMessage": "Nombre y apellido son obligatorios" },
    { "rowNumber": 12, "rowData": "...", "errorMessage": "Email ya existe" }
  ]
}
```

**Hallazgo:** `InventoryModule.CreateProduct()` usa la clave `stock` (no `stockQuantity`) en el Dictionary de entrada.

**Regla adicional:**
28. Para importar archivos, usa `multipart/form-data` con el archivo en `Request.Form.Files[0]`. El BulkImportService es sin estado y se crea on-demand en el controller.

---

## Exportación de Reportes a Excel (Sesión 16)

**Implementación:** Exportación de reportes a Excel (.xlsx) usando ClosedXML 0.104.2 (ya instalado).

**Archivos creados:**
- `Services/ReportExportService.cs` — Servicio de exportación (295 líneas)

**Endpoints disponibles:**

| Endpoint | Método | Auth | Descripción |
|---|---|---|---|
| `/api/reports/sales/excel` | GET | Sí | Exportar ventas a Excel (con filtro por fechas) |
| `/api/reports/members/excel` | GET | Sí | Exportar miembros a Excel (con filtro por estado) |
| `/api/reports/attendance/excel` | GET | Sí | Exportar asistencia a Excel (con filtro por fechas) |
| `/api/reports/inventory/excel` | GET | Sí | Exportar inventario a Excel |

**Formato de los archivos Excel:**
- Encabezados con colores por tipo de reporte (azul=ventas, verde=miembros, naranja=asistencia, púrpura=inventario)
- Columnas auto-ajustadas
- Reporte de ventas incluye fila de totales con fórmulas SUM
- Reporte de inventario resalta en naranja las celdas de stock bajo

**Hallazgo:** QuestPDF 2024.7.1 tiene API incompatible con la documentación actual (`RelativeColumnTableDefinition`, `White()`, `PageNumber()`, `Color()` no existen). Se descartó PDF por ahora.

**Regla adicional:**
29. Para exportar archivos, usa `File(bytes, contentType, filename)` en el controller. El ReportExportService recibe `DatabaseTools` en el constructor.
