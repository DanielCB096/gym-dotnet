using GymService.Modules.Inventory;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class InventoryFlowTests
{
    private readonly DatabaseFixture _fx;

    public InventoryFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Products_Stock_Filters_And_Deactivation_Work()
    {
        if (!_fx.Available) return;

        var inventory = new InventoryModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string productName = $"Proteina_{Guid.NewGuid():N}"[..24];
            string sku = $"SKU-{Guid.NewGuid():N}"[..12];

            Assert.True(inventory.CreateProduct(new Dictionary<string, object>
            {
                ["productName"] = productName,
                ["description"] = "Bote 1 kg",
                ["sku"] = sku,
                ["category"] = "Suplementos",
                ["price"] = 500m,
                ["cost"] = 350m,
                ["stock"] = 10,
                ["minStock"] = 5
            }, out var createResponse), createResponse.GetValueOrDefault("message")?.ToString());

            long productId = Convert.ToInt64(createResponse["productId"]);

            Assert.True(inventory.GetProducts(out var products, productName));
            var productRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(products["data"]);
            var product = Assert.Single(productRows);
            Assert.Equal(sku, product["sku"]);
            Assert.Equal(500m, Convert.ToDecimal(product["price"]));
            Assert.Equal(350m, Convert.ToDecimal(product["cost"]));
            Assert.Equal(10, Convert.ToInt32(product["stock"]));
            Assert.True(Convert.ToBoolean(product["isActive"]));

            // Búsqueda por SKU.
            Assert.True(inventory.GetProducts(out var bySku, sku));
            Assert.Single(Assert.IsAssignableFrom<List<Dictionary<string, object>>>(bySku["data"]));

            // Stock bajo.
            Assert.True(inventory.UpdateStock(productId, 3, out _));
            Assert.True(inventory.GetProducts(out var lowStock, null, 1, 50, true));
            var lowRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(lowStock["data"]);
            Assert.Contains(lowRows, row => Convert.ToInt64(row["id"]) == productId);

            Assert.False(inventory.UpdateStock(productId, -1, out var negativeStock));
            Assert.Contains("negativo", negativeStock.GetValueOrDefault("message")?.ToString() ?? "");

            // Edición de datos sin tocar el stock.
            Assert.True(inventory.UpdateProduct(productId, new Dictionary<string, object>
            {
                ["productName"] = productName + " XL",
                ["price"] = 550m,
                ["minStock"] = 2
            }, out var updateResponse), updateResponse.GetValueOrDefault("message")?.ToString());

            Assert.Equal("Proteina", Convert.ToString(_fx.Scalar("SELECT LEFT(PRODUCT_NAME, 8) FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
            Assert.Equal(3, Convert.ToInt32(_fx.Scalar("SELECT STOCK_QUANTITY FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
            Assert.Equal(550m, Convert.ToDecimal(_fx.Scalar("SELECT PRICE FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));

            // Desactivar: no aparece por defecto, sí con includeInactive.
            Assert.True(inventory.UpdateProduct(productId, new Dictionary<string, object>
            {
                ["isActive"] = false
            }, out _));

            Assert.True(inventory.GetProducts(out var activeOnly, productName));
            Assert.Empty(Assert.IsAssignableFrom<List<Dictionary<string, object>>>(activeOnly["data"]));

            Assert.True(inventory.GetProducts(out var allProducts, productName, 1, 50, false, true));
            Assert.Single(Assert.IsAssignableFrom<List<Dictionary<string, object>>>(allProducts["data"]));

            // Reactivar y eliminar sin ventas asociadas.
            Assert.True(inventory.UpdateProduct(productId, new Dictionary<string, object>
            {
                ["isActive"] = true
            }, out _));

            Assert.True(inventory.DeleteProduct(productId, out _));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));

            // Validaciones de alta.
            Assert.False(inventory.CreateProduct(new Dictionary<string, object>
            {
                ["productName"] = ""
            }, out var emptyName));

            Assert.Contains("nombre", emptyName.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
