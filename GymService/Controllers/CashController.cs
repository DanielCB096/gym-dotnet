using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class CashController : ControllerBase
    {
        [HttpGet]
        [Route("api/cash-sessions/current")]
        public ActionResult<Dictionary<string, object>> GetCurrent()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Cash == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Cash.GetCurrent(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/cash-sessions")]
        public ActionResult<Dictionary<string, object>> GetSessions([FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Cash == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Cash.GetSessions(out var response, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/cash-sessions/open")]
        public ActionResult<Dictionary<string, object>> Open([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Cash == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            decimal openingAmount = data.ContainsKey("openingAmount") ? Convert.ToDecimal(data["openingAmount"]) : 0m;
            string notes = Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "";

            var success = DataCenter.BusinessProcess.Cash.Open(openingAmount, notes, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/cash-sessions/{cashSessionId}/close")]
        public ActionResult<Dictionary<string, object>> Close(long cashSessionId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Cash == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            decimal closingAmount = data.ContainsKey("closingAmount") ? Convert.ToDecimal(data["closingAmount"]) : 0m;
            string notes = Convert.ToString(data.GetValueOrDefault("notes", "")) ?? "";

            var success = DataCenter.BusinessProcess.Cash.Close(cashSessionId, closingAmount, notes, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
