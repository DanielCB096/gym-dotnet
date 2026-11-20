using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Services;

namespace GymService.Modules.Members
{
    /// <summary>
    /// Members module: CRUD de socios, membresías, congelaciones y pagos.
    /// </summary>
    public class MembersModule
    {
        private const string SubModule = "MEMBERS_MODULE";
        private DatabaseTools _tools;
        private readonly EmailService? _email;

        public MembersModule(DatabaseTools tools, EmailService? email = null)
        {
            _tools = tools;
            _email = email ?? DataCenter.EmailService;
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
                var sql = $"SELECT m.*, mem.id AS membership_id, mem.plan_id, mp.plan_name AS membership_type, mem.start_date, mem.end_date, " +
                    $"m.status AS member_status, mem.status AS membership_status, " +
                    $"(SELECT ISNULL(SUM(c.remaining_amount),0) FROM gym_member_credits c " +
                    $" WHERE c.member_id = m.id AND c.status = 'available') AS credit_balance " +
                    $"FROM gym_members m " +
                    $"OUTER APPLY (SELECT TOP 1 mem2.id, mem2.plan_id, mem2.status, mem2.start_date, mem2.end_date " +
                    $"             FROM gym_memberships mem2 WHERE mem2.member_id = m.id " +
                    $"             ORDER BY CASE WHEN mem2.status = 'active' THEN 0 WHEN mem2.status = 'frozen' THEN 1 ELSE 2 END, mem2.end_date DESC) mem " +
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
                        string membershipType = Convert.ToString(GetRowValue(row, result, "membership_type")) ?? "";
                        DateTime? membershipEnd = GetRowValue(row, result, "end_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "end_date")) : (DateTime?)null;
                        string rawMembershipStatus = Convert.ToString(GetRowValue(row, result, "membership_status")) ?? "";

                        string membershipStatus;
                        if (string.IsNullOrWhiteSpace(membershipType))
                            membershipStatus = "none";
                        else if (string.Equals(rawMembershipStatus, "frozen", StringComparison.OrdinalIgnoreCase))
                            membershipStatus = "frozen";
                        else if (string.Equals(rawMembershipStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
                            membershipStatus = "cancelled";
                        else if (string.Equals(rawMembershipStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                                 membershipEnd.HasValue && membershipEnd.Value.Date >= DateTime.Now.Date)
                            membershipStatus = "active";
                        else
                            membershipStatus = "expired";

                        members.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["firstName"] = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "",
                            ["lastName"] = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["phone"] = Convert.ToString(GetRowValue(row, result, "phone")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "member_status")) ?? "active",
                            ["membershipType"] = membershipType,
                            ["membershipStatus"] = membershipStatus,
                            ["hasActiveMembership"] = membershipStatus == "active",
                            ["membershipId"] = GetRowValue(row, result, "membership_id") != null ? Convert.ToInt64(GetRowValue(row, result, "membership_id")) : (long?)null,
                            ["membershipEnd"] = membershipEnd,
                            ["creditBalance"] = Convert.ToDecimal(GetRowValue(row, result, "credit_balance"))
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

                SendWelcomeEmail(email, $"{firstName} {lastName}".Trim());
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
        /// <summary>
        /// Actualiza los datos del socio.
        /// </summary>
        public bool UpdateMember(long memberId, Dictionary<string, object> memberData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetMemberState(memberId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                var firstName = memberData.ContainsKey("firstName")
                    ? (Convert.ToString(memberData["firstName"]) ?? "").Trim()
                    : current.Value.FirstName;

                var lastName = memberData.ContainsKey("lastName")
                    ? (Convert.ToString(memberData["lastName"]) ?? "").Trim()
                    : current.Value.LastName;

                var email = memberData.ContainsKey("email")
                    ? (Convert.ToString(memberData["email"]) ?? "").Trim()
                    : current.Value.Email;

                var phone = memberData.ContainsKey("phone")
                    ? (Convert.ToString(memberData["phone"]) ?? "").Trim()
                    : current.Value.Phone;

                var status = memberData.ContainsKey("status")
                    ? (Convert.ToString(memberData["status"]) ?? "").Trim().ToLowerInvariant()
                    : current.Value.Status;

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre y el apellido son requeridos";
                    return false;
                }

                if (status != "active" && status != "inactive" && status != "pending")
                {
                    response["success"] = false;
                    response["message"] = "Estado de socio no válido";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    long duplicateEmail = _tools.ExecuteQueryObjectCount(
                        "SELECT COUNT(*) FROM GYM_MEMBERS WHERE EMAIL = ? AND ID <> ?", email, memberId);

                    if (duplicateEmail > 0)
                    {
                        response["success"] = false;
                        response["message"] = "Ya existe otro socio con ese correo";
                        return false;
                    }
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_MEMBERS
                {
                    ID = memberId,
                    FIRST_NAME = firstName,
                    LAST_NAME = lastName,
                    EMAIL = email,
                    PHONE = phone,
                    STATUS = status
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el socio";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Socio actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateMember: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Lista el historial de membresías de un socio.
        /// </summary>
        public bool GetMembershipHistory(long memberId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT m.*, p.PLAN_NAME
                    FROM GYM_MEMBERSHIPS m
                    INNER JOIN GYM_MEMBERSHIP_PLANS p ON p.ID = m.PLAN_ID
                    WHERE m.MEMBER_ID = ?
                    ORDER BY m.START_DATE DESC, m.ID DESC", memberId);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["planId"] = Convert.ToInt64(GetRowValue(row, result, "plan_id")),
                            ["planName"] = Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                            ["startDate"] = GetRowValue(row, result, "start_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "start_date")) : (DateTime?)null,
                            ["endDate"] = GetRowValue(row, result, "end_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "end_date")) : (DateTime?)null,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["autoRenew"] = GetRowValue(row, result, "auto_renew") != null && Convert.ToBoolean(GetRowValue(row, result, "auto_renew")),
                            ["cancelledAt"] = GetRowValue(row, result, "cancelled_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "cancelled_at")) : (DateTime?)null,
                            ["cancelReason"] = Convert.ToString(GetRowValue(row, result, "cancel_reason")) ?? ""
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetMembershipHistory: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Lista los pagos de membresía de un socio (más recientes primero, máximo 50).
        /// </summary>
        public bool GetPayments(long memberId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT TOP 50 p.*, pl.PLAN_NAME
                    FROM GYM_PAYMENTS p
                    LEFT JOIN GYM_MEMBERSHIPS m ON m.ID = p.MEMBERSHIP_ID
                    LEFT JOIN GYM_MEMBERSHIP_PLANS pl ON pl.ID = m.PLAN_ID
                    WHERE p.MEMBER_ID = ?
                    ORDER BY p.PAYMENT_DATE DESC, p.ID DESC", memberId);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["membershipId"] = GetRowValue(row, result, "membership_id") != null
                                ? Convert.ToInt64(GetRowValue(row, result, "membership_id"))
                                : (long?)null,
                            ["planName"] = Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                            ["amount"] = Convert.ToDecimal(GetRowValue(row, result, "amount")),
                            ["currency"] = Convert.ToString(GetRowValue(row, result, "currency")) ?? "MXN",
                            ["paymentMethod"] = Convert.ToString(GetRowValue(row, result, "payment_method")) ?? "",
                            ["paymentDate"] = GetRowValue(row, result, "payment_date") != null
                                ? Convert.ToDateTime(GetRowValue(row, result, "payment_date"))
                                : (DateTime?)null,
                            ["reference"] = Convert.ToString(GetRowValue(row, result, "reference")) ?? "",
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetPayments: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Genera un recibo HTML imprimible para un pago de membresía del socio indicado.
        /// Devuelve false si el pago no existe o no pertenece al socio.
        /// </summary>
        public bool GetPaymentReceipt(long memberId, long paymentId, out string html)
        {
            html = string.Empty;

            try
            {
                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT p.*, mem.FIRST_NAME, mem.LAST_NAME, mem.EMAIL,
                           m.START_DATE, m.END_DATE,
                           pl.PLAN_NAME
                    FROM GYM_PAYMENTS p
                    INNER JOIN GYM_MEMBERS mem ON mem.ID = p.MEMBER_ID
                    LEFT JOIN GYM_MEMBERSHIPS m ON m.ID = p.MEMBERSHIP_ID
                    LEFT JOIN GYM_MEMBERSHIP_PLANS pl ON pl.ID = m.PLAN_ID
                    WHERE p.ID = ? AND p.MEMBER_ID = ?", paymentId, memberId);

                if (result == null || result.Rows.Count == 0)
                    return false;

                var payment = result.Rows[0];

                long id = Convert.ToInt64(GetRowValue(payment, result, "id"));
                string folio = $"PAGO-{id}";
                string memberName = $"{GetRowValue(payment, result, "first_name")} {GetRowValue(payment, result, "last_name")}".Trim();
                string memberEmail = Convert.ToString(GetRowValue(payment, result, "email")) ?? "";
                string planName = Convert.ToString(GetRowValue(payment, result, "plan_name")) ?? "";
                DateTime? startDate = GetRowValue(payment, result, "start_date") != null
                    ? Convert.ToDateTime(GetRowValue(payment, result, "start_date"))
                    : (DateTime?)null;
                DateTime? endDate = GetRowValue(payment, result, "end_date") != null
                    ? Convert.ToDateTime(GetRowValue(payment, result, "end_date"))
                    : (DateTime?)null;
                decimal amount = Convert.ToDecimal(GetRowValue(payment, result, "amount"));
                string currency = Convert.ToString(GetRowValue(payment, result, "currency")) ?? "MXN";
                string paymentMethod = Convert.ToString(GetRowValue(payment, result, "payment_method")) ?? "";
                DateTime paymentDate = Convert.ToDateTime(GetRowValue(payment, result, "payment_date"));
                string reference = Convert.ToString(GetRowValue(payment, result, "reference")) ?? "";
                string notes = Convert.ToString(GetRowValue(payment, result, "notes")) ?? "";

                string methodLabel = paymentMethod switch
                {
                    "cash" => "Efectivo",
                    "card" => "Tarjeta",
                    "transfer" => "Transferencia",
                    _ => paymentMethod
                };

                string period = startDate.HasValue && endDate.HasValue
                    ? $"{startDate.Value:dd/MM/yyyy} al {endDate.Value:dd/MM/yyyy}"
                    : "—";

                string extra = "";
                if (!string.IsNullOrWhiteSpace(reference))
                    extra += $"<br><strong>Referencia:</strong> {System.Net.WebUtility.HtmlEncode(reference)}";
                if (!string.IsNullOrWhiteSpace(notes))
                    extra += $"<br><strong>Notas:</strong> {System.Net.WebUtility.HtmlEncode(notes)}";

                html = $@"<!doctype html>
<html lang=""es""><head><meta charset=""utf-8""><title>Recibo {folio}</title>
<style>
  body {{ font-family: Arial, sans-serif; max-width: 720px; margin: 32px auto; color:#111827; }}
  header {{ display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #111827; padding-bottom:12px; margin-bottom:20px; }}
  h1 {{ margin:0; font-size:22px; }} .muted {{ color:#6b7280; font-size:13px; }}
  table {{ width:100%; border-collapse:collapse; margin-top:16px; }} th,td {{ padding:8px; border-bottom:1px solid #e5e7eb; font-size:14px; }}
  th {{ text-align:left; background:#f9fafb; }} .right {{ text-align:right; }}
  .total {{ margin-top:16px; text-align:right; font-size:18px; font-weight:bold; }}
  .footer {{ margin-top:28px; font-size:12px; color:#6b7280; text-align:center; }}
  @media print {{ body {{ margin:0; }} }}
</style></head><body>
<header><div><h1>Centro Fit</h1><p class=""muted"">Recibo de pago de membresía</p></div>
<div class=""muted""><strong>{System.Net.WebUtility.HtmlEncode(folio)}</strong><br>{paymentDate:dd/MM/yyyy HH:mm}</div></header>
<p><strong>Socio:</strong> {System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(memberName) ? "Socio" : memberName)}
{(string.IsNullOrWhiteSpace(memberEmail) ? "" : $"<br><strong>Correo:</strong> {System.Net.WebUtility.HtmlEncode(memberEmail)}")}<br>
<strong>Plan:</strong> {System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(planName) ? "—" : planName)}<br>
<strong>Periodo:</strong> {period}<br>
<strong>Método de pago:</strong> {System.Net.WebUtility.HtmlEncode(methodLabel)}{extra}</p>
<table><thead><tr><th>Concepto</th><th class=""right"">Importe</th></tr></thead>
<tbody><tr><td>Membresía {System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(planName) ? "" : planName)}</td><td class=""right"">${amount:0.00} {System.Net.WebUtility.HtmlEncode(currency)}</td></tr></tbody></table>
<p class=""total"">Total: ${amount:0.00} <span class=""muted"">({System.Net.WebUtility.HtmlEncode(currency)})</span></p>
<p class=""footer"">Gracias por tu pago · GymService</p>
</body></html>";

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetPaymentReceipt: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cancela una membresía con motivo.
        /// </summary>
        public bool CancelMembership(long membershipId, string reason, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                reason = (reason ?? "").Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    response["success"] = false;
                    response["message"] = "El motivo de cancelación es requerido";
                    return false;
                }

                var membership = GetMembershipState(membershipId);
                if (membership == null)
                {
                    response["success"] = false;
                    response["message"] = "La membresía no existe";
                    return false;
                }

                if (string.Equals(membership.Value.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "La membresía ya está cancelada";
                    return false;
                }

                DateTime today = DateTime.Now.Date;
                int daysLeft = (membership.Value.EndDate.Date - today).Days;
                decimal creditAmount = 0m;
                string creditCurrency = "MXN";

                if (daysLeft > 0)
                {
                    var plan = GetPlanState(membership.Value.PlanId);
                    if (plan != null && plan.Value.DurationDays > 0 && plan.Value.Price > 0)
                    {
                        creditCurrency = plan.Value.Currency;
                        decimal dailyRate = plan.Value.Price / plan.Value.DurationDays;
                        creditAmount = Math.Round(dailyRate * daysLeft, 2, MidpointRounding.AwayFromZero);
                        if (creditAmount > plan.Value.Price)
                            creditAmount = plan.Value.Price;
                    }
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_MEMBERSHIPS
                {
                    ID = membershipId,
                    STATUS = "cancelled",
                    CANCELLED_AT = DateTime.Now,
                    CANCEL_REASON = reason
                });

                long? creditId = null;
                if (creditAmount > 0)
                {
                    long newCreditId = _tools.GenerateUniqueID(GYM_MEMBER_CREDITS.DatabaseMappingObject, 1).GetInt64UniqueId();
                    creditId = newCreditId;
                    string creditReason = ("Cancelación: " + reason).Trim();
                    if (creditReason.Length > 300)
                        creditReason = creditReason.Substring(0, 300);

                    session.InsertObjectWithID(new GYM_MEMBER_CREDITS
                    {
                        ID = newCreditId,
                        MEMBER_ID = membership.Value.MemberId,
                        MEMBERSHIP_ID = membershipId,
                        AMOUNT = creditAmount,
                        REMAINING_AMOUNT = creditAmount,
                        CURRENCY = creditCurrency,
                        STATUS = "available",
                        REASON = creditReason
                    });
                }

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al cancelar la membresía";
                    return false;
                }

                Logger.AddInfoSubModule(SubModule,
                    $"Membresía {membershipId} cancelada: {reason}. Crédito a favor: {creditAmount:0.00} {creditCurrency} ({daysLeft} días)");

                response["success"] = true;
                response["creditId"] = creditId;
                response["creditAmount"] = creditAmount;
                response["creditCurrency"] = creditCurrency;
                response["creditDays"] = daysLeft > 0 ? daysLeft : 0;
                response["message"] = creditAmount > 0
                    ? $"Membresía cancelada. Crédito a favor: {creditAmount:0.00} {creditCurrency}"
                    : "Membresía cancelada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CancelMembership: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool GetMembershipPlans(out Dictionary<string, object> response, bool includeInactive = false)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, plan_name, description, price, currency, duration_days, max_daily_entries, includes_classes, includes_premium_classes, is_active " +
                    $"FROM gym_membership_plans {(includeInactive ? "" : "WHERE is_active = 1")} ORDER BY price"
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
                            ["includesPremiumClasses"] = Convert.ToBoolean(GetRowValue(row, result, "includes_premium_classes")),
                            ["isActive"] = Convert.ToBoolean(GetRowValue(row, result, "is_active"))
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
        /// Crea un plan de membresía.
        /// </summary>
        public bool CreatePlan(Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var name = (Convert.ToString(data.GetValueOrDefault("planName", "")) ?? "").Trim();
                var description = (Convert.ToString(data.GetValueOrDefault("description", "")) ?? "").Trim();
                var price = data.ContainsKey("price") ? Convert.ToDecimal(data["price"]) : 0m;
                var currency = (Convert.ToString(data.GetValueOrDefault("currency", "MXN")) ?? "MXN").Trim().ToUpperInvariant();
                var durationDays = data.ContainsKey("durationDays") ? Convert.ToInt32(data["durationDays"]) : 30;
                var maxDailyEntries = data.ContainsKey("maxDailyEntries") ? Convert.ToInt32(data["maxDailyEntries"]) : -1;
                var includesClasses = !data.ContainsKey("includesClasses") || Convert.ToBoolean(data["includesClasses"]);
                var includesPremiumClasses = data.ContainsKey("includesPremiumClasses") && Convert.ToBoolean(data["includesPremiumClasses"]);
                var isActive = !data.ContainsKey("isActive") || Convert.ToBoolean(data["isActive"]);

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del plan es requerido";
                    return false;
                }

                if (durationDays <= 0)
                {
                    response["success"] = false;
                    response["message"] = "La duración debe ser mayor a cero días";
                    return false;
                }

                if (price < 0)
                {
                    response["success"] = false;
                    response["message"] = "El precio no puede ser negativo";
                    return false;
                }

                long duplicate = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_MEMBERSHIP_PLANS WHERE PLAN_NAME = ?", name);

                if (duplicate > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe un plan con ese nombre";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long planId = _tools.GenerateUniqueID(GYM_MEMBERSHIP_PLANS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_MEMBERSHIP_PLANS
                {
                    ID = planId,
                    PLAN_NAME = name,
                    DESCRIPTION = description,
                    PRICE = price,
                    CURRENCY = string.IsNullOrWhiteSpace(currency) ? "MXN" : currency,
                    DURATION_DAYS = durationDays,
                    MAX_DAILY_ENTRIES = maxDailyEntries,
                    INCLUDES_CLASSES = includesClasses,
                    INCLUDES_PREMIUM_CLASSES = includesPremiumClasses,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el plan";
                    return false;
                }

                response["success"] = true;
                response["planId"] = planId;
                response["message"] = "Plan creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreatePlan: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Actualiza un plan de membresía.
        /// </summary>
        public bool UpdatePlan(long planId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetPlanState(planId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El plan no existe";
                    return false;
                }

                var name = data.ContainsKey("planName")
                    ? (Convert.ToString(data["planName"]) ?? "").Trim()
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del plan es requerido";
                    return false;
                }

                long duplicate = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_MEMBERSHIP_PLANS WHERE PLAN_NAME = ? AND ID <> ?", name, planId);

                if (duplicate > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe otro plan con ese nombre";
                    return false;
                }

                var durationDays = data.ContainsKey("durationDays") ? Convert.ToInt32(data["durationDays"]) : current.Value.DurationDays;
                if (durationDays <= 0)
                {
                    response["success"] = false;
                    response["message"] = "La duración debe ser mayor a cero días";
                    return false;
                }

                var price = data.ContainsKey("price") ? Convert.ToDecimal(data["price"]) : current.Value.Price;
                if (price < 0)
                {
                    response["success"] = false;
                    response["message"] = "El precio no puede ser negativo";
                    return false;
                }

                var currency = data.ContainsKey("currency")
                    ? (Convert.ToString(data["currency"]) ?? "MXN").Trim().ToUpperInvariant()
                    : current.Value.Currency;

                var maxDailyEntries = data.ContainsKey("maxDailyEntries")
                    ? Convert.ToInt32(data["maxDailyEntries"])
                    : current.Value.MaxDailyEntries;

                var isActive = data.ContainsKey("isActive")
                    ? Convert.ToBoolean(data["isActive"])
                    : current.Value.IsActive;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_MEMBERSHIP_PLANS
                {
                    ID = planId,
                    PLAN_NAME = name,
                    DESCRIPTION = data.ContainsKey("description")
                        ? (Convert.ToString(data["description"]) ?? "").Trim()
                        : current.Value.Description,
                    PRICE = price,
                    CURRENCY = string.IsNullOrWhiteSpace(currency) ? "MXN" : currency,
                    DURATION_DAYS = durationDays,
                    MAX_DAILY_ENTRIES = maxDailyEntries,
                    INCLUDES_CLASSES = data.ContainsKey("includesClasses")
                        ? Convert.ToBoolean(data["includesClasses"])
                        : current.Value.IncludesClasses,
                    INCLUDES_PREMIUM_CLASSES = data.ContainsKey("includesPremiumClasses")
                        ? Convert.ToBoolean(data["includesPremiumClasses"])
                        : current.Value.IncludesPremiumClasses,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el plan";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Plan actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdatePlan: {ex.Message}");
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
                    CURRENCY = "MXN",
                    PAYMENT_METHOD = paymentMethod,
                    PAYMENT_DATE = DateTime.Now,
                    NOTES = "",
                    CASH_SESSION_ID = GymService.Modules.Cash.CashModule.GetOpenSessionId(_tools)
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
        /// Asigna, activa o renueva la membresía de un socio.
        /// Regla: una renovación extiende desde max(hoy, vencimiento actual)
        /// para no perder días ya pagados. No se permite sobre una congelada.
        /// </summary>
        public bool AssignMembership(long memberId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberStatus = GetMemberStatus(memberId);
                if (memberStatus == null)
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (!string.Equals(memberStatus, "active", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "El socio está inactivo";
                    return false;
                }

                long planId = data.ContainsKey("planId") ? Convert.ToInt64(data["planId"]) : 0L;
                var plan = planId > 0 ? GetPlanState(planId) : null;

                if (plan == null || !plan.Value.IsActive)
                {
                    response["success"] = false;
                    response["message"] = "El plan de membresía no existe o está inactivo";
                    return false;
                }

                var latest = GetLatestMembershipState(memberId);

                if (latest != null && string.Equals(latest.Value.Status, "frozen", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "Reanuda la membresía congelada antes de renovarla";
                    return false;
                }

                bool noCharge = data.ContainsKey("noCharge") && Convert.ToBoolean(data["noCharge"]);
                string paymentMethod = (Convert.ToString(data.GetValueOrDefault("paymentMethod", "")) ?? "").Trim().ToLowerInvariant();

                if (!noCharge)
                {
                    if (paymentMethod == "none" || paymentMethod == "courtesy")
                    {
                        noCharge = true;
                        paymentMethod = "";
                    }
                    else if (paymentMethod != "cash" && paymentMethod != "card" && paymentMethod != "transfer")
                    {
                        response["success"] = false;
                        response["message"] = "Método de pago no válido. Usa cash, card o transfer";
                        return false;
                    }
                }

                decimal amount = data.ContainsKey("amount") && Convert.ToString(data["amount"]) != ""
                    ? Convert.ToDecimal(data["amount"])
                    : plan.Value.Price;

                if (!noCharge && amount <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El importe del cobro debe ser mayor a cero";
                    return false;
                }

                bool autoRenew = data.ContainsKey("autoRenew") && Convert.ToBoolean(data["autoRenew"]);
                DateTime today = DateTime.Now.Date;

                var availableCredits = new List<(long Id, decimal Remaining)>();
                if (!noCharge && amount > 0)
                {
                    var creditResult = _tools.ExecuteQueryResultSet(
                        "SELECT ID, REMAINING_AMOUNT FROM GYM_MEMBER_CREDITS " +
                        "WHERE MEMBER_ID = ? AND STATUS = 'available' AND REMAINING_AMOUNT > 0 AND CURRENCY = ? " +
                        "ORDER BY CREATED_AT, ID",
                        memberId, plan.Value.Currency);

                    if (creditResult != null)
                    {
                        foreach (Row creditRow in creditResult.Rows)
                        {
                            availableCredits.Add((
                                Convert.ToInt64(creditRow.GetObject(0)),
                                Convert.ToDecimal(creditRow.GetObject(1))));
                        }
                    }
                }

                DateTime requestedStart = data.ContainsKey("startDate") &&
                    DateTime.TryParse(Convert.ToString(data["startDate"]), out var parsedStart)
                    ? parsedStart.Date
                    : today;

                DateTime startDate;
                DateTime endDate;
                long? membershipId;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                if (latest != null && string.Equals(latest.Value.Status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    // Renovación anticipada: conserva el periodo pagado y suma el nuevo.
                    startDate = latest.Value.EndDate.Date > requestedStart
                        ? latest.Value.EndDate.Date
                        : requestedStart;

                    endDate = startDate.AddDays(plan.Value.DurationDays);
                    membershipId = latest.Value.Id;

                    session.UpdateObjectChanges(new GYM_MEMBERSHIPS
                    {
                        ID = latest.Value.Id,
                        MEMBER_ID = memberId,
                        PLAN_ID = planId,
                        START_DATE = latest.Value.StartDate.Date,
                        END_DATE = endDate,
                        STATUS = "active",
                        AUTO_RENEW = autoRenew,
                        FREEZE_START = null,
                        FREEZE_END = null
                    });
                }
                else
                {
                    startDate = requestedStart < today ? today : requestedStart;
                    endDate = startDate.AddDays(plan.Value.DurationDays);
                    membershipId = _tools.GenerateUniqueID(GYM_MEMBERSHIPS.DatabaseMappingObject, 1).GetInt64UniqueId();

                    session.InsertObjectWithID(new GYM_MEMBERSHIPS
                    {
                        ID = membershipId.Value,
                        MEMBER_ID = memberId,
                        PLAN_ID = planId,
                        START_DATE = startDate,
                        END_DATE = endDate,
                        STATUS = "active",
                        AUTO_RENEW = autoRenew
                    });
                }

                decimal creditApplied = 0m;
                decimal amountPaid = amount;

                if (!noCharge && availableCredits.Count > 0)
                {
                    decimal pending = amount;

                    foreach (var credit in availableCredits)
                    {
                        if (pending <= 0)
                            break;

                        decimal take = credit.Remaining < pending ? credit.Remaining : pending;
                        decimal remaining = credit.Remaining - take;

                        session.UpdateObjectChanges(new GYM_MEMBER_CREDITS
                        {
                            ID = credit.Id,
                            REMAINING_AMOUNT = remaining,
                            STATUS = remaining <= 0 ? "applied" : "available",
                            APPLIED_AT = DateTime.Now
                        });

                        creditApplied += take;
                        pending -= take;
                    }

                    amountPaid = amount - creditApplied;
                }

                if (!noCharge && amountPaid > 0)
                {
                    string paymentNotes = (Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "").Trim();
                    if (creditApplied > 0)
                    {
                        string creditNote = $"Crédito aplicado: {creditApplied:0.00} {plan.Value.Currency}";
                        paymentNotes = paymentNotes.Length > 0 ? $"{creditNote}. {paymentNotes}" : creditNote;
                    }

                    session.InsertObject(new GYM_PAYMENTS
                    {
                        MEMBER_ID = memberId,
                        MEMBERSHIP_ID = membershipId,
                        AMOUNT = amountPaid,
                        CURRENCY = plan.Value.Currency,
                        PAYMENT_METHOD = paymentMethod,
                        PAYMENT_DATE = DateTime.Now,
                        REFERENCE = Convert.ToString(data.GetValueOrDefault("reference", "")) ?? "",
                        NOTES = paymentNotes,
                        CASH_SESSION_ID = GymService.Modules.Cash.CashModule.GetOpenSessionId(_tools)
                    });
                }

                // La activación de la membresía también activa la cuenta del socio.
                session.UpdateObjectChanges(new GYM_MEMBERS { ID = memberId, STATUS = "active" });

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al asignar/renovar la membresía");
                    response["success"] = false;
                    response["message"] = "Error al asignar la membresía";
                    return false;
                }

                Logger.AddInfoSubModule(SubModule,
                    $"Membresía {membershipId} de socio {memberId}: plan '{plan.Value.Name}', {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}, " +
                    (noCharge ? "sin cobro (cortesía)" : $"cobro {paymentMethod} por {amountPaid:0.00} {plan.Value.Currency}" +
                        (creditApplied > 0 ? $" (crédito aplicado {creditApplied:0.00})" : "")));

                response["success"] = true;
                response["membershipId"] = membershipId;
                response["planId"] = planId;
                response["planName"] = plan.Value.Name;
                response["startDate"] = startDate;
                response["endDate"] = endDate;
                response["status"] = "active";
                response["noCharge"] = noCharge;
                response["amount"] = amount;
                response["amountPaid"] = amountPaid;
                response["creditApplied"] = creditApplied;

                string membershipMessage = latest != null && string.Equals(latest.Value.Status, "active", StringComparison.OrdinalIgnoreCase)
                    ? "Membresía renovada correctamente"
                    : "Membresía asignada correctamente";
                if (creditApplied > 0)
                    membershipMessage += $" Se aplicó crédito por {creditApplied:0.00} {plan.Value.Currency}.";

                response["message"] = membershipMessage;

                if (!noCharge && amountPaid > 0)
                {
                    SendMembershipReceipt(
                        memberId,
                        plan.Value.Name,
                        startDate,
                        endDate,
                        amountPaid,
                        plan.Value.Currency,
                        paymentMethod,
                        Convert.ToString(data.GetValueOrDefault("reference", "")) ?? "",
                        creditApplied);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en AssignMembership: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Envía el correo de bienvenida sin bloquear el alta del socio.
        /// </summary>
        private void SendWelcomeEmail(string email, string memberName)
        {
            if (_email == null || !_email.IsEnabled || string.IsNullOrWhiteSpace(email))
                return;

            if (!_email.TrySendWelcome(email, memberName, out var emailError))
                Logger.AddWarningSubModule(SubModule, $"No se envió el correo de bienvenida: {emailError}");
        }

        /// <summary>
        /// Envía el recibo del cobro de membresía sin bloquear la operación.
        /// </summary>
        private void SendMembershipReceipt(
            long memberId,
            string planName,
            DateTime startDate,
            DateTime endDate,
            decimal amount,
            string currency,
            string paymentMethod,
            string reference,
            decimal creditApplied = 0m)
        {
            if (_email == null || !_email.IsEnabled)
                return;

            var contact = GetMemberContact(memberId);
            if (contact == null || string.IsNullOrWhiteSpace(contact.Value.Email))
                return;

            var receipt = new MembershipReceipt
            {
                PlanName = planName,
                StartDate = startDate,
                EndDate = endDate,
                Amount = amount,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Reference = reference,
                PaymentDate = DateTime.Now,
                CreditApplied = creditApplied
            };

            if (!_email.TrySendMembershipReceipt(contact.Value.Email, contact.Value.Name, receipt, out var emailError))
                Logger.AddWarningSubModule(SubModule, $"No se envió el recibo de membresía: {emailError}");
        }

        private (string Name, string Email)? GetMemberContact(long memberId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT FIRST_NAME, LAST_NAME, EMAIL FROM GYM_MEMBERS WHERE ID = ?", memberId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            string name = $"{row.GetObject(0)} {row.GetObject(1)}".Trim();
            string email = Convert.ToString(row.GetObject(2)) ?? "";
            return (name, email);
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

        private (string FirstName, string LastName, string Email, string Phone, string Status)? GetMemberState(long memberId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT FIRST_NAME, LAST_NAME, EMAIL, PHONE, STATUS FROM GYM_MEMBERS WHERE ID = ?", memberId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToString(GetRowValue(row, result, "first_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "last_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                Convert.ToString(GetRowValue(row, result, "phone")) ?? "",
                Convert.ToString(GetRowValue(row, result, "status")) ?? "active"
            );
        }

        private (long Id, long MemberId, long PlanId, string Status, DateTime StartDate, DateTime EndDate)? GetMembershipState(long membershipId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT ID, MEMBER_ID, PLAN_ID, STATUS, START_DATE, END_DATE FROM GYM_MEMBERSHIPS WHERE ID = ?", membershipId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "id")),
                Convert.ToInt64(GetRowValue(row, result, "member_id")),
                Convert.ToInt64(GetRowValue(row, result, "plan_id")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                Convert.ToDateTime(GetRowValue(row, result, "start_date")),
                Convert.ToDateTime(GetRowValue(row, result, "end_date"))
            );
        }

        private string? GetMemberStatus(long memberId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT STATUS FROM GYM_MEMBERS WHERE ID = ?", memberId);

            if (result == null || result.Rows.Count == 0)
                return null;

            return Convert.ToString(GetRowValue(result.Rows[0], result, "status")) ?? "";
        }

        private (long Id, string Name, string Description, decimal Price, string Currency, int DurationDays,
                 int MaxDailyEntries, bool IncludesClasses, bool IncludesPremiumClasses, bool IsActive)? GetPlanState(long planId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_MEMBERSHIP_PLANS WHERE ID = ?", planId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "id")),
                Convert.ToString(GetRowValue(row, result, "plan_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                Convert.ToDecimal(GetRowValue(row, result, "price")),
                Convert.ToString(GetRowValue(row, result, "currency")) ?? "MXN",
                Convert.ToInt32(GetRowValue(row, result, "duration_days")),
                Convert.ToInt32(GetRowValue(row, result, "max_daily_entries")),
                Convert.ToBoolean(GetRowValue(row, result, "includes_classes")),
                Convert.ToBoolean(GetRowValue(row, result, "includes_premium_classes")),
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }

        private (long Id, long PlanId, DateTime StartDate, DateTime EndDate, string Status)? GetLatestMembershipState(long memberId)
        {
            var result = _tools.ExecuteQueryResultSet(@"
                SELECT TOP 1 ID, PLAN_ID, START_DATE, END_DATE, STATUS
                FROM GYM_MEMBERSHIPS
                WHERE MEMBER_ID = ?
                ORDER BY CASE WHEN STATUS = 'active' THEN 0 WHEN STATUS = 'frozen' THEN 1 ELSE 2 END,
                         END_DATE DESC, ID DESC", memberId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "id")),
                Convert.ToInt64(GetRowValue(row, result, "plan_id")),
                Convert.ToDateTime(GetRowValue(row, result, "start_date")),
                Convert.ToDateTime(GetRowValue(row, result, "end_date")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? ""
            );
        }
    }
}
