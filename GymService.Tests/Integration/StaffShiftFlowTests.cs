using GymService.Modules.Identity;
using GymService.Modules.Staff;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class StaffShiftFlowTests
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

    public StaffShiftFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Shifts_Attendance_And_Summary_Work()
    {
        if (!_fx.Available) return;

        var identity = new IdentityModule(_fx.Tools!, Config);
        var staff = new StaffModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string username = $"shift_{Guid.NewGuid():N}"[..18];

            Assert.True(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = $"{username}@test.local",
                ["password"] = "Clave123!",
                ["fullName"] = "Empleado Turnos",
                ["role"] = "STAFF"
            }, out var userResponse), userResponse.GetValueOrDefault("message")?.ToString());

            long userId = Convert.ToInt64(userResponse["userId"]);

            Assert.True(staff.CreateStaff(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["employeeName"] = "Empleado Turnos",
                ["position"] = "Recepción"
            }, out var staffResponse), staffResponse.GetValueOrDefault("message")?.ToString());

            long staffId = Convert.ToInt64(staffResponse["staffId"]);

            var start = DateTime.Today.AddDays(1).AddHours(9);
            var end = start.AddHours(8);

            // Alta del turno.
            Assert.True(staff.CreateShift(new Dictionary<string, object>
            {
                ["staffId"] = staffId,
                ["scheduledStart"] = start.ToString("o"),
                ["scheduledEnd"] = end.ToString("o"),
                ["notes"] = "Turno matutino"
            }, out var shiftResponse), shiftResponse.GetValueOrDefault("message")?.ToString());

            long shiftId = Convert.ToInt64(shiftResponse["shiftId"]);
            Assert.Equal("scheduled", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", shiftId))));

            // Duplicado en el mismo horario.
            Assert.False(staff.CreateShift(new Dictionary<string, object>
            {
                ["staffId"] = staffId,
                ["scheduledStart"] = start.ToString("o")
            }, out var duplicate));

            Assert.Contains("horario", duplicate.GetValueOrDefault("message")?.ToString() ?? "");

            // Fin anterior al inicio.
            Assert.False(staff.CreateShift(new Dictionary<string, object>
            {
                ["staffId"] = staffId,
                ["scheduledStart"] = start.AddHours(1).ToString("o"),
                ["scheduledEnd"] = start.ToString("o")
            }, out var badRange));

            Assert.Contains("posterior", badRange.GetValueOrDefault("message")?.ToString() ?? "");

            // Edición del turno programado.
            Assert.True(staff.UpdateShift(shiftId, new Dictionary<string, object>
            {
                ["scheduledStart"] = start.AddMinutes(30).ToString("o"),
                ["scheduledEnd"] = end.AddMinutes(30).ToString("o"),
                ["notes"] = "Turno ajustado"
            }, out var updated), updated.GetValueOrDefault("message")?.ToString());

            start = start.AddMinutes(30);
            Assert.Equal("Turno ajustado", Convert.ToString(_fx.Scalar("SELECT NOTES FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", shiftId))));

            // Entrada: solo una vez.
            Assert.True(staff.CheckInShift(shiftId, out var checkIn), checkIn.GetValueOrDefault("message")?.ToString());
            Assert.Equal("open", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", shiftId))));
            Assert.False(staff.CheckInShift(shiftId, out var doubleCheckIn));
            Assert.Contains("entrada", doubleCheckIn.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            // Simula 3 horas de trabajo y registra la salida.
            _fx.Execute("UPDATE GYM_STAFF_SHIFTS SET CHECK_IN = @in WHERE ID = @id",
                ("@in", DateTime.Now.AddHours(-3)), ("@id", shiftId));

            Assert.True(staff.CheckOutShift(shiftId, out var checkOut), checkOut.GetValueOrDefault("message")?.ToString());
            Assert.Equal("closed", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", shiftId))));
            Assert.True(Convert.ToInt32(checkOut["workedMinutes"]) >= 179);
            Assert.False(staff.CheckOutShift(shiftId, out _));

            // Listado con horas y estado.
            Assert.True(staff.GetShifts(out var listed, staffId, DateTime.Today, DateTime.Today.AddDays(2), "closed"));
            var rows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(listed["data"]);
            var row = Assert.Single(rows);
            Assert.Equal("Empleado Turnos", row["employeeName"]);
            Assert.True(Convert.ToInt32(row["workedMinutes"]) >= 179);
            Assert.Equal(0, Convert.ToInt32(row["lateMinutes"]));

            // Turno pasado: ausencia y cancelación.
            var pastStart = DateTime.Today.AddDays(-1).AddHours(9);
            Assert.True(staff.CreateShift(new Dictionary<string, object>
            {
                ["staffId"] = staffId,
                ["scheduledStart"] = pastStart.ToString("o"),
                ["scheduledEnd"] = pastStart.AddHours(8).ToString("o")
            }, out var pastShift));

            long pastShiftId = Convert.ToInt64(pastShift["shiftId"]);

            Assert.True(staff.MarkShiftAbsent(pastShiftId, out var absent), absent.GetValueOrDefault("message")?.ToString());
            Assert.Equal("absent", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", pastShiftId))));

            Assert.True(staff.CancelShift(pastShiftId, out _));
            Assert.Equal("cancelled", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_STAFF_SHIFTS WHERE ID = @id", ("@id", pastShiftId))));

            // No se puede marcar ausencia de un turno futuro.
            var futureShift = start.AddDays(3);
            Assert.True(staff.CreateShift(new Dictionary<string, object>
            {
                ["staffId"] = staffId,
                ["scheduledStart"] = futureShift.ToString("o")
            }, out var futureCreated));

            long futureShiftId = Convert.ToInt64(futureCreated["shiftId"]);
            Assert.False(staff.MarkShiftAbsent(futureShiftId, out var futureAbsent));
            Assert.Contains("antes del inicio", futureAbsent.GetValueOrDefault("message")?.ToString() ?? "");

            // Resumen del rango.
            Assert.True(staff.GetShiftSummary(out var summary, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(5)));
            var summaryData = Assert.IsAssignableFrom<Dictionary<string, object>>(summary["data"]);
            Assert.Equal(3, Convert.ToInt32(summaryData["total"]));
            Assert.Equal(1, Convert.ToInt32(summaryData["closedCount"]));
            Assert.Equal(1, Convert.ToInt32(summaryData["cancelledCount"]));
            Assert.Equal(1, Convert.ToInt32(summaryData["scheduledCount"]));
            Assert.True(Convert.ToInt64(summaryData["workedMinutes"]) >= 179);

            // No se puede eliminar un empleado con turnos.
            Assert.False(staff.DeleteStaff(staffId, out var deleteBlocked));
            Assert.Contains("turnos", deleteBlocked.GetValueOrDefault("message")?.ToString() ?? "");
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
