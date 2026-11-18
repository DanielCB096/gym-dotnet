using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class StaffController : ControllerBase
    {
        [HttpGet]
        [Route("api/staff")]
        public ActionResult<Dictionary<string, object>> GetStaff([FromQuery] string? query = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetStaff(out var response, query, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/staff/users")]
        public ActionResult<Dictionary<string, object>> GetAvailableUsers()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetAvailableUsers(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/staff/push-subscriptions")]
        public ActionResult<Dictionary<string, object>> GetPushSubscriptions([FromQuery] long? userId = null, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetPushSubscriptions(out var response, userId, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff")]
        public ActionResult<Dictionary<string, object>> CreateStaff([FromBody] Dictionary<string, object> staffData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.CreateStaff(staffData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/staff/{staffId}")]
        public ActionResult<Dictionary<string, object>> UpdateStaff(long staffId, [FromBody] Dictionary<string, object> staffData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.UpdateStaff(staffId, staffData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/staff/{staffId}")]
        public ActionResult<Dictionary<string, object>> DeleteStaff(long staffId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.DeleteStaff(staffId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
