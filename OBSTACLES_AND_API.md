# Obstáculos y API del Framework GymCore - GymService

## INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs

**ANTES de escribir cualquier código en GymService, LEA este documento completo.**
Todos los errores de compilación encontrados se deben a que el código original usaba una API inventada que NO existe en el framework GymCore. Las secciones abajo documentan la API REAL que debe usarse.

---

## Resumen de Obstáculos Encontrados

### Obstáculo 1: Propiedad mal nombrada en DataCenter.cs

**Error:** `DataCenter.BusinessModules` no existe.
**Causa:** Los controllers referenciaban `DataCenter.BusinessProcess` pero DataCenter definía `BusinessModules`.
**Solución:** La propiedad y clase se llaman `BusinessProcess`.

### Obstáculo 2: Target Framework incompatible

**Error:** `GymService.csproj` debe usar un TFM compatible con las GymCore Libraries.
**Solución:** `<TargetFramework>net10.0-windows7.0</TargetFramework>` (AnxoLibrary/AnxoDatabase/AnxoObjectDatabaseManager son `net10.0` y AnxoServiceTools es `net10.0-windows`).

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

### Obstáculo 10: API de DatabaseAdministrator

**Error:** `admin.TableExists()`, `admin.CreateTableFromMappingObject()`, `admin.UpdateTableStructureFromMapping()` → NO EXISTEN.

**API REAL (usada en `Services/DatabaseService.cs`):**

```csharp
var dba = dbManager.DatabaseAdministrator;

bool exists = dba.ExistTable("TABLA");
var structure = dba.GetTableStructure("TABLA");   // DatabaseTable (columnas, indices, FK)
dba.CreateTable(databaseTable);                    // CREATE TABLE (no es idempotente: comprobar ExistTable antes)
dba.ApplyForeignKeys(structure);                   // crea solo las FK que faltan
dba.CreateIndex(databaseTable, index);             // indice nuevo
dba.ExecuteRawCommand("ALTER TABLE ...");          // DDL suelto
```

Notas:
- `DatabaseColumn.Properties` solo soporta `IDENTITY` y `ROWGUIDCOL` (el DDL parte por comas).
- `IndexTypes` solo tiene `Unique` y `NonUnique`: no hay PK compuesta. Para claves compuestas se usa una PK surrogate + indice UNIQUE.
- Los indices UNIQUE de columnas nullable se crean filtrados (`WHERE col IS NOT NULL`) porque SQL Server solo admite un NULL por indice UNIQUE.

### Obstáculo 11: LogLevel ambiguo

**Error:** `LogLevel.Debug` es ambiguo entre `AxLogger.LogLevel` y `Microsoft.Extensions.Logging.LogLevel`.

**Solución:** Usa el namespace completo: `AxLogger.LogLevel.Debug` o `Microsoft.Extensions.Logging.LogLevel.Debug`.

---

## Estructura del Framework GymCore

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
            ├── ExecuteReportObject<T>(obj) / ExecuteReportObjects<T>(...) → entidades
            ├── CreateSession(log, userId) → Session
            │       ├── InsertObject(obj) / InsertObjectWithID(obj)
            │       ├── UpdateObjectChanges(obj) / DeleteObject(obj)
            │       └── TryExecute() → ProcessResult
            └── GenerateUniqueID(mappingObject, qty) → SequenceManager
```

### Modelo de objetos (OBLIGATORIO para escrituras)

Las entidades están en `GymService/Models/Generated/GYM_*.cs` (namespace `AxDatabase`, clase = tabla).
Las PK ya NO son IDENTITY: las asigna GymCore con secuencias.

```csharp
// INSERT
var s = tools.CreateSession("MODULO", userId);
var obj = new GYM_MEMBERS { FIRST_NAME = "Ana", LAST_NAME = "Pérez", STATUS = "active" };
s.InsertObject(obj);
if (s.TryExecute().IsError) { /* manejar */ }
long newId = obj.ID;                       // asignado por la secuencia

// UPDATE parcial (solo las propiedades asignadas)
var s2 = tools.CreateSession("MODULO", userId);
s2.UpdateObjectChanges(new GYM_MEMBERS { ID = newId, STATUS = "inactive" });
s2.TryExecute();

// DELETE
var s3 = tools.CreateSession("MODULO", userId);
s3.DeleteObject(new GYM_MEMBERS { ID = newId });
s3.TryExecute();

// Cabecera + detalle en UNA transacción (ID conocido antes de insertar)
var seq = tools.GenerateUniqueID(GYM_SALES.DatabaseMappingObject, 1);
long saleId = seq.GetInt64UniqueId();
var s4 = tools.CreateSession("MODULO", userId);
s4.InsertObjectWithID(new GYM_SALES { ID = saleId, SALE_NUMBER = "V1", ... });
s4.InsertObject(new GYM_SALE_ITEMS { SALE_ID = saleId, PRODUCT_ID = p, QUANTITY = 1, ... });
s4.TryExecute();
```

Reglas:
- NUNCA escribir `id`, `created_at`, `updated_at`, `object_version` ni `created_by` en SQL:
  GymCore los rellena (`created_by`/auditoría vienen de `Session.UserID`).
- Los SELECT de listados/reportes pueden seguir en SQL parametrizado con `?`.
- Los nombres de propiedad son las columnas en MAYÚSCULAS; las columnas nullable generan tipos nullable.

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
11. Para INSERT/UPDATE/DELETE usa SIEMPRE objetos: `tools.CreateSession(log, userId)` + `InsertObject`/`UpdateObjectChanges`/`DeleteObject` + `TryExecute()`.
12. ServiceDefinition usa strings simples: `StartType = "auto"`, `ServiceAccount = "localsystem"`.
13. ServiceConsole.Execute requiere 3 args: `(args, serviceDefinition, serviceImplementation)`.
14. Logger.ConfigLogger requiere: `(name, level, localtime, logToConsole, filePerSubModule, defaultPath)`.
15. Los módulos de negocio reciben `DatabaseTools`, no `OODatabaseManager`.
16. NUNCA escribas `id`, `created_at`, `updated_at`, `object_version` ni `created_by`: GymCore los gestiona.
17. Si necesitas el ID antes de insertar (cabecera+detalle), usa `GenerateUniqueID(ENTIDAD.DatabaseMappingObject, 1)` + `InsertObjectWithID`.
18. Las entidades se regeneran desde `Models/_ModelGym.xml` con `AnxoDatabaseGenerator`; no se editan a mano.

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
- **GymCore Libraries**: `third_party\gym-core\`

Para ver ejemplos reales de uso de DatabaseTools, consulta:
- `CombuExpressBusinessProcess\Modules\Fuel\FuelRequestsModule.cs`

---

## Cómo decompilar DLLs del framework

Si necesitas ver la API real de una clase del framework:

```powershell
# Instalar ilspycmd (una vez):
dotnet tool install --global ilspycmd

# Ver una clase específica:
ilspycmd --type ClassName "third_party\gym-core\AnxoLibrary\bin\Debug\net8.0\AnxoLibrary.dll"

# Ver métodos públicos:
ilspycmd --type ClassName "path\to\assembly.dll" | Select-String -Pattern 'public '
```

---

## Estado actual del proyecto

- **Compilación:** ✓ Exitosa (0 errores)
- **IdentityModule:** ✓ Implementado (PBKDF2, MFA AES-256, bootstrap atómico)
- **Modulos de negocio:** ✓ Implementados sobre `DatabaseTools`/`Session` + SQL parametrizado
- **DatabaseService:** ✓ Implementado con `DatabaseAdministrator` (tablas, columnas, indices UNIQUE y FK aditivos)
