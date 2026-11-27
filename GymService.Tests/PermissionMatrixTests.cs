using GymService.Services;
using Xunit;

namespace GymService.Tests;

public class PermissionMatrixTests
{
    [Theory]
    [InlineData("/api/members", "GET", "members.view")]
    [InlineData("/api/members/1/payments", "POST", "members.create")]
    [InlineData("/api/members/plans", "GET", "members.view")]
    [InlineData("/api/members/collections", "GET", "members.view")]
    [InlineData("/api/memberships/1/freeze", "POST", "members.edit")]
    [InlineData("/api/access/code/generate", "POST", "access.view")]
    [InlineData("/api/access/logs", "GET", "access.view")]
    [InlineData("/api/access/clean/30", "DELETE", "access.override")]
    [InlineData("/api/sales", "POST", "sales.create")]
    [InlineData("/api/sales/10/return", "POST", "sales.return")]
    [InlineData("/api/inventory/5/stock", "PUT", "inventory.edit")]
    [InlineData("/api/classes/sessions/5/reservations", "GET", "classes.view")]
    [InlineData("/api/billing/invoices", "POST", "billing.create")]
    [InlineData("/api/billing/invoices/5/status", "PUT", "billing.create")]
    [InlineData("/api/finance/accounts", "GET", "finance.view")]
    [InlineData("/api/finance/transactions", "POST", "finance.reconcile")]
    [InlineData("/api/reports/sales/excel", "GET", "reports.view")]
    [InlineData("/api/kiosk/devices", "GET", "admin.settings")]
    [InlineData("/api/kiosk/devices/9/rotate", "POST", "admin.settings")]
    [InlineData("/api/import/members/csv", "POST", "members.create")]
    [InlineData("/api/import/inventory/csv", "POST", "inventory.edit")]
    [InlineData("/api/import/templates/members/csv", "GET", "members.view")]
    [InlineData("/api/staff/users", "POST", "staff.manage")]
    [InlineData("/api/staff/users/all", "GET", "staff.view")]
    public void Resolves_Subroutes_To_The_Expected_Permission(string path, string method, string expected)
    {
        Assert.Equal(expected, PermissionMatrix.GetRequiredPermission(path, method));
    }

    [Fact]
    public void Unknown_Routes_Do_Not_Require_A_Permission()
    {
        Assert.Null(PermissionMatrix.GetRequiredPermission("/api/desconocido", "GET"));
    }

    [Fact]
    public void Staff_Role_Has_Operational_Permissions_But_Not_Financial_Ones()
    {
        var staff = PermissionMatrix.RolePermissions["STAFF"];

        Assert.Contains("members.view", staff);
        Assert.Contains("classes.manage", staff);
        Assert.DoesNotContain("finance.view", staff);
        Assert.DoesNotContain("billing.view", staff);
        Assert.DoesNotContain("staff.view", staff);
    }

    [Fact]
    public void Cashier_Role_Can_Sell_But_Not_Manage_Staff()
    {
        var cashier = PermissionMatrix.RolePermissions["CASHIER"];

        Assert.Contains("sales.create", cashier);
        Assert.Contains("billing.view", cashier);
        Assert.DoesNotContain("staff.view", cashier);
        Assert.DoesNotContain("finance.view", cashier);
    }
}
