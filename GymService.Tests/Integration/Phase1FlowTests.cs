using GymService.Modules.Cash;
using GymService.Modules.Inventory;
using GymService.Modules.Kiosk;
using GymService.Modules.Access;
using GymService.Modules.Members;
using GymService.Modules.Sales;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class Phase1FlowTests
{
    private readonly DatabaseFixture _fx;

    public Phase1FlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Member_Update_Plans_And_Membership_Cancel_Work()
    {
        if (!_fx.Available) return;

        var members = new MembersModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string email = $"phase1_{Guid.NewGuid():N}@test.local";
            Assert.True(members.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Antes",
                ["lastName"] = "Apellido",
                ["email"] = email,
                ["membershipType"] = ""
            }, out var created));

            long memberId = Convert.ToInt64(created["memberId"]);

            Assert.True(members.UpdateMember(memberId, new Dictionary<string, object>
            {
                ["firstName"] = "Después",
                ["phone"] = "5550001111"
            }, out var updated), updated.GetValueOrDefault("message")?.ToString());

            Assert.Equal("Después", Convert.ToString(_fx.Scalar("SELECT FIRST_NAME FROM GYM_MEMBERS WHERE ID = @id", ("@id", memberId))));
            Assert.Equal("5550001111", Convert.ToString(_fx.Scalar("SELECT PHONE FROM GYM_MEMBERS WHERE ID = @id", ("@id", memberId))));

            // Plan CRUD
            string planName = $"Plan_{Guid.NewGuid():N}"[..18];
            Assert.True(members.CreatePlan(new Dictionary<string, object>
            {
                ["planName"] = planName,
                ["price"] = 300m,
                ["durationDays"] = 15,
                ["maxDailyEntries"] = 1
            }, out var planCreated), planCreated.GetValueOrDefault("message")?.ToString());

            long planId = Convert.ToInt64(planCreated["planId"]);
            Assert.True(members.UpdatePlan(planId, new Dictionary<string, object>
            {
                ["price"] = 350m
            }, out _));

            Assert.True(members.GetMembershipPlans(out var allPlans, true));
            var plans = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(allPlans["data"]);
            var plan = Assert.Single(plans, row => Convert.ToInt64(row["id"]) == planId);
            Assert.Equal(350m, Convert.ToDecimal(plan["price"]));
            Assert.True(Convert.ToBoolean(plan["isActive"]));

            // Asignar y cancelar membresía.
            Assert.True(members.AssignMembership(memberId, new Dictionary<string, object>
            {
                ["planId"] = planId,
                ["noCharge"] = true
            }, out var assigned), assigned.GetValueOrDefault("message")?.ToString());

            long membershipId = Convert.ToInt64(assigned["membershipId"]);

            Assert.True(members.UpdatePlan(planId, new Dictionary<string, object>
            {
                ["isActive"] = false
            }, out _));
            Assert.True(members.GetMembershipPlans(out var activePlans, false));
            Assert.DoesNotContain(
                Assert.IsAssignableFrom<List<Dictionary<string, object>>>(activePlans["data"]),
                row => Convert.ToInt64(row["id"]) == planId);

            Assert.False(members.CancelMembership(membershipId, "", out _));
            Assert.True(members.CancelMembership(membershipId, "Solicitud del socio", out var cancelled), cancelled.GetValueOrDefault("message")?.ToString());

            Assert.Equal("cancelled", Convert.ToString(_fx.Scalar("SELECT STATUS FROM GYM_MEMBERSHIPS WHERE ID = @id", ("@id", membershipId))));
            Assert.Equal("Solicitud del socio", Convert.ToString(_fx.Scalar("SELECT CANCEL_REASON FROM GYM_MEMBERSHIPS WHERE ID = @id", ("@id", membershipId))));

            Assert.True(members.GetMembershipHistory(memberId, out var history));
            var historyRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(history["data"]);
            Assert.Contains(historyRows, row => Convert.ToString(row["cancelReason"]) == "Solicitud del socio");
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Kiosk_Token_Rotation_And_Manual_Access_Work()
    {
        if (!_fx.Available) return;

        var kiosk = new KioskModule(_fx.Tools!);
        var access = new AccessModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            Assert.True(kiosk.RegisterDevice($"Kiosco_{Guid.NewGuid():N}"[..16], out var registered));
            long deviceId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_KIOSK_DEVICES ORDER BY ID DESC"));
            string oldToken = Convert.ToString(registered["token"]) ?? "";
            Assert.NotNull(registered["tokenExpiresAt"]);

            Assert.True(kiosk.ValidateDeviceToken(oldToken, out _));
            Assert.True(kiosk.RotateDeviceToken(deviceId, out var rotated));
            string newToken = Convert.ToString(rotated["token"]) ?? "";
            Assert.NotEqual(oldToken, newToken);
            Assert.False(kiosk.ValidateDeviceToken(oldToken, out _));
            Assert.True(kiosk.ValidateDeviceToken(newToken, out _));

            Assert.True(kiosk.DeactivateDevice(deviceId, out _));
            Assert.False(kiosk.ValidateDeviceToken(newToken, out _));

            var members = new MembersModule(_fx.Tools!);
            string email = $"manual_{Guid.NewGuid():N}@test.local";
            Assert.True(members.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Manual",
                ["lastName"] = "Test",
                ["email"] = email,
                ["membershipType"] = ""
            }, out var memberCreated));

            long memberId = Convert.ToInt64(memberCreated["memberId"]);
            Assert.True(access.RegisterManualAccess(memberId, "Cortesía de recepción", "reception", out var manual));
            Assert.Equal(memberId, Convert.ToInt64(manual["memberId"]));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar(
                "SELECT COUNT(*) FROM GYM_ACCESS_LOG WHERE MEMBER_ID = @m AND ACCESS_TYPE = 'manual' AND REASON = 'Cortesía de recepción'",
                ("@m", memberId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }

    [Fact]
    public void Cash_Session_Tracks_Cash_Sales_And_Closes_With_Difference()
    {
        if (!_fx.Available) return;

        var members = new MembersModule(_fx.Tools!);
        var inventory = new InventoryModule(_fx.Tools!);
        var sales = new SalesModule(_fx.Tools!);
        var cash = new CashModule(_fx.Tools!);
        CurrentUser.Id = 1;

        try
        {
            string email = $"cash_{Guid.NewGuid():N}@test.local";
            Assert.True(members.CreateMember(new Dictionary<string, object>
            {
                ["firstName"] = "Caja",
                ["lastName"] = "Test",
                ["email"] = email,
                ["membershipType"] = ""
            }, out var memberCreated));

            long memberId = Convert.ToInt64(memberCreated["memberId"]);

            Assert.True(inventory.CreateProduct(new Dictionary<string, object>
            {
                ["productName"] = $"Producto_{Guid.NewGuid():N}"[..20],
                ["price"] = 200m,
                ["stock"] = 5,
                ["minStock"] = 1
            }, out var productCreated), productCreated.GetValueOrDefault("message")?.ToString());

            long productId = Convert.ToInt64(productCreated["productId"]);

            Assert.True(cash.Open(100m, "Turno de prueba", out var opened), opened.GetValueOrDefault("message")?.ToString());
            long cashSessionId = Convert.ToInt64(opened["cashSessionId"]);

            Assert.False(cash.Open(50m, "", out _));

            Assert.True(sales.CreateSale(new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["paymentMethod"] = "cash",
                ["taxAmount"] = 0m,
                ["totalAmount"] = 200m,
                ["items"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["productId"] = productId,
                        ["quantity"] = 1,
                        ["price"] = 200m
                    }
                }
            }, out var saleCreated), saleCreated.GetValueOrDefault("message")?.ToString());

            Assert.True(cash.GetCurrent(out var current));
            var currentData = Assert.IsAssignableFrom<Dictionary<string, object>>(current["data"]);
            Assert.Equal(200m, Convert.ToDecimal(currentData["cashSales"]));
            Assert.Equal(300m, Convert.ToDecimal(currentData["expectedCash"]));

            Assert.True(cash.Close(cashSessionId, 290m, "Faltante", out var closed), closed.GetValueOrDefault("message")?.ToString());
            Assert.Equal(300m, Convert.ToDecimal(closed["expectedCash"]));
            Assert.Equal(-10m, Convert.ToDecimal(closed["difference"]));

            Assert.True(cash.GetCurrent(out var afterClose));
            Assert.Null(afterClose["data"]);

            Assert.True(cash.GetSessions(out var history));
            var historyRows = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(history["data"]);
            Assert.Contains(historyRows, row => Convert.ToInt64(row["id"]) == cashSessionId && Convert.ToString(row["status"]) == "closed");
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
