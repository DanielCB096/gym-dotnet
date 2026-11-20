using ClosedXML.Excel;
using GymService.Services;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class ReportsExportTests
{
    private readonly DatabaseFixture _fx;

    public ReportsExportTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    private static XLWorkbook Open(byte[] bytes)
    {
        Assert.NotEmpty(bytes);
        return new XLWorkbook(new MemoryStream(bytes));
    }

    private static int FindRow(IXLWorksheet worksheet, int column, string value)
    {
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            if (worksheet.Cell(row, column).GetString() == value)
                return row;
        }

        Assert.Fail($"No se encontró '{value}' en la columna {column}.");
        return -1;
    }

    [Fact]
    public void CashExport_IncludesSessionAndDifference()
    {
        if (!_fx.Available) return;

        const long id = 910001;
        _fx.Execute(
            "INSERT INTO GYM_CASH_SESSIONS " +
            "(ID, OPENED_AT, OPENED_BY, CLOSED_AT, CLOSED_BY, OPENING_AMOUNT, CLOSING_AMOUNT, EXPECTED_CASH, STATUS, NOTES) " +
            "VALUES (@id, @opened, 1, @closed, 1, 500, 730, 700, 'closed', 'Turno de prueba')",
            ("@id", id),
            ("@opened", new DateTime(2026, 11, 20, 9, 0, 0)),
            ("@closed", new DateTime(2026, 11, 20, 18, 0, 0)));

        var service = new ReportExportService(_fx.Tools!);
        var bytes = service.ExportCashToExcel(new DateTime(2026, 11, 20), new DateTime(2026, 11, 20, 23, 59, 59));

        using var workbook = Open(bytes);
        var sheet = workbook.Worksheet("Caja");
        Assert.Equal("Folio", sheet.Cell(1, 1).GetString());

        int row = FindRow(sheet, 1, id.ToString());
        Assert.Equal(500m, sheet.Cell(row, 6).GetValue<decimal>());
        Assert.Equal(700m, sheet.Cell(row, 9).GetValue<decimal>());
        Assert.Equal(730m, sheet.Cell(row, 10).GetValue<decimal>());
        Assert.Equal(30m, sheet.Cell(row, 11).GetValue<decimal>());
        Assert.Equal("closed", sheet.Cell(row, 12).GetString());

        int totalRow = FindRow(sheet, 1, "TOTAL:");
        Assert.Equal(30m, sheet.Cell(totalRow, 11).GetValue<decimal>());
    }

    [Fact]
    public void MembershipsExport_IncludesPlanAndLastPayment()
    {
        if (!_fx.Available) return;

        const long memberId = 910002;
        const long planId = 910003;
        const long membershipId = 910004;
        const long paymentId = 910005;

        _fx.Execute(
            "INSERT INTO GYM_MEMBERS (ID, FIRST_NAME, LAST_NAME, EMAIL, STATUS) " +
            "VALUES (@id, 'Ana', 'Prueba', 'ana.reportes@test.local', 'active')",
            ("@id", memberId));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIP_PLANS (ID, PLAN_NAME, PRICE, DURATION_DAYS) " +
            "VALUES (@id, 'Plan Prueba', 650, 30)",
            ("@id", planId));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIPS (ID, MEMBER_ID, PLAN_ID, START_DATE, END_DATE, STATUS, AUTO_RENEW) " +
            "VALUES (@id, @member, @plan, @start, @end, 'active', 1)",
            ("@id", membershipId), ("@member", memberId), ("@plan", planId),
            ("@start", new DateTime(2026, 11, 1)), ("@end", new DateTime(2026, 12, 1)));

        _fx.Execute(
            "INSERT INTO GYM_PAYMENTS (ID, MEMBER_ID, MEMBERSHIP_ID, AMOUNT, PAYMENT_METHOD, PAYMENT_DATE) " +
            "VALUES (@id, @member, @membership, 650, 'cash', @date)",
            ("@id", paymentId), ("@member", memberId), ("@membership", membershipId),
            ("@date", new DateTime(2026, 11, 1, 10, 0, 0)));

        var service = new ReportExportService(_fx.Tools!);
        var bytes = service.ExportMembershipsToExcel("active");

        using var workbook = Open(bytes);
        var sheet = workbook.Worksheet("Membresías");

        int row = FindRow(sheet, 1, membershipId.ToString());
        Assert.Equal("Ana Prueba", sheet.Cell(row, 2).GetString());
        Assert.Equal("Plan Prueba", sheet.Cell(row, 4).GetString());
        Assert.Equal("01/11/2026", sheet.Cell(row, 5).GetString());
        Assert.Equal("01/12/2026", sheet.Cell(row, 6).GetString());
        Assert.Equal("Sí", sheet.Cell(row, 8).GetString());
        Assert.Equal(650m, sheet.Cell(row, 13).GetValue<decimal>());
    }

    [Fact]
    public void KardexExport_FiltersByProduct_AndFormatsReference()
    {
        if (!_fx.Available) return;

        const long productId = 910010;
        const long movementId = 910011;

        _fx.Execute(
            "INSERT INTO GYM_INVENTORY (ID, PRODUCT_NAME, SKU, PRICE, STOCK_QUANTITY, MIN_STOCK, IS_ACTIVE) " +
            "VALUES (@id, 'Producto Prueba', 'SKU-REPORT-1', 100, 10, 2, 1)",
            ("@id", productId));

        _fx.Execute(
            "INSERT INTO GYM_INVENTORY_MOVEMENTS " +
            "(ID, PRODUCT_ID, MOVEMENT_TYPE, QUANTITY_DELTA, PREVIOUS_STOCK, NEW_STOCK, REFERENCE_TYPE, REFERENCE_ID, NOTES, MOVEMENT_DATE) " +
            "VALUES (@id, @product, 'purchase', 10, 0, 10, 'purchase_order', 42, 'Entrada de prueba', @date)",
            ("@id", movementId), ("@product", productId),
            ("@date", new DateTime(2026, 11, 20, 12, 0, 0)));

        var service = new ReportExportService(_fx.Tools!);
        var bytes = service.ExportKardexToExcel(productId);

        using var workbook = Open(bytes);
        var sheet = workbook.Worksheet("Kardex");
        int row = FindRow(sheet, 1, "20/11/2026 12:00");

        Assert.Equal("Producto Prueba", sheet.Cell(row, 2).GetString());
        Assert.Equal("SKU-REPORT-1", sheet.Cell(row, 3).GetString());
        Assert.Equal("purchase", sheet.Cell(row, 4).GetString());
        Assert.Equal(10, sheet.Cell(row, 5).GetValue<int>());
        Assert.Equal("purchase_order #42", sheet.Cell(row, 8).GetString());

        using var filtered = Open(service.ExportKardexToExcel(999999));
        var filteredSheet = filtered.Worksheet("Kardex");
        Assert.Equal(1, filteredSheet.LastRowUsed()?.RowNumber() ?? 1);
    }
}
