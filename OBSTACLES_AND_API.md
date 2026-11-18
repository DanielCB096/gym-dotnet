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

### Obstáculo 10: DatabaseAdministrator API inventada

**Error:** `admin.TableExists()`, `admin.CreateTableFromMappingObject()`, `admin.UpdateTableStructureFromMapping()` → NO EXISTEN.

**Solución:** DatabaseService debe implementarse con SQL raw o usar la API correcta de DatabaseAdministrator (que requiere investigación adicional).

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

---

## Archivos corregidos en esta sesión

| Archivo | Cambios principales |
|---|---|
| `IdentityModule.cs` | Reescrito completo: DatabaseTools, ExecuteQueryResultSet, Session.ExecuteTransaction, Logger correcto, helper GetRowValue |
| `ServiceImplementation.cs` | Logger.AddErrorSubModule, BusinessProcess, DatabaseTools para IdentityModule |
| `DataCenter.cs` | ServiceDefinition con strings, BusinessProcess |
| `Program.cs` | Logger.ConfigLogger correcto, ServiceConsole.Execute con 3 args |
| `SecurityValidation.cs` | Logger.AddWarningSubModule, Logger.AddErrorSubModule, Logger.AddErrorDevExceptionSubModule |
| `DatabaseService.cs` | Stub (API de DatabaseAdministrator no disponible) |
| `WebServerService.cs` | Logger.AddErrorDevExceptionSubModule, LogLevel cualificado |
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

## Estado actual del proyecto

- **Compilación:** ✓ Exitosa (0 errores, 6 warnings de NuGet)
- **IdentityModule:** ✓ Implementado con API correcta
- **Otros módulos:** Stubs vacíos (pendiente de implementación)
- **DatabaseService:** Stub (API de DatabaseAdministrator requiere investigación)
- **WebServerService:** ✓ Corregido
- **Program.cs:** ✓ Corregido
- **DataCenter.cs:** ✓ Corregido
