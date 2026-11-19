using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Finance
{
    /// <summary>
    /// Finance module: cuentas bancarias y transacciones financieras.
    /// </summary>
    public class FinanceModule
    {
        private const string SubModule = "FINANCE_MODULE";
        private DatabaseTools _tools;

        public FinanceModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        private int GetColumnIndex(ResultSet result, string columnName)
        {
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private object GetRowValue(Row row, ResultSet result, string columnName)
        {
            int idx = GetColumnIndex(result, columnName);
            if (idx < 0) return null;
            return row.GetObject(idx);
        }

        /// <summary>
        /// Create a new bank account
        /// </summary>
        public bool CreateBankAccount(Dictionary<string, object> accountData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var bankName = Convert.ToString(accountData.GetValueOrDefault("bankName", "")) ?? "";
                var accountNumber = Convert.ToString(accountData.GetValueOrDefault("accountNumber", "")) ?? "";
                var balance = accountData.ContainsKey("balance") ? Convert.ToDecimal(accountData["balance"]) : 0m;
                var currency = Convert.ToString(accountData.GetValueOrDefault("currency", "MXN")) ?? "MXN";

                if (string.IsNullOrEmpty(bankName) || string.IsNullOrEmpty(accountNumber))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del banco y número de cuenta son requeridos";
                    return false;
                }

                if (balance < 0)
                {
                    response["success"] = false;
                    response["message"] = "El saldo inicial no puede ser negativo";
                    return false;
                }

                long duplicateAccount = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_BANK_ACCOUNTS WHERE ACCOUNT_NUMBER = ?", accountNumber);

                if (duplicateAccount > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe una cuenta con ese número";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(new GYM_BANK_ACCOUNTS
                {
                    BANK_NAME = bankName,
                    ACCOUNT_NUMBER = accountNumber,
                    BALANCE = balance,
                    CURRENCY = currency,
                    IS_ACTIVE = true
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Cuenta bancaria creada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la cuenta bancaria";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateBankAccount: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Create a new bank transaction
        /// </summary>
        public bool CreateTransaction(Dictionary<string, object> transactionData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var accountId = transactionData.ContainsKey("accountId") ? Convert.ToInt64(transactionData["accountId"]) : 0L;
                var amount = transactionData.ContainsKey("amount") ? Convert.ToDecimal(transactionData["amount"]) : 0m;
                var description = Convert.ToString(transactionData.GetValueOrDefault("description", "")) ?? "";
                var transactionType = (Convert.ToString(transactionData.GetValueOrDefault("transactionType", "deposit")) ?? "deposit").Trim().ToLowerInvariant();

                var account = GetAccountState(accountId);
                if (account == null)
                {
                    response["success"] = false;
                    response["message"] = "La cuenta bancaria no existe";
                    return false;
                }

                if (amount <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El monto debe ser mayor a cero";
                    return false;
                }

                if (transactionType != "deposit" && transactionType != "withdrawal")
                {
                    response["success"] = false;
                    response["message"] = "Tipo de movimiento no válido";
                    return false;
                }

                decimal newBalance = transactionType == "deposit"
                    ? account.Value.Balance + amount
                    : account.Value.Balance - amount;

                if (newBalance < 0)
                {
                    response["success"] = false;
                    response["message"] = "Saldo insuficiente para el retiro";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(new GYM_BANK_TRANSACTIONS
                {
                    ACCOUNT_ID = accountId,
                    TRANSACTION_TYPE = transactionType,
                    AMOUNT = amount,
                    DESCRIPTION = description.Trim(),
                    TRANSACTION_DATE = DateTime.Now
                });

                session.UpdateObjectChanges(new GYM_BANK_ACCOUNTS
                {
                    ID = accountId,
                    BALANCE = newBalance
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la transacción";
                    return false;
                }

                response["success"] = true;
                response["balance"] = newBalance;
                response["message"] = "Transacción registrada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateTransaction: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a bank account
        /// </summary>
        public bool DeleteBankAccount(long accountId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (GetAccountState(accountId) == null)
                {
                    response["success"] = false;
                    response["message"] = "La cuenta bancaria no existe";
                    return false;
                }

                long transactions = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_BANK_TRANSACTIONS WHERE ACCOUNT_ID = ?", accountId);

                if (transactions > 0)
                {
                    response["success"] = false;
                    response["message"] = "La cuenta tiene movimientos; no se puede eliminar para conservar el historial";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_BANK_ACCOUNTS { ID = accountId });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Cuenta bancaria eliminada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la cuenta bancaria";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteBankAccount: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active bank accounts
        /// </summary>
        public bool GetBankAccounts(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_bank_accounts WHERE is_active = 1 ORDER BY bank_name"
                );

                var accounts = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        accounts.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["bankName"] = Convert.ToString(GetRowValue(row, result, "bank_name")) ?? "",
                            ["accountNumber"] = Convert.ToString(GetRowValue(row, result, "account_number")) ?? "",
                            ["balance"] = Convert.ToDecimal(GetRowValue(row, result, "balance")),
                            ["currency"] = Convert.ToString(GetRowValue(row, result, "currency")) ?? "MXN"
                        });
                    }
                }

                response["success"] = true;
                response["data"] = accounts;
                response["count"] = accounts.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetBankAccounts: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
        /// <summary>
        /// Get bank transactions, optionally filtered by account.
        /// </summary>
        public bool GetTransactions(out Dictionary<string, object> response, long? accountId = null, int page = 1, int limit = 50)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 50;
                int offset = (page - 1) * limit;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (accountId.HasValue)
                {
                    whereParts.Add("t.account_id = ?");
                    parameters.Add(accountId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM gym_bank_transactions t {whereClause}",
                    parameters.ToArray()
                );

                var sql = "SELECT t.*, a.bank_name FROM gym_bank_transactions t " +
                          "LEFT JOIN gym_bank_accounts a ON a.id = t.account_id " +
                          $"{whereClause} ORDER BY t.transaction_date DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                var queryParameters = new List<object>(parameters) { offset, limit };
                var result = _tools.ExecuteQueryResultSet(sql, queryParameters.ToArray());

                var transactions = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        transactions.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["accountId"] = Convert.ToInt64(GetRowValue(row, result, "account_id")),
                            ["bankName"] = Convert.ToString(GetRowValue(row, result, "bank_name")) ?? "",
                            ["transactionType"] = Convert.ToString(GetRowValue(row, result, "transaction_type")) ?? "",
                            ["amount"] = Convert.ToDecimal(GetRowValue(row, result, "amount")),
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["transactionDate"] = GetRowValue(row, result, "transaction_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "transaction_date")) : (DateTime?)null,
                            ["reconciled"] = Convert.ToBoolean(GetRowValue(row, result, "reconciled"))
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = transactions;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = totalCount,
                    ["totalPages"] = totalPages,
                    ["hasNext"] = page < totalPages,
                    ["hasPrev"] = page > 1
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetTransactions: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        private (decimal Balance, string Currency, bool IsActive)? GetAccountState(long accountId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT BALANCE, CURRENCY, IS_ACTIVE FROM GYM_BANK_ACCOUNTS WHERE ID = ?", accountId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToDecimal(GetRowValue(row, result, "balance")),
                Convert.ToString(GetRowValue(row, result, "currency")) ?? "MXN",
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }
    }
}
