using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class AuditController : ControllerBase
    {
        [HttpGet]
        [Route("api/audit/summary")]
        public ActionResult<Dictionary<string, object>> GetSummary()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Audit == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Audit.GetSummary(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/audit/sessions")]
        public ActionResult<Dictionary<string, object>> GetSessions(
            [FromQuery] string? query = null,
            [FromQuery] string? state = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Audit == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Audit.GetSessions(query, state, page, limit, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/audit/access")]
        public ActionResult<Dictionary<string, object>> GetAccessLogs(
            [FromQuery] string? query = null,
            [FromQuery] string? result = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Audit == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? start = null, end = null;
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startDt))
                start = startDt;
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endDt))
                end = endDt;

            var success = DataCenter.BusinessProcess.Audit.GetAccessLogs(query, result, start, end, page, limit, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
