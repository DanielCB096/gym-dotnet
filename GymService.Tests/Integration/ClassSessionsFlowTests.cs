using GymService.Modules.Classes;
using GymService.Modules.Members;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class ClassSessionsFlowTests
{
    private readonly DatabaseFixture _fx;

    public ClassSessionsFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Sessions_And_Reservations_Respect_Capacity_And_Waitlist()
    {
        if (!_fx.Available) return;

        var classes = new ClassesModule(_fx.Tools!);
        long classTypeId = CreateClassType(classes, "Yoga", 2);

        long member1 = CreateMember("cap1");
        long member2 = CreateMember("cap2");
        long member3 = CreateMember("cap3");

        DateTime start = DateTime.Now.AddDays(1).Date.AddHours(9);
        long sessionId = CreateSession(classes, classTypeId, start);

        // Listado con filtro por tipo y rango.
        Assert.True(classes.GetClassSessions(out var list, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(3), classTypeId));
        var sessions = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
        var session = sessions.Single(s => Convert.ToInt64(s["id"]) == sessionId);
        Assert.Equal(2, Convert.ToInt32(session["capacity"]));
        Assert.Equal(0, Convert.ToInt32(session["reserved"]));

        // Dos reservas confirman; la tercera cae en lista de espera.
        Assert.True(classes.ReserveClass(sessionId, member1, out var r1), r1.GetValueOrDefault("message")?.ToString());
        Assert.Equal("reserved", r1["status"]);

        Assert.True(classes.ReserveClass(sessionId, member2, out var r2), r2.GetValueOrDefault("message")?.ToString());
        Assert.Equal("reserved", r2["status"]);

        Assert.True(classes.ReserveClass(sessionId, member3, out var r3), r3.GetValueOrDefault("message")?.ToString());
        Assert.True(Convert.ToBoolean(r3["waitlisted"]));
        Assert.Equal("waitlist", r3["status"]);
        Assert.Equal(1L, Convert.ToInt64(r3["waitlistPosition"]));

        Assert.Equal(2, Spots(sessionId));

        // No se permite duplicar una reserva activa del mismo socio.
        Assert.False(classes.ReserveClass(sessionId, member1, out var duplicate));
        Assert.Contains("ya tiene una reserva", duplicate.GetValueOrDefault("message")?.ToString() ?? "");

        long r1Id = Convert.ToInt64(r1["reservationId"]);
        long r2Id = Convert.ToInt64(r2["reservationId"]);
        long r3Id = Convert.ToInt64(r3["reservationId"]);

        Assert.True(classes.GetSessionReservations(sessionId, out var reservations));
        var reservationRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(reservations["data"]);
        Assert.Equal(3, reservationRows.Count);
        Assert.Equal("waitlist", reservationRows.Single(r => Convert.ToInt64(r["id"]) == r3Id)["status"]);

        // Al cancelar una reserva confirmada, el primero de la lista de espera sube.
        Assert.True(classes.CancelReservation(r1Id, out var cancelled));
        Assert.Equal(r3Id, Convert.ToInt64(cancelled["promotedReservationId"]));
        Assert.Equal("reserved", ReservationStatus(r3Id));
        Assert.Equal(2, Spots(sessionId));

        // Cancelar la reserva promovida libera el lugar (no hay más en espera).
        Assert.True(classes.CancelReservation(r3Id, out _));
        Assert.Equal(1, Spots(sessionId));

        // Asistencia.
        Assert.True(classes.SetReservationAttendance(r2Id, true, out _));
        Assert.True(Convert.ToBoolean(_fx.Scalar("SELECT ATTENDED FROM GYM_CLASS_RESERVATIONS WHERE ID = @id", ("@id", r2Id))));
        Assert.Equal("attended", ReservationStatus(r2Id));

        Assert.True(classes.SetReservationAttendance(r2Id, false, out _));
        Assert.Equal("reserved", ReservationStatus(r2Id));

        // Cancelar la sesión cancela las reservas activas y deja el cupo en 0.
        Assert.True(classes.CancelClassSession(sessionId, out var sessionCancelled));
        Assert.Equal("cancelled", SessionStatus(sessionId));
        Assert.Equal(0, Spots(sessionId));
        Assert.Equal("cancelled", ReservationStatus(r2Id));
        Assert.Equal(1, Convert.ToInt32(sessionCancelled["cancelledReservations"]));
    }

    [Fact]
    public void Session_Can_Be_Updated_And_Deleted_When_It_Has_No_Reservations()
    {
        if (!_fx.Available) return;

        var classes = new ClassesModule(_fx.Tools!);
        long classTypeId = CreateClassType(classes, "Pilates", 10);

        DateTime start = DateTime.Now.AddDays(2).Date.AddHours(10);
        long sessionId = CreateSession(classes, classTypeId, start);

        DateTime newStart = start.AddHours(2);

        Assert.True(classes.UpdateClassSession(sessionId, new Dictionary<string, object>
        {
            ["startTime"] = newStart.ToString("o"),
            ["endTime"] = newStart.AddHours(1).ToString("o"),
            ["room"] = "Sala 2"
        }, out var updated), updated.GetValueOrDefault("message")?.ToString());

        Assert.Equal("Sala 2", Convert.ToString(_fx.Scalar("SELECT ROOM FROM GYM_CLASS_SESSIONS WHERE ID = @id", ("@id", sessionId))));

        Assert.True(classes.DeleteClassSession(sessionId, out _));
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_CLASS_SESSIONS WHERE ID = @id", ("@id", sessionId))));
    }

    [Fact]
    public void Session_With_Invalid_Time_Range_Is_Rejected()
    {
        if (!_fx.Available) return;

        var classes = new ClassesModule(_fx.Tools!);
        long classTypeId = CreateClassType(classes, "Box", 5);

        DateTime start = DateTime.Now.AddDays(1).Date.AddHours(8);

        Assert.False(classes.CreateClassSession(new Dictionary<string, object>
        {
            ["classTypeId"] = classTypeId,
            ["startTime"] = start.ToString("o"),
            ["endTime"] = start.AddMinutes(-30).ToString("o"),
            ["room"] = "Sala 3"
        }, out var response));

        Assert.Contains("posterior", response.GetValueOrDefault("message")?.ToString() ?? "");
    }

    private long CreateClassType(ClassesModule classes, string name, int capacity)
    {
        string typeName = $"{name}_{Guid.NewGuid():N}"[..20];

        Assert.True(classes.CreateClassType(new Dictionary<string, object>
        {
            ["typeName"] = typeName,
            ["description"] = "Pruebas",
            ["durationMinutes"] = 60,
            ["maxParticipants"] = capacity
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_CLASS_TYPES WHERE TYPE_NAME = @n", ("@n", typeName)));
    }

    private long CreateSession(ClassesModule classes, long classTypeId, DateTime start)
    {
        Assert.True(classes.CreateClassSession(new Dictionary<string, object>
        {
            ["classTypeId"] = classTypeId,
            ["instructorId"] = 0L,
            ["room"] = "Sala 1",
            ["startTime"] = start.ToString("o"),
            ["endTime"] = start.AddHours(1).ToString("o")
        }, out var created), created.GetValueOrDefault("message")?.ToString());

        return Convert.ToInt64(created["sessionId"]);
    }

    private long CreateMember(string prefix)
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"{prefix}_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = prefix,
            ["lastName"] = "Test",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }

    private int Spots(long sessionId)
    {
        return Convert.ToInt32(_fx.Scalar("SELECT SPOTS_RESERVED FROM GYM_CLASS_SESSIONS WHERE ID = @id", ("@id", sessionId)));
    }

    private string SessionStatus(long sessionId)
    {
        return Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_CLASS_SESSIONS WHERE ID = @id", ("@id", sessionId))) ?? "";
    }

    private string ReservationStatus(long reservationId)
    {
        return Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_CLASS_RESERVATIONS WHERE ID = @id", ("@id", reservationId))) ?? "";
    }
}
