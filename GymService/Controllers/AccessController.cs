using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class AccessController : ControllerBase
    {
        [HttpPost]
        [Route("api/access/code/generate")]
        public ActionResult<Dictionary<string, object>> GenerateAccessCode([FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Access == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var memberId = long.Parse(request.GetValueOrDefault("memberId", "0"));

            var success = DataCenter.BusinessProcess.Access.GenerateAccessCode(memberId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/access/code/validate")]
        [EnableRateLimiting("strict")]
        public ActionResult<Dictionary<string, object>> ValidateAccessCode([FromBody] Dictionary<string, string> request)
        {
            if (DataCenter.BusinessProcess.Access == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var code = request.GetValueOrDefault("code", "");
            var deviceId = request.GetValueOrDefault("deviceId", "unknown");

            var success = DataCenter.BusinessProcess.Access.ValidateAccessCode(code, deviceId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/access/manual")]
        public ActionResult<Dictionary<string, object>> RegisterManualAccess([FromBody] Dictionary<string, object> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Access == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            long memberId = request.ContainsKey("memberId") ? Convert.ToInt64(request["memberId"]) : 0L;
            string reason = Convert.ToString(request.GetValueOrDefault("reason", "")) ?? "";
            string deviceId = Convert.ToString(request.GetValueOrDefault("deviceId", "reception")) ?? "reception";

            var success = DataCenter.BusinessProcess.Access.RegisterManualAccess(memberId, reason, deviceId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/access/logs")]
        public ActionResult<Dictionary<string, object>> GetAccessLogs([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Access == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? start = null, end = null;
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var startDt))
                start = startDt;
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var endDt))
                end = endDt;

            var success = DataCenter.BusinessProcess.Access.GetAccessLogs(out var response, start, end, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/access/clean/{daysOld}")]
        public ActionResult<Dictionary<string, object>> CleanOldAccessCodes(int daysOld)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Access == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Access.CleanOldAccessCodes(daysOld, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
