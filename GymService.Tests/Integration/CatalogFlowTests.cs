using GymService.Modules.Billing;
using GymService.Modules.Classes;
using GymService.Modules.Kiosk;
using GymService.Modules.Members;
using GymService.Modules.Reports;
using GymService.Modules.Routines;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class CatalogFlowTests
{
    private readonly DatabaseFixture _fx;

    public CatalogFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void ClassTypes_Crud_RoundTrips_MaxCapacity()
    {
        if (!_fx.Available) return;

        var module = new ClassesModule(_fx.Tools!);
        string name = $"Clase_{Guid.NewGuid():N}"[..20];

        Assert.True(module.CreateClassType(new Dictionary<string, object>
        {
            ["typeName"] = name,
            ["description"] = "Prueba",
            ["durationMinutes"] = 45,
            ["maxParticipants"] = 20
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        long id = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_CLASS_TYPES WHERE TYPE_NAME = @n", ("@n", name)));

        Assert.True(module.GetClassTypes(out var list));
        var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
        var row = data.Single(r => Convert.ToInt64(r["id"]) == id);
        Assert.Equal(20, Convert.ToInt32(row["maxParticipants"]));
        Assert.Equal(45, Convert.ToInt32(row["durationMinutes"]));

        Assert.True(module.UpdateClassType(id, new Dictionary<string, object>
        {
            ["typeName"] = name + " II",
            ["description"] = "Actualizada",
            ["durationMinutes"] = 60,
            ["maxParticipants"] = 12
        }, out _));

        Assert.Equal(12, Convert.ToInt32(_fx.Scalar("SELECT MAX_CAPACITY FROM GYM_CLASS_TYPES WHERE ID = @id", ("@id", id))));

        Assert.True(module.DeleteClassType(id, out _));
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_CLASS_TYPES WHERE ID = @id", ("@id", id))));
    }

    [Fact]
    public void RoutinePlans_Persist_Difficulty_And_Duration()
    {
        if (!_fx.Available) return;

        var module = new RoutinesModule(_fx.Tools!);
        string name = $"Rutina_{Guid.NewGuid():N}"[..20];

        Assert.True(module.CreateRoutinePlan(new Dictionary<string, object>
        {
            ["planName"] = name,
            ["description"] = "Prueba",
            ["difficulty"] = "intermediate",
            ["durationMinutes"] = 60
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        Assert.True(module.GetRoutinePlans(out var list));
        var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
        var row = data.Single(r => Convert.ToString(r["planName"]) == name);

        Assert.Equal("intermediate", row["difficulty"]);
        Assert.Equal(60, Convert.ToInt32(row["durationMinutes"]));
    }

    [Fact]
    public void ScheduledReports_Crud_Works()
    {
        if (!_fx.Available) return;

        var module = new ReportsModule(_fx.Tools!);
        string name = $"Reporte_{Guid.NewGuid():N}"[..20];

        Assert.True(module.CreateScheduledReport(new Dictionary<string, object>
        {
            ["reportName"] = name,
            ["reportType"] = "sales",
            ["schedule"] = "daily"
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        long id = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_SCHEDULED_REPORTS WHERE REPORT_NAME = @n", ("@n", name)));

        Assert.True(module.GetScheduledReports(out var list));
        var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
        Assert.Contains(data, r => Convert.ToInt64(r["id"]) == id);

        Assert.True(module.DeleteScheduledReport(id, out _));
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_SCHEDULED_REPORTS WHERE ID = @id", ("@id", id))));
    }

    [Fact]
    public void Kiosk_Device_Registration_And_Deactivation()
    {
        if (!_fx.Available) return;

        var module = new KioskModule(_fx.Tools!);
        string name = $"Kiosco_{Guid.NewGuid():N}"[..20];

        Assert.True(module.RegisterDevice(name, out var registered), registered.GetValueOrDefault("message")?.ToString());

        string token = Convert.ToString(registered["token"]) ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(module.ValidateDeviceToken(token, out var validated));
        Assert.Equal(name, validated["deviceName"]);

        long deviceId = Convert.ToInt64(validated["deviceId"]);
        Assert.True(module.GetUnavailableEquipment(out _));

        Assert.True(module.DeactivateDevice(deviceId, out _));
        Assert.False(module.ValidateDeviceToken(token, out _));
    }

    [Fact]
    public void Invoices_Crud_Works()
    {
        if (!_fx.Available) return;

        long memberId = CreateMember();

        var module = new BillingModule(_fx.Tools!);
        string number = $"INV-{Guid.NewGuid():N}"[..20];

        Assert.True(module.CreateInvoice(new Dictionary<string, object>
        {
            ["memberId"] = memberId,
            ["invoiceNumber"] = number,
            ["amount"] = 500m,
            ["status"] = "pending"
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        long id = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_INVOICES WHERE INVOICE_NUMBER = @n", ("@n", number)));

        Assert.True(module.GetInvoices(out var list, memberId));
        var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
        Assert.Contains(data, r => Convert.ToInt64(r["id"]) == id);

        Assert.True(module.UpdateInvoiceStatus(id, "paid", out _));
        Assert.Equal("paid", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_INVOICES WHERE ID = @id", ("@id", id))));

        // Una factura pagada no se elimina: se conserva el historial.
        Assert.False(module.DeleteInvoice(id, out _));

        Assert.True(module.UpdateInvoiceStatus(id, "pending", out _));
        Assert.True(module.DeleteInvoice(id, out _));
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_INVOICES WHERE ID = @id", ("@id", id))));
    }

    private long CreateMember()
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"billing_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Factura",
            ["lastName"] = "Prueba",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }
}
