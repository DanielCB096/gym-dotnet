using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Members
{
    /// <summary>
    /// Members module: CRUD de socios, membresías, congelaciones y pagos.
    /// </summary>
    public class MembersModule
    {
        private const string SubModule = "MEMBERS_MODULE";
        private DatabaseTools _tools;

        public MembersModule(DatabaseTools tools)
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
        /// Get members list with optional filter by id, search query, and pagination
        /// </summary>
        public bool GetMembers(out Dictionary<string, object> response, long? id = null, string? query = null, int page = 1, int limit = 50)
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

                if (id.HasValue)
                {
                    whereParts.Add("m.id = ?");
                    parameters.Add(id.Value);
                }
                else if (!string.IsNullOrEmpty(query))
                {
                    whereParts.Add("(m.first_name LIKE ? OR m.last_name LIKE ? OR m.email LIKE ?)");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = 0;
                var countSql = $"SELECT COUNT(*) FROM gym_members m {whereClause}";
                var countResult = _tools.ExecuteQueryObjectCount(countSql, parameters.ToArray());
                totalCount = countResult;

                // Main query with pagination (SQL Server OFFSET/FETCH)
                var sql = $"SELECT m.*, mem.plan_id, mp.plan_name AS membership_type, mem.start_date, mem.end_date, mem.status " +
                    $"FROM gym_members m LEFT JOIN gym_memberships mem ON m.id = mem.member_id AND mem.status = 'active' " +
                    $"LEFT JOIN gym_membership_plans mp ON mem.plan_id = mp.id " +
                    $"{whereClause} ORDER BY m.id OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                parameters.Add(offset);
                parameters.Add(limit);

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var members = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        members.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["firstName"] = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "",
                            ["lastName"] = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["phone"] = Convert.ToString(GetRowValue(row, result, "phone")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "active",
                            ["membershipType"] = Convert.ToString(GetRowValue(row, result, "membership_type")) ?? "",
                            ["membershipEnd"] = GetRowValue(row, result, "end_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "end_date")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = members;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetMembers: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Create a new member and optionally a membership
        /// </summary>
        public bool CreateMember(Dictionary<string, object> memberData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var firstName = memberData.ContainsKey("firstName") ? Convert.ToString(memberData["firstName"]) : "";
                var lastName = memberData.ContainsKey("lastName") ? Convert.ToString(memberData["lastName"]) : "";
                var email = memberData.ContainsKey("email") ? Convert.ToString(memberData["email"]) : "";
                var phone = memberData.ContainsKey("phone") ? Convert.ToString(memberData["phone"]) : "";
                var membershipType = memberData.ContainsKey("membershipType") ? Convert.ToString(memberData["membershipType"]) : "monthly";

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "INSERT INTO gym_members (first_name, last_name, email, phone, status, created_at) VALUES (?, ?, ?, ?, 'active', GETDATE())",
                    firstName, lastName, email, phone
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear el socio");
                    response["success"] = false;
                    response["message"] = "Error al crear el socio";
                    return false;
                }

                // Get the newly created member ID. SCOPE_IDENTITY() no sirve aquí porque la consulta
                // se ejecuta en otra conexión; se busca la fila recién insertada por sus datos.
                var lastResult = _tools.ExecuteQueryResultSet(
                    "SELECT ISNULL(MAX(id), 0) AS id FROM gym_members WHERE first_name = ? AND last_name = ? AND ISNULL(email, '') = ?",
                    firstName, lastName, email ?? ""
                );
                var memberId = lastResult != null && lastResult.Rows.Count > 0
                    ? Convert.ToInt64(lastResult.Rows[0].GetObject(0))
                    : 0;

                // Create membership if type specified
                if (!string.IsNullOrEmpty(membershipType) && memberId > 0)
                {
                    // Look up plan_id by membershipType (plan_name)
                    var planResult = _tools.ExecuteQueryResultSet(
                        "SELECT id, duration_days FROM gym_membership_plans WHERE plan_name = ? AND is_active = 1", membershipType
                    );
                    long planId = 0;
                    int durationDays = 30;
                    if (planResult != null && planResult.Rows.Count > 0)
                    {
                        planId = Convert.ToInt64(planResult.Rows[0].GetObject(0));
                        var durationValue = planResult.Rows[0].GetObject(1);
                        if (durationValue != null && durationValue != DBNull.Value)
                            durationDays = Convert.ToInt32(durationValue);
                    }
                    if (planId > 0)
                    {
                        var endDate = DateTime.Now.AddDays(durationDays > 0 ? durationDays : 30);
                        session = _tools.CreateSession(SubModule);
                        session.ExecuteTransaction(
                            "INSERT INTO gym_memberships (member_id, plan_id, start_date, end_date, status, created_at) VALUES (?, ?, GETDATE(), ?, 'active', GETDATE())",
                            memberId, planId, endDate
                        );

                        if (session.TryExecute().IsError)
                        {
                            Logger.AddErrorSubModule(SubModule, "Error al crear la membresía");
                        }
                    }
                    else
                    {
                        Logger.AddWarningSubModule(SubModule, $"Plan '{membershipType}' no encontrado, membresía no creada");
                    }
                }

                response["success"] = true;
                response["memberId"] = memberId;
                response["message"] = "Socio creado exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateMember: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active membership plans
        /// </summary>
        public bool GetMembershipPlans(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, plan_name, description, price, currency, duration_days, max_daily_entries, includes_classes, includes_premium_classes " +
                    "FROM gym_membership_plans WHERE is_active = 1 ORDER BY price"
                );

                var plans = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        plans.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["planName"] = Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["price"] = Convert.ToDecimal(GetRowValue(row, result, "price")),
                            ["currency"] = Convert.ToString(GetRowValue(row, result, "currency")) ?? "MXN",
                            ["durationDays"] = Convert.ToInt32(GetRowValue(row, result, "duration_days")),
                            ["maxDailyEntries"] = Convert.ToInt32(GetRowValue(row, result, "max_daily_entries")),
                            ["includesClasses"] = Convert.ToBoolean(GetRowValue(row, result, "includes_classes")),
                            ["includesPremiumClasses"] = Convert.ToBoolean(GetRowValue(row, result, "includes_premium_classes"))
                        });
                    }
                }

                response["success"] = true;
                response["data"] = plans;
                response["count"] = plans.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetMembershipPlans: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Freeze a membership temporarily
        /// </summary>
        public bool FreezeMembership(long membershipId, DateTime freezeStart, DateTime freezeEnd, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_memberships SET status = 'frozen', freeze_start = ?, freeze_end = ? WHERE id = ?",
                    freezeStart, freezeEnd, membershipId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al congelar la membresía");
                    response["success"] = false;
                    response["message"] = "Error al congelar la membresía";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Membresía congelada exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en FreezeMembership: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Resume a frozen membership
        /// </summary>
        public bool ResumeMembership(long membershipId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_memberships SET status = 'active', freeze_start = NULL, freeze_end = NULL WHERE id = ?",
                    membershipId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al reanudar la membresía");
                    response["success"] = false;
                    response["message"] = "Error al reanudar la membresía";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Membresía reanudada exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ResumeMembership: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Record a payment for a member
        /// </summary>
        public bool RecordPayment(long memberId, decimal amount, string paymentMethod, long? membershipId, long createdBy, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);

                if (membershipId.HasValue)
                {
                    session.ExecuteTransaction(
                        "INSERT INTO gym_payments (member_id, membership_id, amount, payment_method, status, created_by, created_at) VALUES (?, ?, ?, ?, 'completed', ?, GETDATE())",
                        memberId, membershipId.Value, amount, paymentMethod, createdBy
                    );
                }
                else
                {
                    session.ExecuteTransaction(
                        "INSERT INTO gym_payments (member_id, amount, payment_method, status, created_by, created_at) VALUES (?, ?, ?, 'completed', ?, GETDATE())",
                        memberId, amount, paymentMethod, createdBy
                    );
                }

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al registrar el pago");
                    response["success"] = false;
                    response["message"] = "Error al registrar el pago";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Pago registrado exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en RecordPayment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
