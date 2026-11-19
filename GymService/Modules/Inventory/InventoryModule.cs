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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_INVENTORY
                {
                    ID = productId,
                    STOCK_QUANTITY = newStock
                });

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

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
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
