using GymService.Modules.Inventory;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class ProcurementFlowTests
{
    private readonly DatabaseFixture _fx;

    public ProcurementFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Supplier_PurchaseOrder_And_Kardex_Work()
    {
        if (!_fx.Available) return;

        var inventory = new InventoryModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            // Proveedor
            Assert.True(inventory.CreateSupplier(new Dictionary<string, object>
            {
                ["supplierName"] = $"Proveedor_{Guid.NewGuid():N}"[..20],
                ["contactName"] = "Contacto",
                ["email"] = "proveedor@test.local",
                ["phone"] = "5551234567"
            }, out var supplierCreated), supplierCreated.GetValueOrDefault("message")?.ToString());

            long supplierId = Convert.ToInt64(supplierCreated["supplierId"]);
            Assert.True(inventory.UpdateSupplier(supplierId, new Dictionary<string, object>
            {
                ["phone"] = "5559999999"
            }, out _));

            Assert.True(inventory.GetSuppliers(out var suppliers));
            var supplierRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(suppliers["data"]);
            var supplier = Assert.Single(supplierRows, row => Convert.ToInt64(row["id"]) == supplierId);
            Assert.Equal("5559999999", supplier["phone"]);

            // Producto
            Assert.True(inventory.CreateProduct(new Dictionary<string, object>
            {
                ["productName"] = $"POProducto_{Guid.NewGuid():N}"[..20],
                ["price"] = 100m,
                ["stock"] = 5,
                ["minStock"] = 2
            }, out var productCreated));

            long productId = Convert.ToInt64(productCreated["productId"]);

            // Orden de compra
            Assert.True(inventory.CreatePurchaseOrder(new Dictionary<string, object>
            {
                ["supplierId"] = supplierId,
                ["items"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["productId"] = productId,
                        ["quantity"] = 10,
                        ["unitCost"] = 50m
                    }
                }
            }, out var orderCreated), orderCreated.GetValueOrDefault("message")?.ToString());

            long orderId = Convert.ToInt64(orderCreated["purchaseOrderId"]);
            Assert.Equal(500m, Convert.ToDecimal(_fx.Scalar("SELECT TOTAL_AMOUNT FROM GYM_PURCHASE_ORDERS WHERE ID = @id", ("@id", orderId))));

            Assert.True(inventory.GetPurchaseOrder(orderId, out var orderDetail));
            var order = Assert.IsAssignableFrom<Dictionary<string, object>>(orderDetail["data"]);
            var items = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(order["items"]);
            Assert.Single(items);
            Assert.Equal(10, Convert.ToInt32(items[0]["quantity"]));

            Assert.True(inventory.SendPurchaseOrder(orderId, out _));
            Assert.True(inventory.ReceivePurchaseOrder(orderId, out var received), received.GetValueOrDefault("message")?.ToString());

            Assert.Equal(15, Convert.ToInt32(_fx.Scalar("SELECT STOCK_QUANTITY FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
            Assert.Equal("received", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_PURCHASE_ORDERS WHERE ID = @id", ("@id", orderId))));

            // Segunda recepción no permitida
            Assert.False(inventory.ReceivePurchaseOrder(orderId, out _));

            // Kardex de la recepción
            Assert.True(inventory.GetMovements(out var movements, productId, "purchase"));
            var movementRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(movements["data"]);
            var purchaseMovement = Assert.Single(movementRows);
            Assert.Equal(10, Convert.ToInt32(purchaseMovement["quantityDelta"]));
            Assert.Equal(5, Convert.ToInt32(purchaseMovement["previousStock"]));
            Assert.Equal(15, Convert.ToInt32(purchaseMovement["newStock"]));
            Assert.Equal(orderId, Convert.ToInt64(purchaseMovement["referenceId"]));

            // Ajuste manual genera movimiento
            Assert.True(inventory.UpdateStock(productId, 12, out _));
            Assert.True(inventory.GetMovements(out var adjustments, productId, "adjustment"));
            var adjustmentRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(adjustments["data"]);
            var adjustment = Assert.Single(adjustmentRows);
            Assert.Equal(-3, Convert.ToInt32(adjustment["quantityDelta"]));

            // Cancelar una segunda orden
            Assert.True(inventory.CreatePurchaseOrder(new Dictionary<string, object>
            {
                ["supplierId"] = supplierId,
                ["items"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["productId"] = productId,
                        ["quantity"] = 1,
                        ["unitCost"] = 50m
                    }
                }
            }, out var secondOrder));

            long secondId = Convert.ToInt64(secondOrder["purchaseOrderId"]);
            Assert.True(inventory.CancelPurchaseOrder(secondId, out _));
            Assert.Equal("cancelled", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_PURCHASE_ORDERS WHERE ID = @id", ("@id", secondId))));

            Assert.True(inventory.GetPurchaseOrders(out var orders));
            Assert.IsAssignableFrom<List<Dictionary<string, object>>>(orders["data"]);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
