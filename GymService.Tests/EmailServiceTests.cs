using GymService.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests;

public class EmailServiceTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void IsConfigured_Is_False_Without_Smtp_Settings()
    {
        var service = new EmailService(BuildConfig(new Dictionary<string, string?>
        {
            ["Email:SmtpServer"] = "",
            ["Email:FromAddress"] = "noreply@test.local",
            ["Email:SmtpUser"] = ""
        }));

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_Is_True_With_Server_From_And_User()
    {
        var service = new EmailService(BuildConfig(new Dictionary<string, string?>
        {
            ["Email:SmtpServer"] = "smtp.test.local",
            ["Email:FromAddress"] = "noreply@test.local",
            ["Email:SmtpUser"] = "user"
        }));

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void TrySendOtp_Reports_Error_When_Not_Configured()
    {
        var service = new EmailService(BuildConfig(new Dictionary<string, string?>
        {
            ["Email:SmtpServer"] = "",
            ["Email:FromAddress"] = "noreply@test.local",
            ["Email:SmtpUser"] = ""
        }));

        Assert.False(service.TrySendOtp("socio@test.local", "123456", "login", out string error));
        Assert.Contains("no estÃ¡ configurado", error);
    }
}

