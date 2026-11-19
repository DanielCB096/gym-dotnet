using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Staff
{
    /// <summary>
    /// Staff module: personal del gimnasio y notificaciones push.
    /// </summary>
    public class StaffModule
    {
        private const string SubModule = "STAFF_MODULE";
        private DatabaseTools _tools;

        public StaffModule(DatabaseTools tools)
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
        /// Create a new staff member
        /// </summary>
        public bool CreateStaff(Dictionary<string, object> staffData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var userId = staffData.ContainsKey("userId") ? Convert.ToInt64(staffData["userId"]) : 0L;
                var employeeName = (Convert.ToString(staffData.GetValueOrDefault("employeeName", "")) ?? "").Trim();
                var position = (Convert.ToString(staffData.GetValueOrDefault("position", "")) ?? "").Trim();
                var department = (Convert.ToString(staffData.GetValueOrDefault("department", "")) ?? "").Trim();

                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del empleado es requerido";
                    return false;
                }

                if (userId <= 0 || !UserExists(userId))
                {
                    response["success"] = false;
                    response["message"] = "El usuario del sistema no existe";
                    return false;
                }

                if (!UserIsActive(userId))
                {
                    response["success"] = false;
                    response["message"] = "El usuario del sistema está inactivo";
                    return false;
                }

                long alreadyLinked = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_STAFF WHERE USER_ID = ?", userId);

                if (alreadyLinked > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ese usuario ya está vinculado a un empleado";
                    return false;
                }

                DateTime hireDate = DateTime.TryParse(
                    Convert.ToString(staffData.GetValueOrDefault("hireDate", "")), out var parsedHire)
                    ? parsedHire.Date
                    : DateTime.Now.Date;

                bool isActive = !staffData.ContainsKey("isActive") || Convert.ToBoolean(staffData["isActive"]);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long staffId = _tools.GenerateUniqueID(GYM_STAFF.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_STAFF
                {
                    ID = staffId,
                    USER_ID = userId,
                    EMPLOYEE_NAME = employeeName,
                    POSITION = position,
                    DEPARTMENT = string.IsNullOrWhiteSpace(department) ? null : department,
                    HIRE_DATE = hireDate,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el empleado";
                    return false;
                }

                response["success"] = true;
                response["staffId"] = staffId;
                response["message"] = "Empleado creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateStaff: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Update a staff member
        /// </summary>
        public bool UpdateStaff(long staffId, Dictionary<string, object> staffData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetStaffState(staffId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El empleado no existe";
                    return false;
                }

                var employeeName = staffData.ContainsKey("employeeName")
                    ? (Convert.ToString(staffData["employeeName"]) ?? "").Trim()
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del empleado es requerido";
                    return false;
                }

                var position = staffData.ContainsKey("position")
                    ? (Convert.ToString(staffData["position"]) ?? "").Trim()
                    : current.Value.Position;

                var department = staffData.ContainsKey("department")
                    ? (Convert.ToString(staffData["department"]) ?? "").Trim()
                    : current.Value.Department;

                DateTime hireDate = staffData.ContainsKey("hireDate") &&
                    DateTime.TryParse(Convert.ToString(staffData["hireDate"]), out var parsedHire)
                    ? parsedHire.Date
                    : current.Value.HireDate;

                bool isActive = staffData.ContainsKey("isActive")
                    ? Convert.ToBoolean(staffData["isActive"])
                    : current.Value.IsActive;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF
                {
                    ID = staffId,
                    USER_ID = current.Value.UserId,
                    EMPLOYEE_NAME = employeeName,
                    POSITION = position,
                    DEPARTMENT = string.IsNullOrWhiteSpace(department) ? null : department,
                    HIRE_DATE = hireDate,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el empleado";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Empleado actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateStaff: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a staff member
        /// </summary>
        public bool DeleteStaff(long staffId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (GetStaffState(staffId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El empleado no existe";
                    return false;
                }

                long shiftCount = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_STAFF_SHIFTS WHERE STAFF_ID = ?", staffId);

                if (shiftCount > 0)
                {
                    response["success"] = false;
                    response["message"] = "El empleado tiene turnos registrados; desactívalo en lugar de eliminarlo";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_STAFF { ID = staffId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el empleado";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Empleado eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteStaff: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active staff members with optional search and pagination
        /// </summary>
        public bool GetStaff(out Dictionary<string, object> response, string? query = null, int page = 1, int limit = 50, bool includeInactive = false)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Clamp pagination values
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!includeInactive)
                    whereParts.Add("s.is_active = 1");

                if (!string.IsNullOrEmpty(query))
                {
                    whereParts.Add("(s.employee_name LIKE ? OR u.username LIKE ? OR u.email LIKE ?)");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_staff s INNER JOIN gym_users u ON s.user_id = u.id {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT s.*, u.username, u.email, u.role FROM gym_staff s INNER JOIN gym_users u ON s.user_id = u.id {whereClause} ORDER BY s.employee_name OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var staff = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        staff.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["userId"] = Convert.ToInt64(GetRowValue(row, result, "user_id")),
                            ["employeeName"] = Convert.ToString(GetRowValue(row, result, "employee_name")) ?? "",
                            ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["role"] = Convert.ToString(GetRowValue(row, result, "role")) ?? "",
                            ["position"] = Convert.ToString(GetRowValue(row, result, "position")) ?? "",
                            ["department"] = Convert.ToString(GetRowValue(row, result, "department")) ?? "",
                            ["hireDate"] = GetRowValue(row, result, "hire_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "hire_date")) : (DateTime?)null,
                            ["isActive"] = GetRowValue(row, result, "is_active") != null && Convert.ToBoolean(GetRowValue(row, result, "is_active"))
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = staff;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetStaff: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get push notification subscriptions with optional filter and pagination
        /// </summary>
        public bool GetPushSubscriptions(out Dictionary<string, object> response, long? userId = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Clamp pagination values
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (userId.HasValue)
                {
                    whereParts.Add("user_id = ?");
                    parameters.Add(userId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_push_subscriptions {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT * FROM gym_push_subscriptions {whereClause} ORDER BY created_at DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var subscriptions = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        subscriptions.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["userId"] = Convert.ToInt64(GetRowValue(row, result, "user_id")),
                            ["endpoint"] = Convert.ToString(GetRowValue(row, result, "endpoint")) ?? "",
                            ["createdAt"] = GetRowValue(row, result, "created_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "created_at")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = subscriptions;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetPushSubscriptions: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active system users available to link as staff
        /// </summary>
        public bool GetAvailableUsers(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT u.id, u.username, u.email, u.role, u.full_name " +
                    "FROM gym_users u " +
                    "WHERE u.is_active = 1 " +
                    "AND NOT EXISTS (SELECT 1 FROM gym_staff s WHERE s.user_id = u.id) " +
                    "ORDER BY u.username"
                );

                var users = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        users.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["role"] = Convert.ToString(GetRowValue(row, result, "role")) ?? "",
                            ["fullName"] = Convert.ToString(GetRowValue(row, result, "full_name")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = users;
                response["count"] = users.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetAvailableUsers: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Lista todos los usuarios del sistema y el empleado vinculado (si existe).
        /// </summary>
        public bool GetSystemUsers(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT u.id, u.username, u.email, u.role, u.full_name, u.is_active, u.is_owner, " +
                    "s.staff_id, s.employee_name " +
                    "FROM gym_users u " +
                    "OUTER APPLY (SELECT TOP 1 s2.id AS staff_id, s2.employee_name " +
                    "             FROM gym_staff s2 WHERE s2.user_id = u.id " +
                    "             ORDER BY s2.is_active DESC, s2.id ASC) s " +
                    "ORDER BY u.username"
                );

                var users = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        users.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["role"] = Convert.ToString(GetRowValue(row, result, "role")) ?? "",
                            ["fullName"] = Convert.ToString(GetRowValue(row, result, "full_name")) ?? "",
                            ["isActive"] = GetRowValue(row, result, "is_active") != null && Convert.ToBoolean(GetRowValue(row, result, "is_active")),
                            ["isOwner"] = GetRowValue(row, result, "is_owner") != null && Convert.ToBoolean(GetRowValue(row, result, "is_owner")),
                            ["staffId"] = GetRowValue(row, result, "staff_id") != null ? Convert.ToInt64(GetRowValue(row, result, "staff_id")) : (long?)null,
                            ["employeeName"] = Convert.ToString(GetRowValue(row, result, "employee_name")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = users;
                response["count"] = users.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSystemUsers: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Turnos y asistencia
        // =====================================================================

        private static readonly string[] ShiftStatuses = { "scheduled", "open", "closed", "absent", "cancelled" };

        /// <summary>
        /// Lista turnos con filtros por empleado, rango de fechas y estado.
        /// </summary>
        public bool GetShifts(out Dictionary<string, object> response, long? staffId = null, DateTime? from = null, DateTime? to = null, string? status = null, int page = 1, int limit = 100)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 100;
                int offset = (page - 1) * limit;

                status = (status ?? "").Trim().ToLowerInvariant();
                if (status != "" && !ShiftStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de turno no válido";
                    return false;
                }

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (staffId.HasValue)
                {
                    whereParts.Add("sh.staff_id = ?");
                    parameters.Add(staffId.Value);
                }

                if (from.HasValue)
                {
                    whereParts.Add("sh.shift_date >= ?");
                    parameters.Add(from.Value.Date);
                }

                if (to.HasValue)
                {
                    whereParts.Add("sh.shift_date <= ?");
                    parameters.Add(to.Value.Date);
                }

                if (status != "")
                {
                    whereParts.Add("sh.status = ?");
                    parameters.Add(status);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_staff_shifts sh INNER JOIN gym_staff s ON s.id = sh.staff_id {whereClause}",
                    parameters.ToArray());

                parameters.Add(offset);
                parameters.Add(limit);

                var result = _tools.ExecuteQueryResultSet(
                    $"SELECT sh.*, s.employee_name, s.position, s.department FROM gym_staff_shifts sh INNER JOIN gym_staff s ON s.id = sh.staff_id {whereClause} ORDER BY sh.scheduled_start DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    parameters.ToArray());

                var shifts = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        var scheduledStart = Convert.ToDateTime(GetRowValue(row, result, "scheduled_start"));
                        var scheduledEnd = GetRowValue(row, result, "scheduled_end") != null
                            ? Convert.ToDateTime(GetRowValue(row, result, "scheduled_end"))
                            : (DateTime?)null;
                        var checkIn = GetRowValue(row, result, "check_in") != null
                            ? Convert.ToDateTime(GetRowValue(row, result, "check_in"))
                            : (DateTime?)null;
                        var checkOut = GetRowValue(row, result, "check_out") != null
                            ? Convert.ToDateTime(GetRowValue(row, result, "check_out"))
                            : (DateTime?)null;

                        int workedMinutes = checkIn.HasValue && checkOut.HasValue && checkOut.Value > checkIn.Value
                            ? (int)(checkOut.Value - checkIn.Value).TotalMinutes
                            : 0;

                        int lateMinutes = checkIn.HasValue && checkIn.Value > scheduledStart.AddMinutes(10)
                            ? (int)(checkIn.Value - scheduledStart).TotalMinutes
                            : 0;

                        shifts.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["staffId"] = Convert.ToInt64(GetRowValue(row, result, "staff_id")),
                            ["employeeName"] = Convert.ToString(GetRowValue(row, result, "employee_name")) ?? "",
                            ["position"] = Convert.ToString(GetRowValue(row, result, "position")) ?? "",
                            ["department"] = Convert.ToString(GetRowValue(row, result, "department")) ?? "",
                            ["shiftDate"] = Convert.ToDateTime(GetRowValue(row, result, "shift_date")),
                            ["scheduledStart"] = scheduledStart,
                            ["scheduledEnd"] = scheduledEnd,
                            ["checkIn"] = checkIn,
                            ["checkOut"] = checkOut,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "scheduled",
                            ["notes"] = Convert.ToString(GetRowValue(row, result, "notes")) ?? "",
                            ["workedMinutes"] = workedMinutes,
                            ["lateMinutes"] = lateMinutes,
                            ["isOpen"] = checkIn.HasValue && !checkOut.HasValue
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = shifts;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetShifts: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Resumen de turnos: totales, horas trabajadas, retardos y ausencias.
        /// </summary>
        public bool GetShiftSummary(out Dictionary<string, object> response, DateTime? from = null, DateTime? to = null)
        {
            response = new Dictionary<string, object>();

            try
            {
                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (from.HasValue)
                {
                    whereParts.Add("shift_date >= ?");
                    parameters.Add(from.Value.Date);
                }

                if (to.HasValue)
                {
                    whereParts.Add("shift_date <= ?");
                    parameters.Add(to.Value.Date);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT " +
                    "COUNT(*) AS TOTAL, " +
                    "SUM(CASE WHEN STATUS = 'scheduled' THEN 1 ELSE 0 END) AS SCHEDULED_COUNT, " +
                    "SUM(CASE WHEN STATUS = 'open' THEN 1 ELSE 0 END) AS OPEN_COUNT, " +
                    "SUM(CASE WHEN STATUS = 'closed' THEN 1 ELSE 0 END) AS CLOSED_COUNT, " +
                    "SUM(CASE WHEN STATUS = 'absent' THEN 1 ELSE 0 END) AS ABSENT_COUNT, " +
                    "SUM(CASE WHEN STATUS = 'cancelled' THEN 1 ELSE 0 END) AS CANCELLED_COUNT, " +
                    "ISNULL(SUM(CASE WHEN CHECK_IN IS NOT NULL AND CHECK_OUT IS NOT NULL THEN DATEDIFF(MINUTE, CHECK_IN, CHECK_OUT) ELSE 0 END),0) AS WORKED_MINUTES, " +
                    "SUM(CASE WHEN CHECK_IN IS NOT NULL AND CHECK_IN > DATEADD(MINUTE, 10, SCHEDULED_START) THEN 1 ELSE 0 END) AS LATE_COUNT " +
                    $"FROM GYM_STAFF_SHIFTS {whereClause}",
                    parameters.ToArray());

                var row = result.Rows[0];
                response["success"] = true;
                response["data"] = new Dictionary<string, object>
                {
                    ["total"] = Convert.ToInt64(row.GetObject(0)),
                    ["scheduledCount"] = Convert.ToInt64(row.GetObject(1)),
                    ["openCount"] = Convert.ToInt64(row.GetObject(2)),
                    ["closedCount"] = Convert.ToInt64(row.GetObject(3)),
                    ["absentCount"] = Convert.ToInt64(row.GetObject(4)),
                    ["cancelledCount"] = Convert.ToInt64(row.GetObject(5)),
                    ["workedMinutes"] = Convert.ToInt64(row.GetObject(6)),
                    ["lateCount"] = Convert.ToInt64(row.GetObject(7))
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetShiftSummary: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Programa un turno para un empleado.
        /// </summary>
        public bool CreateShift(Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long staffId = data.ContainsKey("staffId") ? Convert.ToInt64(data["staffId"]) : 0L;
                if (staffId <= 0 || GetStaffState(staffId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El empleado no existe";
                    return false;
                }

                var staff = GetStaffState(staffId)!.Value;
                if (!staff.IsActive)
                {
                    response["success"] = false;
                    response["message"] = "El empleado está inactivo";
                    return false;
                }

                if (!DateTime.TryParse(Convert.ToString(data.GetValueOrDefault("scheduledStart", "")), out var scheduledStart))
                {
                    response["success"] = false;
                    response["message"] = "La fecha y hora de inicio son requeridas";
                    return false;
                }

                DateTime? scheduledEnd = null;
                var rawEnd = Convert.ToString(data.GetValueOrDefault("scheduledEnd", ""));
                if (!string.IsNullOrWhiteSpace(rawEnd))
                {
                    if (!DateTime.TryParse(rawEnd, out var parsedEnd))
                    {
                        response["success"] = false;
                        response["message"] = "La hora de fin no es válida";
                        return false;
                    }
                    scheduledEnd = parsedEnd;
                }

                if (scheduledEnd.HasValue && scheduledEnd.Value <= scheduledStart)
                {
                    response["success"] = false;
                    response["message"] = "La hora de fin debe ser posterior a la de inicio";
                    return false;
                }

                DateTime shiftDate = scheduledStart.Date;
                var rawDate = Convert.ToString(data.GetValueOrDefault("shiftDate", ""));
                if (!string.IsNullOrWhiteSpace(rawDate) && DateTime.TryParse(rawDate, out var parsedDate))
                    shiftDate = parsedDate.Date;

                long duplicates = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_STAFF_SHIFTS WHERE STAFF_ID = ? AND SCHEDULED_START = ? AND STATUS <> 'cancelled'",
                    staffId, scheduledStart);

                if (duplicates > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe un turno para ese empleado en el mismo horario";
                    return false;
                }

                string notes = (Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "").Trim();

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long shiftId = _tools.GenerateUniqueID(GYM_STAFF_SHIFTS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    STAFF_ID = staffId,
                    SHIFT_DATE = shiftDate,
                    SCHEDULED_START = scheduledStart,
                    SCHEDULED_END = scheduledEnd,
                    STATUS = "scheduled",
                    NOTES = string.IsNullOrWhiteSpace(notes) ? null : notes
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el turno";
                    return false;
                }

                response["success"] = true;
                response["shiftId"] = shiftId;
                response["message"] = "Turno programado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateShift: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Registra la entrada del empleado a su turno.
        /// </summary>
        public bool CheckInShift(long shiftId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var shift = GetShiftState(shiftId);
                if (shift == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno no existe";
                    return false;
                }

                if (shift.Value.Status == "open")
                {
                    response["success"] = false;
                    response["message"] = "La entrada ya fue registrada";
                    return false;
                }

                if (shift.Value.Status != "scheduled")
                {
                    response["success"] = false;
                    response["message"] = "Solo se puede registrar entrada en turnos programados";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    CHECK_IN = DateTime.Now,
                    STATUS = "open"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la entrada";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Entrada registrada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CheckInShift: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Registra la salida del empleado y cierra el turno.
        /// </summary>
        public bool CheckOutShift(long shiftId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var shift = GetShiftState(shiftId);
                if (shift == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno no existe";
                    return false;
                }

                if (shift.Value.Status != "open" || !shift.Value.CheckIn.HasValue)
                {
                    response["success"] = false;
                    response["message"] = "El turno no tiene una entrada registrada";
                    return false;
                }

                var checkOut = DateTime.Now;
                if (checkOut <= shift.Value.CheckIn.Value)
                {
                    response["success"] = false;
                    response["message"] = "La salida debe ser posterior a la entrada";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    CHECK_OUT = checkOut,
                    STATUS = "closed"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la salida";
                    return false;
                }

                int workedMinutes = (int)(checkOut - shift.Value.CheckIn.Value).TotalMinutes;
                response["success"] = true;
                response["workedMinutes"] = workedMinutes;
                response["message"] = $"Salida registrada ({workedMinutes} min trabajados)";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CheckOutShift: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Marca un turno programado como ausencia.
        /// </summary>
        public bool MarkShiftAbsent(long shiftId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var shift = GetShiftState(shiftId);
                if (shift == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno no existe";
                    return false;
                }

                if (shift.Value.Status != "scheduled")
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden marcar ausencias en turnos programados";
                    return false;
                }

                if (shift.Value.ScheduledStart > DateTime.Now)
                {
                    response["success"] = false;
                    response["message"] = "No se puede marcar ausencia antes del inicio del turno";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    STATUS = "absent"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al marcar la ausencia";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Ausencia registrada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en MarkShiftAbsent: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Cancela un turno que aún no ha iniciado.
        /// </summary>
        public bool CancelShift(long shiftId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var shift = GetShiftState(shiftId);
                if (shift == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno no existe";
                    return false;
                }

                if (shift.Value.Status != "scheduled" && shift.Value.Status != "absent")
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden cancelar turnos programados o ausentes";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    STATUS = "cancelled"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al cancelar el turno";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Turno cancelado";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CancelShift: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Actualiza horario o notas de un turno programado.
        /// </summary>
        public bool UpdateShift(long shiftId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var shift = GetShiftState(shiftId);
                if (shift == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno no existe";
                    return false;
                }

                if (shift.Value.Status != "scheduled")
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden editar turnos programados";
                    return false;
                }

                var scheduledStart = shift.Value.ScheduledStart;
                var rawStart = Convert.ToString(data.GetValueOrDefault("scheduledStart", ""));
                if (!string.IsNullOrWhiteSpace(rawStart))
                {
                    if (!DateTime.TryParse(rawStart, out scheduledStart))
                    {
                        response["success"] = false;
                        response["message"] = "La fecha y hora de inicio no son válidas";
                        return false;
                    }
                }

                DateTime? scheduledEnd = shift.Value.ScheduledEnd;
                if (data.ContainsKey("scheduledEnd"))
                {
                    var rawEnd = Convert.ToString(data["scheduledEnd"]);
                    if (string.IsNullOrWhiteSpace(rawEnd))
                    {
                        scheduledEnd = null;
                    }
                    else if (!DateTime.TryParse(rawEnd, out var parsedEnd))
                    {
                        response["success"] = false;
                        response["message"] = "La hora de fin no es válida";
                        return false;
                    }
                    else
                    {
                        scheduledEnd = parsedEnd;
                    }
                }

                if (scheduledEnd.HasValue && scheduledEnd.Value <= scheduledStart)
                {
                    response["success"] = false;
                    response["message"] = "La hora de fin debe ser posterior a la de inicio";
                    return false;
                }

                long duplicates = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_STAFF_SHIFTS WHERE STAFF_ID = ? AND SCHEDULED_START = ? AND STATUS <> 'cancelled' AND ID <> ?",
                    shift.Value.StaffId, scheduledStart, shiftId);

                if (duplicates > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe un turno para ese empleado en el mismo horario";
                    return false;
                }

                string notes = data.ContainsKey("notes")
                    ? (Convert.ToString(data["notes"]) ?? "").Trim()
                    : shift.Value.Notes;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_STAFF_SHIFTS
                {
                    ID = shiftId,
                    SHIFT_DATE = scheduledStart.Date,
                    SCHEDULED_START = scheduledStart,
                    SCHEDULED_END = scheduledEnd,
                    NOTES = string.IsNullOrWhiteSpace(notes) ? null : notes
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el turno";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Turno actualizado";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateShift: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private (long StaffId, DateTime ScheduledStart, DateTime? ScheduledEnd, DateTime? CheckIn, string Status, string Notes)? GetShiftState(long shiftId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_STAFF_SHIFTS WHERE ID = ?", shiftId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "staff_id")),
                Convert.ToDateTime(GetRowValue(row, result, "scheduled_start")),
                GetRowValue(row, result, "scheduled_end") != null ? Convert.ToDateTime(GetRowValue(row, result, "scheduled_end")) : (DateTime?)null,
                GetRowValue(row, result, "check_in") != null ? Convert.ToDateTime(GetRowValue(row, result, "check_in")) : (DateTime?)null,
                Convert.ToString(GetRowValue(row, result, "status")) ?? "scheduled",
                Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
            );
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private bool UserExists(long userId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_USERS WHERE ID = ?", userId) > 0;
        }

        private bool UserIsActive(long userId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_USERS WHERE ID = ? AND IS_ACTIVE = 1", userId) > 0;
        }

        private (long UserId, string Name, string Position, string Department, DateTime HireDate, bool IsActive)? GetStaffState(long staffId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_STAFF WHERE ID = ?", staffId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "user_id")),
                Convert.ToString(GetRowValue(row, result, "employee_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "position")) ?? "",
                Convert.ToString(GetRowValue(row, result, "department")) ?? "",
                Convert.ToDateTime(GetRowValue(row, result, "hire_date")),
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }
    }
}
