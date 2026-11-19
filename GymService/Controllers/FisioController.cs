using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class FisioController : ControllerBase
    {
        // ============================ Paquetes ============================

        [HttpGet]
        [Route("api/fisio/packages")]
        public ActionResult<Dictionary<string, object>> GetPackages(
            [FromQuery] long? memberId = null,
            [FromQuery] bool includeInactive = false)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.GetPackages(out var response, memberId, includeInactive);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/fisio/packages")]
        public ActionResult<Dictionary<string, object>> CreatePackage([FromBody] Dictionary<string, object> packageData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.CreatePackage(packageData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/fisio/packages/{packageId}")]
        public ActionResult<Dictionary<string, object>> UpdatePackage(long packageId, [FromBody] Dictionary<string, object> packageData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.UpdatePackage(packageId, packageData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/fisio/packages/{packageId}")]
        public ActionResult<Dictionary<string, object>> DeletePackage(long packageId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.DeletePackage(packageId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Citas ============================

        [HttpGet]
        [Route("api/fisio/appointments")]
        public ActionResult<Dictionary<string, object>> GetAppointments(
            [FromQuery] long? memberId = null,
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            DateTime? fromDate = DateTime.TryParse(from, out var parsedFrom) ? parsedFrom : null;
            DateTime? toDate = DateTime.TryParse(to, out var parsedTo) ? parsedTo : null;

            var success = DataCenter.BusinessProcess.Fisio.GetAppointments(
                out var response, memberId, fromDate, toDate, status, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/fisio/appointments")]
        public ActionResult<Dictionary<string, object>> CreateAppointment([FromBody] Dictionary<string, object> appointmentData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.CreateAppointment(appointmentData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/fisio/appointments/{appointmentId}")]
        public ActionResult<Dictionary<string, object>> UpdateAppointment(long appointmentId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.UpdateAppointment(appointmentId, data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/fisio/appointments/{appointmentId}/status")]
        public ActionResult<Dictionary<string, object>> SetAppointmentStatus(long appointmentId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            string status = Convert.ToString(data.GetValueOrDefault("status", "")) ?? "";
            var success = DataCenter.BusinessProcess.Fisio.SetAppointmentStatus(appointmentId, status, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/fisio/appointments/{appointmentId}")]
        public ActionResult<Dictionary<string, object>> DeleteAppointment(long appointmentId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Fisio == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Fisio.DeleteAppointment(appointmentId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
