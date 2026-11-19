using GymService.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class EmailFlowTests
{
    private readonly DatabaseFixture _fx;

    public EmailFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void RequestOtp_Returns_Code_In_Development_Mode()
    {
        if (!_fx.Available) return;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OtpExpiryMinutes"] = "10",
                ["Email:SmtpServer"] = "",
                ["Email:FromAddress"] = "noreply@test.local",
                ["Email:SmtpUser"] = "",
                ["Email:ShowCodeInResponse"] = "true"
            })
            .Build();

        var identity = new IdentityModule(_fx.Tools!, config);
        string email = $"otp_{Guid.NewGuid():N}@test.local";

        Assert.True(identity.RequestOtpByEmail(email, "login", out var response), response.GetValueOrDefault("message")?.ToString());

        string code = Convert.ToString(response["code"]) ?? string.Empty;
        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsDigit(c)));

        Assert.Equal(1, Convert.ToInt32(_fx.Scalar(
            "SELECT COUNT(*) FROM GYM_OTP_CODES WHERE EMAIL = @e AND USED = 0", ("@e", email))));
    }

    [Fact]
    public void RequestOtp_Fails_When_Smtp_Is_Required_But_Not_Configured()
    {
        if (!_fx.Available) return;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OtpExpiryMinutes"] = "10",
                ["Email:SmtpServer"] = "",
                ["Email:FromAddress"] = "noreply@test.local",
                ["Email:SmtpUser"] = "",
                ["Email:ShowCodeInResponse"] = "false"
            })
            .Build();

        var identity = new IdentityModule(_fx.Tools!, config);
        string email = $"otp_{Guid.NewGuid():N}@test.local";

        Assert.False(identity.RequestOtpByEmail(email, "login", out var response));
        Assert.Contains("no está configurado", response.GetValueOrDefault("message")?.ToString() ?? "");

        // No debe quedar un OTP huérfano.
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar(
            "SELECT COUNT(*) FROM GYM_OTP_CODES WHERE EMAIL = @e", ("@e", email))));
    }
}

