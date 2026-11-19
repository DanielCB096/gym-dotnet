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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
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
                long exercises = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_ROUTINE_PLAN_EXERCISES WHERE PLAN_ID = ?", routineId);

                long assignments = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_ROUTINE_ASSIGNMENTS WHERE PLAN_ID = ?", routineId);

                if (exercises > 0 || assignments > 0)
                {
                    response["success"] = false;
                    response["message"] = "La rutina tiene ejercicios o está asignada a socios; no se puede eliminar para no perder el historial. Quita los ejercicios y desactiva las asignaciones activas.";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
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

        // =====================================================================
        // Ejercicios
        // =====================================================================

        public bool GetExercises(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM GYM_EXERCISES ORDER BY EXERCISE_NAME");

                var exercises = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        exercises.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["exerciseName"] = Convert.ToString(GetRowValue(row, result, "exercise_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["muscleGroup"] = Convert.ToString(GetRowValue(row, result, "muscle_group")) ?? "",
                            ["equipment"] = Convert.ToString(GetRowValue(row, result, "equipment")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = exercises;
                response["count"] = exercises.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetExercises: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateExercise(Dictionary<string, object> exerciseData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var exerciseName = Convert.ToString(exerciseData.GetValueOrDefault("exerciseName", "")) ?? "";

                if (string.IsNullOrWhiteSpace(exerciseName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del ejercicio es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_EXERCISES.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_EXERCISES
                {
                    ID = id,
                    EXERCISE_NAME = exerciseName.Trim(),
                    DESCRIPTION = Convert.ToString(exerciseData.GetValueOrDefault("description", "")) ?? "",
                    MUSCLE_GROUP = Convert.ToString(exerciseData.GetValueOrDefault("muscleGroup", "")) ?? "",
                    EQUIPMENT = Convert.ToString(exerciseData.GetValueOrDefault("equipment", "")) ?? ""
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el ejercicio";
                    return false;
                }

                response["success"] = true;
                response["exerciseId"] = id;
                response["message"] = "Ejercicio creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdateExercise(long exerciseId, Dictionary<string, object> exerciseData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var exerciseName = Convert.ToString(exerciseData.GetValueOrDefault("exerciseName", "")) ?? "";

                if (string.IsNullOrWhiteSpace(exerciseName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del ejercicio es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_EXERCISES
                {
                    ID = exerciseId,
                    EXERCISE_NAME = exerciseName.Trim(),
                    DESCRIPTION = Convert.ToString(exerciseData.GetValueOrDefault("description", "")) ?? "",
                    MUSCLE_GROUP = Convert.ToString(exerciseData.GetValueOrDefault("muscleGroup", "")) ?? "",
                    EQUIPMENT = Convert.ToString(exerciseData.GetValueOrDefault("equipment", "")) ?? ""
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el ejercicio";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Ejercicio actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteExercise(long exerciseId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long planUses = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_ROUTINE_PLAN_EXERCISES WHERE EXERCISE_ID = ?", exerciseId);

                long workoutUses = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_WORKOUTS WHERE EXERCISE_ID = ?", exerciseId);

                if (planUses > 0 || workoutUses > 0)
                {
                    response["success"] = false;
                    response["message"] = "El ejercicio está en uso en rutinas o entrenamientos y no se puede eliminar";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_EXERCISES { ID = exerciseId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el ejercicio";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Ejercicio eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Ejercicios por plan
        // =====================================================================

        public bool GetPlanExercises(long planId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT pe.*, e.EXERCISE_NAME, e.MUSCLE_GROUP, e.EQUIPMENT " +
                    "FROM GYM_ROUTINE_PLAN_EXERCISES pe " +
                    "INNER JOIN GYM_EXERCISES e ON e.ID = pe.EXERCISE_ID " +
                    "WHERE pe.PLAN_ID = ? " +
                    "ORDER BY pe.DAY_NUMBER, pe.SORT_ORDER, pe.ID",
                    planId);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["planId"] = Convert.ToInt64(GetRowValue(row, result, "plan_id")),
                            ["exerciseId"] = Convert.ToInt64(GetRowValue(row, result, "exercise_id")),
                            ["exerciseName"] = Convert.ToString(GetRowValue(row, result, "exercise_name")) ?? "",
                            ["muscleGroup"] = Convert.ToString(GetRowValue(row, result, "muscle_group")) ?? "",
                            ["equipment"] = Convert.ToString(GetRowValue(row, result, "equipment")) ?? "",
                            ["dayNumber"] = Convert.ToInt32(GetRowValue(row, result, "day_number")),
                            ["sortOrder"] = Convert.ToInt32(GetRowValue(row, result, "sort_order")),
                            ["sets"] = GetRowValue(row, result, "sets") != null ? Convert.ToInt32(GetRowValue(row, result, "sets")) : (int?)null,
                            ["reps"] = GetRowValue(row, result, "reps") != null ? Convert.ToInt32(GetRowValue(row, result, "reps")) : (int?)null,
                            ["notes"] = Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = items;
                response["count"] = items.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetPlanExercises: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool AddPlanExercise(long planId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long exerciseId = data.ContainsKey("exerciseId") ? Convert.ToInt64(data["exerciseId"]) : 0L;
                int dayNumber = data.ContainsKey("dayNumber") ? Convert.ToInt32(data["dayNumber"]) : 1;
                int sortOrder = data.ContainsKey("sortOrder") ? Convert.ToInt32(data["sortOrder"]) : 1;

                if (exerciseId <= 0 || !ExerciseExists(exerciseId))
                {
                    response["success"] = false;
                    response["message"] = "El ejercicio no existe";
                    return false;
                }

                if (!PlanExists(planId))
                {
                    response["success"] = false;
                    response["message"] = "La rutina no existe";
                    return false;
                }

                if (dayNumber < 1) dayNumber = 1;
                if (sortOrder < 1) sortOrder = 1;

                long duplicates = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_ROUTINE_PLAN_EXERCISES WHERE PLAN_ID = ? AND EXERCISE_ID = ? AND DAY_NUMBER = ?",
                    planId, exerciseId, dayNumber);

                if (duplicates > 0)
                {
                    response["success"] = false;
                    response["message"] = "El ejercicio ya está agregado en ese día de la rutina";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_ROUTINE_PLAN_EXERCISES.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_ROUTINE_PLAN_EXERCISES
                {
                    ID = id,
                    PLAN_ID = planId,
                    EXERCISE_ID = exerciseId,
                    DAY_NUMBER = dayNumber,
                    SORT_ORDER = sortOrder,
                    SETS = data.ContainsKey("sets") ? Convert.ToInt32(data["sets"]) : (int?)null,
                    REPS = data.ContainsKey("reps") ? Convert.ToInt32(data["reps"]) : (int?)null,
                    NOTES = Convert.ToString(data.GetValueOrDefault("notes", "")) ?? ""
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al agregar el ejercicio a la rutina";
                    return false;
                }

                response["success"] = true;
                response["planExerciseId"] = id;
                response["message"] = "Ejercicio agregado a la rutina";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en AddPlanExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdatePlanExercise(long planExerciseId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                var changes = new GYM_ROUTINE_PLAN_EXERCISES { ID = planExerciseId };

                if (data.ContainsKey("dayNumber")) changes.DAY_NUMBER = Math.Max(1, Convert.ToInt32(data["dayNumber"]));
                if (data.ContainsKey("sortOrder")) changes.SORT_ORDER = Math.Max(1, Convert.ToInt32(data["sortOrder"]));
                if (data.ContainsKey("sets")) changes.SETS = Convert.ToInt32(data["sets"]);
                if (data.ContainsKey("reps")) changes.REPS = Convert.ToInt32(data["reps"]);
                if (data.ContainsKey("notes")) changes.NOTES = Convert.ToString(data["notes"]) ?? "";

                session.UpdateObjectChanges(changes);

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el ejercicio de la rutina";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Ejercicio de la rutina actualizado";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdatePlanExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeletePlanExercise(long planExerciseId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_ROUTINE_PLAN_EXERCISES { ID = planExerciseId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al quitar el ejercicio de la rutina";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Ejercicio quitado de la rutina";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeletePlanExercise: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Asignaciones de rutinas a socios
        // =====================================================================

        public bool GetAssignments(out Dictionary<string, object> response, long? memberId = null)
        {
            response = new Dictionary<string, object>();

            try
            {
                string where = memberId.HasValue ? "WHERE a.MEMBER_ID = ?" : "";
                var parameters = memberId.HasValue ? new object[] { memberId.Value } : Array.Empty<object>();

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT a.*, m.FIRST_NAME, m.LAST_NAME, p.PLAN_NAME " +
                    "FROM GYM_ROUTINE_ASSIGNMENTS a " +
                    "INNER JOIN GYM_MEMBERS m ON m.ID = a.MEMBER_ID " +
                    "INNER JOIN GYM_ROUTINE_PLANS p ON p.ID = a.PLAN_ID " +
                    $"{where} ORDER BY a.IS_ACTIVE DESC, a.ASSIGNED_AT DESC",
                    parameters);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["planId"] = Convert.ToInt64(GetRowValue(row, result, "plan_id")),
                            ["planName"] = Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                            ["assignedBy"] = GetRowValue(row, result, "assigned_by") != null ? Convert.ToInt64(GetRowValue(row, result, "assigned_by")) : (long?)null,
                            ["assignedAt"] = GetRowValue(row, result, "assigned_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "assigned_at")) : (DateTime?)null,
                            ["isActive"] = Convert.ToBoolean(GetRowValue(row, result, "is_active"))
                        });
                    }
                }

                response["success"] = true;
                response["data"] = items;
                response["count"] = items.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetAssignments: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool AssignPlan(long memberId, long planId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (!MemberIsActive(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe o no está activo";
                    return false;
                }

                if (!PlanExists(planId))
                {
                    response["success"] = false;
                    response["message"] = "La rutina no existe";
                    return false;
                }

                var activeAssignments = _tools.ExecuteQueryResultSet(
                    "SELECT ID FROM GYM_ROUTINE_ASSIGNMENTS WHERE MEMBER_ID = ? AND IS_ACTIVE = 1",
                    memberId);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                if (activeAssignments != null)
                {
                    foreach (Row row in activeAssignments.Rows)
                    {
                        session.UpdateObjectChanges(new GYM_ROUTINE_ASSIGNMENTS
                        {
                            ID = Convert.ToInt64(GetRowValue(row, activeAssignments, "id")),
                            IS_ACTIVE = false
                        });
                    }
                }

                long id = _tools.GenerateUniqueID(GYM_ROUTINE_ASSIGNMENTS.DatabaseMappingObject, 1).GetInt64UniqueId();
                long auditUser = CurrentUser.ForAudit;

                session.InsertObjectWithID(new GYM_ROUTINE_ASSIGNMENTS
                {
                    ID = id,
                    MEMBER_ID = memberId,
                    PLAN_ID = planId,
                    ASSIGNED_BY = auditUser > 0 ? auditUser : (long?)null,
                    ASSIGNED_AT = DateTime.Now,
                    IS_ACTIVE = true
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al asignar la rutina";
                    return false;
                }

                response["success"] = true;
                response["assignmentId"] = id;
                response["message"] = "Rutina asignada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en AssignPlan: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeactivateAssignment(long assignmentId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_ROUTINE_ASSIGNMENTS
                {
                    ID = assignmentId,
                    IS_ACTIVE = false
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al desactivar la asignación";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Asignación desactivada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeactivateAssignment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Entrenamientos
        // =====================================================================

        public bool GetWorkouts(out Dictionary<string, object> response, long? memberId = null, DateTime? from = null, DateTime? to = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (memberId.HasValue)
                {
                    whereParts.Add("w.MEMBER_ID = ?");
                    parameters.Add(memberId.Value);
                }

                if (from.HasValue)
                {
                    whereParts.Add("w.WORKOUT_DATE >= ?");
                    parameters.Add(from.Value);
                }

                if (to.HasValue)
                {
                    whereParts.Add("w.WORKOUT_DATE <= ?");
                    parameters.Add(to.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM GYM_WORKOUTS w {whereClause}", parameters.ToArray());

                var queryParameters = new List<object>(parameters) { offset, limit };

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT w.*, m.FIRST_NAME, m.LAST_NAME, e.EXERCISE_NAME " +
                    "FROM GYM_WORKOUTS w " +
                    "INNER JOIN GYM_MEMBERS m ON m.ID = w.MEMBER_ID " +
                    "INNER JOIN GYM_EXERCISES e ON e.ID = w.EXERCISE_ID " +
                    $"{whereClause} ORDER BY w.WORKOUT_DATE DESC, w.ID DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    queryParameters.ToArray());

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["exerciseId"] = Convert.ToInt64(GetRowValue(row, result, "exercise_id")),
                            ["exerciseName"] = Convert.ToString(GetRowValue(row, result, "exercise_name")) ?? "",
                            ["sets"] = Convert.ToInt32(GetRowValue(row, result, "sets")),
                            ["reps"] = Convert.ToInt32(GetRowValue(row, result, "reps")),
                            ["weight"] = GetRowValue(row, result, "weight") != null ? Convert.ToDecimal(GetRowValue(row, result, "weight")) : (decimal?)null,
                            ["durationMinutes"] = GetRowValue(row, result, "duration_minutes") != null ? Convert.ToInt32(GetRowValue(row, result, "duration_minutes")) : (int?)null,
                            ["workoutDate"] = GetRowValue(row, result, "workout_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "workout_date")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = items;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = totalCount,
                    ["totalPages"] = totalPages,
                    ["hasNext"] = page < totalPages,
                    ["hasPrev"] = page > 1
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetWorkouts: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateWorkout(Dictionary<string, object> workoutData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long memberId = workoutData.ContainsKey("memberId") ? Convert.ToInt64(workoutData["memberId"]) : 0L;
                long exerciseId = workoutData.ContainsKey("exerciseId") ? Convert.ToInt64(workoutData["exerciseId"]) : 0L;
                int sets = workoutData.ContainsKey("sets") ? Convert.ToInt32(workoutData["sets"]) : 0;
                int reps = workoutData.ContainsKey("reps") ? Convert.ToInt32(workoutData["reps"]) : 0;

                if (!MemberExists(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (!ExerciseExists(exerciseId))
                {
                    response["success"] = false;
                    response["message"] = "El ejercicio no existe";
                    return false;
                }

                if (sets <= 0 || reps <= 0)
                {
                    response["success"] = false;
                    response["message"] = "Las series y repeticiones deben ser mayores a cero";
                    return false;
                }

                DateTime workoutDate = DateTime.TryParse(Convert.ToString(workoutData.GetValueOrDefault("workoutDate", "")), out DateTime parsed)
                    ? parsed
                    : DateTime.Now;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_WORKOUTS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_WORKOUTS
                {
                    ID = id,
                    MEMBER_ID = memberId,
                    EXERCISE_ID = exerciseId,
                    SETS = sets,
                    REPS = reps,
                    WEIGHT = workoutData.ContainsKey("weight") ? Convert.ToDecimal(workoutData["weight"]) : (decimal?)null,
                    DURATION_MINUTES = workoutData.ContainsKey("durationMinutes") ? Convert.ToInt32(workoutData["durationMinutes"]) : (int?)null,
                    WORKOUT_DATE = workoutDate
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar el entrenamiento";
                    return false;
                }

                response["success"] = true;
                response["workoutId"] = id;
                response["message"] = "Entrenamiento registrado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateWorkout: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteWorkout(long workoutId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_WORKOUTS { ID = workoutId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el entrenamiento";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Entrenamiento eliminado";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteWorkout: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Medidas corporales
        // =====================================================================

        public bool GetMeasurements(out Dictionary<string, object> response, long? memberId = null, string? measurementType = null)
        {
            response = new Dictionary<string, object>();

            try
            {
                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (memberId.HasValue)
                {
                    whereParts.Add("ms.MEMBER_ID = ?");
                    parameters.Add(memberId.Value);
                }

                if (!string.IsNullOrWhiteSpace(measurementType))
                {
                    whereParts.Add("ms.MEASUREMENT_TYPE = ?");
                    parameters.Add(measurementType);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT ms.*, m.FIRST_NAME, m.LAST_NAME " +
                    "FROM GYM_MEASUREMENTS ms " +
                    "INNER JOIN GYM_MEMBERS m ON m.ID = ms.MEMBER_ID " +
                    $"{whereClause} ORDER BY ms.MEASURED_AT DESC, ms.ID DESC",
                    parameters.ToArray());

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["measurementType"] = Convert.ToString(GetRowValue(row, result, "measurement_type")) ?? "",
                            ["value"] = Convert.ToDecimal(GetRowValue(row, result, "value")),
                            ["unit"] = Convert.ToString(GetRowValue(row, result, "unit")) ?? "",
                            ["measuredAt"] = GetRowValue(row, result, "measured_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "measured_at")) : (DateTime?)null,
                            ["measuredBy"] = GetRowValue(row, result, "measured_by") != null ? Convert.ToInt64(GetRowValue(row, result, "measured_by")) : (long?)null
                        });
                    }
                }

                response["success"] = true;
                response["data"] = items;
                response["count"] = items.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetMeasurements: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateMeasurement(Dictionary<string, object> measurementData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long memberId = measurementData.ContainsKey("memberId") ? Convert.ToInt64(measurementData["memberId"]) : 0L;
                string measurementType = Convert.ToString(measurementData.GetValueOrDefault("measurementType", "")) ?? "";
                string unit = Convert.ToString(measurementData.GetValueOrDefault("unit", "")) ?? "";

                if (!MemberExists(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(measurementType) || string.IsNullOrWhiteSpace(unit))
                {
                    response["success"] = false;
                    response["message"] = "El tipo y la unidad de la medida son requeridos";
                    return false;
                }

                if (!measurementData.ContainsKey("value") || !decimal.TryParse(Convert.ToString(measurementData["value"]), out decimal value))
                {
                    response["success"] = false;
                    response["message"] = "El valor de la medida es requerido";
                    return false;
                }

                DateTime measuredAt = DateTime.TryParse(Convert.ToString(measurementData.GetValueOrDefault("measuredAt", "")), out DateTime parsed)
                    ? parsed
                    : DateTime.Now;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_MEASUREMENTS.DatabaseMappingObject, 1).GetInt64UniqueId();
                long auditUser = CurrentUser.ForAudit;

                session.InsertObjectWithID(new GYM_MEASUREMENTS
                {
                    ID = id,
                    MEMBER_ID = memberId,
                    MEASUREMENT_TYPE = measurementType.Trim(),
                    VALUE = value,
                    UNIT = unit.Trim(),
                    MEASURED_AT = measuredAt,
                    MEASURED_BY = auditUser > 0 ? auditUser : (long?)null
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la medida";
                    return false;
                }

                response["success"] = true;
                response["measurementId"] = id;
                response["message"] = "Medida registrada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateMeasurement: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteMeasurement(long measurementId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_MEASUREMENTS { ID = measurementId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la medida";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Medida eliminada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteMeasurement: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private bool PlanExists(long planId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_ROUTINE_PLANS WHERE ID = ?", planId) > 0;
        }

        private bool ExerciseExists(long exerciseId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_EXERCISES WHERE ID = ?", exerciseId) > 0;
        }

        private bool MemberExists(long memberId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = ?", memberId) > 0;
        }

        private bool MemberIsActive(long memberId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = ? AND STATUS = 'active'", memberId) > 0;
        }
    }
}
