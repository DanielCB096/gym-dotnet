using System;
using System.Collections.Generic;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Cash
{
    /// <summary>
    /// Caja: turnos de caja y cierre con efectivo esperado.
    /// </summary>
    public class CashModule
    {
        private const string SubModule = "CASH_MODULE";
        private DatabaseTools _tools;

        public CashModule(DatabaseTools tools)
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

        public bool GetCurrent(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = GetOpenSessionState();
                response["success"] = true;
                response["data"] = session == null ? null : BuildSummary(session.Value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetCurrent: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool Open(decimal openingAmount, string notes, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (openingAmount < 0)
                {
                    response["success"] = false;
                    response["message"] = "El fondo inicial no puede ser negativo";
                    return false;
                }

                if (GetOpenSessionState() != null)
                {
                    response["success"] = false;
                    response["message"] = "Ya hay una caja abierta";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_CASH_SESSIONS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_CASH_SESSIONS
                {
                    ID = id,
                    OPENED_AT = DateTime.Now,
                    OPENED_BY = CurrentUser.ForAudit > 0 ? CurrentUser.ForAudit : 1,
                    OPENING_AMOUNT = openingAmount,
                    STATUS = "open",
                    NOTES = notes ?? ""
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al abrir la caja";
                    return false;
                }

                response["success"] = true;
                response["cashSessionId"] = id;
                response["message"] = "Caja abierta correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en Open: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool Close(long cashSessionId, decimal closingAmount, string notes, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetSessionState(cashSessionId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El turno de caja no existe";
                    return false;
                }

                if (!string.Equals(current.Value.Status, "open", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "El turno de caja ya está cerrado";
                    return false;
                }

                if (closingAmount < 0)
                {
                    response["success"] = false;
                    response["message"] = "El efectivo contado no puede ser negativo";
                    return false;
                }

                var totals = GetTotals(cashSessionId);
                decimal expected = current.Value.OpeningAmount + totals.CashSales + totals.CashMemberships;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_CASH_SESSIONS
                {
                    ID = cashSessionId,
                    CLOSED_AT = DateTime.Now,
                    CLOSED_BY = CurrentUser.ForAudit > 0 ? CurrentUser.ForAudit : 1,
                    CLOSING_AMOUNT = closingAmount,
                    EXPECTED_CASH = expected,
                    STATUS = "closed",
                    NOTES = string.IsNullOrWhiteSpace(notes) ? current.Value.Notes : notes
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al cerrar la caja";
                    return false;
                }

                response["success"] = true;
                response["expectedCash"] = expected;
                response["closingAmount"] = closingAmount;
                response["difference"] = closingAmount - expected;
                response["message"] = "Caja cerrada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en Close: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool GetSessions(out Dictionary<string, object> response, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 200) limit = 50;
                int offset = (page - 1) * limit;

                long total = _tools.ExecuteQueryObjectCount("SELECT COUNT(*) FROM GYM_CASH_SESSIONS");

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM GYM_CASH_SESSIONS ORDER BY OPENED_AT DESC, ID DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    offset, limit);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["openedAt"] = GetRowValue(row, result, "opened_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "opened_at")) : (DateTime?)null,
                            ["closedAt"] = GetRowValue(row, result, "closed_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "closed_at")) : (DateTime?)null,
                            ["openingAmount"] = Convert.ToDecimal(GetRowValue(row, result, "opening_amount")),
                            ["closingAmount"] = GetRowValue(row, result, "closing_amount") != null ? Convert.ToDecimal(GetRowValue(row, result, "closing_amount")) : (decimal?)null,
                            ["expectedCash"] = GetRowValue(row, result, "expected_cash") != null ? Convert.ToDecimal(GetRowValue(row, result, "expected_cash")) : (decimal?)null,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["notes"] = Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((total + limit - 1) / limit) : 0;
                response["success"] = true;
                response["data"] = items;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = total,
                    ["totalPages"] = totalPages,
                    ["hasNext"] = page < totalPages,
                    ["hasPrev"] = page > 1
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

        public static long? GetOpenSessionId(DatabaseTools tools)
        {
            var result = tools.ExecuteQueryResultSet(
                "SELECT TOP 1 ID FROM GYM_CASH_SESSIONS WHERE STATUS = 'open' ORDER BY OPENED_AT DESC");

            if (result == null || result.Rows.Count == 0)
                return null;

            return Convert.ToInt64(result.Rows[0].GetObject(0));
        }

        private (long Id, DateTime OpenedAt, long OpenedBy, decimal OpeningAmount, string Status, string Notes)? GetOpenSessionState()
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT TOP 1 * FROM GYM_CASH_SESSIONS WHERE STATUS = 'open' ORDER BY OPENED_AT DESC");

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return (
                Convert.ToInt64(GetRowValue(row, result, "id")),
                Convert.ToDateTime(GetRowValue(row, result, "opened_at")),
                Convert.ToInt64(GetRowValue(row, result, "opened_by")),
                Convert.ToDecimal(GetRowValue(row, result, "opening_amount")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
            );
        }

        private (long Id, decimal OpeningAmount, string Status, string Notes)? GetSessionState(long id)
        {
            var result = _tools.ExecuteQueryResultSet("SELECT * FROM GYM_CASH_SESSIONS WHERE ID = ?", id);
            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return (
                Convert.ToInt64(GetRowValue(row, result, "id")),
                Convert.ToDecimal(GetRowValue(row, result, "opening_amount")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
            );
        }

        private (decimal CashSales, int SalesCount, decimal CashMemberships, int PaymentsCount) GetTotals(long cashSessionId)
        {
            decimal cashSales = 0m;
            long salesCount = 0;
            decimal cashMemberships = 0m;
            long paymentsCount = 0;

            var result = _tools.ExecuteQueryResultSet(@"
                SELECT
                  (SELECT ISNULL(SUM(TOTAL_AMOUNT),0) FROM GYM_SALES WHERE CASH_SESSION_ID = ? AND PAYMENT_METHOD = 'cash' AND STATUS = 'completed') AS CASH_SALES,
                  (SELECT COUNT(*) FROM GYM_SALES WHERE CASH_SESSION_ID = ? AND PAYMENT_METHOD = 'cash' AND STATUS = 'completed') AS SALES_COUNT,
                  (SELECT ISNULL(SUM(AMOUNT),0) FROM GYM_PAYMENTS WHERE CASH_SESSION_ID = ? AND PAYMENT_METHOD = 'cash') AS CASH_MEMBERSHIPS,
                  (SELECT COUNT(*) FROM GYM_PAYMENTS WHERE CASH_SESSION_ID = ? AND PAYMENT_METHOD = 'cash') AS PAYMENTS_COUNT",
                cashSessionId, cashSessionId, cashSessionId, cashSessionId);

            if (result != null && result.Rows.Count > 0)
            {
                var row = result.Rows[0];
                cashSales = Convert.ToDecimal(GetRowValue(row, result, "cash_sales"));
                salesCount = Convert.ToInt64(GetRowValue(row, result, "sales_count"));
                cashMemberships = Convert.ToDecimal(GetRowValue(row, result, "cash_memberships"));
                paymentsCount = Convert.ToInt64(GetRowValue(row, result, "payments_count"));
            }

            return (cashSales, (int)salesCount, cashMemberships, (int)paymentsCount);
        }

        private Dictionary<string, object> BuildSummary((long Id, DateTime OpenedAt, long OpenedBy, decimal OpeningAmount, string Status, string Notes) session)
        {
            var totals = GetTotals(session.Id);
            decimal expected = session.OpeningAmount + totals.CashSales + totals.CashMemberships;

            return new Dictionary<string, object>
            {
                ["id"] = session.Id,
                ["openedAt"] = session.OpenedAt,
                ["openedBy"] = session.OpenedBy,
                ["openingAmount"] = session.OpeningAmount,
                ["cashSales"] = totals.CashSales,
                ["salesCount"] = totals.SalesCount,
                ["cashMemberships"] = totals.CashMemberships,
                ["paymentsCount"] = totals.PaymentsCount,
                ["expectedCash"] = expected,
                ["status"] = session.Status,
                ["notes"] = session.Notes
            };
        }
    }
}
