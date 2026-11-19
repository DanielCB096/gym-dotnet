using GymService.Modules.Finance;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class FinanceFlowTests
{
    private readonly DatabaseFixture _fx;

    public FinanceFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Account_And_Transaction_Are_Listed_And_Filtered()
    {
        if (!_fx.Available) return;

        var finance = new FinanceModule(_fx.Tools!);
        string accountNumber = $"ACC{Guid.NewGuid():N}"[..20];

        CurrentUser.Id = 1;

        try
        {
            Assert.True(finance.CreateBankAccount(new Dictionary<string, object>
            {
                ["bankName"] = "Banco Pruebas",
                ["accountNumber"] = accountNumber,
                ["balance"] = 1000m,
                ["currency"] = "MXN"
            }, out var accountResponse), accountResponse.GetValueOrDefault("message")?.ToString());

            long accountId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_BANK_ACCOUNTS WHERE ACCOUNT_NUMBER = @n", ("@n", accountNumber)));

            Assert.True(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["amount"] = 250m,
                ["transactionType"] = "deposit",
                ["description"] = "Deposito de prueba"
            }, out var transactionResponse), transactionResponse.GetValueOrDefault("message")?.ToString());

            Assert.True(finance.GetTransactions(out var all));
            var allData = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(all["data"]);
            Assert.Contains(allData, row => Convert.ToInt64(row["accountId"]) == accountId);

            Assert.True(finance.GetTransactions(out var filtered, accountId));
            var filteredData = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(filtered["data"]);
            Assert.Single(filteredData);
            Assert.Equal("Banco Pruebas", filteredData[0]["bankName"]);
            Assert.Equal("deposit", filteredData[0]["transactionType"]);
            Assert.Equal(250m, Convert.ToDecimal(filteredData[0]["amount"]));

            // El saldo se actualiza con cada movimiento.
            Assert.True(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["amount"] = 100m,
                ["transactionType"] = "withdrawal",
                ["description"] = "Retiro de prueba"
            }, out _));

            // No se permite sobregirar la cuenta.
            Assert.False(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["amount"] = 999999m,
                ["transactionType"] = "withdrawal"
            }, out var overdraft));
            Assert.Contains("Saldo insuficiente", overdraft.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.False(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["amount"] = -10m
            }, out _));

            Assert.False(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = 999999,
                ["amount"] = 10m
            }, out _));

            Assert.True(finance.GetBankAccounts(out var accounts));
            var accountData = Assert.IsAssignableFrom<List<Dictionary<string, object>>>(accounts["data"]);
            var account = accountData.Single(row => Convert.ToInt64(row["id"]) == accountId);
            Assert.Equal(1150m, Convert.ToDecimal(account["balance"]));
            Assert.Equal("MXN", account["currency"]);

            // Una cuenta con movimientos no se elimina.
            Assert.False(finance.DeleteBankAccount(accountId, out var deleteAccount));
            Assert.Contains("movimientos", deleteAccount.GetValueOrDefault("message")?.ToString() ?? "");

            Assert.Equal(1L, Convert.ToInt64(_fx.Scalar("SELECT CREATED_BY FROM GYM_BANK_TRANSACTIONS WHERE ACCOUNT_ID = @id", ("@id", accountId))));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}

