using AxDatabase;
using GymService.Modules.Members;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class MembersFlowTests
{
    private readonly DatabaseFixture _fx;

    public MembersFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void CreateMember_With_Membership_Persists_Both_And_Audit()
    {
        if (!_fx.Available) return;

        EnsurePlan("Mensual", 30);

        var module = new MembersModule(_fx.Tools!);
        string email = $"member_{Guid.NewGuid():N}@test.local";

        CurrentUser.Id = 1;

        try
        {
            bool ok = module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Ana",
                ["lastName"] = "Perez",
                ["email"] = email,
                ["phone"] = "5551234567",
                ["membershipType"] = "Mensual",
                ["createdBy"] = 1L
            }, out var response);

            Assert.True(ok, response.GetValueOrDefault("message")?.ToString());

            long memberId = Convert.ToInt64(response["memberId"]);
            Assert.True(memberId > 0);

            Assert.Equal(1, Count("SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = @id AND EMAIL = @email", ("@id", memberId), ("@email", email)));
            Assert.Equal(1, Count("SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE MEMBER_ID = @id", ("@id", memberId)));
            Assert.Equal(1L, Convert.ToInt64(_fx.Scalar("SELECT CREATED_BY FROM GYM_MEMBERS WHERE ID = @id", ("@id", memberId))));

            Assert.True(module.GetMembers(out var list, id: memberId));
            var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
            Assert.Single(data);
            Assert.Equal(email, data[0]["email"]);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Freeze_And_Resume_Membership_Updates_Status()
    {
        if (!_fx.Available) return;

        var module = new MembersModule(_fx.Tools!);
        string email = $"freeze_{Guid.NewGuid():N}@test.local";

        EnsurePlan("Mensual", 30);

        Assert.True(module.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Luis",
            ["lastName"] = "Gomez",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        long memberId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
        long membershipId = InsertMembership(memberId);

        Assert.True(module.FreezeMembership(membershipId, DateTime.Now, DateTime.Now.AddDays(7), out _));
        Assert.Equal("frozen", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_MEMBERSHIPS WHERE ID = @id", ("@id", membershipId))));

        Assert.True(module.ResumeMembership(membershipId, out _));
        Assert.Equal("active", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_MEMBERSHIPS WHERE ID = @id", ("@id", membershipId))));
    }

    private void EnsurePlan(string name, int days)
    {
        if (Count("SELECT COUNT(*) FROM GYM_MEMBERSHIP_PLANS WHERE PLAN_NAME = @n", ("@n", name)) > 0)
            return;

        var session = _fx.Tools!.CreateSession("TEST");
        session.InsertObject(new GYM_MEMBERSHIP_PLANS
        {
            PLAN_NAME = name,
            DESCRIPTION = "Plan de pruebas",
            PRICE = 500m,
            CURRENCY = "MXN",
            DURATION_DAYS = days,
            MAX_DAILY_ENTRIES = -1,
            INCLUDES_CLASSES = true,
            INCLUDES_PREMIUM_CLASSES = false,
            IS_ACTIVE = true
        });

        Assert.False(session.TryExecute().IsError);
    }

    private long InsertMembership(long memberId)
    {
        long planId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERSHIP_PLANS WHERE PLAN_NAME = 'Mensual'"));
        long id = _fx.Tools!.GenerateUniqueID(GYM_MEMBERSHIPS.DatabaseMappingObject, 1).GetInt64UniqueId();

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIPS (ID, MEMBER_ID, PLAN_ID, START_DATE, END_DATE, STATUS, AUTO_RENEW) " +
            "VALUES (@id, @member, @plan, GETDATE(), DATEADD(DAY, 30, GETDATE()), 'active', 0)",
            ("@id", id), ("@member", memberId), ("@plan", planId));

        return id;
    }

    private int Count(string sql, params (string, object?)[] parameters)
    {
        return Convert.ToInt32(_fx.Scalar(sql, parameters));
    }
}
