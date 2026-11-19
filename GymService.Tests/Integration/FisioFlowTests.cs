using GymService.Modules.Fisio;
using GymService.Modules.Members;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class FisioFlowTests
{
    private readonly DatabaseFixture _fx;

    public FisioFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Packages_And_Appointments_Control_Sessions_And_History()
    {
        if (!_fx.Available) return;

        var fisio = new FisioModule(_fx.Tools!);
        long memberId = CreateMember("Fisio");

        CurrentUser.Id = 1;

        try
        {
            string packageName = $"Paquete_{Guid.NewGuid():N}"[..24];

            Assert.True(fisio.CreatePackage(new Dictionary<string, object>
            {
                ["name"] = packageName,
                ["memberId"] = memberId,
                ["sessions"] = 2,
                ["price"] = 500m,
                ["startDate"] = DateTime.Now.Date.ToString("o"),
                ["endDate"] = DateTime.Now.Date.AddDays(30).ToString("o")
            }, out var packageResponse), packageResponse.GetValueOrDefault("message")?.ToString());

            long packageId = Convert.ToInt64(packageResponse["packageId"]);

            Assert.True(fisio.GetPackages(out var packages, memberId));
            var packageRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(packages["data"]);
            var package = Assert.Single(packageRows);
            Assert.Equal(2, Convert.ToInt32(package["sessions"]));
            Assert.Equal(2, Convert.ToInt32(package["sessionsRemaining"]));
            Assert.Equal(memberId, Convert.ToInt64(package["memberId"]));

            // Cita válida y consumo de una sesión.
            Assert.True(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["packageId"] = packageId,
                ["appointmentDate"] = DateTime.Now.AddDays(1).ToString("o"),
                ["notes"] = "Primera sesión"
            }, out var firstAppointment), firstAppointment.GetValueOrDefault("message")?.ToString());

            long firstId = Convert.ToInt64(firstAppointment["appointmentId"]);

            Assert.True(fisio.SetAppointmentStatus(firstId, "completed", out var completed));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT SESSIONS_USED FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", packageId))));

            // Una cita completada no se edita ni se elimina.
            Assert.False(fisio.UpdateAppointment(firstId, new Dictionary<string, object>
            {
                ["notes"] = "Cambio"
            }, out _));

            Assert.False(fisio.DeleteAppointment(firstId, out _));

            // Segunda cita consume la última sesión y desactiva el paquete.
            Assert.True(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["packageId"] = packageId,
                ["appointmentDate"] = DateTime.Now.AddDays(2).ToString("o")
            }, out var secondAppointment), secondAppointment.GetValueOrDefault("message")?.ToString());

            long secondId = Convert.ToInt64(secondAppointment["appointmentId"]);
            Assert.True(fisio.SetAppointmentStatus(secondId, "completed", out _));

            Assert.Equal(2, Convert.ToInt32(_fx.Scalar("SELECT SESSIONS_USED FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", packageId))));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT IS_ACTIVE FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", packageId))));

            // Sin sesiones no se puede agendar otra cita con el paquete.
            Assert.False(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["packageId"] = packageId,
                ["appointmentDate"] = DateTime.Now.AddDays(3).ToString("o")
            }, out var exhausted));

            Assert.Contains("sesiones", exhausted.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            // Reprogramación y cancelación de una cita programada sin paquete.
            Assert.True(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["appointmentDate"] = DateTime.Now.AddDays(4).ToString("o")
            }, out var plainAppointment), plainAppointment.GetValueOrDefault("message")?.ToString());

            long plainId = Convert.ToInt64(plainAppointment["appointmentId"]);

            Assert.True(fisio.UpdateAppointment(plainId, new Dictionary<string, object>
            {
                ["appointmentDate"] = DateTime.Now.AddDays(5).ToString("o"),
                ["notes"] = "Reprogramada"
            }, out _));

            Assert.True(fisio.SetAppointmentStatus(plainId, "cancelled", out var cancelled));
            Assert.Equal("cancelled", Convert.ToString(cancelled["status"]));

            Assert.True(fisio.DeleteAppointment(plainId, out _));

            // La lista respeta filtros de estado.
            Assert.True(fisio.GetAppointments(out var completedAppointments, memberId, null, null, "completed"));
            var completedRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(completedAppointments["data"]);
            Assert.Equal(2, completedRows.Count);

            // Una cita fuera de la vigencia del paquete se rechaza.
            Assert.True(fisio.CreatePackage(new Dictionary<string, object>
            {
                ["name"] = $"Corto_{Guid.NewGuid():N}"[..18],
                ["memberId"] = memberId,
                ["sessions"] = 1,
                ["price"] = 100m,
                ["startDate"] = DateTime.Now.Date.ToString("o"),
                ["endDate"] = DateTime.Now.Date.AddDays(7).ToString("o")
            }, out var shortPackage), shortPackage.GetValueOrDefault("message")?.ToString());

            long shortPackageId = Convert.ToInt64(shortPackage["packageId"]);

            Assert.False(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["packageId"] = shortPackageId,
                ["appointmentDate"] = DateTime.Now.AddDays(20).ToString("o")
            }, out var outOfRange));

            Assert.Contains("vigencia", outOfRange.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            // No se puede eliminar un paquete con historial; sí desactivarlo.
            Assert.False(fisio.DeletePackage(packageId, out _));

            Assert.True(fisio.UpdatePackage(shortPackageId, new Dictionary<string, object>
            {
                ["isActive"] = false
            }, out var updatedPackage), updatedPackage.GetValueOrDefault("message")?.ToString());

            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT IS_ACTIVE FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", shortPackageId))));

            // Paquete sin uso se elimina.
            Assert.True(fisio.DeletePackage(shortPackageId, out _));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", shortPackageId))));

            // Un paquete general queda asignado al socio en la primera cita.
            Assert.True(fisio.CreatePackage(new Dictionary<string, object>
            {
                ["name"] = $"General_{Guid.NewGuid():N}"[..20],
                ["sessions"] = 1,
                ["price"] = 250m,
                ["startDate"] = DateTime.Now.Date.ToString("o"),
                ["endDate"] = DateTime.Now.Date.AddDays(15).ToString("o")
            }, out var generalPackage), generalPackage.GetValueOrDefault("message")?.ToString());

            long generalPackageId = Convert.ToInt64(generalPackage["packageId"]);
            Assert.Equal(DBNull.Value, _fx.Scalar("SELECT MEMBER_ID FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", generalPackageId)) ?? DBNull.Value);

            Assert.True(fisio.CreateAppointment(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["packageId"] = generalPackageId,
                ["appointmentDate"] = DateTime.Now.AddDays(2).ToString("o")
            }, out _));

            Assert.Equal(memberId, Convert.ToInt64(_fx.Scalar("SELECT MEMBER_ID FROM GYM_FISIO_PACKAGES WHERE ID = @id", ("@id", generalPackageId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    private long CreateMember(string prefix)
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"fisio_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = prefix,
            ["lastName"] = "Test",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }
}
