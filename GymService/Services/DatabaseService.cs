using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxDatabase.Administration;
using AxObjectOrientedDatabase;

namespace GymService.Services
{
    /// <summary>
    /// Crea y valida el esquema a partir del modelo de objetos de Anxo.
    ///
    /// El modelo es la unica fuente de verdad: Models/Generated contiene las entidades
    /// (IDatabaseObject) y ModelGym.MappingObjects, generados desde Models/_ModelGym.xml
    /// con AnxoDatabaseGenerator.
    ///
    /// El mantenimiento del esquema es ADITIVO e IDEMPOTENTE: solo crea tablas que faltan,
    /// solo agrega indices/foreign keys que faltan y nunca elimina datos.
    /// </summary>
    public class DatabaseService
    {
        private const string SubModule = "DATABASE_SERVICE";

        private readonly OODatabaseManager _dbManager;

        public DatabaseService(OODatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        /// <summary>
        /// Crea/valida todas las tablas, indices y foreign keys del modelo.
        /// </summary>
        public void InitializeSchema()
        {
            Logger.AddInfo("========================================");
            Logger.AddInfo("Inicializando esquema de base de datos GymManager");
            Logger.AddInfo("========================================");

            // Fuerza la construccion/registro de todos los MappingObject.
            ModelGym.Register();

            var dba = _dbManager.DatabaseAdministrator;

            int created = 0;
            int existing = 0;
            int differences = 0;

            foreach (var mapping in ModelGym.MappingObjects)
            {
                var modelTable = BuildDatabaseTable(mapping);

                if (!dba.ExistTable(modelTable))
                {
                    dba.CreateTable(modelTable);
                    created++;
                    Logger.AddInfo($"  Tabla '{modelTable.TableName}' creada");
                }
                else
                {
                    existing++;
                    EnsureMissingColumns(dba, modelTable);
                    differences += ValidateTable(dba, modelTable);
                }

                EnsureIndexes(dba, modelTable);
                EnsureForeignKeys(dba, modelTable);
            }

            Logger.AddInfo($"Esquema de base de datos verificado. Tablas nuevas:{created} existentes:{existing} con diferencias:{differences}");
        }

        /// <summary>
        /// Construye la DatabaseTable del mapping. GetDatabaseTable ya incluye
        /// columnas, indices y foreign keys del modelo.
        /// </summary>
        private DatabaseTable BuildDatabaseTable(MappingObject mapping)
        {
            return mapping.GetDatabaseTable();
        }

        /// <summary>
        /// Agrega de forma aditiva las columnas del modelo que falten en una tabla existente.
        /// No modifica ni elimina columnas existentes. Es la migracion segura por defecto;
        /// MatchTableStructureEx (destructivo) queda detras de Database:MatchStructure.
        /// </summary>
        private void EnsureMissingColumns(DatabaseAdministrator dba, DatabaseTable modelTable)
        {
            DatabaseTable current;

            try
            {
                current = dba.GetTableStructure(modelTable.TableName);
            }
            catch (Exception ex)
            {
                Logger.AddWarningSubModule(SubModule, $"  No se pudo leer la estructura de '{modelTable.TableName}': {ex.Message}");
                return;
            }

            if (current == null)
                return;

            var existing = new HashSet<string>(
                current.Columns.Select(c => c.ColumnName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var column in modelTable.Columns)
            {
                if (existing.Contains(column.ColumnName))
                    continue;

                if (column.ColumnType == ColumnType.PrimaryKey)
                {
                    Logger.AddWarningSubModule(SubModule,
                        $"  La columna PK '{column.ColumnName}' falta en '{modelTable.TableName}' y no se puede agregar automaticamente");
                    continue;
                }

                try
                {
                    dba.ExecuteRawCommand(BuildAddColumnSql(modelTable.TableName, column));
                    Logger.AddInfo($"  Columna '{modelTable.TableName}.{column.ColumnName}' agregada");
                }
                catch (Exception ex)
                {
                    Logger.AddWarningSubModule(SubModule,
                        $"  No se pudo agregar la columna '{modelTable.TableName}.{column.ColumnName}': {ex.Message}");
                }
            }
        }

        private static string Quote(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private static string BuildAddColumnSql(string tableName, DatabaseColumn column)
        {
            string nullability = column.ColumnType == ColumnType.Null ? "NULL" : "NOT NULL";
            string defaultValue = string.Empty;

            if (column.ColumnType != ColumnType.Null)
            {
                string expression = string.IsNullOrEmpty(column.DefaultValue)
                    ? FallbackDefault(column.ColumnDataType)
                    : ToSqlDefault(column);

                string constraint = Quote($"DF_{tableName}_{column.ColumnName}");
                defaultValue = $" CONSTRAINT {constraint} DEFAULT ({expression})";
            }

            return $"ALTER TABLE {Quote(tableName)} ADD {Quote(column.ColumnName)} {MapColumnType(column)} {nullability}{defaultValue}";
        }

        private static string MapColumnType(DatabaseColumn column)
        {
            return column.ColumnDataType switch
            {
                DataType.Int16 => "smallint",
                DataType.Int32 => "int",
                DataType.Int64 => "bigint",
                DataType.String => column.ColumnStringSize > 0 && column.ColumnStringSize <= 4000
                    ? $"nvarchar({column.ColumnStringSize})"
                    : "nvarchar(max)",
                DataType.Boolean => "bit",
                DataType.Decimal => $"decimal({column.Precision},{column.Scale})",
                DataType.Double => "float",
                DataType.Float => "real",
                DataType.DateTime => "datetime",
                DataType.DateTime2 => "datetime2",
                DataType.Date => "date",
                DataType.Time => "time",
                DataType.Guid => "uniqueidentifier",
                DataType.Byte => "tinyint",
                DataType.Block => "varbinary(max)",
                DataType.Xml => "nvarchar(max)",
                DataType.Geography => "geography",
                DataType.DateTimeOffset => "datetimeoffset",
                _ => "nvarchar(max)"
            };
        }

        private static string ToSqlDefault(DatabaseColumn column)
        {
            string value = column.DefaultValue ?? string.Empty;

            switch (column.ColumnDataType)
            {
                case DataType.String:
                case DataType.Xml:
                case DataType.Geography:
                    if (value == "[EMPTY]" || value == "[EMPTYSTRING]" || value.Length == 0)
                        return "''";
                    return "'" + value.Replace("'", "''") + "'";

                case DataType.Boolean:
                    return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "1" : "0";

                default:
                    return value.Length == 0 ? FallbackDefault(column.ColumnDataType) : value;
            }
        }

        private static string FallbackDefault(DataType dataType)
        {
            return dataType switch
            {
                DataType.Int16 or DataType.Int32 or DataType.Int64 or DataType.Byte => "0",
                DataType.Decimal or DataType.Double or DataType.Float => "0",
                DataType.Boolean => "0",
                DataType.DateTime or DataType.DateTime2 or DataType.Date or DataType.DateTimeOffset => "GETDATE()",
                DataType.Time => "'00:00:00'",
                DataType.Guid => "NEWID()",
                DataType.Block => "0x",
                _ => "''"
            };
        }

        /// <summary>
        /// Compara la tabla existente con el modelo. Devuelve 1 si hay diferencias.
        /// Si Database:MatchStructure=true, intenta igualar la estructura (puede borrar
        /// columnas que no esten en el modelo: usar solo en entornos controlados).
        /// </summary>
        private int ValidateTable(DatabaseAdministrator dba, DatabaseTable modelTable)
        {
            try
            {
                var validation = dba.ValidateTableStructureEx(modelTable);

                switch (validation)
                {
                    case DatabaseAdministrator.TableValidation.Equal:
                        Logger.AddDebugSubModule(SubModule, $"  Tabla '{modelTable.TableName}' igual al modelo");
                        return 0;

                    case DatabaseAdministrator.TableValidation.Compatible:
                        Logger.AddWarningSubModule(SubModule, $"  Tabla '{modelTable.TableName}' es compatible pero no identica al modelo");
                        return 0;

                    default:
                        if (IsMatchStructureEnabled())
                        {
                            Logger.AddWarningSubModule(SubModule, $"  Tabla '{modelTable.TableName}' diferente al modelo, aplicando MatchTableStructure");
                            dba.MatchTableStructureEx(modelTable);
                            return 0;
                        }

                        Logger.AddWarningSubModule(SubModule, $"  Tabla '{modelTable.TableName}' diferente al modelo (no se modifica; Database:MatchStructure=false)");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.AddWarningSubModule(SubModule, $"  No se pudo validar la tabla '{modelTable.TableName}': {ex.Message}");
                return 1;
            }
        }

        private static bool IsMatchStructureEnabled()
        {
            string value = DataCenter.Configuration?["Database:MatchStructure"];
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Crea los indices del modelo (UNIQUE incluidos) que aun no existan.
        /// </summary>
        private void EnsureIndexes(DatabaseAdministrator dba, DatabaseTable table)
        {
            foreach (var index in table.Indexes)
            {
                try
                {
                    if (IndexExists(index.IndexName, table.TableName))
                        continue;

                    dba.CreateIndex(table, index);
                    Logger.AddInfo($"  Indice '{index.IndexName}' creado en '{table.TableName}'");
                }
                catch (Exception ex)
                {
                    Logger.AddWarningSubModule(SubModule,
                        $"  No se pudo crear el indice '{index.IndexName}' en '{table.TableName}': {ex.Message}");
                }
            }
        }

        private bool IndexExists(string indexName, string tableName)
        {
            var result = _dbManager.Tools.ExecuteQueryResultSet(
                "SELECT COUNT(*) AS CNT FROM sys.indexes WHERE name = ? AND object_id = OBJECT_ID(?)",
                indexName, tableName);

            return result != null && result.Rows.Count > 0 && Convert.ToInt32(result.Rows[0].GetObject(0)) > 0;
        }

        /// <summary>
        /// Aplica las foreign keys del modelo. ApplyForeignKeys es idempotente:
        /// compara contra las FK existentes y solo crea las que faltan.
        /// </summary>
        private void EnsureForeignKeys(DatabaseAdministrator dba, DatabaseTable table)
        {
            if (table.ForeignKeys.Count == 0)
                return;

            try
            {
                dba.ApplyForeignKeys(table);
            }
            catch (Exception ex)
            {
                // Tipicamente: filas huerfanas en datos existentes que impiden crear la FK.
                Logger.AddWarningSubModule(SubModule,
                    $"  No se pudieron aplicar las foreign keys de '{table.TableName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Crea una tabla especifica si no existe (utilidad para herramientas).
        /// </summary>
        public void CreateTableIfNotExists(MappingObject mapping)
        {
            var dba = _dbManager.DatabaseAdministrator;
            var dbTable = BuildDatabaseTable(mapping);

            if (!dba.ExistTable(dbTable))
            {
                dba.CreateTable(dbTable);
                EnsureIndexes(dba, dbTable);
                EnsureForeignKeys(dba, dbTable);
                Logger.AddInfo($"Tabla '{dbTable.TableName}' creada desde MappingObject");
            }
        }

        /// <summary>
        /// Elimina todas las tablas del modelo (SOLO para desarrollo/tests).
        /// </summary>
        public void DropAllTables()
        {
            Logger.AddWarningSubModule(SubModule, "Eliminando todas las tablas del esquema GymManager");

            var dba = _dbManager.DatabaseAdministrator;

            // En orden inverso para respetar las foreign keys.
            var tables = ModelGym.MappingObjects.Select(m => m.TableName).Reverse().ToList();

            foreach (var tableName in tables)
            {
                try
                {
                    if (dba.ExistTable(tableName))
                    {
                        dba.DropTable(tableName);
                        Logger.AddDebugSubModule(SubModule, $"Tabla '{tableName}' eliminada");
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddWarningSubModule(SubModule, $"Error al eliminar tabla '{tableName}': {ex.Message}");
                }
            }

            Logger.AddInfo("Todas las tablas del esquema GymManager han sido eliminadas");
        }
    }
}
