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
                ["curp"] = "PEPJ900101HDFRRN01",
                ["rfc"] = "PEPJ900101AB1",
                ["address"] = "Calle Prueba 123",
                ["emergencyContact"] = "Maria Perez",
                ["emergencyPhone"] = "5559876543",
                ["photoUrl"] = "https://example.com/foto.jpg",
                ["notes"] = "Nota de prueba",
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
            Assert.Equal("PEPJ900101HDFRRN01", data[0]["curp"]);
            Assert.Equal("PEPJ900101AB1", data[0]["rfc"]);
            Assert.Equal("Calle Prueba 123", data[0]["address"]);
            Assert.Equal("Maria Perez", data[0]["emergencyContact"]);
            Assert.Equal("5559876543", data[0]["emergencyPhone"]);
            Assert.Equal("https://example.com/foto.jpg", data[0]["photoUrl"]);
            Assert.Equal("Nota de prueba", data[0]["notes"]);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void CreateMember_Rejects_Duplicate_Email_And_Unknown_Plan()
    {
        if (!_fx.Available) return;

        EnsurePlan("Mensual", 30);

        var module = new MembersModule(_fx.Tools!);
        string email = $"duplicate_{Guid.NewGuid():N}@test.local";

        CurrentUser.Id = 1;

        try
        {
            Assert.True(module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Original",
                ["lastName"] = "Test",
                ["email"] = email,
                ["membershipType"] = "Mensual"
            }, out _));

            Assert.False(module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Duplicado",
                ["lastName"] = "Test",
                ["email"] = email,
                ["membershipType"] = "Mensual"
            }, out var duplicate));

            Assert.Contains("correo", duplicate.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            Assert.False(module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Plan",
                ["lastName"] = "Inexistente",
                ["email"] = $"plan_{Guid.NewGuid():N}@test.local",
                ["membershipType"] = "PlanQueNoExiste"
            }, out var unknownPlan));

            Assert.Contains("plan", unknownPlan.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

            Assert.False(module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "",
                ["lastName"] = "SinNombre",
                ["email"] = $"noname_{Guid.NewGuid():N}@test.local"
            }, out var noName));

            Assert.Contains("nombre", noName.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Member_Without_Plan_Is_Not_Reported_As_Having_A_Membership()
    {
        if (!_fx.Available) return;

        var module = new MembersModule(_fx.Tools!);
        string email = $"noplan_{Guid.NewGuid():N}@test.local";

        CurrentUser.Id = 1;

        try
        {
            Assert.True(module.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Sin",
                ["lastName"] = "Plan",
                ["email"] = email,
                ["membershipType"] = ""
            }, out var created));

            long memberId = Convert.ToInt64(created["memberId"]);

            Assert.True(module.GetMembers(out var list, id: memberId));
            var data = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(list["data"]);
            var row = Assert.Single(data);

            Assert.Equal("active", row["status"]);
            Assert.Equal("none", row["membershipStatus"]);
            Assert.False(Convert.ToBoolean(row["hasActiveMembership"]));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Assign_And_Renew_Membership_Extends_Period_And_Records_Payment()
    {
        if (!_fx.Available) return;

        EnsurePlan("Mensual", 30);
        var module = new MembersModule(_fx.Tools!);
        long planId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERSHIP_PLANS WHERE PLAN_NAME = 'Mensual'"));
        long memberId = CreateMemberWithoutPlan(module);
        CurrentUser.Id = 1;

        try
        {
            Assert.True(module.AssignMembership(memberId, new Dictionary<string, object>
            {
                ["planId"] = planId,
                ["paymentMethod"] = "cash",
                ["amount"] = 500m
            }, out var assigned), assigned.GetValueOrDefault("message")?.ToString());

            long membershipId = Convert.ToInt64(assigned["membershipId"]);
            DateTime firstEnd = Convert.ToDateTime(assigned["endDate"]);
            Assert.Equal("active", assigned["status"]);
            Assert.Equal(firstEnd.Date, DateTime.Now.Date.AddDays(30));

            Assert.Equal(1, Convert.ToInt32(_fx.Scalar(
                "SELECT COUNT(*) FROM GYM_PAYMENTS WHERE MEMBER_ID = @m AND MEMBERSHIP_ID = @id AND PAYMENT_METHOD = 'cash'",
                ("@m", memberId), ("@id", membershipId))));

            // Renovación anticipada: extiende desde el vencimiento actual.
            Assert.True(module.AssignMembership(memberId, new Dictionary<string, object>
            {
                ["planId"] = planId,
                ["paymentMethod"] = "transfer",
                ["amount"] = 500m
            }, out var renewed), renewed.GetValueOrDefault("message")?.ToString());

            DateTime secondEnd = Convert.ToDateTime(renewed["endDate"]);
            Assert.Equal(firstEnd.Date.AddDays(30), secondEnd.Date);
            Assert.Equal(membershipId, Convert.ToInt64(renewed["membershipId"]));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE MEMBER_ID = @m", ("@m", memberId))));
            Assert.Equal(2, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_PAYMENTS WHERE MEMBER_ID = @m", ("@m", memberId))));

            // Cortesía: activa sin crear pago.
            long courtesyMember = CreateMemberWithoutPlan(module);
            Assert.True(module.AssignMembership(courtesyMember, new Dictionary<string, object>
            {
                ["planId"] = planId,
                ["noCharge"] = true
            }, out var courtesy), courtesy.GetValueOrDefault("message")?.ToString());

            Assert.True(Convert.ToBoolean(courtesy["noCharge"]));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_PAYMENTS WHERE MEMBER_ID = @m", ("@m", courtesyMember))));

            // Una membresía congelada no se renueva.
            Assert.True(module.FreezeMembership(membershipId, DateTime.Now, DateTime.Now.AddDays(5), out _));
            Assert.False(module.AssignMembership(memberId, new Dictionary<string, object>
            {
                ["planId"] = planId,
                ["paymentMethod"] = "cash",
                ["amount"] = 500m
            }, out var frozen));

            Assert.Contains("congelada", frozen.GetValueOrDefault("message")?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    private long CreateMemberWithoutPlan(MembersModule module)
    {
        string email = $"assign_{Guid.NewGuid():N}@test.local";

        Assert.True(module.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Membresía",
            ["lastName"] = "Test",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
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
