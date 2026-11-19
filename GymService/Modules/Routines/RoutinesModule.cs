using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Routines
{
    /// <summary>
    /// Routines module: planes de rutinas de entrenamiento.
    /// </summary>
    public class RoutinesModule
    {
        private const string SubModule = "ROUTINES_MODULE";
        private DatabaseTools _tools;

        public RoutinesModule(DatabaseTools tools)
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
        /// Create a new routine plan
        /// </summary>
        public bool CreateRoutinePlan(Dictionary<string, object> routineData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var planName = Convert.ToString(routineData.GetValueOrDefault("planName", "")) ?? "";
                var description = Convert.ToString(routineData.GetValueOrDefault("description", "")) ?? "";
                var difficulty = Convert.ToString(routineData.GetValueOrDefault("difficulty", "beginner")) ?? "beginner";
                var durationMinutes = routineData.ContainsKey("durationMinutes") ? Convert.ToInt32(routineData["durationMinutes"]) : 45;

                if (string.IsNullOrEmpty(planName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre de la rutina es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule);
                long newRoutineId = _tools.GenerateUniqueID(GYM_ROUTINE_PLANS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_ROUTINE_PLANS
                {
                    ID = newRoutineId,
                    PLAN_NAME = planName,
                    DESCRIPTION = description,
                    DIFFICULTY = difficulty,
                    DURATION_MINUTES = durationMinutes,
                    IS_ACTIVE = true
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Rutina creada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la rutina";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateRoutinePlan: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Update a routine plan
        /// </summary>
        public bool UpdateRoutinePlan(long routineId, Dictionary<string, object> routineData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var planName = Convert.ToString(routineData.GetValueOrDefault("planName", "")) ?? "";
                var description = Convert.ToString(routineData.GetValueOrDefault("description", "")) ?? "";
                var difficulty = Convert.ToString(routineData.GetValueOrDefault("difficulty", "beginner")) ?? "beginner";
                var durationMinutes = routineData.ContainsKey("durationMinutes") ? Convert.ToInt32(routineData["durationMinutes"]) : 45;

                var session = _tools.CreateSession(SubModule);
                session.UpdateObjectChanges(new GYM_ROUTINE_PLANS
                {
                    ID = routineId,
                    PLAN_NAME = planName,
                    DESCRIPTION = description,
                    DIFFICULTY = difficulty,
                    DURATION_MINUTES = durationMinutes
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Rutina actualizada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la rutina";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateRoutinePlan: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a routine plan
        /// </summary>
        public bool DeleteRoutinePlan(long routineId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.DeleteObject(new GYM_ROUTINE_PLANS { ID = routineId });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Rutina eliminada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la rutina";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteRoutinePlan: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get routine plans
        /// </summary>
        public bool GetRoutinePlans(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_routine_plans WHERE is_active = 1 ORDER BY plan_name"
                );

                var routines = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        routines.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["planName"] = Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["difficulty"] = Convert.ToString(GetRowValue(row, result, "difficulty")) ?? "beginner",
                            ["durationMinutes"] = Convert.ToInt32(GetRowValue(row, result, "duration_minutes"))
                        });
                    }
                }

                response["success"] = true;
                response["data"] = routines;
                response["count"] = routines.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetRoutinePlans: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
