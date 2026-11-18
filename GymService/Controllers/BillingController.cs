using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class BillingController : ControllerBase
    {
        [HttpGet]
        [Route("api/billing/invoices")]
        public ActionResult<Dictionary<string, object>> GetInvoices([FromQuery] long? memberId = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Billing == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Billing.GetInvoices(out var response, memberId, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/billing/invoices")]
        public ActionResult<Dictionary<string, object>> CreateInvoice([FromBody] Dictionary<string, object> invoiceData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Billing == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Billing.CreateInvoice(invoiceData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/billing/invoices/{invoiceId}/status")]
        public ActionResult<Dictionary<string, object>> UpdateInvoiceStatus(long invoiceId, [FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Billing == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var status = Convert.ToString(request.GetValueOrDefault("status", "")) ?? "";
            var success = DataCenter.BusinessProcess.Billing.UpdateInvoiceStatus(invoiceId, status, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/billing/invoices/{invoiceId}")]
        public ActionResult<Dictionary<string, object>> DeleteInvoice(long invoiceId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Billing == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Billing.DeleteInvoice(invoiceId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
