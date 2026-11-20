using GymService.Modules.Dashboard;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class DashboardFlowTests
{
    private readonly DatabaseFixture _fx;

    public DashboardFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Summary_Includes_Today_Totals_Trends_And_Upcoming_Expirations()
    {
        if (!_fx.Available) return;

        const long memberId = 940001;
        const long planId = 940002;
        const long membershipId = 940003;
        const long paymentId = 940004;
        const long saleId = 940005;
        const long accessId = 940006;

        DateTime today = DateTime.Today;
        DateTime paymentDate = today.AddHours(10);
        DateTime saleDate = today.AddHours(11);
        DateTime accessDate = today.AddHours(12);
        DateTime endDate = today.AddDays(3);

        _fx.Execute(
            "INSERT INTO GYM_MEMBERS (ID, FIRST_NAME, LAST_NAME, EMAIL, STATUS) " +
            "VALUES (@id, 'Dora', 'Dashboard', 'dora.dashboard@test.local', 'active')",
            ("@id", memberId));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIP_PLANS (ID, PLAN_NAME, PRICE, DURATION_DAYS) " +
            "VALUES (@id, 'Plan Dashboard', 650, 30)",
            ("@id", planId));

        _fx.Execute(
            "INSERT INTO GYM_MEMBERSHIPS (ID, MEMBER_ID, PLAN_ID, START_DATE, END_DATE, STATUS) " +
            "VALUES (@id, @member, @plan, @start, @end, 'active')",
            ("@id", membershipId), ("@member", memberId), ("@plan", planId),
            ("@start", today.AddDays(-27)), ("@end", endDate));

        _fx.Execute(
            "INSERT INTO GYM_PAYMENTS (ID, MEMBER_ID, MEMBERSHIP_ID, AMOUNT, PAYMENT_METHOD, PAYMENT_DATE) " +
            "VALUES (@id, @member, @membership, 650, 'cash', @date)",
            ("@id", paymentId), ("@member", memberId), ("@membership", membershipId),
            ("@date", paymentDate));

        _fx.Execute(
            "INSERT INTO GYM_SALES (ID, SALE_NUMBER, TOTAL_AMOUNT, TAX_AMOUNT, PAYMENT_METHOD, SALE_DATE, STATUS) " +
            "VALUES (@id, 'DASH-940005', 100, 0, 'cash', @date, 'completed')",
            ("@id", saleId), ("@date", saleDate));

        _fx.Execute(
            "INSERT INTO GYM_ACCESS_LOG (ID, MEMBER_ID, ACCESS_TYPE, ACCESS_RESULT, ACCESSED_AT) " +
            "VALUES (@id, @member, 'code', 'granted', @date)",
            ("@id", accessId), ("@member", memberId), ("@date", accessDate));

        var module = new DashboardModule(_fx.Tools!);

        Assert.True(module.GetSummary(out var response),
            response.GetValueOrDefault("message")?.ToString());

        var data = Assert.IsAssignableFrom<Dictionary<string, object>>(response["data"]);
        Assert.True(Convert.ToInt64(data["expiringSoon"]) >= 1);
        Assert.True(Convert.ToDecimal(data["salesToday"]) >= 100m);
        Assert.True(Convert.ToDecimal(data["paymentsToday"]) >= 650m);
        Assert.True(Convert.ToInt64(data["accessToday"]) >= 1);

        var trends = Assert.IsAssignableFrom<Dictionary<string, object>>(data["trends"]);

        var revenue = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(trends["revenue"]);
        Assert.Equal(7, revenue.Count);
        var lastRevenue = revenue[^1];
        Assert.Equal(today.ToString("yyyy-MM-dd"), lastRevenue["date"]);
        Assert.True(Convert.ToDecimal(lastRevenue["total"]) >= 750m);
        Assert.True(Convert.ToDecimal(lastRevenue["sales"]) >= 100m);
        Assert.True(Convert.ToDecimal(lastRevenue["payments"]) >= 650m);

        var access = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(trends["access"]);
        Assert.Equal(7, access.Count);
        Assert.True(Convert.ToInt64(access[^1]["count"]) >= 1);

        var upcoming = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(trends["upcoming"]);
        var item = Assert.Single(upcoming, u => Convert.ToInt64(u["id"]) == membershipId);
        Assert.Equal("Dora Dashboard", item["memberName"]);
        Assert.Equal("Plan Dashboard", item["planName"]);
        Assert.Equal(3, Convert.ToInt32(item["daysLeft"]));
    }
}
