using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Classes
{
    /// <summary>
    /// Classes module: tipos de clases grupales.
    /// </summary>
    public class ClassesModule
    {
        private const string SubModule = "CLASSES_MODULE";
        private DatabaseTools _tools;

        public ClassesModule(DatabaseTools tools)
        {
            _tools = tools;
        }

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

        /// <summary>
        /// Create a new class type
        /// </summary>
        public bool CreateClassType(Dictionary<string, object> classData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var typeName = Convert.ToString(classData.GetValueOrDefault("typeName", "")) ?? "";
                var description = Convert.ToString(classData.GetValueOrDefault("description", "")) ?? "";
                var durationMinutes = classData.ContainsKey("durationMinutes") ? Convert.ToInt32(classData["durationMinutes"]) : 60;
                var maxParticipants = classData.ContainsKey("maxParticipants") ? Convert.ToInt32(classData["maxParticipants"]) : 30;

                if (string.IsNullOrEmpty(typeName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del tipo de clase es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long newClassTypeId = _tools.GenerateUniqueID(GYM_CLASS_TYPES.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_CLASS_TYPES
                {
                    ID = newClassTypeId,
                    TYPE_NAME = typeName,
                    DESCRIPTION = description,
                    MAX_CAPACITY = maxParticipants,
                    DURATION_MINUTES = durationMinutes,
                    IS_ACTIVE = true
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Tipo de clase creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el tipo de clase";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateClassType: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Update a class type
        /// </summary>
        public bool UpdateClassType(long classTypeId, Dictionary<string, object> classData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var typeName = Convert.ToString(classData.GetValueOrDefault("typeName", "")) ?? "";
                var description = Convert.ToString(classData.GetValueOrDefault("description", "")) ?? "";
                var durationMinutes = classData.ContainsKey("durationMinutes") ? Convert.ToInt32(classData["durationMinutes"]) : 60;
                var maxParticipants = classData.ContainsKey("maxParticipants") ? Convert.ToInt32(classData["maxParticipants"]) : 30;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_CLASS_TYPES
                {
                    ID = classTypeId,
                    TYPE_NAME = typeName,
                    DESCRIPTION = description,
                    MAX_CAPACITY = maxParticipants,
                    DURATION_MINUTES = durationMinutes
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Tipo de clase actualizado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el tipo de clase";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateClassType: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a class type
        /// </summary>
        public bool DeleteClassType(long classTypeId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_CLASS_TYPES { ID = classTypeId });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Tipo de clase eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el tipo de clase";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteClassType: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active class types
        /// </summary>
        public bool GetClassTypes(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_class_types WHERE is_active = 1 ORDER BY type_name"
                );

                var classTypes = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        classTypes.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["typeName"] = Convert.ToString(GetRowValue(row, result, "type_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["durationMinutes"] = Convert.ToInt32(GetRowValue(row, result, "duration_minutes")),
                            ["maxParticipants"] = Convert.ToInt32(GetRowValue(row, result, "max_capacity"))
                        });
                    }
                }

                response["success"] = true;
                response["data"] = classTypes;
                response["count"] = classTypes.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetClassTypes: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
