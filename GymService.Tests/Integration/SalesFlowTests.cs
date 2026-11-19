using AxDatabase;
using GymService.Modules.Inventory;
using GymService.Modules.Sales;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class SalesFlowTests
{
    private readonly DatabaseFixture _fx;

    public SalesFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Sale_Decrements_Stock_And_Return_Restores_It()
    {
        if (!_fx.Available) return;

        var inventory = new InventoryModule(_fx.Tools!);
        var sales = new SalesModule(_fx.Tools!);

        string productName = $"Producto_{Guid.NewGuid():N}";

        Assert.True(inventory.CreateProduct(new Dictionary<string, object>
        {
            ["productName"] = productName,
            ["category"] = "Pruebas",
            ["price"] = 10m,
            ["stock"] = 10,
            ["minStock"] = 1
        }, out _));

        long productId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_INVENTORY WHERE PRODUCT_NAME = @n", ("@n", productName)));

        CurrentUser.Id = 1;

        try
        {
            bool ok = sales.CreateSale(new Dictionary<string, object>
            {
                ["memberId"] = 0L,
                ["paymentMethod"] = "cash",
                ["isOffline"] = false,
                ["totalAmount"] = 999m,
                ["taxAmount"] = 0m,
                ["items"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["productId"] = productId,
                        ["quantity"] = 2,
                        ["price"] = 999m
                    }
                }
            }, out var response);

            Assert.True(ok, response.GetValueOrDefault("message")?.ToString());

            long saleId = Convert.ToInt64(response["saleId"]);
            Assert.True(saleId > 0);

            Assert.Equal(8, Convert.ToInt32(_fx.Scalar("SELECT STOCK_QUANTITY FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
            Assert.Equal("completed", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_SALES WHERE ID = @id", ("@id", saleId))));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_SALE_ITEMS WHERE SALE_ID = @id", ("@id", saleId))));
            // El servidor ignora total/price del cliente y usa el catálogo.
            Assert.Equal(20m, Convert.ToDecimal(_fx.Scalar("SELECT TOTAL_AMOUNT FROM GYM_SALES WHERE ID = @id", ("@id", saleId))));
            Assert.Equal(10m, Convert.ToDecimal(_fx.Scalar("SELECT UNIT_PRICE FROM GYM_SALE_ITEMS WHERE SALE_ID = @id", ("@id", saleId))));
            Assert.Equal(1L, Convert.ToInt64(_fx.Scalar("SELECT CREATED_BY FROM GYM_SALES WHERE ID = @id", ("@id", saleId))));

            Assert.True(sales.ReturnSale(saleId, out _));

            Assert.Equal(10, Convert.ToInt32(_fx.Scalar("SELECT STOCK_QUANTITY FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
            Assert.Equal("returned", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_SALES WHERE ID = @id", ("@id", saleId))));
            Assert.NotNull(_fx.Scalar("SELECT RETURNED_AT FROM GYM_SALES WHERE ID = @id", ("@id", saleId)));

            // Segunda devolución: debe fallar y no reponer stock dos veces.
            Assert.False(sales.ReturnSale(saleId, out var secondResponse));
            Assert.Contains("ya fue devuelta", secondResponse.GetValueOrDefault("message")?.ToString());
            Assert.Equal(10, Convert.ToInt32(_fx.Scalar("SELECT STOCK_QUANTITY FROM GYM_INVENTORY WHERE ID = @id", ("@id", productId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Sale_Without_Items_Is_Rejected()
    {
        if (!_fx.Available) return;

        var sales = new SalesModule(_fx.Tools!);

        bool ok = sales.CreateSale(new Dictionary<string, object>
        {
            ["paymentMethod"] = "cash",
            ["totalAmount"] = 0m,
            ["items"] = new List<object>()
        }, out var response);

        Assert.False(ok);
        Assert.Contains("al menos un producto", response.GetValueOrDefault("message")?.ToString());
    }
}
