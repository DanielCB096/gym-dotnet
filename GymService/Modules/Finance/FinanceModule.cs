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

                var session = _tools.CreateSession(SubModule);
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
                var transactionType = Convert.ToString(transactionData.GetValueOrDefault("transactionType", "deposit")) ?? "deposit";

                var session = _tools.CreateSession(SubModule);
                session.InsertObject(new GYM_BANK_TRANSACTIONS
                {
                    ACCOUNT_ID = accountId,
                    TRANSACTION_TYPE = transactionType,
                    AMOUNT = amount,
                    DESCRIPTION = description,
                    TRANSACTION_DATE = DateTime.Now
                });

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Transacción registrada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al registrar la transacción";
                }

                return (bool)response["success"];
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
                var session = _tools.CreateSession(SubModule);
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
    }
}
