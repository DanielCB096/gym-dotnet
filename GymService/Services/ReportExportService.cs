using System;
using System.Collections.Generic;
using System.IO;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using ClosedXML.Excel;

namespace GymService.Services
{
    /// <summary>
    /// Service for exporting reports to Excel format.
    /// Supports sales, members, attendance, and inventory reports.
    /// </summary>
    public class ReportExportService
    {
        private const string SubModule = "REPORT_EXPORT";
        private DatabaseTools _tools;

        public ReportExportService(DatabaseTools tools)
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
        /// Export sales report to Excel for a date range.
        /// </summary>
        public byte[] ExportSalesToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var whereParts = new List<string>();
                if (startDate.HasValue) whereParts.Add("sale_date >= ?");
                if (endDate.HasValue) whereParts.Add("sale_date <= ?");
                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = $"SELECT s.*, m.first_name + ' ' + m.last_name as member_name " +
                    $"FROM gym_sales s LEFT JOIN gym_members m ON s.member_id = m.id {whereClause} " +
                    $"ORDER BY s.sale_date DESC";

                var parameters = new List<object>();
                if (startDate.HasValue) parameters.Add(startDate.Value);
                if (endDate.HasValue) parameters.Add(endDate.Value);

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Ventas");

                var headers = new[] { "No. Venta", "Fecha", "Miembro", "Total", "Impuestos", "Método de Pago" };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.DarkBlue);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        worksheet.Cell(row, 1).Value = Convert.ToString(GetRowValue(r, result, "sale_number")) ?? "";
                        worksheet.Cell(row, 2).Value = Convert.ToDateTime(GetRowValue(r, result, "sale_date")).ToString("dd/MM/yyyy HH:mm");
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "member_name")) ?? "Público";
                        worksheet.Cell(row, 4).Value = Convert.ToDecimal(GetRowValue(r, result, "total_amount"));
                        worksheet.Cell(row, 5).Value = Convert.ToDecimal(GetRowValue(r, result, "tax_amount"));
                        worksheet.Cell(row, 6).Value = Convert.ToString(GetRowValue(r, result, "payment_method")) ?? "";
                        row++;
                    }
                }

                worksheet.Cell(row, 1).Value = "TOTAL:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 4).FormulaA1 = $"=SUM(D2:D{row - 1})";
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 5).FormulaA1 = $"=SUM(E2:E{row - 1})";
                worksheet.Cell(row, 5).Style.Font.Bold = true;

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting sales to Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Export members list to Excel.
        /// </summary>
        public byte[] ExportMembersToExcel(string? status = null)
        {
            try
            {
                var whereParts = new List<string>();
                if (!string.IsNullOrEmpty(status)) whereParts.Add("m.status = ?");
                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = $"SELECT m.*, mem.status as membership_status, mem.end_date " +
                    $"FROM gym_members m LEFT JOIN gym_memberships mem ON m.id = mem.member_id AND mem.status = 'active' " +
                    $"{whereClause} ORDER BY m.last_name, m.first_name";

                var parameters = new List<object>();
                if (!string.IsNullOrEmpty(status)) parameters.Add(status);

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Miembros");

                var headers = new[] { "ID", "Nombre", "Apellido", "Email", "Teléfono", "CURP", "Estado", "Membresía", "Vence" };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.DarkGreen);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        worksheet.Cell(row, 1).Value = Convert.ToInt64(GetRowValue(r, result, "id"));
                        worksheet.Cell(row, 2).Value = Convert.ToString(GetRowValue(r, result, "first_name")) ?? "";
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "last_name")) ?? "";
                        worksheet.Cell(row, 4).Value = Convert.ToString(GetRowValue(r, result, "email")) ?? "";
                        worksheet.Cell(row, 5).Value = Convert.ToString(GetRowValue(r, result, "phone")) ?? "";
                        worksheet.Cell(row, 6).Value = Convert.ToString(GetRowValue(r, result, "curp")) ?? "";
                        worksheet.Cell(row, 7).Value = Convert.ToString(GetRowValue(r, result, "status")) ?? "";
                        worksheet.Cell(row, 8).Value = Convert.ToString(GetRowValue(r, result, "membership_status")) ?? "";
                        var endDate = GetRowValue(r, result, "end_date");
                        worksheet.Cell(row, 9).Value = endDate != null ? Convert.ToDateTime(endDate).ToString("dd/MM/yyyy") : "";
                        row++;
                    }
                }

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting members to Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Export attendance report to Excel for a date range.
        /// </summary>
        public byte[] ExportAttendanceToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var whereParts = new List<string>();
                if (startDate.HasValue) whereParts.Add("al.accessed_at >= ?");
                if (endDate.HasValue) whereParts.Add("al.accessed_at <= ?");
                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = $"SELECT al.*, m.first_name + ' ' + m.last_name as member_name " +
                    $"FROM gym_access_log al LEFT JOIN gym_members m ON al.member_id = m.id {whereClause} " +
                    $"ORDER BY al.accessed_at DESC";

                var parameters = new List<object>();
                if (startDate.HasValue) parameters.Add(startDate.Value);
                if (endDate.HasValue) parameters.Add(endDate.Value);

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Asistencia");

                var headers = new[] { "Fecha", "Hora", "Miembro", "Tipo", "Resultado", "Dispositivo", "Motivo" };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.DarkOrange);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        var accessedAt = Convert.ToDateTime(GetRowValue(r, result, "accessed_at"));
                        worksheet.Cell(row, 1).Value = accessedAt.ToString("dd/MM/yyyy");
                        worksheet.Cell(row, 2).Value = accessedAt.ToString("HH:mm:ss");
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "member_name")) ?? "";
                        worksheet.Cell(row, 4).Value = Convert.ToString(GetRowValue(r, result, "access_type")) ?? "";
                        worksheet.Cell(row, 5).Value = Convert.ToString(GetRowValue(r, result, "access_result")) ?? "";
                        worksheet.Cell(row, 6).Value = Convert.ToString(GetRowValue(r, result, "device_id")) ?? "";
                        worksheet.Cell(row, 7).Value = Convert.ToString(GetRowValue(r, result, "reason")) ?? "";
                        row++;
                    }
                }

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting attendance to Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Export inventory report to Excel.
        /// </summary>
        public byte[] ExportInventoryToExcel()
        {
            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_inventory WHERE is_active = 1 ORDER BY product_name"
                );

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Inventario");

                var headers = new[] { "Producto", "SKU", "Categoría", "Precio", "Costo", "Stock", "Stock Mín.", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.Purple);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        worksheet.Cell(row, 1).Value = Convert.ToString(GetRowValue(r, result, "product_name")) ?? "";
                        worksheet.Cell(row, 2).Value = Convert.ToString(GetRowValue(r, result, "sku")) ?? "";
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "category")) ?? "";
                        worksheet.Cell(row, 4).Value = Convert.ToDecimal(GetRowValue(r, result, "price"));
                        worksheet.Cell(row, 5).Value = GetRowValue(r, result, "cost") != null ? Convert.ToDecimal(GetRowValue(r, result, "cost")) : 0m;
                        var stockCol = GetColumnIndex(result, "stock_quantity");
                        if (stockCol < 0) stockCol = GetColumnIndex(result, "stock");
                        var stock = stockCol >= 0 ? Convert.ToInt32(r.GetObject(stockCol)) : 0;
                        worksheet.Cell(row, 6).Value = stock;
                        worksheet.Cell(row, 7).Value = Convert.ToInt32(GetRowValue(r, result, "min_stock"));

                        var minStock = Convert.ToInt32(GetRowValue(r, result, "min_stock"));
                        if (stock <= minStock)
                            worksheet.Cell(row, 6).Style.Fill.SetBackgroundColor(XLColor.Orange);

                        worksheet.Cell(row, 8).Value = Convert.ToBoolean(GetRowValue(r, result, "is_active")) ? "Activo" : "Inactivo";
                        row++;
                    }
                }

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting inventory to Excel: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Export cash sessions (shift totals) to Excel for a date range.
        /// </summary>
        public byte[] ExportCashToExcel(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var whereParts = new List<string>();
                if (startDate.HasValue) whereParts.Add("cs.opened_at >= ?");
                if (endDate.HasValue) whereParts.Add("cs.opened_at <= ?");
                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = "SELECT cs.id, cs.opened_at, cs.closed_at, cs.opening_amount, cs.closing_amount, " +
                    "cs.expected_cash, cs.status, cs.notes, " +
                    "uo.username AS opened_by_name, uc.username AS closed_by_name, " +
                    "(SELECT ISNULL(SUM(s.total_amount),0) FROM gym_sales s " +
                    " WHERE s.cash_session_id = cs.id AND s.status = 'completed') AS sales_total, " +
                    "(SELECT ISNULL(SUM(p.amount),0) FROM gym_payments p " +
                    " WHERE p.cash_session_id = cs.id) AS payments_total " +
                    "FROM gym_cash_sessions cs " +
                    "LEFT JOIN gym_users uo ON uo.id = cs.opened_by " +
                    "LEFT JOIN gym_users uc ON uc.id = cs.closed_by " +
                    $"{whereClause} ORDER BY cs.opened_at DESC";

                var parameters = new List<object>();
                if (startDate.HasValue) parameters.Add(startDate.Value);
                if (endDate.HasValue) parameters.Add(endDate.Value);

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Caja");

                var headers = new[]
                {
                    "Folio", "Apertura", "Abrió", "Cierre", "Cerró", "Fondo inicial",
                    "Ventas", "Cobros", "Efectivo esperado", "Efectivo contado", "Diferencia", "Estado", "Notas"
                };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.DarkRed);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                decimal totalOpening = 0, totalSales = 0, totalPayments = 0, totalExpected = 0, totalClosing = 0;
                int row = 2;

                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        var opening = ToDecimal(GetRowValue(r, result, "opening_amount"));
                        var sales = ToDecimal(GetRowValue(r, result, "sales_total"));
                        var payments = ToDecimal(GetRowValue(r, result, "payments_total"));
                        var expected = GetRowValue(r, result, "expected_cash") == DBNull.Value
                            ? (decimal?)null
                            : ToDecimal(GetRowValue(r, result, "expected_cash"));
                        var closing = GetRowValue(r, result, "closing_amount") == DBNull.Value
                            ? (decimal?)null
                            : ToDecimal(GetRowValue(r, result, "closing_amount"));

                        var effectiveExpected = expected ?? opening + sales + payments;
                        var difference = closing.HasValue ? closing.Value - effectiveExpected : (decimal?)null;

                        worksheet.Cell(row, 1).Value = Convert.ToInt64(GetRowValue(r, result, "id"));
                        worksheet.Cell(row, 2).Value = FormatDateTime(GetRowValue(r, result, "opened_at"));
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "opened_by_name")) ?? "";
                        worksheet.Cell(row, 4).Value = FormatDateTime(GetRowValue(r, result, "closed_at"));
                        worksheet.Cell(row, 5).Value = Convert.ToString(GetRowValue(r, result, "closed_by_name")) ?? "";
                        worksheet.Cell(row, 6).Value = opening;
                        worksheet.Cell(row, 7).Value = sales;
                        worksheet.Cell(row, 8).Value = payments;
                        worksheet.Cell(row, 9).Value = effectiveExpected;
                        worksheet.Cell(row, 10).Value = closing ?? 0m;
                        worksheet.Cell(row, 11).Value = difference ?? 0m;
                        worksheet.Cell(row, 12).Value = Convert.ToString(GetRowValue(r, result, "status")) ?? "";
                        worksheet.Cell(row, 13).Value = Convert.ToString(GetRowValue(r, result, "notes")) ?? "";

                        totalOpening += opening;
                        totalSales += sales;
                        totalPayments += payments;
                        totalExpected += effectiveExpected;
                        totalClosing += closing ?? 0m;
                        row++;
                    }
                }

                worksheet.Cell(row, 1).Value = "TOTAL:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Value = totalOpening;
                worksheet.Cell(row, 7).Value = totalSales;
                worksheet.Cell(row, 8).Value = totalPayments;
                worksheet.Cell(row, 9).Value = totalExpected;
                worksheet.Cell(row, 10).Value = totalClosing;
                worksheet.Cell(row, 11).Value = totalClosing - totalExpected;
                worksheet.Range(row, 6, row, 11).Style.Font.Bold = true;

                for (int col = 6; col <= 11; col++)
                    worksheet.Column(col).Style.NumberFormat.Format = "#,##0.00";

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting cash to Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Export memberships to Excel with optional status and start-date filters.
        /// </summary>
        public byte[] ExportMembershipsToExcel(string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!string.IsNullOrEmpty(status))
                {
                    whereParts.Add("mem.status = ?");
                    parameters.Add(status);
                }
                if (startDate.HasValue)
                {
                    whereParts.Add("mem.start_date >= ?");
                    parameters.Add(startDate.Value);
                }
                if (endDate.HasValue)
                {
                    whereParts.Add("mem.start_date <= ?");
                    parameters.Add(endDate.Value);
                }

                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = "SELECT mem.id, mem.start_date, mem.end_date, mem.status, mem.auto_renew, " +
                    "mem.freeze_start, mem.freeze_end, mem.cancelled_at, mem.cancel_reason, " +
                    "m.first_name + ' ' + m.last_name AS member_name, m.email, " +
                    "pl.plan_name, p.amount AS last_payment " +
                    "FROM gym_memberships mem " +
                    "INNER JOIN gym_members m ON m.id = mem.member_id " +
                    "LEFT JOIN gym_membership_plans pl ON pl.id = mem.plan_id " +
                    "OUTER APPLY (SELECT TOP 1 amount FROM gym_payments p " +
                    "             WHERE p.membership_id = mem.id ORDER BY p.payment_date DESC, p.id DESC) p " +
                    $"{whereClause} ORDER BY mem.start_date DESC";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Membresías");

                var headers = new[]
                {
                    "ID", "Socio", "Email", "Plan", "Inicio", "Vence", "Estado", "Renovación automática",
                    "Congelada desde", "Congelada hasta", "Cancelada", "Motivo de cancelación", "Último pago"
                };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.DarkGreen);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        worksheet.Cell(row, 1).Value = Convert.ToInt64(GetRowValue(r, result, "id"));
                        worksheet.Cell(row, 2).Value = Convert.ToString(GetRowValue(r, result, "member_name")) ?? "";
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "email")) ?? "";
                        worksheet.Cell(row, 4).Value = Convert.ToString(GetRowValue(r, result, "plan_name")) ?? "";
                        worksheet.Cell(row, 5).Value = FormatDate(GetRowValue(r, result, "start_date"));
                        worksheet.Cell(row, 6).Value = FormatDate(GetRowValue(r, result, "end_date"));
                        worksheet.Cell(row, 7).Value = Convert.ToString(GetRowValue(r, result, "status")) ?? "";
                        worksheet.Cell(row, 8).Value = ToBool(GetRowValue(r, result, "auto_renew")) ? "Sí" : "No";
                        worksheet.Cell(row, 9).Value = FormatDate(GetRowValue(r, result, "freeze_start"));
                        worksheet.Cell(row, 10).Value = FormatDate(GetRowValue(r, result, "freeze_end"));
                        worksheet.Cell(row, 11).Value = FormatDateTime(GetRowValue(r, result, "cancelled_at"));
                        worksheet.Cell(row, 12).Value = Convert.ToString(GetRowValue(r, result, "cancel_reason")) ?? "";
                        var lastPayment = GetRowValue(r, result, "last_payment");
                        worksheet.Cell(row, 13).Value = lastPayment == DBNull.Value ? 0m : ToDecimal(lastPayment);
                        row++;
                    }
                }

                worksheet.Column(13).Style.NumberFormat.Format = "#,##0.00";
                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting memberships to Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Export inventory movements (kardex) to Excel with optional product and date filters.
        /// </summary>
        public byte[] ExportKardexToExcel(long? productId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (productId.HasValue)
                {
                    whereParts.Add("im.product_id = ?");
                    parameters.Add(productId.Value);
                }
                if (startDate.HasValue)
                {
                    whereParts.Add("im.movement_date >= ?");
                    parameters.Add(startDate.Value);
                }
                if (endDate.HasValue)
                {
                    whereParts.Add("im.movement_date <= ?");
                    parameters.Add(endDate.Value);
                }

                var whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var sql = "SELECT im.id, im.movement_date, im.movement_type, im.quantity_delta, " +
                    "im.previous_stock, im.new_stock, im.reference_type, im.reference_id, im.notes, " +
                    "i.product_name, i.sku " +
                    "FROM gym_inventory_movements im " +
                    "INNER JOIN gym_inventory i ON i.id = im.product_id " +
                    $"{whereClause} ORDER BY im.movement_date DESC, im.id DESC";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Kardex");

                var headers = new[]
                {
                    "Fecha", "Producto", "SKU", "Tipo", "Movimiento", "Stock anterior", "Stock nuevo", "Referencia", "Notas"
                };
                for (int i = 0; i < headers.Length; i++)
                    worksheet.Cell(1, i + 1).Value = headers[i];

                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Fill.SetBackgroundColor(XLColor.Purple);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.SetFontColor(XLColor.White);

                int row = 2;
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row r in result.Rows)
                    {
                        var referenceType = Convert.ToString(GetRowValue(r, result, "reference_type")) ?? "";
                        var referenceId = GetRowValue(r, result, "reference_id");
                        var reference = referenceType.Length == 0
                            ? ""
                            : referenceId == DBNull.Value ? referenceType : $"{referenceType} #{referenceId}";

                        worksheet.Cell(row, 1).Value = FormatDateTime(GetRowValue(r, result, "movement_date"));
                        worksheet.Cell(row, 2).Value = Convert.ToString(GetRowValue(r, result, "product_name")) ?? "";
                        worksheet.Cell(row, 3).Value = Convert.ToString(GetRowValue(r, result, "sku")) ?? "";
                        worksheet.Cell(row, 4).Value = Convert.ToString(GetRowValue(r, result, "movement_type")) ?? "";
                        worksheet.Cell(row, 5).Value = Convert.ToInt32(GetRowValue(r, result, "quantity_delta"));
                        worksheet.Cell(row, 6).Value = Convert.ToInt32(GetRowValue(r, result, "previous_stock"));
                        worksheet.Cell(row, 7).Value = Convert.ToInt32(GetRowValue(r, result, "new_stock"));
                        worksheet.Cell(row, 8).Value = reference;
                        worksheet.Cell(row, 9).Value = Convert.ToString(GetRowValue(r, result, "notes")) ?? "";
                        row++;
                    }
                }

                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule(SubModule, $"Error exporting kardex to Excel: {ex.Message}");
                throw;
            }
        }

        private static decimal ToDecimal(object? value)
        {
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static bool ToBool(object? value)
        {
            return value != null && value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static string FormatDate(object? value)
        {
            return value == null || value == DBNull.Value
                ? ""
                : Convert.ToDateTime(value).ToString("dd/MM/yyyy");
        }

        private static string FormatDateTime(object? value)
        {
            return value == null || value == DBNull.Value
                ? ""
                : Convert.ToDateTime(value).ToString("dd/MM/yyyy HH:mm");
        }
    }
}
