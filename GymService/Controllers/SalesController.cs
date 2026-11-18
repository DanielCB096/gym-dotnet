using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class SalesController : ControllerBase
    {
        [HttpPost]
        [Route("api/sales")]
        public ActionResult<Dictionary<string, object>> CreateSale([FromBody] Dictionary<string, object> saleData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Sales == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            // Enrich saleData with cashierId from authenticated user
            if (!saleData.ContainsKey("cashierId"))
                saleData["cashierId"] = SecurityValidation.GetUserId(userData!);

            var success = DataCenter.BusinessProcess.Sales.CreateSale(saleData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/sales")]
        public ActionResult<Dictionary<string, object>> GetSales([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Sales == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? start = null, end = null;
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startDt))
                start = startDt;
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endDt))
                end = endDt;

            var success = DataCenter.BusinessProcess.Sales.GetSales(out var response, start, end, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/sales/{saleId}")]
        public ActionResult<Dictionary<string, object>> DeleteSale(long saleId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Sales == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Sales.DeleteSale(saleId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
