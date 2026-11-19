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
        /// Elemento de venta ya validado contra el inventario.
        /// </summary>
        private sealed class SaleItem
        {
            public long ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public int StockOnHand { get; set; }
            public string ProductName { get; set; } = "";
        }

        /// <summary>
        /// Create a new sale transaction with items.
        /// Valida existencias y descuenta stock del inventario.
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

                var saleItems = new List<SaleItem>();
                if (saleData.TryGetValue("items", out var rawItems) && rawItems is IEnumerable<object?> rawList)
                {
                    foreach (var rawItem in rawList)
                    {
                        if (rawItem is Dictionary<string, object?> item)
                        {
                            saleItems.Add(new SaleItem
                            {
                                ProductId = item.ContainsKey("productId") ? Convert.ToInt64(item["productId"]) : 0L,
                                Quantity = item.ContainsKey("quantity") ? Convert.ToInt32(item["quantity"]) : 1,
                                Price = item.ContainsKey("price") ? Convert.ToDecimal(item["price"]) : 0m
                            });
                        }
                    }
                }

                if (saleItems.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La venta debe incluir al menos un producto.";
                    return false;
                }

                // Validar existencias antes de registrar nada
                foreach (var item in saleItems)
                {
                    if (item.ProductId <= 0)
                    {
                        response["success"] = false;
                        response["message"] = "Hay un producto inválido en la venta.";
                        return false;
                    }

                    if (item.Quantity <= 0)
                    {
                        response["success"] = false;
                        response["message"] = "Las cantidades deben ser mayores a cero.";
                        return false;
                    }

                    var products = _tools.ExecuteQueryResultSet(
                        "SELECT product_name, stock_quantity, is_active FROM gym_inventory WHERE id = ?", item.ProductId);

                    if (products == null || products.Rows.Count == 0)
                    {
                        response["success"] = false;
                        response["message"] = $"El producto {item.ProductId} no existe en el inventario.";
                        return false;
                    }

                    var row = products.Rows[0];
                    var name = Convert.ToString(GetRowValue(row, products, "product_name")) ?? $"Producto {item.ProductId}";
                    var stock = Convert.ToInt32(GetRowValue(row, products, "stock_quantity"));
                    var isActive = Convert.ToBoolean(GetRowValue(row, products, "is_active"));

                    if (!isActive)
                    {
                        response["success"] = false;
                        response["message"] = $"El producto '{name}' está inactivo y no se puede vender.";
                        return false;
                    }

                    if (stock < item.Quantity)
                    {
                        response["success"] = false;
                        response["message"] = $"Stock insuficiente de '{name}': disponible {stock}, solicitado {item.Quantity}.";
                        return false;
                    }

                    item.ProductName = name;
                    item.StockOnHand = stock;
                }

                // sale_number is nvarchar(20): NEWID() would not fit, so build a compact unique code
                var saleNumber = "V" + DateTime.Now.ToString("yyMMddHHmmssfff") + Random.Shared.Next(100, 1000);

                // Stock resultante por producto: si un producto viene repetido, las cantidades se acumulan.
                var newStockByProduct = new Dictionary<long, int>();
                foreach (var item in saleItems)
                {
                    if (!newStockByProduct.TryGetValue(item.ProductId, out var remainingStock))
                        remainingStock = item.StockOnHand;

                    newStockByProduct[item.ProductId] = remainingStock - item.Quantity;
                }

                // El ID se genera antes de insertar para poder enlazar cabecera, items e inventario
                // dentro de una sola sesión/transacción.
                var seq = _tools.GenerateUniqueID(GYM_SALES.DatabaseMappingObject, 1);
                var saleId = seq.GetInt64UniqueId();

                var session = _tools.CreateSession(SubModule, createdBy);
                session.InsertObjectWithID(new GYM_SALES
                {
                    ID = saleId,
                    SALE_NUMBER = saleNumber,
                    MEMBER_ID = memberId,
                    TOTAL_AMOUNT = totalAmount,
                    TAX_AMOUNT = taxAmount,
                    PAYMENT_METHOD = paymentMethod,
                    SALE_DATE = DateTime.Now,
                    IS_OFFLINE = isOffline
                });

                foreach (var item in saleItems)
                {
                    session.InsertObject(new GYM_SALE_ITEMS
                    {
                        SALE_ID = saleId,
                        PRODUCT_ID = item.ProductId,
                        QUANTITY = item.Quantity,
                        UNIT_PRICE = item.Price,
                        TOTAL_PRICE = item.Quantity * item.Price
                    });
                }

                foreach (var stock in newStockByProduct)
                {
                    session.UpdateObjectChanges(new GYM_INVENTORY
                    {
                        ID = stock.Key,
                        STOCK_QUANTITY = stock.Value
                    });
                }

                if (session.TryExecute().IsError)
                {
                    // Al ser una sola transacción, Anxo revierte cabecera, items e inventario juntos.
                    Logger.AddErrorSubModule(SubModule, "Error al guardar los items o al descontar el inventario");
                    response["success"] = false;
                    response["message"] = "No se pudo actualizar el inventario; la venta fue cancelada.";
                    return false;
                }

                response["success"] = true;
                response["saleId"] = saleId;
                response["saleNumber"] = saleNumber;
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
        /// Registra la devolución de una venta: repone el stock de los productos vendidos.
        /// </summary>
        public bool ReturnSale(long saleId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var sales = _tools.ExecuteQueryResultSet(
                    "SELECT id, sale_number, ISNULL(status, 'completed') AS status FROM gym_sales WHERE id = ?", saleId);

                if (sales == null || sales.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La venta no existe.";
                    return false;
                }

                var currentStatus = Convert.ToString(GetRowValue(sales.Rows[0], sales, "status")) ?? "completed";
                if (string.Equals(currentStatus, "returned", StringComparison.OrdinalIgnoreCase))
                {
                    response["success"] = false;
                    response["message"] = "La venta ya fue devuelta anteriormente.";
                    return false;
                }

                var saleNumber = Convert.ToString(GetRowValue(sales.Rows[0], sales, "sale_number")) ?? saleId.ToString();

                // Stock actual de cada producto vendido (se repone al registrar la devolución).
                var items = _tools.ExecuteQueryResultSet(
                    "SELECT i.product_id, i.quantity, inv.stock_quantity FROM gym_sale_items i LEFT JOIN gym_inventory inv ON inv.id = i.product_id WHERE i.sale_id = ?",
                    saleId
                );

                var session = _tools.CreateSession(SubModule);

                // Marca la venta como devuelta en la misma transaccion que repone el stock.
                session.UpdateObjectChanges(new GYM_SALES
                {
                    ID = saleId,
                    STATUS = "returned",
                    RETURNED_AT = DateTime.Now
                });

                if (items != null)
                {
                    var restoredStock = new Dictionary<long, int>();
                    foreach (Row row in items.Rows)
                    {
                        var productId = Convert.ToInt64(GetRowValue(row, items, "product_id"));
                        var quantity = Convert.ToInt32(GetRowValue(row, items, "quantity"));
                        var rawStock = GetRowValue(row, items, "stock_quantity");
                        var currentStock = rawStock != null ? Convert.ToInt32(rawStock) : 0;

                        if (!restoredStock.TryGetValue(productId, out var newStock))
                            newStock = currentStock;

                        restoredStock[productId] = newStock + quantity;
                    }

                    foreach (var stock in restoredStock)
                    {
                        session.UpdateObjectChanges(new GYM_INVENTORY
                        {
                            ID = stock.Key,
                            STOCK_QUANTITY = stock.Value
                        });
                    }
                }

                if (session.Count > 0 && session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al registrar la devolución");
                    response["success"] = false;
                    response["message"] = "Error al registrar la devolución.";
                    return false;
                }

                response["success"] = true;
                response["message"] = $"Devolución registrada para la venta {saleNumber}. Inventario repuesto.";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ReturnSale: {ex.Message}");
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

                var sql = $"SELECT s.id, s.sale_number, s.member_id, s.payment_method, s.total_amount, s.tax_amount, s.sale_date, s.is_offline, s.created_by, ISNULL(s.status, 'completed') AS status, m.first_name, m.last_name FROM gym_sales s LEFT JOIN gym_members m ON s.member_id = m.id {whereClause} ORDER BY s.sale_date DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

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
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "completed",
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
        /// Delete a sale by ID. Repone el inventario de sus productos antes de borrarla.
        /// </summary>
        public bool DeleteSale(long saleId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var sales = _tools.ExecuteQueryResultSet(
                    "SELECT id FROM gym_sales WHERE id = ?", saleId);

                if (sales == null || sales.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La venta no existe.";
                    return false;
                }

                // Sus productos están descontados: reponerlos antes de borrar.
                var items = _tools.ExecuteQueryResultSet(
                    "SELECT i.product_id, i.quantity, inv.stock_quantity FROM gym_sale_items i LEFT JOIN gym_inventory inv ON inv.id = i.product_id WHERE i.sale_id = ?",
                    saleId
                );

                var session = _tools.CreateSession(SubModule);

                if (items != null)
                {
                    var restoredStock = new Dictionary<long, int>();
                    foreach (Row row in items.Rows)
                    {
                        var productId = Convert.ToInt64(GetRowValue(row, items, "product_id"));
                        var quantity = Convert.ToInt32(GetRowValue(row, items, "quantity"));
                        var rawStock = GetRowValue(row, items, "stock_quantity");
                        var currentStock = rawStock != null ? Convert.ToInt32(rawStock) : 0;

                        if (!restoredStock.TryGetValue(productId, out var newStock))
                            newStock = currentStock;

                        restoredStock[productId] = newStock + quantity;
                    }

                    foreach (var stock in restoredStock)
                    {
                        session.UpdateObjectChanges(new GYM_INVENTORY
                        {
                            ID = stock.Key,
                            STOCK_QUANTITY = stock.Value
                        });
                    }
                }

                // Los items se borran antes que la cabecera para respetar la llave foránea.
                session.DeleteMultipleObjects(new GYM_SALE_ITEMS { SALE_ID = saleId });
                session.DeleteObject(new GYM_SALES { ID = saleId });

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
