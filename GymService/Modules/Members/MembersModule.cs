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
                var firstName = (memberData.ContainsKey("firstName") ? Convert.ToString(memberData["firstName"]) : "")?.Trim() ?? "";
                var lastName = (memberData.ContainsKey("lastName") ? Convert.ToString(memberData["lastName"]) : "")?.Trim() ?? "";
                var email = (memberData.ContainsKey("email") ? Convert.ToString(memberData["email"]) : "")?.Trim() ?? "";
                var phone = (memberData.ContainsKey("phone") ? Convert.ToString(memberData["phone"]) : "")?.Trim() ?? "";
                var membershipType = (memberData.ContainsKey("membershipType") ? Convert.ToString(memberData["membershipType"]) : "")?.Trim() ?? "";
                var createdBy = memberData.ContainsKey("createdBy") ? Convert.ToInt64(memberData["createdBy"]) : CurrentUser.ForAudit;

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre y el apellido son requeridos";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    long duplicateEmail = _tools.ExecuteQueryObjectCount(
                        "SELECT COUNT(*) FROM GYM_MEMBERS WHERE EMAIL = ?", email);

                    if (duplicateEmail > 0)
                    {
                        response["success"] = false;
                        response["message"] = "Ya existe un socio con ese correo";
                        return false;
                    }
                }

                // Look up plan_id by membershipType (plan_name) before opening the transaction
                long planId = 0;
                int durationDays = 30;
                if (!string.IsNullOrEmpty(membershipType))
                {
                    var planResult = _tools.ExecuteQueryResultSet(
                        "SELECT id, duration_days FROM gym_membership_plans WHERE plan_name = ? AND is_active = 1", membershipType
                    );
                    if (planResult != null && planResult.Rows.Count > 0)
                    {
                        planId = Convert.ToInt64(planResult.Rows[0].GetObject(0));
                        var durationValue = planResult.Rows[0].GetObject(1);
                        if (durationValue != null && durationValue != DBNull.Value)
                            durationDays = Convert.ToInt32(durationValue);
                    }
                    else
                    {
                        response["success"] = false;
                        response["message"] = "El plan de membresía no existe o está inactivo";
                        return false;
                    }
                }

                // Pre-generate the IDs so the member and its membership are inserted in the same transaction
                long memberId = _tools.GenerateUniqueID(GYM_MEMBERS.DatabaseMappingObject, 1).GetInt64UniqueId();

                var session = _tools.CreateSession(SubModule, createdBy);
                session.InsertObjectWithID(new GYM_MEMBERS
                {
                    ID = memberId,
                    FIRST_NAME = firstName ?? "",
                    LAST_NAME = lastName ?? "",
                    EMAIL = email,
                    PHONE = phone,
                    STATUS = "active"
                });

                // Create membership if type specified
                if (planId > 0)
                {
                    var endDate = DateTime.Now.AddDays(durationDays > 0 ? durationDays : 30);
                    long membershipId = _tools.GenerateUniqueID(GYM_MEMBERSHIPS.DatabaseMappingObject, 1).GetInt64UniqueId();
                    session.InsertObjectWithID(new GYM_MEMBERSHIPS
                    {
                        ID = membershipId,
                        MEMBER_ID = memberId,
                        PLAN_ID = planId,
                        START_DATE = DateTime.Now,
                        END_DATE = endDate,
                        STATUS = "active",
                        AUTO_RENEW = false
                    });
                }

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear el socio");
                    response["success"] = false;
                    response["message"] = "Error al crear el socio";
                    return false;
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
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_MEMBERSHIPS
                {
                    ID = membershipId,
                    STATUS = "frozen",
                    FREEZE_START = freezeStart,
                    FREEZE_END = freezeEnd
                });

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
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_MEMBERSHIPS
                {
                    ID = membershipId,
                    STATUS = "active",
                    FREEZE_START = null,
                    FREEZE_END = null
                });

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
                // The session user fills CREATED_BY automatically
                long auditUser = createdBy > 0 ? createdBy : CurrentUser.ForAudit;
                var session = _tools.CreateSession(SubModule, auditUser);
                session.InsertObject(new GYM_PAYMENTS
                {
                    MEMBER_ID = memberId,
                    MEMBERSHIP_ID = membershipId,
                    AMOUNT = amount,
                    PAYMENT_METHOD = paymentMethod
                });

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

        /// <summary>
        /// Crea los planes de membresía base en instalaciones nuevas.
        /// </summary>
        public void EnsureDefaultPlans()
        {
            try
            {
                long existing = _tools.ExecuteQueryObjectCount("SELECT COUNT(*) FROM GYM_MEMBERSHIP_PLANS");
                if (existing > 0) return;

                var defaults = new[]
                {
                    ("Mensual", "Acceso general por 30 días", 500m, 30, 1, true),
                    ("Trimestral", "Acceso general por 90 días", 1350m, 90, 1, true),
                    ("Anual", "Acceso general por 365 días", 4800m, 365, 1, true)
                };

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                foreach (var (name, description, price, days, maxEntries, includesClasses) in defaults)
                {
                    long planId = _tools.GenerateUniqueID(GYM_MEMBERSHIP_PLANS.DatabaseMappingObject, 1).GetInt64UniqueId();

                    session.InsertObjectWithID(new GYM_MEMBERSHIP_PLANS
                    {
                        ID = planId,
                        PLAN_NAME = name,
                        DESCRIPTION = description,
                        PRICE = price,
                        CURRENCY = "MXN",
                        DURATION_DAYS = days,
                        MAX_DAILY_ENTRIES = maxEntries,
                        INCLUDES_CLASSES = includesClasses,
                        INCLUDES_PREMIUM_CLASSES = false,
                        IS_ACTIVE = true
                    });
                }

                if (session.TryExecute().IsError)
                {
                    Logger.AddWarningSubModule(SubModule, "No se pudieron crear los planes de membresía por defecto");
                    return;
                }

                Logger.AddInfoSubModule(SubModule, "Planes de membresía por defecto creados");
            }
            catch (Exception ex)
            {
                Logger.AddWarningSubModule(SubModule, $"Error en EnsureDefaultPlans: {ex.Message}");
            }
        }
    }
}
