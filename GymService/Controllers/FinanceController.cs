using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class FinanceController : ControllerBase
    {
        [HttpGet]
        [Route("api/finance/accounts")]
        public ActionResult<Dictionary<string, object>> GetBankAccounts()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.GetBankAccounts(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/accounts")]
        public ActionResult<Dictionary<string, object>> CreateBankAccount([FromBody] Dictionary<string, object> accountData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.CreateBankAccount(accountData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/transactions")]
        public ActionResult<Dictionary<string, object>> CreateTransaction([FromBody] Dictionary<string, object> transactionData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.CreateTransaction(transactionData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/finance/transactions")]
        public ActionResult<Dictionary<string, object>> GetTransactions(
            [FromQuery] long? accountId = null,
            [FromQuery] bool? reconciled = null,
            [FromQuery] bool? flagged = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.GetTransactions(out var response, accountId, reconciled, flagged, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/transactions/{transactionId}/reconcile")]
        public ActionResult<Dictionary<string, object>> ReconcileTransaction(long transactionId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            string withType = Convert.ToString(data.GetValueOrDefault("withType", "")) ?? "";
            long? withId = data.ContainsKey("withId") && Convert.ToString(data["withId"]) != ""
                ? Convert.ToInt64(data["withId"])
                : (long?)null;
            string notes = Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "";

            var success = DataCenter.BusinessProcess.Finance.ReconcileTransaction(transactionId, withType, withId, notes, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/transactions/{transactionId}/unreconcile")]
        public ActionResult<Dictionary<string, object>> UnreconcileTransaction(long transactionId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.UnreconcileTransaction(transactionId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/transactions/{transactionId}/flag")]
        public ActionResult<Dictionary<string, object>> FlagTransaction(long transactionId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            bool flagged = !data.ContainsKey("flagged") || Convert.ToBoolean(data["flagged"]);
            string notes = Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "";

            var success = DataCenter.BusinessProcess.Finance.FlagTransaction(transactionId, flagged, notes, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/finance/accounts/{accountId}/movements/import")]
        public ActionResult<Dictionary<string, object>> ImportMovements(long accountId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var movements = new List<Dictionary<string, object>>();
            if (data.TryGetValue("movements", out var raw) && raw is IEnumerable<object?> rows)
            {
                foreach (var row in rows)
                {
                    if (row is Dictionary<string, object?> movement)
                    {
                        movements.Add(movement.ToDictionary(item => item.Key, item => item.Value ?? ""));
                    }
                }
            }

            var success = DataCenter.BusinessProcess.Finance.ImportMovements(accountId, movements, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/finance/reconciliation")]
        public ActionResult<Dictionary<string, object>> GetReconciliationSummary([FromQuery] long? accountId = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.GetReconciliationSummary(out var response, accountId);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/finance/accounts/{accountId}")]
        public ActionResult<Dictionary<string, object>> DeleteBankAccount(long accountId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Finance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Finance.DeleteBankAccount(accountId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
