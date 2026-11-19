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
    public void Reconciliation_And_Imported_Movements_Work()
    {
        if (!_fx.Available) return;

        var finance = new FinanceModule(_fx.Tools!);
        string accountNumber = $"REC{Guid.NewGuid():N}"[..20];
        CurrentUser.Id = 1;

        try
        {
            Assert.True(finance.CreateBankAccount(new Dictionary<string, object>
            {
                ["bankName"] = "Banco Conciliación",
                ["accountNumber"] = accountNumber,
                ["balance"] = 1000m,
                ["currency"] = "MXN"
            }, out var accountCreated), accountCreated.GetValueOrDefault("message")?.ToString());

            long accountId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_BANK_ACCOUNTS WHERE ACCOUNT_NUMBER = @n", ("@n", accountNumber)));

            Assert.True(finance.CreateTransaction(new Dictionary<string, object>
            {
                ["accountId"] = accountId,
                ["amount"] = 250m,
                ["transactionType"] = "deposit",
                ["description"] = "Depósito a conciliar"
            }, out _));

            long transactionId = Convert.ToInt64(_fx.Scalar("SELECT TOP 1 ID FROM GYM_BANK_TRANSACTIONS WHERE ACCOUNT_ID = @a ORDER BY ID DESC", ("@a", accountId)));

            Assert.True(finance.ReconcileTransaction(transactionId, "manual", null, "Conciliado en prueba", out var reconciled), reconciled.GetValueOrDefault("message")?.ToString());
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT RECONCILED FROM GYM_BANK_TRANSACTIONS WHERE ID = @id", ("@id", transactionId))));
            Assert.Equal("manual", Convert.ToString(_fx.Scalar("SELECT RECONCILED_WITH_TYPE FROM GYM_BANK_TRANSACTIONS WHERE ID = @id", ("@id", transactionId))));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT RECONCILED_BY FROM GYM_BANK_TRANSACTIONS WHERE ID = @id", ("@id", transactionId))));

            Assert.True(finance.GetReconciliationSummary(out var summary, accountId));
            var summaryData = Assert.IsAssignableFrom<Dictionary<string, object>>(summary["data"]);
            Assert.Equal(1, Convert.ToInt32(summaryData["reconciledCount"]));
            Assert.Equal(0, Convert.ToInt32(summaryData["pendingCount"]));

            Assert.True(finance.UnreconcileTransaction(transactionId, out _));
            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT RECONCILED FROM GYM_BANK_TRANSACTIONS WHERE ID = @id", ("@id", transactionId))));

            Assert.True(finance.FlagTransaction(transactionId, true, "Revisar", out _));
            Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT FLAGGED FROM GYM_BANK_TRANSACTIONS WHERE ID = @id", ("@id", transactionId))));
            Assert.True(finance.FlagTransaction(transactionId, false, "", out _));

            // Importar estado de cuenta: no altera el saldo interno.
            Assert.True(finance.ImportMovements(accountId, new List<Dictionary<string, object>>
            {
                new() { ["transactionType"] = "deposit", ["amount"] = 100m, ["description"] = "Importado 1", ["transactionDate"] = DateTime.Now.ToString("o") },
                new() { ["transactionType"] = "withdrawal", ["amount"] = 50m, ["description"] = "Importado 2", ["transactionDate"] = DateTime.Now.ToString("o") }
            }, out var imported), imported.GetValueOrDefault("message")?.ToString());

            Assert.Equal(2, Convert.ToInt32(imported["imported"]));
            Assert.Equal(1250m, Convert.ToDecimal(_fx.Scalar("SELECT BALANCE FROM GYM_BANK_ACCOUNTS WHERE ID = @id", ("@id", accountId))));
            Assert.Equal(2, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_BANK_TRANSACTIONS WHERE ACCOUNT_ID = @a AND SOURCE = 'import'", ("@a", accountId))));

            Assert.True(finance.GetTransactions(out var pending, accountId, false));
            var pendingPagination = Assert.IsAssignableFrom<Dictionary<string, object>>(pending["pagination"]);
            Assert.Equal(3, Convert.ToInt32(pendingPagination["totalCount"]));
        }
        finally
        {
            CurrentUser.Id = 0;
        }
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

