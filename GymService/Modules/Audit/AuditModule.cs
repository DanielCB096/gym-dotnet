using System;
using System.Collections.Generic;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Audit
{
    /// <summary>
    /// Visor de auditoría de seguridad: sesiones de usuario, inicios de sesión
    /// recientes y accesos al gimnasio (concedidos/denegados).
    /// Solo lecturas; no modifica datos.
    /// </summary>
    public class AuditModule
    {
        private const string SubModule = "AUDIT_MODULE";

        private readonly DatabaseTools _tools;

        public AuditModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        /// <summary>
        /// Contadores para el encabezado del visor.
        /// </summary>
        public bool GetSummary(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var now = DateTime.Now;
                var since24h = now.AddHours(-24);
                var today = now.Date;

                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT
                      (SELECT COUNT(*) FROM gym_sessions WHERE is_active = 1 AND expires_at > ?) AS ACTIVE_SESSIONS,
                      (SELECT COUNT(*) FROM gym_sessions WHERE created_at >= ?) AS LOGINS_24H,
                      (SELECT COUNT(DISTINCT user_id) FROM gym_sessions WHERE created_at >= ?) AS USERS_24H,
                      (SELECT COUNT(*) FROM gym_access_log WHERE access_result = 'granted' AND CAST(accessed_at AS DATE) = CAST(? AS DATE)) AS ACCESS_GRANTED_TODAY,
                      (SELECT COUNT(*) FROM gym_access_log WHERE access_result = 'denied' AND CAST(accessed_at AS DATE) = CAST(? AS DATE)) AS ACCESS_DENIED_TODAY",
                    now, since24h, since24h, today, today);

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "No se pudo obtener el resumen de auditoría";
                    return false;
                }

                var row = result.Rows[0];
                response["success"] = true;
                response["data"] = new Dictionary<string, object>
                {
                    ["activeSessions"] = Convert.ToInt64(row.GetObject(0)),
                    ["logins24h"] = Convert.ToInt64(row.GetObject(1)),
                    ["users24h"] = Convert.ToInt64(row.GetObject(2)),
                    ["accessGrantedToday"] = Convert.ToInt64(row.GetObject(3)),
                    ["accessDeniedToday"] = Convert.ToInt64(row.GetObject(4))
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSummary: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Sesiones de usuario con estado calculado: activa, expirada o revocada.
        /// </summary>
        public bool GetSessions(string? query, string? state, int page, int limit,
            out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 200) limit = 50;
                var offset = (page - 1) * limit;
                var now = DateTime.Now;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var like = "%" + query.Trim() + "%";
                    whereParts.Add("(u.username LIKE ? OR u.full_name LIKE ? OR ISNULL(s.ip_address, '') LIKE ? OR ISNULL(s.device_info, '') LIKE ?)");
                    parameters.Add(like);
                    parameters.Add(like);
                    parameters.Add(like);
                    parameters.Add(like);
                }

                var normalizedState = (state ?? string.Empty).Trim().ToLowerInvariant();
                if (normalizedState == "active")
                {
                    whereParts.Add("s.is_active = 1 AND s.expires_at > ?");
                    parameters.Add(now);
                }
                else if (normalizedState == "expired")
                {
                    whereParts.Add("s.is_active = 1 AND s.expires_at <= ?");
                    parameters.Add(now);
                }
                else if (normalizedState == "revoked")
                {
                    whereParts.Add("s.is_active = 0");
                }

                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_sessions s INNER JOIN gym_users u ON s.user_id = u.id {whereClause}",
                    parameters.ToArray());

                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $@"
                    SELECT s.id, s.user_id, u.username, u.full_name, u.role,
                           s.device_info, s.ip_address, s.created_at, s.expires_at, s.is_active
                    FROM gym_sessions s
                    INNER JOIN gym_users u ON s.user_id = u.id
                    {whereClause}
                    ORDER BY s.created_at DESC
                    OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());
                var sessions = new List<Dictionary<string, object>>();

                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        var isActive = Convert.ToBoolean(GetRowValue(row, result, "is_active"));
                        var createdAt = ToDateTime(GetRowValue(row, result, "created_at"));
                        var expiresAt = ToDateTime(GetRowValue(row, result, "expires_at"));
                        var sessionState = !isActive
                            ? "revoked"
                            : (expiresAt.HasValue && expiresAt.Value <= now ? "expired" : "active");

                        sessions.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["userId"] = Convert.ToInt64(GetRowValue(row, result, "user_id")),
                            ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? "",
                            ["fullName"] = Convert.ToString(GetRowValue(row, result, "full_name")) ?? "",
                            ["role"] = Convert.ToString(GetRowValue(row, result, "role")) ?? "",
                            ["deviceInfo"] = Convert.ToString(GetRowValue(row, result, "device_info")) ?? "",
                            ["ipAddress"] = Convert.ToString(GetRowValue(row, result, "ip_address")) ?? "",
                            ["createdAt"] = createdAt?.ToString("o") ?? "",
                            ["expiresAt"] = expiresAt?.ToString("o") ?? "",
                            ["state"] = sessionState
                        });
                    }
                }

                var totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = sessions;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = totalCount,
                    ["totalPages"] = totalPages,
                    ["hasPrev"] = page > 1,
                    ["hasNext"] = page < totalPages
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSessions: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Accesos al gimnasio con filtros por fecha, resultado y texto.
        /// </summary>
        public bool GetAccessLogs(string? query, string? result, DateTime? startDate, DateTime? endDate,
            int page, int limit, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 200) limit = 50;
                var offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (startDate.HasValue)
                {
                    whereParts.Add("al.accessed_at >= ?");
                    parameters.Add(startDate.Value);
                }
                if (endDate.HasValue)
                {
                    whereParts.Add("al.accessed_at <= ?");
                    parameters.Add(endDate.Value);
                }

                var normalizedResult = (result ?? string.Empty).Trim().ToLowerInvariant();
                if (normalizedResult == "granted" || normalizedResult == "denied")
                {
                    whereParts.Add("al.access_result = ?");
                    parameters.Add(normalizedResult);
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var like = "%" + query.Trim() + "%";
                    whereParts.Add("(m.first_name LIKE ? OR m.last_name LIKE ? OR ISNULL(al.reason, '') LIKE ? OR ISNULL(al.device_id, '') LIKE ?)");
                    parameters.Add(like);
                    parameters.Add(like);
                    parameters.Add(like);
                    parameters.Add(like);
                }

                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_access_log al INNER JOIN gym_members m ON al.member_id = m.id {whereClause}",
                    parameters.ToArray());

                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $@"
                    SELECT al.id, al.member_id, al.access_type, al.access_result,
                           al.device_id, al.reason, al.authorized_by, al.accessed_at,
                           m.first_name, m.last_name
                    FROM gym_access_log al
                    INNER JOIN gym_members m ON al.member_id = m.id
                    {whereClause}
                    ORDER BY al.accessed_at DESC
                    OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var resultSet = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());
                var logs = new List<Dictionary<string, object>>();

                if (resultSet != null && resultSet.Rows.Count > 0)
                {
                    foreach (Row row in resultSet.Rows)
                    {
                        var firstName = Convert.ToString(GetRowValue(row, resultSet, "first_name")) ?? "";
                        var lastName = Convert.ToString(GetRowValue(row, resultSet, "last_name")) ?? "";
                        var accessedAt = ToDateTime(GetRowValue(row, resultSet, "accessed_at"));

                        logs.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, resultSet, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, resultSet, "member_id")),
                            ["memberName"] = $"{firstName} {lastName}".Trim(),
                            ["accessType"] = Convert.ToString(GetRowValue(row, resultSet, "access_type")) ?? "",
                            ["accessResult"] = Convert.ToString(GetRowValue(row, resultSet, "access_result")) ?? "",
                            ["deviceId"] = Convert.ToString(GetRowValue(row, resultSet, "device_id")) ?? "",
                            ["reason"] = Convert.ToString(GetRowValue(row, resultSet, "reason")) ?? "",
                            ["authorizedBy"] = GetRowValue(row, resultSet, "authorized_by") != null
                                ? Convert.ToInt64(GetRowValue(row, resultSet, "authorized_by"))
                                : (long?)null,
                            ["accessedAt"] = accessedAt?.ToString("o") ?? ""
                        });
                    }
                }

                var totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = logs;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = totalCount,
                    ["totalPages"] = totalPages,
                    ["hasPrev"] = page > 1,
                    ["hasNext"] = page < totalPages
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetAccessLogs: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private static DateTime? ToDateTime(object? value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            if (value is DateTime date)
                return date;
            return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
        }

        private static object? GetRowValue(Row row, ResultSet result, string columnName)
        {
            var index = GetColumnIndex(result, columnName);
            return index < 0 ? null : row.GetObject(index);
        }

        private static int GetColumnIndex(ResultSet result, string columnName)
        {
            for (var i = 0; i < result.Columns.Count; i++)
            {
                if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
