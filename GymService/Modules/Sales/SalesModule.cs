using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Sales
{
    /// <summary>
    /// Sales module: ventas y transacciones de punto de venta.
    /// </summary>
    public class SalesModule
    {
        private const string SubModule = "SALES_MODULE";
        private DatabaseTools _tools;

        public SalesModule(DatabaseTools tools)
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
        /// Create a new sale transaction with items
        /// </summary>
        public bool CreateSale(Dictionary<string, object> saleData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberId = saleData.ContainsKey("memberId") ? Convert.ToInt64(saleData["memberId"]) : 0L;
                var createdBy = saleData.ContainsKey("createdBy") ? Convert.ToInt64(saleData["createdBy"]) : 0L;
                var paymentMethod = saleData.ContainsKey("paymentMethod") ? Convert.ToString(saleData["paymentMethod"]) : "cash";
                var totalAmount = saleData.ContainsKey("totalAmount") ? Convert.ToDecimal(saleData["totalAmount"]) : 0m;
                var taxAmount = saleData.ContainsKey("taxAmount") ? Convert.ToDecimal(saleData["taxAmount"]) : 0m;
                var isOffline = saleData.ContainsKey("isOffline") ? Convert.ToBoolean(saleData["isOffline"]) : false;
                var items = saleData.ContainsKey("items") ? (List<Dictionary<string, object>>)saleData["items"] : new List<Dictionary<string, object>>();

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "INSERT INTO gym_sales (sale_number, member_id, total_amount, tax_amount, payment_method, sale_date, is_offline, created_by) VALUES (NEWID(), ?, ?, ?, ?, GETDATE(), ?, ?)",
                    memberId, totalAmount, taxAmount, paymentMethod, isOffline, createdBy
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear la venta");
                    response["success"] = false;
                    response["message"] = "Error al crear la venta";
                    return false;
                }

                // Get the newly created sale ID
                var lastResult = _tools.ExecuteQueryResultSet("SELECT SCOPE_IDENTITY() AS id");
                var saleId = lastResult != null && lastResult.Rows.Count > 0
                    ? Convert.ToInt64(lastResult.Rows[0].GetObject(0))
                    : 0;

                // Add sale items
                if (saleId > 0 && items.Count > 0)
                {
                    session = _tools.CreateSession(SubModule);
                    foreach (var item in items)
                    {
                        var productId = item.ContainsKey("productId") ? Convert.ToInt64(item["productId"]) : 0L;
                        var quantity = item.ContainsKey("quantity") ? Convert.ToInt32(item["quantity"]) : 1;
                        var price = item.ContainsKey("price") ? Convert.ToDecimal(item["price"]) : 0m;

                        session.ExecuteTransaction(
                            "INSERT INTO gym_sale_items (sale_id, product_id, quantity, unit_price, total_price) VALUES (?, ?, ?, ?, ?)",
                            saleId, productId, quantity, price, quantity * price
                        );
                    }

                    if (session.TryExecute().IsError)
                    {
                        Logger.AddErrorSubModule(SubModule, "Error al guardar los items de la venta");
                    }
                }

                response["success"] = true;
                response["saleId"] = saleId;
                response["message"] = "Venta registrada exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateSale: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
        /// <summary>
        /// Get sales history with optional date range filter and pagination
        /// </summary>
        public bool GetSales(out Dictionary<string, object> response, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int limit = 50)
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
                    whereParts.Add("s.sale_date BETWEEN ? AND ?");
                    parameters.Add(startDate.Value);
                    parameters.Add(endDate.Value);
                }
                else if (startDate.HasValue)
                {
                    whereParts.Add("s.sale_date >= ?");
                    parameters.Add(startDate.Value);
                }
                else if (endDate.HasValue)
                {
                    whereParts.Add("s.sale_date <= ?");
                    parameters.Add(endDate.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_sales s {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT s.id, s.sale_number, s.member_id, s.payment_method, s.total_amount, s.tax_amount, s.sale_date, s.is_offline, s.created_by, m.first_name, m.last_name FROM gym_sales s LEFT JOIN gym_members m ON s.member_id = m.id {whereClause} ORDER BY s.sale_date DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var sales = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        sales.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["saleNumber"] = Convert.ToString(GetRowValue(row, result, "sale_number")) ?? "",
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = $"{GetRowValue(row, result, "first_name")} {GetRowValue(row, result, "last_name")}",
                            ["paymentMethod"] = Convert.ToString(GetRowValue(row, result, "payment_method")) ?? "",
                            ["totalAmount"] = Convert.ToDecimal(GetRowValue(row, result, "total_amount")),
                            ["taxAmount"] = Convert.ToDecimal(GetRowValue(row, result, "tax_amount")),
                            ["isOffline"] = Convert.ToBoolean(GetRowValue(row, result, "is_offline")),
                            ["saleDate"] = Convert.ToString(GetRowValue(row, result, "sale_date")) ?? ""
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = sales;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetSales: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a sale by ID
        /// </summary>
        public bool DeleteSale(long saleId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "DELETE FROM gym_sale_items WHERE sale_id = ?",
                    saleId
                );
                session.ExecuteTransaction(
                    "DELETE FROM gym_sales WHERE id = ?",
                    saleId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al eliminar la venta");
                    response["success"] = false;
                    response["message"] = "Error al eliminar la venta";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Venta eliminada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteSale: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
