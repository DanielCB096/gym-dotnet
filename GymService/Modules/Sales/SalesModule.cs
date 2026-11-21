using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Modules.Inventory;

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
                var createdBy = saleData.ContainsKey("createdBy") ? Convert.ToInt64(saleData["createdBy"]) : CurrentUser.ForAudit;
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
                        "SELECT product_name, stock_quantity, is_active, price FROM gym_inventory WHERE id = ?", item.ProductId);

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
                    var catalogPrice = Convert.ToDecimal(GetRowValue(row, products, "price"));

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
                    // El precio se toma del catálogo, no del cliente.
                    item.Price = catalogPrice;
                }

                // El servidor calcula el subtotal y valida el IVA; nunca confía en totalAmount del cliente.
                decimal subtotal = saleItems.Sum(item => item.Price * item.Quantity);

                if (subtotal <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El total de la venta debe ser mayor a cero.";
                    return false;
                }

                if (taxAmount < 0 || taxAmount > subtotal)
                {
                    response["success"] = false;
                    response["message"] = "El IVA no puede ser negativo ni mayor que el subtotal.";
                    return false;
                }

                totalAmount = subtotal + taxAmount;

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
                    IS_OFFLINE = isOffline,
                    CASH_SESSION_ID = GymService.Modules.Cash.CashModule.GetOpenSessionId(_tools)
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

                var soldQuantityByProduct = saleItems
                    .GroupBy(item => item.ProductId)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

                foreach (var stock in newStockByProduct)
                {
                    session.UpdateObjectChanges(new GYM_INVENTORY
                    {
                        ID = stock.Key,
                        STOCK_QUANTITY = stock.Value
                    });

                    int sold = soldQuantityByProduct[stock.Key];
                    InventoryModule.RecordMovement(session, stock.Key, "sale", -sold, stock.Value + sold, stock.Value,
                        "sale", saleId, $"Venta {saleNumber}");
                }

                if (session.TryExecute().IsError)
                {
                    // Al ser una sola transacción, GymCore revierte cabecera, items e inventario juntos.
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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

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
                    var returnedByProduct = new Dictionary<long, int>();
                    foreach (Row row in items.Rows)
                    {
                        var productId = Convert.ToInt64(GetRowValue(row, items, "product_id"));
                        var quantity = Convert.ToInt32(GetRowValue(row, items, "quantity"));
                        var rawStock = GetRowValue(row, items, "stock_quantity");
                        var currentStock = rawStock != null ? Convert.ToInt32(rawStock) : 0;

                        if (!restoredStock.TryGetValue(productId, out var newStock))
                            newStock = currentStock;

                        restoredStock[productId] = newStock + quantity;
                        returnedByProduct[productId] = returnedByProduct.GetValueOrDefault(productId) + quantity;
                    }

                    foreach (var stock in restoredStock)
                    {
                        session.UpdateObjectChanges(new GYM_INVENTORY
                        {
                            ID = stock.Key,
                            STOCK_QUANTITY = stock.Value
                        });

                        int returned = returnedByProduct[stock.Key];
                        InventoryModule.RecordMovement(session, stock.Key, "return", returned, stock.Value - returned, stock.Value,
                            "sale_return", saleId, $"Devolución venta {saleNumber}");
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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

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

        /// <summary>
        /// Genera un recibo HTML imprimible para una venta.
        /// </summary>
        public bool GetSaleReceipt(long saleId, out string html)
        {
            html = string.Empty;

            try
            {
                var saleResult = _tools.ExecuteQueryResultSet(@"
                    SELECT s.*, m.FIRST_NAME, m.LAST_NAME
                    FROM GYM_SALES s
                    LEFT JOIN GYM_MEMBERS m ON m.ID = s.MEMBER_ID
                    WHERE s.ID = ?", saleId);

                if (saleResult == null || saleResult.Rows.Count == 0)
                    return false;

                var sale = saleResult.Rows[0];
                var itemsResult = _tools.ExecuteQueryResultSet(@"
                    SELECT i.*, p.PRODUCT_NAME
                    FROM GYM_SALE_ITEMS i
                    LEFT JOIN GYM_INVENTORY p ON p.ID = i.PRODUCT_ID
                    WHERE i.SALE_ID = ?", saleId);

                string saleNumber = Convert.ToString(GetRowValue(sale, saleResult, "sale_number")) ?? saleId.ToString();
                string memberName = $"{GetRowValue(sale, saleResult, "first_name")} {GetRowValue(sale, saleResult, "last_name")}".Trim();
                DateTime saleDate = Convert.ToDateTime(GetRowValue(sale, saleResult, "sale_date"));
                decimal total = Convert.ToDecimal(GetRowValue(sale, saleResult, "total_amount"));
                decimal tax = Convert.ToDecimal(GetRowValue(sale, saleResult, "tax_amount"));
                string paymentMethod = Convert.ToString(GetRowValue(sale, saleResult, "payment_method")) ?? "";

                var rows = new System.Text.StringBuilder();
                if (itemsResult != null)
                {
                    foreach (Row item in itemsResult.Rows)
                    {
                        string product = System.Net.WebUtility.HtmlEncode(Convert.ToString(GetRowValue(item, itemsResult, "product_name")) ?? "");
                        int quantity = Convert.ToInt32(GetRowValue(item, itemsResult, "quantity"));
                        decimal unit = Convert.ToDecimal(GetRowValue(item, itemsResult, "unit_price"));
                        decimal lineTotal = Convert.ToDecimal(GetRowValue(item, itemsResult, "total_price"));

                        rows.Append($"<tr><td>{product}</td><td class=\"right\">{quantity}</td><td class=\"right\">${unit:0.00}</td><td class=\"right\">${lineTotal:0.00}</td></tr>");
                    }
                }

                html = $@"<!doctype html>
<html lang=""es""><head><meta charset=""utf-8""><title>Recibo {saleNumber}</title>
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
<header><div><h1>Centro Fit</h1><p class=""muted"">Recibo de venta</p></div>
<div class=""muted""><strong>{System.Net.WebUtility.HtmlEncode(saleNumber)}</strong><br>{saleDate:dd/MM/yyyy HH:mm}</div></header>
<p><strong>Socio:</strong> {System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(memberName) ? "Público general" : memberName)}<br>
<strong>Método de pago:</strong> {System.Net.WebUtility.HtmlEncode(paymentMethod)}</p>
<table><thead><tr><th>Producto</th><th class=""right"">Cant.</th><th class=""right"">Precio</th><th class=""right"">Importe</th></tr></thead>
<tbody>{rows}</tbody></table>
<p class=""total"">Total: ${total:0.00} <span class=""muted"">(IVA ${tax:0.00})</span></p>
<p class=""footer"">Gracias por tu compra · GymService</p>
</body></html>";

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSaleReceipt: {ex.Message}");
                return false;
            }
        }
    }
}
