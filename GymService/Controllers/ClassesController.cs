using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class ClassesController : ControllerBase
    {
        [HttpGet]
        [Route("api/classes")]
        public ActionResult<Dictionary<string, object>> GetClassTypes()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.GetClassTypes(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/classes")]
        public ActionResult<Dictionary<string, object>> CreateClassType([FromBody] Dictionary<string, object> classData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.CreateClassType(classData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/classes/{classTypeId}")]
        public ActionResult<Dictionary<string, object>> UpdateClassType(long classTypeId, [FromBody] Dictionary<string, object> classData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.UpdateClassType(classTypeId, classData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/classes/{classTypeId}")]
        public ActionResult<Dictionary<string, object>> DeleteClassType(long classTypeId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.DeleteClassType(classTypeId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Sesiones ============================

        [HttpGet]
        [Route("api/classes/sessions")]
        public ActionResult<Dictionary<string, object>> GetClassSessions(
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] long? classTypeId = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? fromDate = DateTime.TryParse(from, out var parsedFrom) ? parsedFrom : null;
            DateTime? toDate = DateTime.TryParse(to, out var parsedTo) ? parsedTo : null;

            var success = DataCenter.BusinessProcess.Classes.GetClassSessions(out var response, fromDate, toDate, classTypeId, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/classes/sessions")]
        public ActionResult<Dictionary<string, object>> CreateClassSession([FromBody] Dictionary<string, object> sessionData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.CreateClassSession(sessionData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/classes/sessions/{sessionId}")]
        public ActionResult<Dictionary<string, object>> UpdateClassSession(long sessionId, [FromBody] Dictionary<string, object> sessionData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.UpdateClassSession(sessionId, sessionData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/classes/sessions/{sessionId}")]
        public ActionResult<Dictionary<string, object>> DeleteClassSession(long sessionId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.DeleteClassSession(sessionId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/classes/sessions/{sessionId}/cancel")]
        public ActionResult<Dictionary<string, object>> CancelClassSession(long sessionId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.CancelClassSession(sessionId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Reservas ============================

        [HttpGet]
        [Route("api/classes/sessions/{sessionId}/reservations")]
        public ActionResult<Dictionary<string, object>> GetSessionReservations(long sessionId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.GetSessionReservations(sessionId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/classes/sessions/{sessionId}/reservations")]
        public ActionResult<Dictionary<string, object>> CreateReservation(long sessionId, [FromBody] Dictionary<string, object> reservationData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            long memberId = reservationData.ContainsKey("memberId") ? Convert.ToInt64(reservationData["memberId"]) : 0L;

            var success = DataCenter.BusinessProcess.Classes.ReserveClass(sessionId, memberId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/classes/reservations/{reservationId}")]
        public ActionResult<Dictionary<string, object>> CancelReservation(long reservationId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Classes.CancelReservation(reservationId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/classes/reservations/{reservationId}/attendance")]
        public ActionResult<Dictionary<string, object>> SetReservationAttendance(long reservationId, [FromBody] Dictionary<string, object> attendanceData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Classes == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            bool attended = attendanceData.ContainsKey("attended") && Convert.ToBoolean(attendanceData["attended"]);

            var success = DataCenter.BusinessProcess.Classes.SetReservationAttendance(reservationId, attended, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
