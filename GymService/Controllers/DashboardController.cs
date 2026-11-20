using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class DashboardController : ControllerBase
    {
        [HttpGet]
        [Route("api/dashboard/summary")]
        public ActionResult<Dictionary<string, object>> GetSummary()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Dashboard == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Dashboard.GetSummary(out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
