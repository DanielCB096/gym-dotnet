using GymService.Modules.Billing;
using GymService.Modules.Identity;
using GymService.Modules.Members;
using GymService.Modules.Staff;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class StaffBillingFlowTests
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TokenLifetimeHours"] = "8",
            ["Authentication:OtpExpiryMinutes"] = "10",
            ["Authentication:MfaEncryptionKey"] = Convert.ToBase64String(new byte[32]),
            ["Email:FromAddress"] = "test@test.local"
        })
        .Build();

    private readonly DatabaseFixture _fx;

    public StaffBillingFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Staff_Users_And_Employees_Flow()
    {
        if (!_fx.Available) return;

        var identity = new IdentityModule(_fx.Tools!, Config);
        var staff = new StaffModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string username = $"staff_{Guid.NewGuid():N}"[..18];
            string email = $"{username}@test.local";

            Assert.True(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["password"] = "Clave123!",
                ["fullName"] = "Empleado de Prueba",
                ["role"] = "STAFF"
            }, out var userResponse), userResponse.GetValueOrDefault("message")?.ToString());

            long userId = Convert.ToInt64(userResponse["userId"]);

            // Duplicados y rol inválido.
            Assert.False(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = $"otro_{Guid.NewGuid():N}@test.local",
                ["password"] = "Clave123!",
                ["fullName"] = "Duplicado",
                ["role"] = "STAFF"
            }, out var duplicateUser));

            Assert.Contains("usuario", duplicateUser.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            Assert.False(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = $"x_{Guid.NewGuid():N}"[..12],
                ["email"] = $"x_{Guid.NewGuid():N}@test.local",
                ["password"] = "Clave123!",
                ["fullName"] = "Rol malo",
                ["role"] = "OWNER"
            }, out var badRole));

            Assert.Contains("Rol", badRole.GetValueOrDefault("message")?.ToString() ?? "");

            // El usuario está disponible antes de vincularlo.
            Assert.True(staff.GetAvailableUsers(out var availableBefore));
            var beforeRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(availableBefore["data"]);
            Assert.Contains(beforeRows, row => Convert.ToInt64(row["id"]) == userId);

            // Alta de empleado vinculado.
            Assert.True(staff.CreateStaff(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["employeeName"] = "Empleado de Prueba",
                ["position"] = "Recepción",
                ["department"] = "Operaciones",
                ["hireDate"] = DateTime.Now.Date.ToString("o")
            }, out var staffResponse), staffResponse.GetValueOrDefault("message")?.ToString());

            long staffId = Convert.ToInt64(staffResponse["staffId"]);

            // El mismo usuario no se puede vincular dos veces.
            Assert.False(staff.CreateStaff(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["employeeName"] = "Duplicado"
            }, out var duplicateStaff));

            Assert.Contains("vinculado", duplicateStaff.GetValueOrDefault("message")?.ToString() ?? "");

            // Ya no aparece entre los usuarios disponibles.
            Assert.True(staff.GetAvailableUsers(out var availableAfter));
            var afterRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(availableAfter["data"]);
            Assert.DoesNotContain(afterRows, row => Convert.ToInt64(row["id"]) == userId);

            // Listado con datos del usuario y del empleado.
            Assert.True(staff.GetStaff(out var staffList, "Empleado de Prueba", 1, 50, true));
            var staffRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(staffList["data"]);
            var employee = Assert.Single(staffRows);
            Assert.Equal(userId, Convert.ToInt64(employee["userId"]));
            Assert.Equal(username, employee["username"]);
            Assert.True(Convert.ToBoolean(employee["isActive"]));

            // Edición y baja lógica.
            Assert.True(staff.UpdateStaff(staffId, new Dictionary<string, object>
            {
                ["position"] = "Gerencia",
                ["department"] = "Administración",
                ["isActive"] = false
            }, out var updated), updated.GetValueOrDefault("message")?.ToString());

            Assert.False(Convert.ToBoolean(_fx.Scalar("SELECT IS_ACTIVE FROM GYM_STAFF WHERE ID = @id", ("@id", staffId))));

            Assert.True(staff.GetStaff(out var activeOnly, "Empleado de Prueba", 1, 50, false));
            Assert.Empty(Assert.IsAssignableFrom<List<Dictionary<string, object>>>(activeOnly["data"]));

            // Lista de usuarios del sistema con el empleado vinculado.
            Assert.True(staff.GetSystemUsers(out var allUsers));
            var userRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(allUsers["data"]);
            var userRow = Assert.Single(userRows, row => Convert.ToInt64(row["id"]) == userId);
            Assert.Equal(staffId, Convert.ToInt64(userRow["staffId"]));

            // Baja física del empleado; el usuario permanece.
            Assert.True(staff.DeleteStaff(staffId, out _));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_STAFF WHERE ID = @id", ("@id", staffId))));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_USERS WHERE ID = @id", ("@id", userId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Invoices_Validate_Data_And_Filters()
    {
        if (!_fx.Available) return;

        var billing = new BillingModule(_fx.Tools!);
        long memberId = CreateMember();
        CurrentUser.Id = 1;

        try
        {
            string invoiceNumber = $"INV-{Guid.NewGuid():N}"[..20];

            Assert.True(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["invoiceNumber"] = invoiceNumber,
                ["amount"] = 1160m,
                ["taxAmount"] = 160m,
                ["status"] = "pending"
            }, out var createResponse), createResponse.GetValueOrDefault("message")?.ToString());

            long invoiceId = Convert.ToInt64(createResponse["invoiceId"]);

            // Duplicado, importe inválido, estado inválido y socio inexistente.
            Assert.False(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["invoiceNumber"] = invoiceNumber,
                ["amount"] = 100m
            }, out var duplicateInvoice));
            Assert.Contains("número", duplicateInvoice.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.False(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["invoiceNumber"] = $"NEG-{Guid.NewGuid():N}"[..16],
                ["amount"] = -50m
            }, out var negativeAmount));
            Assert.Contains("importe", negativeAmount.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            Assert.False(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["invoiceNumber"] = $"BAD-{Guid.NewGuid():N}"[..16],
                ["amount"] = 100m,
                ["status"] = "inventado"
            }, out var badStatus));
            Assert.Contains("Estado", badStatus.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.False(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = 999999,
                ["invoiceNumber"] = $"NO-{Guid.NewGuid():N}"[..16],
                ["amount"] = 100m
            }, out var noMember));
            Assert.Contains("socio", noMember.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            // Listado con nombre de socio e IVA.
            Assert.True(billing.GetInvoices(out var list, memberId, "pending", "INV-"));
            var invoices = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
            var invoice = Assert.Single(invoices);
            Assert.Equal(invoiceNumber, invoice["invoiceNumber"]);
            Assert.Equal(1160m, Convert.ToDecimal(invoice["amount"]));
            Assert.Equal(160m, Convert.ToDecimal(invoice["taxAmount"]));
            Assert.Equal(1000m, Convert.ToDecimal(invoice["subtotal"]));
            Assert.False(string.IsNullOrWhiteSpace(Convert.ToString(invoice["memberName"])));

            // Cambio de estado y protección de borrado.
            Assert.True(billing.UpdateInvoiceStatus(invoiceId, "paid", out var paid));
            Assert.Equal("paid", Convert.ToString(paid["status"]));

            Assert.False(billing.DeleteInvoice(invoiceId, out var deletePaid));
            Assert.Contains("pendientes", deletePaid.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.False(billing.UpdateInvoiceStatus(invoiceId, "otro", out _));

            // Factura pendiente se puede eliminar.
            Assert.True(billing.CreateInvoice(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["invoiceNumber"] = $"DEL-{Guid.NewGuid():N}"[..16],
                ["amount"] = 200m
            }, out var pendingInvoice));

            long pendingId = Convert.ToInt64(pendingInvoice["invoiceId"]);
            Assert.True(billing.DeleteInvoice(pendingId, out _));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    private long CreateMember()
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"billing_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Factura",
            ["lastName"] = "Test",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }
}
