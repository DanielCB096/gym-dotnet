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
                var whereClause = string.IsNullOrEmpty(status) ? "" : $"WHERE m.status = '{status}'";
                var sql = $"SELECT m.*, mem.status as membership_status, mem.end_date " +
                    $"FROM gym_members m LEFT JOIN gym_memberships mem ON m.id = mem.member_id AND mem.status = 'active' " +
                    $"{whereClause} ORDER BY m.last_name, m.first_name";

                var result = _tools.ExecuteQueryResultSet(sql);

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
    }
}
