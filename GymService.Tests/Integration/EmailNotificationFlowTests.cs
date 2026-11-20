using GymService.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class EmailNotificationFlowTests
{
    private readonly DatabaseFixture _fx;

    public EmailNotificationFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void ExpiryNotice_IsSentOnce_AndMarkedInDatabase()
    {
        if (!_fx.Available) return;

        string outbox = Path.Combine(Path.GetTempPath(), "gym-email-tests", Guid.NewGuid().ToString("N"));

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:OutboxDirectory"] = outbox,
                    ["Notifications:ExpiryNoticeDays"] = "7"
                })
                .Build();

            var email = new EmailService(configuration);
            var maintenance = new MaintenanceService(_fx.Tools!, configuration, email);

            const long memberId = 930001;
            const long planId = 930002;
            const long membershipId = 930003;

            _fx.Execute(
                "INSERT INTO GYM_MEMBERS (ID, FIRST_NAME, LAST_NAME, EMAIL, STATUS) " +
                "VALUES (@id, 'Eva', 'Aviso', 'eva.aviso@test.local', 'active')",
                ("@id", memberId));

            _fx.Execute(
                "INSERT INTO GYM_MEMBERSHIP_PLANS (ID, PLAN_NAME, PRICE, DURATION_DAYS) " +
                "VALUES (@id, 'Plan Aviso', 500, 30)",
                ("@id", planId));

            _fx.Execute(
                "INSERT INTO GYM_MEMBERSHIPS (ID, MEMBER_ID, PLAN_ID, START_DATE, END_DATE, STATUS) " +
                "VALUES (@id, @member, @plan, '2026-11-01', '2026-11-25', 'active')",
                ("@id", membershipId), ("@member", memberId), ("@plan", planId));

            Assert.Equal(1, maintenance.SendExpiryNotices(new DateTime(2026, 11, 20)));

            var files = Directory.GetFiles(outbox, "*.html");
            Assert.Single(files);

            string content = File.ReadAllText(files[0]);
            Assert.Contains("Eva Aviso", content);
            Assert.Contains("Plan Aviso", content);
            Assert.Contains("en 5 días", content);
            Assert.Contains("25/11/2026", content);

            var sentAt = _fx.Scalar(
                "SELECT EXPIRY_NOTICE_SENT_AT FROM GYM_MEMBERSHIPS WHERE ID = @id",
                ("@id", membershipId));
            Assert.IsType<DateTime>(sentAt);

            Assert.Equal(0, maintenance.SendExpiryNotices(new DateTime(2026, 11, 20)));
            Assert.Single(Directory.GetFiles(outbox, "*.html"));
        }
        finally
        {
            if (Directory.Exists(outbox))
                Directory.Delete(outbox, true);
        }
    }
}
