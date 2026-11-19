using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Inventory
{
    /// <summary>
    /// Inventory module: productos e inventario.
    /// </summary>
    public class InventoryModule
    {
        private const string SubModule = "INVENTORY_MODULE";
        private DatabaseTools _tools;

        public InventoryModule(DatabaseTools tools)
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
        /// Crea un producto del inventario.
        /// </summary>
        public bool CreateProduct(Dictionary<string, object> productData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var productName = Convert.ToString(productData.GetValueOrDefault("productName", "")) ?? "";
                var category = Convert.ToString(productData.GetValueOrDefault("category", "")) ?? "";
                var description = Convert.ToString(productData.GetValueOrDefault("description", "")) ?? "";
                var sku = Convert.ToString(productData.GetValueOrDefault("sku", "")) ?? "";
                var price = productData.ContainsKey("price") ? Convert.ToDecimal(productData["price"]) : 0m;
                var stock = productData.ContainsKey("stock") ? Convert.ToInt32(productData["stock"]) : 0;
                var minStock = productData.ContainsKey("minStock") ? Convert.ToInt32(productData["minStock"]) : 5;
                decimal? cost = productData.ContainsKey("cost") && Convert.ToString(productData["cost"]) != ""
                    ? Convert.ToDecimal(productData["cost"])
                    : (decimal?)null;

                if (string.IsNullOrWhiteSpace(productName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del producto es requerido";
                    return false;
                }

                if (price < 0 || (cost.HasValue && cost.Value < 0))
                {
                    response["success"] = false;
                    response["message"] = "El precio y el costo no pueden ser negativos";
                    return false;
                }

                if (stock < 0 || minStock < 0)
                {
                    response["success"] = false;
                    response["message"] = "El stock y el mínimo no pueden ser negativos";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long productId = _tools.GenerateUniqueID(GYM_INVENTORY.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_INVENTORY
                {
                    ID = productId,
                    PRODUCT_NAME = productName.Trim(),
                    DESCRIPTION = description.Trim(),
                    SKU = string.IsNullOrWhiteSpace(sku) ? $"AUTO-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}" : sku.Trim(),
                    CATEGORY = category.Trim(),
                    PRICE = price,
                    COST = cost,
                    STOCK_QUANTITY = stock,
                    MIN_STOCK = minStock,
                    IS_ACTIVE = true
                });

                RecordMovement(session, productId, "initial", stock, 0, stock, "product", productId, "Stock inicial");

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el producto";
                    return false;
                }

                response["success"] = true;
                response["productId"] = productId;
                response["message"] = "Producto creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateProduct: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Actualiza los datos del producto (el stock se ajusta aparte).
        /// </summary>
        public bool UpdateProduct(long productId, Dictionary<string, object> productData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetProductState(productId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El producto no existe";
                    return false;
                }

                string productName = productData.ContainsKey("productName")
                    ? Convert.ToString(productData["productName"]) ?? ""
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(productName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del producto es requerido";
                    return false;
                }

                string category = productData.ContainsKey("category")
                    ? Convert.ToString(productData["category"]) ?? ""
                    : current.Value.Category;

                string description = productData.ContainsKey("description")
                    ? Convert.ToString(productData["description"]) ?? ""
                    : current.Value.Description;

                string sku = productData.ContainsKey("sku")
                    ? Convert.ToString(productData["sku"]) ?? ""
                    : current.Value.Sku;

                decimal price = productData.ContainsKey("price")
                    ? Convert.ToDecimal(productData["price"])
                    : current.Value.Price;

                decimal? cost = current.Value.Cost;
                if (productData.ContainsKey("cost"))
                {
                    var rawCost = Convert.ToString(productData["cost"]);
                    cost = string.IsNullOrWhiteSpace(rawCost) ? (decimal?)null : Convert.ToDecimal(rawCost);
                }

                int minStock = productData.ContainsKey("minStock")
                    ? Convert.ToInt32(productData["minStock"])
                    : current.Value.MinStock;

                bool isActive = productData.ContainsKey("isActive")
                    ? Convert.ToBoolean(productData["isActive"])
                    : current.Value.IsActive;

                if (price < 0 || (cost.HasValue && cost.Value < 0))
                {
                    response["success"] = false;
                    response["message"] = "El precio y el costo no pueden ser negativos";
                    return false;
                }

                if (minStock < 0)
                {
                    response["success"] = false;
                    response["message"] = "El stock mínimo no puede ser negativo";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_INVENTORY
                {
                    ID = productId,
                    PRODUCT_NAME = productName.Trim(),
                    DESCRIPTION = description.Trim(),
                    SKU = string.IsNullOrWhiteSpace(sku)
                        ? (string.IsNullOrWhiteSpace(current.Value.Sku) ? $"AUTO-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}" : current.Value.Sku)
                        : sku.Trim(),
                    CATEGORY = category.Trim(),
                    PRICE = price,
                    COST = cost,
                    STOCK_QUANTITY = current.Value.Stock,
                    MIN_STOCK = minStock,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el producto";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Producto actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateProduct: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Ajusta el stock de un producto.
        /// </summary>
        public bool UpdateStock(long productId, int newStock, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (newStock < 0)
                {
                    response["success"] = false;
                    response["message"] = "El stock no puede ser negativo";
                    return false;
                }

                if (GetProductState(productId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El producto no existe";
                    return false;
                }

                var current = GetProductState(productId)!.Value;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_INVENTORY
                {
                    ID = productId,
                    STOCK_QUANTITY = newStock
                });

                if (newStock != current.Stock)
                {
                    RecordMovement(session, productId, "adjustment", newStock - current.Stock, current.Stock, newStock,
                        "manual", null, "Ajuste manual de stock");
                }

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el stock";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Stock actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateStock: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Elimina un producto sin ventas asociadas.
        /// </summary>
        public bool DeleteProduct(long productId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (GetProductState(productId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El producto no existe";
                    return false;
                }

                long saleItems = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_SALE_ITEMS WHERE PRODUCT_ID = ?", productId);

                if (saleItems > 0)
                {
                    response["success"] = false;
                    response["message"] = "El producto tiene ventas asociadas; no se puede eliminar para no perder el historial. Desactívalo en su lugar.";
                    return false;
                }

                long purchaseItems = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_PURCHASE_ORDER_ITEMS WHERE PRODUCT_ID = ?", productId);

                if (purchaseItems > 0)
                {
                    response["success"] = false;
                    response["message"] = "El producto tiene órdenes de compra asociadas; desactívalo en lugar de eliminarlo.";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                // El producto se elimina físicamente: sus movimientos de kardex no tienen sentido sin él.
                session.DeleteMultipleObjects(new GYM_INVENTORY_MOVEMENTS { PRODUCT_ID = productId });
                session.DeleteObject(new GYM_INVENTORY { ID = productId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el producto";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Producto eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteProduct: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Lista productos del inventario con búsqueda, filtro de stock bajo e inactivos.
        /// </summary>
        public bool GetProducts(
            out Dictionary<string, object> response,
            string? query = null,
            int page = 1,
            int limit = 50,
            bool lowStockOnly = false,
            bool includeInactive = false)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!includeInactive)
                    whereParts.Add("is_active = 1");

                if (!string.IsNullOrWhiteSpace(query))
                {
                    whereParts.Add("(product_name LIKE ? OR category LIKE ? OR sku LIKE ?)");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                }

                if (lowStockOnly)
                    whereParts.Add("stock_quantity <= min_stock");

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_inventory {whereClause}",
                    parameters.ToArray()
                );

                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT * FROM gym_inventory {whereClause} ORDER BY product_name OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var products = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        int stock = Convert.ToInt32(GetRowValue(row, result, "stock_quantity"));

                        products.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["productName"] = Convert.ToString(GetRowValue(row, result, "product_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["sku"] = Convert.ToString(GetRowValue(row, result, "sku")) ?? "",
                            ["category"] = Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                            ["price"] = Convert.ToDecimal(GetRowValue(row, result, "price")),
                            ["cost"] = GetRowValue(row, result, "cost") != null ? Convert.ToDecimal(GetRowValue(row, result, "cost")) : (decimal?)null,
                            ["stock"] = stock,
                            ["minStock"] = Convert.ToInt32(GetRowValue(row, result, "min_stock")),
                            ["isActive"] = GetRowValue(row, result, "is_active") != null && Convert.ToBoolean(GetRowValue(row, result, "is_active"))
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = products;
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetProducts: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Proveedores
        // =====================================================================

        public bool GetSuppliers(out Dictionary<string, object> response, bool includeInactive = true)
        {
            response = new Dictionary<string, object>();

            try
            {
                var where = includeInactive ? "" : "WHERE IS_ACTIVE = 1";
                var result = _tools.ExecuteQueryResultSet(
                    $"SELECT * FROM GYM_SUPPLIERS {where} ORDER BY SUPPLIER_NAME");

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["supplierName"] = Convert.ToString(GetRowValue(row, result, "supplier_name")) ?? "",
                            ["contactName"] = Convert.ToString(GetRowValue(row, result, "contact_name")) ?? "",
                            ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                            ["phone"] = Convert.ToString(GetRowValue(row, result, "phone")) ?? "",
                            ["address"] = Convert.ToString(GetRowValue(row, result, "address")) ?? "",
                            ["rfc"] = Convert.ToString(GetRowValue(row, result, "rfc")) ?? "",
                            ["isActive"] = Convert.ToBoolean(GetRowValue(row, result, "is_active"))
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetSuppliers: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateSupplier(Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                string name = (Convert.ToString(data.GetValueOrDefault("supplierName", "")) ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del proveedor es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long id = _tools.GenerateUniqueID(GYM_SUPPLIERS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_SUPPLIERS
                {
                    ID = id,
                    SUPPLIER_NAME = name,
                    CONTACT_NAME = (Convert.ToString(data.GetValueOrDefault("contactName", "")) ?? "").Trim(),
                    EMAIL = (Convert.ToString(data.GetValueOrDefault("email", "")) ?? "").Trim(),
                    PHONE = (Convert.ToString(data.GetValueOrDefault("phone", "")) ?? "").Trim(),
                    ADDRESS = (Convert.ToString(data.GetValueOrDefault("address", "")) ?? "").Trim(),
                    RFC = (Convert.ToString(data.GetValueOrDefault("rfc", "")) ?? "").Trim(),
                    IS_ACTIVE = true
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el proveedor";
                    return false;
                }

                response["success"] = true;
                response["supplierId"] = id;
                response["message"] = "Proveedor creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateSupplier: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdateSupplier(long supplierId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetSupplierState(supplierId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El proveedor no existe";
                    return false;
                }

                string name = data.ContainsKey("supplierName")
                    ? (Convert.ToString(data["supplierName"]) ?? "").Trim()
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del proveedor es requerido";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_SUPPLIERS
                {
                    ID = supplierId,
                    SUPPLIER_NAME = name,
                    CONTACT_NAME = data.ContainsKey("contactName") ? Convert.ToString(data["contactName"]) ?? "" : current.Value.ContactName,
                    EMAIL = data.ContainsKey("email") ? Convert.ToString(data["email"]) ?? "" : current.Value.Email,
                    PHONE = data.ContainsKey("phone") ? Convert.ToString(data["phone"]) ?? "" : current.Value.Phone,
                    ADDRESS = data.ContainsKey("address") ? Convert.ToString(data["address"]) ?? "" : current.Value.Address,
                    RFC = data.ContainsKey("rfc") ? Convert.ToString(data["rfc"]) ?? "" : current.Value.Rfc,
                    IS_ACTIVE = data.ContainsKey("isActive") ? Convert.ToBoolean(data["isActive"]) : current.Value.IsActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el proveedor";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Proveedor actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateSupplier: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Órdenes de compra
        // =====================================================================

        public bool GetPurchaseOrders(out Dictionary<string, object> response, string? status = null)
        {
            response = new Dictionary<string, object>();

            try
            {
                var where = string.IsNullOrWhiteSpace(status) ? "" : "WHERE po.STATUS = ?";
                var parameters = string.IsNullOrWhiteSpace(status) ? Array.Empty<object>() : new object[] { status };

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT po.*, s.SUPPLIER_NAME, " +
                    "(SELECT COUNT(*) FROM GYM_PURCHASE_ORDER_ITEMS i WHERE i.PURCHASE_ORDER_ID = po.ID) AS ITEM_COUNT " +
                    "FROM GYM_PURCHASE_ORDERS po " +
                    "INNER JOIN GYM_SUPPLIERS s ON s.ID = po.SUPPLIER_ID " +
                    $"{where} ORDER BY po.ORDER_DATE DESC, po.ID DESC", parameters);

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["orderNumber"] = Convert.ToString(GetRowValue(row, result, "order_number")) ?? "",
                            ["supplierId"] = Convert.ToInt64(GetRowValue(row, result, "supplier_id")),
                            ["supplierName"] = Convert.ToString(GetRowValue(row, result, "supplier_name")) ?? "",
                            ["orderDate"] = GetRowValue(row, result, "order_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "order_date")) : (DateTime?)null,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["totalAmount"] = Convert.ToDecimal(GetRowValue(row, result, "total_amount")),
                            ["itemCount"] = Convert.ToInt32(GetRowValue(row, result, "item_count"))
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetPurchaseOrders: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool GetPurchaseOrder(long orderId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT po.*, s.SUPPLIER_NAME FROM GYM_PURCHASE_ORDERS po INNER JOIN GYM_SUPPLIERS s ON s.ID = po.SUPPLIER_ID WHERE po.ID = ?",
                    orderId);

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La orden de compra no existe";
                    return false;
                }

                var row = result.Rows[0];
                var itemsResult = _tools.ExecuteQueryResultSet(
                    "SELECT i.*, p.PRODUCT_NAME FROM GYM_PURCHASE_ORDER_ITEMS i INNER JOIN GYM_INVENTORY p ON p.ID = i.PRODUCT_ID WHERE i.PURCHASE_ORDER_ID = ? ORDER BY i.ID",
                    orderId);

                var items = new List<Dictionary<string, object>>();
                if (itemsResult != null)
                {
                    foreach (Row item in itemsResult.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(item, itemsResult, "id")),
                            ["productId"] = Convert.ToInt64(GetRowValue(item, itemsResult, "product_id")),
                            ["productName"] = Convert.ToString(GetRowValue(item, itemsResult, "product_name")) ?? "",
                            ["quantity"] = Convert.ToInt32(GetRowValue(item, itemsResult, "quantity")),
                            ["unitCost"] = Convert.ToDecimal(GetRowValue(item, itemsResult, "unit_cost")),
                            ["totalCost"] = Convert.ToDecimal(GetRowValue(item, itemsResult, "total_cost"))
                        });
                    }
                }

                response["success"] = true;
                response["data"] = new Dictionary<string, object>
                {
                    ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                    ["orderNumber"] = Convert.ToString(GetRowValue(row, result, "order_number")) ?? "",
                    ["supplierId"] = Convert.ToInt64(GetRowValue(row, result, "supplier_id")),
                    ["supplierName"] = Convert.ToString(GetRowValue(row, result, "supplier_name")) ?? "",
                    ["orderDate"] = GetRowValue(row, result, "order_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "order_date")) : (DateTime?)null,
                    ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                    ["totalAmount"] = Convert.ToDecimal(GetRowValue(row, result, "total_amount")),
                    ["items"] = items
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetPurchaseOrder: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreatePurchaseOrder(Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                long supplierId = data.ContainsKey("supplierId") ? Convert.ToInt64(data["supplierId"]) : 0L;
                if (GetSupplierState(supplierId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El proveedor no existe";
                    return false;
                }

                var rawItems = data.GetValueOrDefault("items") as IEnumerable<object?>;
                var items = new List<(long ProductId, int Quantity, decimal UnitCost)>();
                if (rawItems != null)
                {
                    foreach (var raw in rawItems)
                    {
                        if (raw is not Dictionary<string, object?> item) continue;
                        long productId = item.ContainsKey("productId") ? Convert.ToInt64(item["productId"]) : 0L;
                        int quantity = item.ContainsKey("quantity") ? Convert.ToInt32(item["quantity"]) : 0;
                        decimal unitCost = item.ContainsKey("unitCost") ? Convert.ToDecimal(item["unitCost"]) : 0m;

                        if (productId <= 0 || GetProductState(productId) == null)
                        {
                            response["success"] = false;
                            response["message"] = "Hay un producto inválido en la orden";
                            return false;
                        }

                        if (quantity <= 0 || unitCost < 0)
                        {
                            response["success"] = false;
                            response["message"] = "Cantidad y costo deben ser válidos";
                            return false;
                        }

                        items.Add((productId, quantity, unitCost));
                    }
                }

                if (items.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "La orden debe incluir al menos un producto";
                    return false;
                }

                decimal total = items.Sum(i => i.Quantity * i.UnitCost);
                string orderNumber = $"OC-{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(100, 999)}";

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long orderId = _tools.GenerateUniqueID(GYM_PURCHASE_ORDERS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_PURCHASE_ORDERS
                {
                    ID = orderId,
                    ORDER_NUMBER = orderNumber,
                    SUPPLIER_ID = supplierId,
                    ORDER_DATE = DateTime.Now,
                    STATUS = "draft",
                    TOTAL_AMOUNT = total
                });

                foreach (var item in items)
                {
                    session.InsertObject(new GYM_PURCHASE_ORDER_ITEMS
                    {
                        PURCHASE_ORDER_ID = orderId,
                        PRODUCT_ID = item.ProductId,
                        QUANTITY = item.Quantity,
                        UNIT_COST = item.UnitCost,
                        TOTAL_COST = item.Quantity * item.UnitCost
                    });
                }

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la orden de compra";
                    return false;
                }

                response["success"] = true;
                response["purchaseOrderId"] = orderId;
                response["orderNumber"] = orderNumber;
                response["message"] = "Orden de compra creada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreatePurchaseOrder: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool SendPurchaseOrder(long orderId, out Dictionary<string, object> response)
        {
            return ChangePurchaseOrderStatus(orderId, new[] { "draft" }, "sent", "Orden enviada al proveedor", out response);
        }

        public bool CancelPurchaseOrder(long orderId, out Dictionary<string, object> response)
        {
            return ChangePurchaseOrderStatus(orderId, new[] { "draft", "sent" }, "cancelled", "Orden cancelada", out response);
        }

        public bool ReceivePurchaseOrder(long orderId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var order = GetPurchaseOrderState(orderId);
                if (order == null)
                {
                    response["success"] = false;
                    response["message"] = "La orden de compra no existe";
                    return false;
                }

                if (order.Value.Status != "draft" && order.Value.Status != "sent")
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden recibir órdenes pendientes o enviadas";
                    return false;
                }

                var itemsResult = _tools.ExecuteQueryResultSet(
                    "SELECT product_id, quantity FROM GYM_PURCHASE_ORDER_ITEMS WHERE PURCHASE_ORDER_ID = ?", orderId);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                if (itemsResult != null)
                {
                    foreach (Row item in itemsResult.Rows)
                    {
                        long productId = Convert.ToInt64(GetRowValue(item, itemsResult, "product_id"));
                        int quantity = Convert.ToInt32(GetRowValue(item, itemsResult, "quantity"));
                        var product = GetProductState(productId);
                        if (product == null) continue;

                        int previous = product.Value.Stock;
                        int next = previous + quantity;

                        session.UpdateObjectChanges(new GYM_INVENTORY
                        {
                            ID = productId,
                            STOCK_QUANTITY = next
                        });

                        RecordMovement(session, productId, "purchase", quantity, previous, next, "purchase_order", orderId,
                            $"Recepción de orden {order.Value.OrderNumber}");
                    }
                }

                session.UpdateObjectChanges(new GYM_PURCHASE_ORDERS
                {
                    ID = orderId,
                    STATUS = "received"
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al recibir la orden de compra";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Orden recibida y stock actualizado";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ReceivePurchaseOrder: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Movimientos / kardex
        // =====================================================================

        public bool GetMovements(out Dictionary<string, object> response, long? productId = null, string? type = null, int page = 1, int limit = 100)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 100;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();
                if (productId.HasValue) { whereParts.Add("m.PRODUCT_ID = ?"); parameters.Add(productId.Value); }
                if (!string.IsNullOrWhiteSpace(type)) { whereParts.Add("m.MOVEMENT_TYPE = ?"); parameters.Add(type.Trim()); }
                string where = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long total = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM GYM_INVENTORY_MOVEMENTS m {where}", parameters.ToArray());

                parameters.Add(offset);
                parameters.Add(limit);

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT m.*, p.PRODUCT_NAME FROM GYM_INVENTORY_MOVEMENTS m " +
                    "INNER JOIN GYM_INVENTORY p ON p.ID = m.PRODUCT_ID " +
                    $"{where} ORDER BY m.MOVEMENT_DATE DESC, m.ID DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    parameters.ToArray());

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["productId"] = Convert.ToInt64(GetRowValue(row, result, "product_id")),
                            ["productName"] = Convert.ToString(GetRowValue(row, result, "product_name")) ?? "",
                            ["movementType"] = Convert.ToString(GetRowValue(row, result, "movement_type")) ?? "",
                            ["quantityDelta"] = Convert.ToInt32(GetRowValue(row, result, "quantity_delta")),
                            ["previousStock"] = Convert.ToInt32(GetRowValue(row, result, "previous_stock")),
                            ["newStock"] = Convert.ToInt32(GetRowValue(row, result, "new_stock")),
                            ["referenceType"] = Convert.ToString(GetRowValue(row, result, "reference_type")) ?? "",
                            ["referenceId"] = GetRowValue(row, result, "reference_id") != null ? Convert.ToInt64(GetRowValue(row, result, "reference_id")) : (long?)null,
                            ["notes"] = Convert.ToString(GetRowValue(row, result, "notes")) ?? "",
                            ["movementDate"] = GetRowValue(row, result, "movement_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "movement_date")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((total + limit - 1) / limit) : 0;
                response["success"] = true;
                response["data"] = items;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page, ["limit"] = limit, ["totalCount"] = total,
                    ["totalPages"] = totalPages, ["hasNext"] = page < totalPages, ["hasPrev"] = page > 1
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetMovements: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public static void RecordMovement(Session session, long productId, string movementType, int quantityDelta,
            int previousStock, int newStock, string? referenceType, long? referenceId, string notes)
        {
            session.InsertObject(new GYM_INVENTORY_MOVEMENTS
            {
                PRODUCT_ID = productId,
                MOVEMENT_TYPE = movementType,
                QUANTITY_DELTA = quantityDelta,
                PREVIOUS_STOCK = previousStock,
                NEW_STOCK = newStock,
                REFERENCE_TYPE = referenceType ?? "",
                REFERENCE_ID = referenceId,
                NOTES = notes ?? "",
                MOVEMENT_DATE = DateTime.Now
            });
        }

        private bool ChangePurchaseOrderStatus(long orderId, string[] allowedFrom, string newStatus, string message, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetPurchaseOrderState(orderId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "La orden de compra no existe";
                    return false;
                }

                if (!allowedFrom.Contains(current.Value.Status))
                {
                    response["success"] = false;
                    response["message"] = "La orden no está en un estado válido para esta acción";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_PURCHASE_ORDERS { ID = orderId, STATUS = newStatus });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la orden";
                    return false;
                }

                response["success"] = true;
                response["message"] = message;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ChangePurchaseOrderStatus: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private (string Name, string ContactName, string Email, string Phone, string Address, string Rfc, bool IsActive)? GetSupplierState(long supplierId)
        {
            var result = _tools.ExecuteQueryResultSet("SELECT * FROM GYM_SUPPLIERS WHERE ID = ?", supplierId);
            if (result == null || result.Rows.Count == 0) return null;
            var row = result.Rows[0];
            return (
                Convert.ToString(GetRowValue(row, result, "supplier_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "contact_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "email")) ?? "",
                Convert.ToString(GetRowValue(row, result, "phone")) ?? "",
                Convert.ToString(GetRowValue(row, result, "address")) ?? "",
                Convert.ToString(GetRowValue(row, result, "rfc")) ?? "",
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }

        private (string Status, string OrderNumber)? GetPurchaseOrderState(long orderId)
        {
            var result = _tools.ExecuteQueryResultSet("SELECT STATUS, ORDER_NUMBER FROM GYM_PURCHASE_ORDERS WHERE ID = ?", orderId);
            if (result == null || result.Rows.Count == 0) return null;
            var row = result.Rows[0];
            return (
                Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                Convert.ToString(GetRowValue(row, result, "order_number")) ?? ""
            );
        }

        private (string Name, string Description, string Sku, string Category, decimal Price, decimal? Cost, int Stock, int MinStock, bool IsActive)? GetProductState(long productId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM gym_inventory WHERE id = ?", productId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToString(GetRowValue(row, result, "product_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                Convert.ToString(GetRowValue(row, result, "sku")) ?? "",
                Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                Convert.ToDecimal(GetRowValue(row, result, "price")),
                GetRowValue(row, result, "cost") != null ? Convert.ToDecimal(GetRowValue(row, result, "cost")) : (decimal?)null,
                Convert.ToInt32(GetRowValue(row, result, "stock_quantity")),
                Convert.ToInt32(GetRowValue(row, result, "min_stock")),
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }
    }
}
