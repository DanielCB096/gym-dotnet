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
                var employeeName = Convert.ToString(staffData.GetValueOrDefault("employeeName", "")) ?? "";
                var position = Convert.ToString(staffData.GetValueOrDefault("position", "")) ?? "";
                var department = Convert.ToString(staffData.GetValueOrDefault("department", "")) ?? "";

                if (string.IsNullOrEmpty(employeeName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del empleado es requerido";
                    return false;
                }

                var newStaff = new GYM_STAFF
                {
                    USER_ID = userId,
                    EMPLOYEE_NAME = employeeName,
                    POSITION = position,
                    DEPARTMENT = string.IsNullOrWhiteSpace(department) ? null : department,
                    IS_ACTIVE = true
                };

                var session = _tools.CreateSession(SubModule);
                session.InsertObject(newStaff);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Empleado creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el empleado";
                }

                return (bool)response["success"];
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
                var employeeName = Convert.ToString(staffData.GetValueOrDefault("employeeName", "")) ?? "";
                var position = Convert.ToString(staffData.GetValueOrDefault("position", "")) ?? "";
                var department = Convert.ToString(staffData.GetValueOrDefault("department", "")) ?? "";

                var staff = new GYM_STAFF
                {
                    ID = staffId,
                    EMPLOYEE_NAME = employeeName,
                    POSITION = position,
                    DEPARTMENT = string.IsNullOrWhiteSpace(department) ? null : department
                };

                var session = _tools.CreateSession(SubModule);
                session.UpdateObjectChanges(staff);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Empleado actualizado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el empleado";
                }

                return (bool)response["success"];
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
                var staff = new GYM_STAFF { ID = staffId };

                var session = _tools.CreateSession(SubModule);
                session.DeleteObject(staff);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Empleado eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el empleado";
                }

                return (bool)response["success"];
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
        public bool GetStaff(out Dictionary<string, object> response, string? query = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Clamp pagination values
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string> { "s.is_active = 1" };
                var parameters = new List<object>();

                if (!string.IsNullOrEmpty(query))
                {
                    whereParts.Add("(s.employee_name LIKE ? OR u.username LIKE ? OR u.email LIKE ?)");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                }

                string whereClause = "WHERE " + string.Join(" AND ", whereParts);

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
                            ["employeeName"] = Convert.ToString(GetRowValue(row, result, "employee_name")) ?? "",
                            ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["role"] = Convert.ToString(GetRowValue(row, result, "role")) ?? "",
                            ["position"] = Convert.ToString(GetRowValue(row, result, "position")) ?? "",
                            ["department"] = Convert.ToString(GetRowValue(row, result, "department")) ?? ""
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
                    "SELECT id, username, email, role, full_name FROM gym_users WHERE is_active = 1 ORDER BY username"
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
    }
}
