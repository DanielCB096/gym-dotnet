using AxDatabase;
using GymService.Services;
using GymService.Tools;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class MaintenanceTests
{
    private readonly DatabaseFixture _fx;

    public MaintenanceTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Cleanup_Removes_Expired_Sessions_And_Otp_But_Keeps_Valid_Ones()
    {
        if (!_fx.Available) return;

        // Usuario propietario de las sesiones (FK).
        var user = new GYM_USERS
        {
            USERNAME = $"cleanup_{Guid.NewGuid():N}"[..20],
            EMAIL = $"cleanup_{Guid.NewGuid():N}@test.local",
            PASSWORD_HASH = "x",
            ROLE = "OWNER",
            IS_ACTIVE = true
        };

        var userSession = _fx.Tools!.CreateSession("TEST", -1);
        userSession.InsertObject(user);
        Assert.False(userSession.TryExecute().IsError);

        var expiredSession = new GYM_SESSIONS
        {
            USER_ID = user.ID,
            TOKEN_HASH = SecurityValidation.Sha256Hash(Guid.NewGuid().ToString()),
            EXPIRES_AT = DateTime.Now.AddDays(-1),
            IS_ACTIVE = true
        };

        var validSession = new GYM_SESSIONS
        {
            USER_ID = user.ID,
            TOKEN_HASH = SecurityValidation.Sha256Hash(Guid.NewGuid().ToString()),
            EXPIRES_AT = DateTime.Now.AddDays(1),
            IS_ACTIVE = true
        };

        var sessionSession = _fx.Tools.CreateSession("TEST", -1);
        sessionSession.InsertObject(expiredSession);
        sessionSession.InsertObject(validSession);
        Assert.False(sessionSession.TryExecute().IsError);

        var expiredOtp = new GYM_OTP_CODES
        {
            EMAIL = "cleanup@test.local",
            CODE_HASH = "x",
            PURPOSE = "login",
            EXPIRES_AT = DateTime.Now.AddMinutes(-10),
            USED = false
        };

        var validOtp = new GYM_OTP_CODES
        {
            EMAIL = "cleanup@test.local",
            CODE_HASH = "y",
            PURPOSE = "login",
            EXPIRES_AT = DateTime.Now.AddMinutes(10),
            USED = false
        };

        var otpSession = _fx.Tools.CreateSession("TEST", -1);
        otpSession.InsertObject(expiredOtp);
        otpSession.InsertObject(validOtp);
        Assert.False(otpSession.TryExecute().IsError);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maintenance:SessionRetentionDays"] = "7",
                ["Maintenance:OtpRetentionDays"] = "1",
                ["Maintenance:AccessLogRetentionDays"] = "90"
            })
            .Build();

        var rc = new MaintenanceService(_fx.Tools, config).RunCleanup();

        Assert.True(rc.IsOK, rc.Message);

        Assert.Equal(0, Count("SELECT COUNT(*) FROM GYM_SESSIONS WHERE ID = @id", ("@id", expiredSession.ID)));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM GYM_SESSIONS WHERE ID = @id", ("@id", validSession.ID)));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM GYM_OTP_CODES WHERE ID = @id", ("@id", expiredOtp.ID)));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM GYM_OTP_CODES WHERE ID = @id", ("@id", validOtp.ID)));
    }

    private int Count(string sql, params (string, object?)[] parameters)
    {
        return Convert.ToInt32(_fx.Scalar(sql, parameters));
    }
}
