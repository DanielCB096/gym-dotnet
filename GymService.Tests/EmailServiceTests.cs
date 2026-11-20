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
        Assert.Contains("no está configurado", error);
    }

    [Fact]
    public void Welcome_Email_Is_Written_To_Outbox()
    {
        string outbox = NewOutbox();

        try
        {
            var service = NewOutboxService(outbox);

            Assert.True(service.IsEnabled);
            Assert.True(service.TrySendWelcome("ana@test.local", "Ana <Prueba>", out string error), error);

            string content = ReadSingleHtml(outbox);
            Assert.Contains("Ana &lt;Prueba&gt;", content);
            Assert.Contains("Bienvenido a Centro Fit", content);
            Assert.Contains("ana@test.local", content);
        }
        finally
        {
            Directory.Delete(outbox, true);
        }
    }

    [Fact]
    public void MembershipReceipt_Includes_Amount_And_Payment_Method()
    {
        string outbox = NewOutbox();

        try
        {
            var service = NewOutboxService(outbox);
            var receipt = new MembershipReceipt
            {
                PlanName = "Plan Mensual",
                StartDate = new DateTime(2026, 11, 1),
                EndDate = new DateTime(2026, 12, 1),
                Amount = 650m,
                Currency = "MXN",
                PaymentMethod = "cash",
                Reference = "REF-123",
                PaymentDate = new DateTime(2026, 11, 1, 10, 30, 0)
            };

            Assert.True(service.TrySendMembershipReceipt("ana@test.local", "Ana Prueba", receipt, out string error), error);

            string content = ReadSingleHtml(outbox);
            Assert.Contains("Recibo de membresía", content);
            Assert.Contains("650.00 MXN", content);
            Assert.Contains("Efectivo", content);
            Assert.Contains("01/11/2026 — 01/12/2026", content);
            Assert.Contains("REF-123", content);
        }
        finally
        {
            Directory.Delete(outbox, true);
        }
    }

    [Fact]
    public void ExpiryNotice_Uses_Days_Left_And_Plan_Name()
    {
        string outbox = NewOutbox();

        try
        {
            var service = NewOutboxService(outbox);

            Assert.True(service.TrySendExpiryNotice(
                "ana@test.local", "Ana Prueba", "Plan Mensual",
                new DateTime(2026, 11, 23), 3, out string error), error);

            string content = ReadSingleHtml(outbox);
            Assert.Contains("Asunto: Tu membresía vence en 3 días - Centro Fit", content);
            Assert.Contains("Plan Mensual", content);
            Assert.Contains("23/11/2026", content);
            Assert.Contains("en 3 días", content);

            Assert.True(service.TrySendExpiryNotice(
                "ana@test.local", "Ana Prueba", "Plan Mensual",
                new DateTime(2026, 11, 21), 1, out error), error);
            Assert.Contains("vence mañana", Directory.GetFiles(outbox, "*.html")
                .Select(File.ReadAllText)
                .Single(text => text.Contains("mañana")));
        }
        finally
        {
            Directory.Delete(outbox, true);
        }
    }

    [Fact]
    public void Test_Email_Is_Written_To_Outbox()
    {
        string outbox = NewOutbox();

        try
        {
            var service = NewOutboxService(outbox);

            Assert.True(service.TrySendTest("ana@test.local", out string error), error);

            string content = ReadSingleHtml(outbox);
            Assert.Contains("Asunto: Prueba de correo - Centro Fit", content);
            Assert.Contains("correo de prueba", content);
            Assert.Contains("El correo funciona", content);
        }
        finally
        {
            Directory.Delete(outbox, true);
        }
    }

    [Theory]
    [InlineData(465, true, "SslOnConnect")]
    [InlineData(587, true, "StartTls")]
    [InlineData(25, false, "None")]
    public void ResolveSocketOptions_Uses_SSL_On_465(int port, bool useSsl, string expected)
    {
        Assert.Equal(expected, EmailService.ResolveSocketOptions(port, useSsl).ToString());
    }

    private static string NewOutbox()
    {
        return Path.Combine(Path.GetTempPath(), "gym-email-tests", Guid.NewGuid().ToString("N"));
    }

    private static EmailService NewOutboxService(string outbox)
    {
        return new EmailService(BuildConfig(new Dictionary<string, string?>
        {
            ["Email:SmtpServer"] = "",
            ["Email:FromAddress"] = "noreply@test.local",
            ["Email:SmtpUser"] = "",
            ["Email:OutboxDirectory"] = outbox
        }));
    }

    private static string ReadSingleHtml(string outbox)
    {
        return File.ReadAllText(Directory.GetFiles(outbox, "*.html").Single());
    }
}

