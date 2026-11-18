using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxDatabase.Administration;
using AxObjectOrientedDatabase;
using GymService.Models;

namespace GymService.Services
{
    /// <summary>
    /// Manages database schema creation and validation.
    /// Converts ModelGym TableDefinitions to framework DatabaseTable objects
    /// and uses DatabaseAdministrator to create them on SQL Server.
    /// </summary>
    public class DatabaseService
    {
        private OODatabaseManager _dbManager;

        public DatabaseService(OODatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        /// <summary>
        /// Initialize database schema - create all tables defined in ModelGym
        /// </summary>
        public void InitializeSchema()
        {
            Logger.AddInfo("========================================");
            Logger.AddInfo("Inicializando esquema de base de datos GymManager");
            Logger.AddInfo("========================================");

            var dba = _dbManager.DatabaseAdministrator;

            // Create tables first (without foreign keys)
            int tableCount = 0;
            foreach (var tableDef in ModelGym.TableDefinitions)
            {
                var dbTable = ConvertToDatabaseTable(tableDef);
                CreateOrUpdateTable(dba, dbTable);
                tableCount++;
                Logger.AddInfo($"  Tabla '{tableDef.TableName}' creada/actualizada");
            }

            Logger.AddInfo($"Se procesaron {tableCount} tablas");

            // Add foreign keys after all tables exist
            int fkCount = 0;
            foreach (var fkDef in ModelGym.ForeignKeyDefinitions)
            {
                AddForeignKey(dba, fkDef);
                fkCount++;
            }

            Logger.AddInfo($"Se agregaron {fkCount} foreign keys");
            Logger.AddInfo("Esquema de base de datos inicializado correctamente");
        }

        /// <summary>
        /// Create or recreate a table using DatabaseAdministrator
        /// </summary>
        private void CreateOrUpdateTable(DatabaseAdministrator dba, DatabaseTable table)
        {
            try
            {
                if (dba.ExistTable(table.TableName))
                {
                    Logger.AddDebugSubModule("DatabaseService", $"Tabla '{table.TableName}' ya existe, recreando...");
                    dba.DropTable(table.TableName);
                }

                dba.CreateTable(table);
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("DatabaseService", $"Error al crear tabla '{table.TableName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convert a ModelGym TableDefinition to a framework DatabaseTable
        /// </summary>
        private DatabaseTable ConvertToDatabaseTable(TableDefinition tableDef)
        {
            var dbTable = new DatabaseTable(tableDef.TableName);

            foreach (var colDef in tableDef.Columns)
            {
                var dbColumn = ConvertToDatabaseColumn(colDef);
                dbTable.Columns.Add(dbColumn);
            }

            // Add composite primary key if defined
            // Note: single-column PK is handled by ColumnType.PrimaryKey on the column itself
            // Composite PKs are added as a Unique index (framework doesn't have IndexTypes.PrimaryKey)
            if (tableDef.PrimaryKey != null && tableDef.PrimaryKey.Count > 0)
            {
                var index = new DatabaseIndex($"PK_{tableDef.TableName}", IndexTypes.Unique);
                int seq = 1;
                foreach (var pkCol in tableDef.PrimaryKey)
                {
                    var idxCol = new DatabaseIndexColumn(pkCol, seq);
                    index.IndexColumns.Add(idxCol);
                    seq++;
                }
                dbTable.Indexes.Add(index);
            }

            return dbTable;
        }

        /// <summary>
        /// Convert a ModelGym ColumnDefinition to a framework DatabaseColumn
        /// </summary>
        private DatabaseColumn ConvertToDatabaseColumn(ColumnDefinition colDef)
        {
            // Determine column type
            ColumnType colType = ColumnType.NotNull;
            if (colDef.Nullable)
            {
                colType = ColumnType.Null;
            }
            if (colDef.PrimaryKey)
            {
                colType = ColumnType.PrimaryKey;
            }

            // Create column with appropriate size
            DatabaseColumn dbColumn;
            if (colDef.DataType == DataType.Decimal && colDef.Precision > 0)
            {
                dbColumn = new DatabaseColumn(colDef.Name, colDef.DataType, colType, colDef.Precision, colDef.Scale);
            }
            else if (colDef.Length > 0)
            {
                dbColumn = new DatabaseColumn(colDef.Name, colDef.DataType, colDef.Length, colType);
            }
            else
            {
                dbColumn = new DatabaseColumn(colDef.Name, colDef.DataType, colType);
            }

            // Set default value if specified
            if (!string.IsNullOrEmpty(colDef.DefaultValue))
            {
                dbColumn.DefaultValue = colDef.DefaultValue;
            }

            // Set properties for Identity and Unique
            var properties = new List<string>();
            if (colDef.Identity)
            {
                properties.Add("IDENTITY");
            }
            if (colDef.Unique && !colDef.PrimaryKey)
            {
                properties.Add("UNIQUE");
            }
            if (properties.Count > 0)
            {
                dbColumn.Properties = string.Join(";", properties);
            }

            return dbColumn;
        }

        /// <summary>
        /// Add a foreign key to an existing table
        /// </summary>
        private void AddForeignKey(DatabaseAdministrator dba, ForeignKeyDefinition fkDef)
        {
            try
            {
                var dbTable = dba.GetTableStructure(fkDef.Table);
                if (dbTable == null)
                {
                    Logger.AddWarningSubModule("DatabaseService", $"No se encontró la tabla '{fkDef.Table}' para agregar foreign key");
                    return;
                }

                // Check if foreign key already exists
                bool exists = false;
                foreach (var existingFk in dbTable.ForeignKeys)
                {
                    if (existingFk.ExternalTable.ToUpper() == fkDef.ReferencesTable.ToUpper())
                    {
                        foreach (var col in existingFk.ForeignKeyColumns)
                        {
                            if (col.LocalColumnName.ToUpper() == fkDef.Column.ToUpper() &&
                                col.ExternalColumnName.ToUpper() == fkDef.ReferencesColumn.ToUpper())
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (exists) break;
                    }
                }

                if (!exists)
                {
                    var fk = new DatabaseForeignKey(fkDef.ReferencesTable);
                    fk.ForeignKeyName = $"FK_{fkDef.Table}_{fkDef.Column}";
                    fk.ForeignKeyColumns.Add(new DatabaseForeignKeyColumn(fkDef.Column, fkDef.ReferencesColumn));
                    dbTable.ForeignKeys.Add(fk);

                    dba.CreateTable(dbTable);
                    Logger.AddDebugSubModule("DatabaseService", $"Foreign key '{fk.ForeignKeyName}' agregada a '{fkDef.Table}'");
                }
            }
            catch (Exception ex)
            {
                Logger.AddWarningSubModule("DatabaseService", $"Error al agregar foreign key '{fkDef.Table}.{fkDef.Column}': {ex.Message}");
            }
        }

        /// <summary>
        /// Create a specific table if it doesn't exist
        /// </summary>
        public void CreateTableIfNotExists(MappingObject mapping)
        {
            var dba = _dbManager.DatabaseAdministrator;
            var dbTable = mapping.GetDatabaseTable();

            if (!dba.ExistTable(dbTable))
            {
                dba.CreateTable(dbTable);
                Logger.AddInfo($"Tabla '{dbTable.TableName}' creada desde MappingObject");
            }
        }

        /// <summary>
        /// Drop all tables (for testing/development only)
        /// </summary>
        public void DropAllTables()
        {
            Logger.AddWarningSubModule("DatabaseService", "Eliminando todas las tablas del esquema GymManager");

            var dba = _dbManager.DatabaseAdministrator;

            // Drop in reverse order to respect foreign key constraints
            var tables = ModelGym.TableDefinitions.Select(t => t.TableName).Reverse().ToList();

            foreach (var tableName in tables)
            {
                try
                {
                    if (dba.ExistTable(tableName))
                    {
                        dba.DropTable(tableName);
                        Logger.AddDebugSubModule("DatabaseService", $"Tabla '{tableName}' eliminada");
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddWarningSubModule("DatabaseService", $"Error al eliminar tabla '{tableName}': {ex.Message}");
                }
            }

            Logger.AddInfo("Todas las tablas del esquema GymManager han sido eliminadas");
        }
    }
}
