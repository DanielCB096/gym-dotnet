using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Tools;

namespace GymService.Modules.Access
{
    /// <summary>
    /// Access module: generación y validación de códigos OTP para acceso al gimnasio.
    /// </summary>
    public class AccessModule
    {
        private const string SubModule = "ACCESS_MODULE";
        private DatabaseTools _tools;

        public AccessModule(DatabaseTools tools)
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
        /// Generate a one-time access code for a member
        /// </summary>
        public bool GenerateAccessCode(long memberId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberState = GetMemberAccessState(memberId);
                if (memberState == null)
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (!string.Equals(memberState.Value.MemberStatus, "active", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "El socio no está activo";
                    return false;
                }

                if (string.Equals(memberState.Value.MembershipStatus, "frozen", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "La membresía del socio está congelada";
                    return false;
                }

                if (!string.Equals(memberState.Value.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase) ||
                    !memberState.Value.MembershipEnd.HasValue ||
                    memberState.Value.MembershipEnd.Value.Date < DateTime.Now.Date)
                {
                    response["success"] = false;
                    response["message"] = "El socio no tiene una membresía activa";
                    return false;
                }

                var code = SecurityValidation.GenerateOtp(6);
                var codeHash = SecurityValidation.Sha256Hash(code);
                var hmacKey = SecurityValidation.GenerateToken(32);
                var expiresAt = DateTime.Now.AddMinutes(10);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(new GYM_ACCESS_CODES
                {
                    MEMBER_ID = memberId,
                    CODE_HASH = codeHash,
                    HMAC_KEY = hmacKey,
                    GENERATED_AT = DateTime.Now,
                    EXPIRES_AT = expiresAt,
                    USED = false
                });

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al generar código de acceso");
                    response["success"] = false;
                    response["message"] = "Error al generar código de acceso";
                    return false;
                }

                response["success"] = true;
                response["code"] = code;
                response["expiresIn"] = 600;
                response["message"] = "Código generado exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GenerateAccessCode: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Validate an access code and log the access
        /// </summary>
        public bool ValidateAccessCode(string code, string deviceId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var codeHash = SecurityValidation.Sha256Hash(code);

                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT ac.id, ac.member_id, ac.used, m.first_name, m.last_name,
                           m.status AS member_status, mem.status AS membership_status, mem.end_date AS membership_end
                    FROM gym_access_codes ac 
                    INNER JOIN gym_members m ON ac.member_id = m.id 
                    OUTER APPLY (SELECT TOP 1 mem2.status, mem2.end_date
                                 FROM gym_memberships mem2 WHERE mem2.member_id = m.id
                                 ORDER BY CASE WHEN mem2.status = 'active' THEN 0 WHEN mem2.status = 'frozen' THEN 1 ELSE 2 END,
                                          mem2.end_date DESC) mem
                    WHERE ac.code_hash = ? AND ac.used = 0 AND ac.expires_at > GETDATE()",
                    codeHash
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Código inválido o expirado";
                    return false;
                }

                var row = result.Rows[0];
                var accessCodeId = Convert.ToInt64(GetRowValue(row, result, "id"));
                var memberId = Convert.ToInt64(GetRowValue(row, result, "member_id"));
                var memberName = $"{GetRowValue(row, result, "first_name")} {GetRowValue(row, result, "last_name")}";
                string memberStatus = Convert.ToString(GetRowValue(row, result, "member_status")) ?? "";
                string membershipStatus = Convert.ToString(GetRowValue(row, result, "membership_status")) ?? "";
                DateTime? membershipEnd = GetRowValue(row, result, "membership_end") != null
                    ? Convert.ToDateTime(GetRowValue(row, result, "membership_end"))
                    : (DateTime?)null;

                bool hasAccess = string.Equals(memberStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(membershipStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                    membershipEnd.HasValue && membershipEnd.Value.Date >= DateTime.Now.Date;

                if (!hasAccess)
                {
                    var deniedSession = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                    deniedSession.InsertObject(new GYM_ACCESS_LOG
                    {
                        MEMBER_ID = memberId,
                        ACCESS_TYPE = "code",
                        ACCESS_RESULT = "denied",
                        DEVICE_ID = deviceId,
                        ACCESSED_AT = DateTime.Now
                    });
                    deniedSession.TryExecute();

                    response["success"] = false;
                    response["message"] = membershipStatus == "frozen"
                        ? "La membresía del socio está congelada"
                        : "El socio no tiene una membresía activa";
                    return false;
                }

                // Mark as used and log the access in a single transaction.
                // Nota: con objetos no se puede condicionar el UPDATE a USED = 0, se conserva el flujo actual.
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_ACCESS_CODES
                {
                    ID = accessCodeId,
                    USED = true,
                    USED_AT = DateTime.Now
                });
                session.InsertObject(new GYM_ACCESS_LOG
                {
                    MEMBER_ID = memberId,
                    ACCESS_TYPE = "code",
                    ACCESS_RESULT = "granted",
                    DEVICE_ID = deviceId,
                    ACCESSED_AT = DateTime.Now
                });

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al validar código de acceso");
                    response["success"] = false;
                    response["message"] = "Error al procesar el acceso";
                    return false;
                }

                response["success"] = true;
                response["memberId"] = memberId;
                response["memberName"] = memberName;
                response["message"] = "Acceso concedido";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ValidateAccessCode: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
        /// <summary>
        /// Get access logs with optional date range filter and pagination
        /// </summary>
        public bool GetAccessLogs(out Dictionary<string, object> response, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int limit = 50)
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

                if (startDate.HasValue && endDate.HasValue)
                {
                    whereParts.Add("al.accessed_at BETWEEN ? AND ?");
                    parameters.Add(startDate.Value);
                    parameters.Add(endDate.Value);
                }
                else if (startDate.HasValue)
                {
                    whereParts.Add("al.accessed_at >= ?");
                    parameters.Add(startDate.Value);
                }
                else if (endDate.HasValue)
                {
                    whereParts.Add("al.accessed_at <= ?");
                    parameters.Add(endDate.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_access_log al INNER JOIN gym_members m ON al.member_id = m.id {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT al.id, al.member_id, al.access_type, al.access_result, al.device_id, al.accessed_at, m.first_name, m.last_name FROM gym_access_log al INNER JOIN gym_members m ON al.member_id = m.id {whereClause} ORDER BY al.accessed_at DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var logs = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        logs.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = $"{GetRowValue(row, result, "first_name")} {GetRowValue(row, result, "last_name")}",
                            ["accessType"] = Convert.ToString(GetRowValue(row, result, "access_type")) ?? "",
                            ["accessResult"] = Convert.ToString(GetRowValue(row, result, "access_result")) ?? "",
                            ["deviceId"] = Convert.ToString(GetRowValue(row, result, "device_id")) ?? "",
                            ["accessedAt"] = Convert.ToString(GetRowValue(row, result, "accessed_at")) ?? ""
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = logs;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetAccessLogs: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Clean old access codes and logs older than specified days
        /// </summary>
        public bool CleanOldAccessCodes(int daysOld, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysOld);

                // Reads stay as parameterized SQL; deletes are performed through Anxo objects by primary key
                var codesResult = _tools.ExecuteQueryResultSet(
                    "SELECT id FROM gym_access_codes WHERE generated_at < ? AND used = 0",
                    cutoffDate
                );
                var logsResult = _tools.ExecuteQueryResultSet(
                    "SELECT id FROM gym_access_log WHERE accessed_at < ?",
                    cutoffDate
                );

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                bool hasOperations = false;

                if (codesResult != null && codesResult.Rows.Count > 0)
                {
                    foreach (Row row in codesResult.Rows)
                    {
                        session.DeleteObject(new GYM_ACCESS_CODES { ID = Convert.ToInt64(row.GetObject(0)) });
                        hasOperations = true;
                    }
                }

                if (logsResult != null && logsResult.Rows.Count > 0)
                {
                    foreach (Row row in logsResult.Rows)
                    {
                        session.DeleteObject(new GYM_ACCESS_LOG { ID = Convert.ToInt64(row.GetObject(0)) });
                        hasOperations = true;
                    }
                }

                if (hasOperations && session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al limpiar registros antiguos de acceso");
                    response["success"] = false;
                    response["message"] = "Error al limpiar registros antiguos";
                    return false;
                }

                response["success"] = true;
                response["message"] = $"Registros mayores a {daysOld} días eliminados";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CleanOldAccessCodes: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private (string MemberStatus, string MembershipStatus, DateTime? MembershipEnd)? GetMemberAccessState(long memberId)
        {
            var result = _tools.ExecuteQueryResultSet(@"
                SELECT m.status AS member_status, mem.status AS membership_status, mem.end_date AS membership_end
                FROM GYM_MEMBERS m
                OUTER APPLY (SELECT TOP 1 mem2.status, mem2.end_date
                             FROM GYM_MEMBERSHIPS mem2 WHERE mem2.member_id = m.id
                             ORDER BY CASE WHEN mem2.status = 'active' THEN 0 WHEN mem2.status = 'frozen' THEN 1 ELSE 2 END,
                                      mem2.end_date DESC) mem
                WHERE m.id = ?", memberId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToString(GetRowValue(row, result, "member_status")) ?? "",
                Convert.ToString(GetRowValue(row, result, "membership_status")) ?? "",
                GetRowValue(row, result, "membership_end") != null
                    ? Convert.ToDateTime(GetRowValue(row, result, "membership_end"))
                    : (DateTime?)null
            );
        }
    }
}
