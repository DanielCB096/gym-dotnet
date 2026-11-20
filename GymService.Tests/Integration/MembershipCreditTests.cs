using GymService.Modules.Members;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class MembershipCreditTests
{
    private readonly DatabaseFixture _fx;

    public MembershipCreditTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Cancel_Creates_Prorated_Credit_And_Renewal_Applies_It()
    {
        if (!_fx.Available) return;

        const long memberId = 970001;
        const long planId = 970002;
        const long membershipId = 970003;

        DateTime today = DateTime.Today;
        SeedMembership(memberId, planId, membershipId, today.AddDays(-15), today.AddDays(15), 900m);

        var module = new MembersModule(_fx.Tools!);

        Assert.True(module.CancelMembership(membershipId, "Cambio de ciudad", out var cancellation),
            cancellation.GetValueOrDefault("message")?.ToString());

        Assert.Equal(450m, Convert.ToDecimal(cancellation["creditAmount"]));
        Assert.Equal(15, Convert.ToInt32(cancellation["creditDays"]));
        long creditId = Convert.ToInt64(cancellation["creditId"]);

        Assert.Equal(450m, Convert.ToDecimal(_fx.Scalar(
            "SELECT REMAINING_AMOUNT FROM GYM_MEMBER_CREDITS WHERE ID = @id", ("@id", creditId))));

        Assert.True(module.AssignMembership(memberId, new Dictionary<string, object>
        {
            ["planId"] = planId,
            ["paymentMethod"] = "cash",
            ["amount"] = 900m
        }, out var renewal), renewal.GetValueOrDefault("message")?.ToString());

        Assert.Equal(450m, Convert.ToDecimal(renewal["creditApplied"]));
        Assert.Equal(450m, Convert.ToDecimal(renewal["amountPaid"]));
        Assert.Contains("crédito", renewal["message"]!.ToString(), StringComparison.OrdinalIgnoreCase);

        Assert.Equal("applied", Convert.ToString(_fx.Scalar(
            "SELECT STATUS FROM GYM_MEMBER_CREDITS WHERE ID = @id", ("@id", creditId))));
        Assert.Equal(0m, Convert.ToDecimal(_fx.Scalar(
            "SELECT REMAINING_AMOUNT FROM GYM_MEMBER_CREDITS WHERE ID = @id", ("@id", creditId))));

        Assert.Equal(450m, Convert.ToDecimal(_fx.Scalar(
            "SELECT SUM(AMOUNT) FROM GYM_PAYMENTS WHERE MEMBERSHIP_ID = @id",
            ("@id", Convert.ToInt64(renewal["membershipId"])))));
    }

    [Fact]
    public void Credit_Larger_Than_Charge_Leaves_Remaining_Balance()
    {
        if (!_fx.Available) return;

        const long memberId = 970011;
        const long planId = 970012;
        const long membershipId = 970013;

        DateTime today = DateTime.Today;
        // 600 de crédito (900 / 30 * 20 días), cobro de 500: quedan 100 disponibles.
        SeedMembership(memberId, planId, membershipId, today.AddDays(-10), today.AddDays(20), 900m);

        var module = new MembersModule(_fx.Tools!);

        Assert.True(module.CancelMembership(membershipId, "Prueba de saldo", out var cancellation));
        long creditId = Convert.ToInt64(cancellation["creditId"]);
        Assert.Equal(600m, Convert.ToDecimal(cancellation["creditAmount"]));

        Assert.True(module.AssignMembership(memberId, new Dictionary<string, object>
        {
            ["planId"] = planId,
            ["paymentMethod"] = "cash",
            ["amount"] = 500m
        }, out var renewal), renewal.GetValueOrDefault("message")?.ToString());

        Assert.Equal(500m, Convert.ToDecimal(renewal["creditApplied"]));
        Assert.Equal(0m, Convert.ToDecimal(renewal["amountPaid"]));
        Assert.Equal("available", Convert.ToString(_fx.Scalar(
            "SELECT STATUS FROM GYM_MEMBER_CREDITS WHERE ID = @id", ("@id", creditId))));
        Assert.Equal(100m, Convert.ToDecimal(_fx.Scalar(
            "SELECT REMAINING_AMOUNT FROM GYM_MEMBER_CREDITS WHERE ID = @id", ("@id", creditId))));

        Assert.Equal(0, Convert.ToInt32(_fx.Scalar(
            "SELECT COUNT(*) FROM GYM_PAYMENTS WHERE MEMBERSHIP_ID = @id",
            ("@id", Convert.ToInt64(renewal["membershipId"])))));
    }

    private void SeedMembership(long memberId, long planId, long membershipId, DateTime start, DateTime end, decimal price)
    {
        _fx.Execute(
            "INSERT INTO GYM_MEMBERS (ID, FIRST_NAME, LAST_NAME, EMAIL, STATUS) " +
            "VALUES (@id, 'Credito', 'Prueba', @email, 'active')",
            ("@id", memberId), ("@email", $"credito{memberId}@test.local"));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIP_PLANS (ID, PLAN_NAME, PRICE, DURATION_DAYS) " +
            "VALUES (@id, @name, @price, 30)",
            ("@id", planId), ("@name", $"Plan credito {planId}"), ("@price", price));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIPS (ID, MEMBER_ID, PLAN_ID, START_DATE, END_DATE, STATUS) " +
            "VALUES (@id, @member, @plan, @start, @end, 'active')",
            ("@id", membershipId), ("@member", memberId), ("@plan", planId),
            ("@start", start), ("@end", end));
    }
}
