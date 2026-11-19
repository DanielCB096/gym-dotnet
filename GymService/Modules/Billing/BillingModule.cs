using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Billing
{
    /// <summary>
    /// Billing module: facturación y gestión de facturas.
    /// </summary>
    public class BillingModule
    {
        private const string SubModule = "BILLING_MODULE";
        private DatabaseTools _tools;

        public BillingModule(DatabaseTools tools)
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
        /// Create a new invoice
        /// </summary>
        public bool CreateInvoice(Dictionary<string, object> invoiceData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberId = invoiceData.ContainsKey("memberId") ? Convert.ToInt64(invoiceData["memberId"]) : 0L;
                var invoiceNumber = Convert.ToString(invoiceData.GetValueOrDefault("invoiceNumber", "")) ?? "";
                var amount = invoiceData.ContainsKey("amount") ? Convert.ToDecimal(invoiceData["amount"]) : 0m;
                var status = Convert.ToString(invoiceData.GetValueOrDefault("status", "pending")) ?? "pending";

                if (string.IsNullOrEmpty(invoiceNumber))
                {
                    invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                var session = _tools.CreateSession(SubModule);
                session.InsertObject(new GYM_INVOICES
                {
                    MEMBER_ID = memberId,
                    INVOICE_NUMBER = invoiceNumber,
                    AMOUNT = amount,
                    TAX_AMOUNT = 0m,
                    STATUS = status,
                    INVOICE_DATE = DateTime.Now
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Factura creada correctamente";
                    response["invoiceNumber"] = invoiceNumber;
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la factura";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateInvoice: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Update invoice status
        /// </summary>
        public bool UpdateInvoiceStatus(long invoiceId, string status, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.UpdateObjectChanges(new GYM_INVOICES
                {
                    ID = invoiceId,
                    STATUS = status
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Estado de factura actualizado";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la factura";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateInvoiceStatus: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete an invoice
        /// </summary>
        public bool DeleteInvoice(long invoiceId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.DeleteObject(new GYM_INVOICES { ID = invoiceId });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Factura eliminada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la factura";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteInvoice: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get invoices list with optional filter by member and pagination
        /// </summary>
        public bool GetInvoices(out Dictionary<string, object> response, long? memberId = null, int page = 1, int limit = 50)
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

                if (memberId.HasValue)
                {
                    whereParts.Add("member_id = ?");
                    parameters.Add(memberId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_invoices {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT * FROM gym_invoices {whereClause} ORDER BY invoice_date DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var invoices = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        invoices.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["invoiceNumber"] = Convert.ToString(GetRowValue(row, result, "invoice_number")) ?? "",
                            ["amount"] = Convert.ToDecimal(GetRowValue(row, result, "amount")),
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "pending",
                            ["invoiceDate"] = GetRowValue(row, result, "invoice_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "invoice_date")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = invoices;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetInvoices: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
