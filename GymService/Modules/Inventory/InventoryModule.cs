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
        /// Create a new product
        /// </summary>
        public bool CreateProduct(Dictionary<string, object> productData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var productName = Convert.ToString(productData.GetValueOrDefault("productName", "")) ?? "";
                var category = Convert.ToString(productData.GetValueOrDefault("category", "")) ?? "";
                var price = productData.ContainsKey("price") ? Convert.ToDecimal(productData["price"]) : 0m;
                var stock = productData.ContainsKey("stock") ? Convert.ToInt32(productData["stock"]) : 0;
                var minStock = productData.ContainsKey("minStock") ? Convert.ToInt32(productData["minStock"]) : 5;

                if (string.IsNullOrEmpty(productName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del producto es requerido";
                    return false;
                }

                var session = _tools.CreateSession();
                session.ExecuteTransaction("INSERT INTO gym_inventory (product_name, category, price, stock, min_stock, is_active) VALUES (?, ?, ?, ?, ?, 1)", productName, category, price, stock, minStock);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Producto creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el producto";
                }

                return (bool)response["success"];
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
        /// Update product stock
        /// </summary>
        public bool UpdateStock(long productId, int newStock, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("UPDATE gym_inventory SET stock = ? WHERE id = ?", newStock, productId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Stock actualizado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el stock";
                }

                return (bool)response["success"];
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
        /// Delete a product
        /// </summary>
        public bool DeleteProduct(long productId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("DELETE FROM gym_inventory WHERE id = ?", productId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Producto eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el producto";
                }

                return (bool)response["success"];
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
        /// Get active products in inventory with optional search and pagination
        /// </summary>
        public bool GetProducts(out Dictionary<string, object> response, string? query = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Clamp pagination values
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string> { "is_active = 1" };
                var parameters = new List<object>();

                if (!string.IsNullOrEmpty(query))
                {
                    whereParts.Add("(product_name LIKE ? OR category LIKE ?)");
                    parameters.Add("%" + query + "%");
                    parameters.Add("%" + query + "%");
                }

                string whereClause = "WHERE " + string.Join(" AND ", whereParts);

                // Count total for pagination metadata
                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_inventory {whereClause}",
                    parameters.ToArray()
                );

                // Main query with pagination
                parameters.Add(offset);
                parameters.Add(limit);

                var sql = $"SELECT * FROM gym_inventory {whereClause} ORDER BY product_name OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var result = _tools.ExecuteQueryResultSet(sql, parameters.ToArray());

                var products = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        products.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["productName"] = Convert.ToString(GetRowValue(row, result, "product_name")) ?? "",
                            ["category"] = Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                            ["price"] = Convert.ToDecimal(GetRowValue(row, result, "price")),
                            ["stock"] = Convert.ToInt32(GetRowValue(row, result, "stock")),
                            ["minStock"] = Convert.ToInt32(GetRowValue(row, result, "min_stock"))
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
    }
}
