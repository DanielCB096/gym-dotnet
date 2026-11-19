using System;
using System.Collections.Concurrent;
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

        // =====================================================================
        // Sesiones de clase
        // =====================================================================

        // Serializa las reservas de una misma sesion dentro de esta instancia para
        // no sobrepasar el cupo con peticiones concurrentes.
        private static readonly ConcurrentDictionary<long, object> ReservationLocks = new();

        private static object LockFor(long sessionId)
        {
            return ReservationLocks.GetOrAdd(sessionId, _ => new object());
        }

        public bool GetClassSessions(out Dictionary<string, object> response, DateTime? from = null, DateTime? to = null, long? classTypeId = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (from.HasValue)
                {
                    whereParts.Add("s.START_TIME >= ?");
                    parameters.Add(from.Value);
                }

                if (to.HasValue)
                {
                    whereParts.Add("s.START_TIME <= ?");
                    parameters.Add(to.Value);
                }

                if (classTypeId.HasValue)
                {
                    whereParts.Add("s.CLASS_TYPE_ID = ?");
                    parameters.Add(classTypeId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM GYM_CLASS_SESSIONS s {whereClause}",
                    parameters.ToArray());

                var sql = "SELECT s.*, ct.TYPE_NAME, ct.MAX_CAPACITY, st.EMPLOYEE_NAME AS INSTRUCTOR_NAME " +
                          "FROM GYM_CLASS_SESSIONS s " +
                          "INNER JOIN GYM_CLASS_TYPES ct ON ct.ID = s.CLASS_TYPE_ID " +
                          "LEFT JOIN GYM_STAFF st ON st.ID = s.INSTRUCTOR_ID " +
                          $"{whereClause} ORDER BY s.START_TIME OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var queryParameters = new List<object>(parameters) { offset, limit };
                var result = _tools.ExecuteQueryResultSet(sql, queryParameters.ToArray());

                var sessions = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        int capacity = Convert.ToInt32(GetRowValue(row, result, "max_capacity"));
                        int reserved = Convert.ToInt32(GetRowValue(row, result, "spots_reserved"));
                        object instructorValue = GetRowValue(row, result, "instructor_id");

                        sessions.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["classTypeId"] = Convert.ToInt64(GetRowValue(row, result, "class_type_id")),
                            ["className"] = Convert.ToString(GetRowValue(row, result, "type_name")) ?? "",
                            ["instructorId"] = instructorValue == null || instructorValue == DBNull.Value ? (long?)null : Convert.ToInt64(instructorValue),
                            ["instructorName"] = Convert.ToString(GetRowValue(row, result, "instructor_name")) ?? "",
                            ["room"] = Convert.ToString(GetRowValue(row, result, "room")) ?? "",
                            ["startTime"] = GetRowValue(row, result, "start_time") != null ? Convert.ToDateTime(GetRowValue(row, result, "start_time")) : (DateTime?)null,
                            ["endTime"] = GetRowValue(row, result, "end_time") != null ? Convert.ToDateTime(GetRowValue(row, result, "end_time")) : (DateTime?)null,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["capacity"] = capacity,
                            ["reserved"] = reserved,
                            ["available"] = Math.Max(0, capacity - reserved)
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = sessions;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetClassSessions: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateClassSession(Dictionary<string, object> sessionData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var classTypeId = sessionData.ContainsKey("classTypeId") ? Convert.ToInt64(sessionData["classTypeId"]) : 0L;
                var instructorId = sessionData.ContainsKey("instructorId") ? Convert.ToInt64(sessionData["instructorId"]) : 0L;
                var room = Convert.ToString(sessionData.GetValueOrDefault("room", "")) ?? "";

                if (classTypeId <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El tipo de clase es requerido";
                    return false;
                }

                if (!DateTime.TryParse(Convert.ToString(sessionData.GetValueOrDefault("startTime", "")), out DateTime startTime))
                {
                    response["success"] = false;
                    response["message"] = "La fecha/hora de inicio es requerida";
                    return false;
                }

                var classType = _tools.ExecuteQueryResultSet(
                    "SELECT MAX_CAPACITY, DURATION_MINUTES, IS_ACTIVE FROM GYM_CLASS_TYPES WHERE ID = ?",
                    classTypeId);

                if (classType == null || classType.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "El tipo de clase no existe";
                    return false;
                }

                if (!Convert.ToBoolean(GetRowValue(classType.Rows[0], classType, "is_active")))
                {
                    response["success"] = false;
                    response["message"] = "El tipo de clase está inactivo";
                    return false;
                }

                DateTime endTime;
                if (!DateTime.TryParse(Convert.ToString(sessionData.GetValueOrDefault("endTime", "")), out endTime))
                {
                    int duration = Convert.ToInt32(GetRowValue(classType.Rows[0], classType, "duration_minutes"));
                    endTime = startTime.AddMinutes(duration > 0 ? duration : 60);
                }

                if (endTime <= startTime)
                {
                    response["success"] = false;
                    response["message"] = "La hora de fin debe ser posterior a la de inicio";
                    return false;
                }

                if (instructorId > 0 && !ExistsStaff(instructorId))
                {
                    response["success"] = false;
                    response["message"] = "El instructor seleccionado no existe";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long newSessionId = _tools.GenerateUniqueID(GYM_CLASS_SESSIONS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_CLASS_SESSIONS
                {
                    ID = newSessionId,
                    CLASS_TYPE_ID = classTypeId,
                    INSTRUCTOR_ID = instructorId > 0 ? instructorId : (long?)null,
                    ROOM = room,
                    START_TIME = startTime,
                    END_TIME = endTime,
                    STATUS = "scheduled",
                    SPOTS_RESERVED = 0
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la sesión de clase";
                    return false;
                }

                response["success"] = true;
                response["sessionId"] = newSessionId;
                response["message"] = "Sesión de clase creada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateClassSession: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdateClassSession(long sessionId, Dictionary<string, object> sessionData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = _tools.ExecuteQueryResultSet(
                    "SELECT START_TIME, END_TIME, ROOM, INSTRUCTOR_ID, STATUS FROM GYM_CLASS_SESSIONS WHERE ID = ?",
                    sessionId);

                if (current == null || current.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La sesión de clase no existe";
                    return false;
                }

                var row = current.Rows[0];

                if (string.Equals(Convert.ToString(GetRowValue(row, current, "status")), "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "No se puede editar una sesión cancelada";
                    return false;
                }

                DateTime startTime = DateTime.TryParse(Convert.ToString(sessionData.GetValueOrDefault("startTime", "")), out DateTime parsedStart)
                    ? parsedStart
                    : Convert.ToDateTime(GetRowValue(row, current, "start_time"));

                DateTime endTime = DateTime.TryParse(Convert.ToString(sessionData.GetValueOrDefault("endTime", "")), out DateTime parsedEnd)
                    ? parsedEnd
                    : Convert.ToDateTime(GetRowValue(row, current, "end_time"));

                if (endTime <= startTime)
                {
                    response["success"] = false;
                    response["message"] = "La hora de fin debe ser posterior a la de inicio";
                    return false;
                }

                string room = sessionData.ContainsKey("room")
                    ? Convert.ToString(sessionData["room"]) ?? ""
                    : Convert.ToString(GetRowValue(row, current, "room")) ?? "";

                long instructorId = sessionData.ContainsKey("instructorId")
                    ? Convert.ToInt64(sessionData["instructorId"])
                    : 0L;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_CLASS_SESSIONS
                {
                    ID = sessionId,
                    ROOM = room,
                    START_TIME = startTime,
                    END_TIME = endTime,
                    INSTRUCTOR_ID = instructorId > 0 ? instructorId : (long?)null
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la sesión de clase";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Sesión de clase actualizada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateClassSession: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CancelClassSession(long sessionId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = _tools.ExecuteQueryResultSet(
                    "SELECT STATUS FROM GYM_CLASS_SESSIONS WHERE ID = ?", sessionId);

                if (current == null || current.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La sesión de clase no existe";
                    return false;
                }

                if (string.Equals(Convert.ToString(GetRowValue(current.Rows[0], current, "status")), "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = true;
                    response["message"] = "La sesión ya estaba cancelada";
                    return true;
                }

                var reservations = _tools.ExecuteQueryResultSet(
                    "SELECT ID FROM GYM_CLASS_RESERVATIONS WHERE SESSION_ID = ? AND STATUS IN ('reserved', 'waitlist')",
                    sessionId);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_CLASS_SESSIONS
                {
                    ID = sessionId,
                    STATUS = "cancelled",
                    SPOTS_RESERVED = 0
                });

                int cancelled = 0;
                if (reservations != null && reservations.Rows.Count > 0)
                {
                    foreach (Row row in reservations.Rows)
                    {
                        session.UpdateObjectChanges(new GYM_CLASS_RESERVATIONS
                        {
                            ID = Convert.ToInt64(GetRowValue(row, reservations, "id")),
                            STATUS = "cancelled"
                        });
                        cancelled++;
                    }
                }

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al cancelar la sesión de clase";
                    return false;
                }

                response["success"] = true;
                response["cancelledReservations"] = cancelled;
                response["message"] = "Sesión cancelada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CancelClassSession: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteClassSession(long sessionId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long reservations = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_CLASS_RESERVATIONS WHERE SESSION_ID = ?", sessionId);

                if (reservations > 0)
                {
                    response["success"] = false;
                    response["message"] = "La sesión tiene reservas; cancélala en lugar de eliminarla";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_CLASS_SESSIONS { ID = sessionId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la sesión de clase";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Sesión de clase eliminada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteClassSession: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool GetSessionReservations(long sessionId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT r.*, m.FIRST_NAME, m.LAST_NAME, m.EMAIL " +
                    "FROM GYM_CLASS_RESERVATIONS r " +
                    "LEFT JOIN GYM_MEMBERS m ON m.ID = r.MEMBER_ID " +
                    "WHERE r.SESSION_ID = ? " +
                    "ORDER BY CASE r.STATUS WHEN 'reserved' THEN 0 WHEN 'waitlist' THEN 1 ELSE 2 END, r.RESERVED_AT, r.ID",
                    sessionId);

                var reservations = new List<Dictionary<string, object>>();
                int waitlistPosition = 0;

                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        string status = Convert.ToString(GetRowValue(row, result, "status")) ?? "";
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        var item = new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["sessionId"] = Convert.ToInt64(GetRowValue(row, result, "session_id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["status"] = status,
                            ["attended"] = Convert.ToBoolean(GetRowValue(row, result, "attended")),
                            ["reservedAt"] = GetRowValue(row, result, "reserved_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "reserved_at")) : (DateTime?)null
                        };

                        if (status == "waitlist")
                        {
                            waitlistPosition++;
                            item["waitlistPosition"] = waitlistPosition;
                        }

                        reservations.Add(item);
                    }
                }

                response["success"] = true;
                response["data"] = reservations;
                response["count"] = reservations.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSessionReservations: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool ReserveClass(long sessionId, long memberId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (memberId <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El socio es requerido";
                    return false;
                }

                lock (LockFor(sessionId))
                {
                    var sessionData = _tools.ExecuteQueryResultSet(
                        "SELECT s.STATUS, s.START_TIME, s.SPOTS_RESERVED, ct.MAX_CAPACITY " +
                        "FROM GYM_CLASS_SESSIONS s INNER JOIN GYM_CLASS_TYPES ct ON ct.ID = s.CLASS_TYPE_ID " +
                        "WHERE s.ID = ?",
                        sessionId);

                    if (sessionData == null || sessionData.Rows.Count == 0)
                    {
                        response["success"] = false;
                        response["message"] = "La sesión de clase no existe";
                        return false;
                    }

                    var row = sessionData.Rows[0];
                    string status = Convert.ToString(GetRowValue(row, sessionData, "status")) ?? "";

                    if (status != "scheduled")
                    {
                        response["success"] = false;
                        response["message"] = "La sesión no está disponible para reservar";
                        return false;
                    }

                    DateTime startTime = Convert.ToDateTime(GetRowValue(row, sessionData, "start_time"));
                    if (startTime < DateTime.Now)
                    {
                        response["success"] = false;
                        response["message"] = "La sesión ya comenzó";
                        return false;
                    }

                    var member = _tools.ExecuteQueryResultSet(
                        "SELECT STATUS FROM GYM_MEMBERS WHERE ID = ?", memberId);

                    if (member == null || member.Rows.Count == 0)
                    {
                        response["success"] = false;
                        response["message"] = "El socio no existe";
                        return false;
                    }

                    if (!string.Equals(Convert.ToString(GetRowValue(member.Rows[0], member, "status")), "active", StringComparison.OrdinalIgnoreCase))
                    {
                        response["success"] = false;
                        response["message"] = "El socio no está activo";
                        return false;
                    }

                    long duplicates = _tools.ExecuteQueryObjectCount(
                        "SELECT COUNT(*) FROM GYM_CLASS_RESERVATIONS WHERE SESSION_ID = ? AND MEMBER_ID = ? AND STATUS IN ('reserved', 'waitlist')",
                        sessionId, memberId);

                    if (duplicates > 0)
                    {
                        response["success"] = false;
                        response["message"] = "El socio ya tiene una reserva para esta sesión";
                        return false;
                    }

                    int capacity = Convert.ToInt32(GetRowValue(row, sessionData, "max_capacity"));
                    int reserved = Convert.ToInt32(GetRowValue(row, sessionData, "spots_reserved"));
                    bool waitlisted = reserved >= capacity;

                    var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                    long reservationId = _tools.GenerateUniqueID(GYM_CLASS_RESERVATIONS.DatabaseMappingObject, 1).GetInt64UniqueId();

                    session.InsertObjectWithID(new GYM_CLASS_RESERVATIONS
                    {
                        ID = reservationId,
                        SESSION_ID = sessionId,
                        MEMBER_ID = memberId,
                        STATUS = waitlisted ? "waitlist" : "reserved",
                        ATTENDED = false,
                        RESERVED_AT = DateTime.Now
                    });

                    if (!waitlisted)
                    {
                        session.UpdateObjectChanges(new GYM_CLASS_SESSIONS
                        {
                            ID = sessionId,
                            SPOTS_RESERVED = reserved + 1
                        });
                    }

                    if (session.TryExecute().IsError)
                    {
                        response["success"] = false;
                        response["message"] = "Error al registrar la reserva";
                        return false;
                    }

                    response["success"] = true;
                    response["reservationId"] = reservationId;
                    response["status"] = waitlisted ? "waitlist" : "reserved";
                    response["waitlisted"] = waitlisted;

                    if (waitlisted)
                    {
                        long position = _tools.ExecuteQueryObjectCount(
                            "SELECT COUNT(*) FROM GYM_CLASS_RESERVATIONS WHERE SESSION_ID = ? AND STATUS = 'waitlist' AND ID <= ?",
                            sessionId, reservationId);
                        response["waitlistPosition"] = position;
                        response["message"] = $"La sesión está llena; el socio quedó en lista de espera (posición {position})";
                    }
                    else
                    {
                        response["message"] = "Reserva registrada correctamente";
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ReserveClass: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CancelReservation(long reservationId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var reservationData = _tools.ExecuteQueryResultSet(
                    "SELECT SESSION_ID, STATUS FROM GYM_CLASS_RESERVATIONS WHERE ID = ?", reservationId);

                if (reservationData == null || reservationData.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La reserva no existe";
                    return false;
                }

                long sessionId = Convert.ToInt64(GetRowValue(reservationData.Rows[0], reservationData, "session_id"));
                string status = Convert.ToString(GetRowValue(reservationData.Rows[0], reservationData, "status")) ?? "";

                if (status != "reserved" && status != "waitlist")
                {
                    response["success"] = false;
                    response["message"] = "La reserva no se puede cancelar en su estado actual";
                    return false;
                }

                lock (LockFor(sessionId))
                {
                    var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                    session.UpdateObjectChanges(new GYM_CLASS_RESERVATIONS
                    {
                        ID = reservationId,
                        STATUS = "cancelled"
                    });

                    long promotedId = 0;

                    if (status == "reserved")
                    {
                        var waitlist = _tools.ExecuteQueryResultSet(
                            "SELECT TOP 1 ID FROM GYM_CLASS_RESERVATIONS WHERE SESSION_ID = ? AND STATUS = 'waitlist' ORDER BY RESERVED_AT, ID",
                            sessionId);

                        if (waitlist != null && waitlist.Rows.Count > 0)
                        {
                            // El lugar libre pasa al primero de la lista de espera.
                            promotedId = Convert.ToInt64(GetRowValue(waitlist.Rows[0], waitlist, "id"));
                            session.UpdateObjectChanges(new GYM_CLASS_RESERVATIONS
                            {
                                ID = promotedId,
                                STATUS = "reserved"
                            });
                        }
                        else
                        {
                            long spots = _tools.ExecuteQueryObjectCount(
                                "SELECT SPOTS_RESERVED FROM GYM_CLASS_SESSIONS WHERE ID = ?", sessionId);

                            session.UpdateObjectChanges(new GYM_CLASS_SESSIONS
                            {
                                ID = sessionId,
                                SPOTS_RESERVED = (int)Math.Max(0, spots - 1)
                            });
                        }
                    }

                    if (session.TryExecute().IsError)
                    {
                        response["success"] = false;
                        response["message"] = "Error al cancelar la reserva";
                        return false;
                    }

                    response["success"] = true;
                    response["message"] = promotedId > 0
                        ? "Reserva cancelada; el primer socio en lista de espera obtuvo el lugar"
                        : "Reserva cancelada correctamente";

                    if (promotedId > 0)
                        response["promotedReservationId"] = promotedId;

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CancelReservation: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool SetReservationAttendance(long reservationId, bool attended, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var reservation = _tools.ExecuteQueryResultSet(
                    "SELECT STATUS FROM GYM_CLASS_RESERVATIONS WHERE ID = ?", reservationId);

                if (reservation == null || reservation.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La reserva no existe";
                    return false;
                }

                string status = Convert.ToString(GetRowValue(reservation.Rows[0], reservation, "status")) ?? "";

                if (status != "reserved" && status != "attended")
                {
                    response["success"] = false;
                    response["message"] = "Solo se puede marcar asistencia a reservas confirmadas";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_CLASS_RESERVATIONS
                {
                    ID = reservationId,
                    ATTENDED = attended,
                    STATUS = attended ? "attended" : "reserved"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la asistencia";
                    return false;
                }

                response["success"] = true;
                response["attended"] = attended;
                response["message"] = attended ? "Asistencia registrada" : "Asistencia desmarcada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en SetReservationAttendance: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private bool ExistsStaff(long staffId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_STAFF WHERE ID = ? AND IS_ACTIVE = 1", staffId) > 0;
        }
    }
}
