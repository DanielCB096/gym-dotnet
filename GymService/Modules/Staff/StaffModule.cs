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
