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
