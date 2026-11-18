using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class RoutinesController : ControllerBase
    {
        [HttpGet]
        [Route("api/routines")]
        public ActionResult<Dictionary<string, object>> GetRoutinePlans()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.GetRoutinePlans(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/routines")]
        public ActionResult<Dictionary<string, object>> CreateRoutinePlan([FromBody] Dictionary<string, object> routineData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.CreateRoutinePlan(routineData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/routines/{routineId}")]
        public ActionResult<Dictionary<string, object>> UpdateRoutinePlan(long routineId, [FromBody] Dictionary<string, object> routineData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.UpdateRoutinePlan(routineId, routineData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/routines/{routineId}")]
        public ActionResult<Dictionary<string, object>> DeleteRoutinePlan(long routineId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Routines == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Routines.DeleteRoutinePlan(routineId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
