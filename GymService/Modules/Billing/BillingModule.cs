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

        private static readonly HashSet<string> InvoiceStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending", "paid", "cancelled"
        };

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
        /// Crea una factura para un socio.
        /// </summary>
        public bool CreateInvoice(Dictionary<string, object> invoiceData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberId = invoiceData.ContainsKey("memberId") ? Convert.ToInt64(invoiceData["memberId"]) : 0L;
                var invoiceNumber = (Convert.ToString(invoiceData.GetValueOrDefault("invoiceNumber", "")) ?? "").Trim();
                var amount = invoiceData.ContainsKey("amount") ? Convert.ToDecimal(invoiceData["amount"]) : 0m;
                var taxAmount = invoiceData.ContainsKey("taxAmount") && Convert.ToString(invoiceData["taxAmount"]) != ""
                    ? Convert.ToDecimal(invoiceData["taxAmount"])
                    : 0m;
                var status = (Convert.ToString(invoiceData.GetValueOrDefault("status", "pending")) ?? "pending").Trim().ToLowerInvariant();

                if (!MemberExists(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (amount <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El importe de la factura debe ser mayor a cero";
                    return false;
                }

                if (taxAmount < 0 || taxAmount > amount)
                {
                    response["success"] = false;
                    response["message"] = "El IVA no puede ser negativo ni mayor que el importe";
                    return false;
                }

                if (!InvoiceStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de factura no válido";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    invoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
                }

                long duplicate = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_INVOICES WHERE INVOICE_NUMBER = ?", invoiceNumber);

                if (duplicate > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe una factura con ese número";
                    return false;
                }

                DateTime invoiceDate = invoiceData.ContainsKey("invoiceDate") &&
                    DateTime.TryParse(Convert.ToString(invoiceData["invoiceDate"]), out var parsedDate)
                    ? parsedDate
                    : DateTime.Now;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long invoiceId = _tools.GenerateUniqueID(GYM_INVOICES.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_INVOICES
                {
                    ID = invoiceId,
                    MEMBER_ID = memberId,
                    INVOICE_NUMBER = invoiceNumber,
                    AMOUNT = amount,
                    TAX_AMOUNT = taxAmount,
                    STATUS = status,
                    INVOICE_DATE = invoiceDate,
                    UUID = Convert.ToString(invoiceData.GetValueOrDefault("uuid", "")) ?? "",
                    PDF_URL = Convert.ToString(invoiceData.GetValueOrDefault("pdfUrl", "")) ?? "",
                    XML_URL = Convert.ToString(invoiceData.GetValueOrDefault("xmlUrl", "")) ?? ""
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la factura";
                    return false;
                }

                response["success"] = true;
                response["invoiceId"] = invoiceId;
                response["invoiceNumber"] = invoiceNumber;
                response["message"] = "Factura creada correctamente";
                return true;
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
        /// Cambia el estado de una factura.
        /// </summary>
        public bool UpdateInvoiceStatus(long invoiceId, string status, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                status = (status ?? "").Trim().ToLowerInvariant();

                if (!InvoiceStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de factura no válido";
                    return false;
                }

                if (GetInvoiceState(invoiceId) == null)
                {
                    response["success"] = false;
                    response["message"] = "La factura no existe";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_INVOICES
                {
                    ID = invoiceId,
                    STATUS = status
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la factura";
                    return false;
                }

                response["success"] = true;
                response["status"] = status;
                response["message"] = status switch
                {
                    "paid" => "Factura marcada como pagada",
                    "cancelled" => "Factura cancelada",
                    _ => "Factura marcada como pendiente"
                };
                return true;
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
        /// Elimina una factura pendiente.
        /// </summary>
        public bool DeleteInvoice(long invoiceId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var invoice = GetInvoiceState(invoiceId);
                if (invoice == null)
                {
                    response["success"] = false;
                    response["message"] = "La factura no existe";
                    return false;
                }

                if (!string.Equals(invoice.Value.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden eliminar facturas pendientes; cancela las facturas pagadas para conservar el historial";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_INVOICES { ID = invoiceId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la factura";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Factura eliminada correctamente";
                return true;
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
        /// Lista facturas con filtros por socio y estado.
        /// </summary>
        public bool GetInvoices(
            out Dictionary<string, object> response,
            long? memberId = null,
            string? status = null,
            string? query = null,
            int page = 1,
            int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (memberId.HasValue)
                {
                    whereParts.Add("i.member_id = ?");
                    parameters.Add(memberId.Value);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    whereParts.Add("i.status = ?");
                    parameters.Add(status.Trim().ToLowerInvariant());
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    whereParts.Add("(i.invoice_number LIKE ? OR m.first_name LIKE ? OR m.last_name LIKE ?)");
                    parameters.Add("%" + query.Trim() + "%");
                    parameters.Add("%" + query.Trim() + "%");
                    parameters.Add("%" + query.Trim() + "%");
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM gym_invoices i LEFT JOIN gym_members m ON m.ID = i.member_id " + whereClause,
                    parameters.ToArray()
                );

                parameters.Add(offset);
                parameters.Add(limit);

                var sql =
                    "SELECT i.*, m.FIRST_NAME, m.LAST_NAME " +
                    "FROM gym_invoices i " +
                    "LEFT JOIN gym_members m ON m.ID = i.member_id " +
                    $"{whereClause} ORDER BY i.invoice_date DESC, i.ID DESC " +
                    "OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var invoices = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        decimal amount = Convert.ToDecimal(GetRowValue(row, result, "amount"));
                        decimal taxAmount = GetRowValue(row, result, "tax_amount") != null
                            ? Convert.ToDecimal(GetRowValue(row, result, "tax_amount"))
                            : 0m;

                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        invoices.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["invoiceNumber"] = Convert.ToString(GetRowValue(row, result, "invoice_number")) ?? "",
                            ["amount"] = amount,
                            ["taxAmount"] = taxAmount,
                            ["subtotal"] = amount - taxAmount,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "pending",
                            ["invoiceDate"] = GetRowValue(row, result, "invoice_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "invoice_date")) : (DateTime?)null,
                            ["uuid"] = Convert.ToString(GetRowValue(row, result, "uuid")) ?? "",
                            ["pdfUrl"] = Convert.ToString(GetRowValue(row, result, "pdf_url")) ?? "",
                            ["xmlUrl"] = Convert.ToString(GetRowValue(row, result, "xml_url")) ?? ""
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

        // =====================================================================
        // Helpers
        // =====================================================================

        private bool MemberExists(long memberId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = ?", memberId) > 0;
        }

        private (long MemberId, string Status)? GetInvoiceState(long invoiceId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT MEMBER_ID, STATUS FROM GYM_INVOICES WHERE ID = ?", invoiceId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "member_id")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? ""
            );
        }
    }
}
