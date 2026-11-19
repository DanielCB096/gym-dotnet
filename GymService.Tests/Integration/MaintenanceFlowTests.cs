using GymService.Modules.Maintenance;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class MaintenanceFlowTests
{
    private readonly DatabaseFixture _fx;

    public MaintenanceFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Equipment_Tickets_Statuses_And_History_Work_Together()
    {
        if (!_fx.Available) return;

        var maintenance = new MaintenanceModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string equipmentName = $"Caminadora_{Guid.NewGuid():N}"[..24];

            Assert.True(maintenance.CreateEquipment(new Dictionary<string, object>
            {
                ["name"] = equipmentName,
                ["category"] = "Cardio",
                ["location"] = "Sala 1",
                ["status"] = "operational",
                ["purchaseDate"] = DateTime.Now.Date.AddMonths(-6).ToString("o"),
                ["warrantyEnd"] = DateTime.Now.Date.AddMonths(6).ToString("o")
            }, out var equipmentResponse), equipmentResponse.GetValueOrDefault("message")?.ToString());

            long equipmentId = Convert.ToInt64(equipmentResponse["equipmentId"]);

            Assert.True(maintenance.GetEquipment(out var equipmentList));
            var equipmentRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(equipmentList["data"]);
            var equipment = Assert.Single(equipmentRows);
            Assert.Equal(equipmentName, equipment["name"]);
            Assert.Equal(0, Convert.ToInt32(equipment["openTickets"]));

            // Ticket abierto: el equipo pasa a mantenimiento.
            Assert.True(maintenance.CreateTicket(new Dictionary<string, object>
            {
                ["equipmentId"] = equipmentId,
                ["description"] = "Banda desgastada",
                ["priority"] = "high"
            }, out var ticketResponse), ticketResponse.GetValueOrDefault("message")?.ToString());

            long ticketId = Convert.ToInt64(ticketResponse["ticketId"]);
            Assert.Equal("maintenance", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", equipmentId))));

            // No se permite un segundo ticket abierto para el mismo equipo.
            Assert.False(maintenance.CreateTicket(new Dictionary<string, object>
            {
                ["equipmentId"] = equipmentId,
                ["description"] = "Otra falla"
            }, out var duplicateTicket));

            Assert.Contains("abierto", duplicateTicket.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.True(maintenance.GetTickets(out var openTickets, "open", null, equipmentId));
            var openRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(openTickets["data"]);
            var ticket = Assert.Single(openRows);
            Assert.Equal(equipmentName, ticket["equipmentName"]);
            Assert.Equal("high", ticket["priority"]);

            // En progreso y luego resuelto.
            Assert.True(maintenance.UpdateTicketStatus(ticketId, "in_progress", out _));
            Assert.True(maintenance.UpdateTicketStatus(ticketId, "resolved", out var resolved));
            Assert.Equal("resolved", Convert.ToString(resolved["status"]));
            Assert.NotNull(_fx.Scalar("SELECT RESOLVED_AT FROM GYM_MAINTENANCE_TICKETS WHERE ID = @id", ("@id", ticketId)));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT RESOLVED_BY FROM GYM_MAINTENANCE_TICKETS WHERE ID = @id", ("@id", ticketId))));
            Assert.Equal("operational", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", equipmentId))));

            // Un ticket resuelto es final y no se borra.
            Assert.False(maintenance.UpdateTicketStatus(ticketId, "open", out _));
            Assert.False(maintenance.DeleteTicket(ticketId, out _));

            // Segundo ticket cancelado: se puede borrar y libera el equipo.
            Assert.True(maintenance.CreateTicket(new Dictionary<string, object>
            {
                ["equipmentId"] = equipmentId,
                ["description"] = "Ruido extraño",
                ["priority"] = "normal"
            }, out var secondTicket));

            long secondId = Convert.ToInt64(secondTicket["ticketId"]);
            Assert.True(maintenance.UpdateTicketStatus(secondId, "cancelled", out _));
            Assert.Equal("operational", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", equipmentId))));
            Assert.True(maintenance.DeleteTicket(secondId, out _));

            // Actualización de datos del equipo.
            Assert.True(maintenance.UpdateEquipment(equipmentId, new Dictionary<string, object>
            {
                ["location"] = "Sala 2",
                ["status"] = "out_of_service"
            }, out var updatedEquipment), updatedEquipment.GetValueOrDefault("message")?.ToString());

            Assert.Equal("Sala 2", Convert.ToString(_fx.Scalar("SELECT LOCATION FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", equipmentId))));
            Assert.Equal("out_of_service", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", equipmentId))));

            // Equipo con historial no se elimina; uno sin tickets sí.
            Assert.False(maintenance.DeleteEquipment(equipmentId, out _));

            Assert.True(maintenance.CreateEquipment(new Dictionary<string, object>
            {
                ["name"] = $"Temporal_{Guid.NewGuid():N}"[..20],
                ["category"] = "Fuerza"
            }, out var tempEquipment));

            long tempId = Convert.ToInt64(tempEquipment["equipmentId"]);
            Assert.True(maintenance.DeleteEquipment(tempId, out _));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_EQUIPMENT WHERE ID = @id", ("@id", tempId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
