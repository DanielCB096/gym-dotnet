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
        public ActionResult<Dictionary<string, object>> GetStaff(
            [FromQuery] string? query = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] bool includeInactive = false)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetStaff(out var response, query, page, limit, includeInactive);
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
        [Route("api/staff/users/all")]
        public ActionResult<Dictionary<string, object>> GetSystemUsers()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetSystemUsers(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/users")]
        public ActionResult<Dictionary<string, object>> CreateUser([FromBody] Dictionary<string, object> userData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userDataAuth, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.CreateUser(userData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/staff/users/{userId}")]
        public ActionResult<Dictionary<string, object>> UpdateUser(long userId, [FromBody] Dictionary<string, object> userData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userDataAuth, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.UpdateUser(
                userId,
                SecurityValidation.GetUserId(userDataAuth!),
                SecurityValidation.GetUserRole(userDataAuth!),
                userData,
                out var response);

            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/users/{userId}/password")]
        public ActionResult<Dictionary<string, object>> ResetUserPassword(long userId, [FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userDataAuth, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var newPassword = request.GetValueOrDefault("newPassword", "");
            var success = DataCenter.BusinessProcess.Identity.ResetUserPassword(
                userId,
                SecurityValidation.GetUserId(userDataAuth!),
                SecurityValidation.GetUserRole(userDataAuth!),
                newPassword,
                out var response);

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

        // ========================= Turnos y asistencia =======================

        [HttpGet]
        [Route("api/staff/shifts")]
        public ActionResult<Dictionary<string, object>> GetShifts(
            [FromQuery] long? staffId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetShifts(out var response, staffId, from, to, status, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/staff/shifts/summary")]
        public ActionResult<Dictionary<string, object>> GetShiftSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.GetShiftSummary(out var response, from, to);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/shifts")]
        public ActionResult<Dictionary<string, object>> CreateShift([FromBody] Dictionary<string, object> shiftData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.CreateShift(shiftData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/staff/shifts/{shiftId}")]
        public ActionResult<Dictionary<string, object>> UpdateShift(long shiftId, [FromBody] Dictionary<string, object> shiftData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.UpdateShift(shiftId, shiftData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/shifts/{shiftId}/check-in")]
        public ActionResult<Dictionary<string, object>> CheckInShift(long shiftId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.CheckInShift(shiftId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/shifts/{shiftId}/check-out")]
        public ActionResult<Dictionary<string, object>> CheckOutShift(long shiftId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.CheckOutShift(shiftId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/shifts/{shiftId}/absent")]
        public ActionResult<Dictionary<string, object>> MarkShiftAbsent(long shiftId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.MarkShiftAbsent(shiftId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/staff/shifts/{shiftId}/cancel")]
        public ActionResult<Dictionary<string, object>> CancelShift(long shiftId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Staff == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Staff.CancelShift(shiftId, out var response);
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
