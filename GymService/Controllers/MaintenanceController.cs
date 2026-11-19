using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        // ============================ Equipos ============================

        [HttpGet]
        [Route("api/maintenance/equipment")]
        public ActionResult<Dictionary<string, object>> GetEquipment()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.GetEquipment(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/maintenance/equipment")]
        public ActionResult<Dictionary<string, object>> CreateEquipment([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.CreateEquipment(data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/maintenance/equipment/{equipmentId}")]
        public ActionResult<Dictionary<string, object>> UpdateEquipment(long equipmentId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.UpdateEquipment(equipmentId, data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/maintenance/equipment/{equipmentId}")]
        public ActionResult<Dictionary<string, object>> DeleteEquipment(long equipmentId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.DeleteEquipment(equipmentId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ============================ Tickets ============================

        [HttpGet]
        [Route("api/maintenance/tickets")]
        public ActionResult<Dictionary<string, object>> GetTickets(
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] long? equipmentId = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.GetTickets(
                out var response, status, priority, equipmentId, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/maintenance/tickets")]
        public ActionResult<Dictionary<string, object>> CreateTicket([FromBody] Dictionary<string, object> ticketData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.CreateTicket(ticketData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/maintenance/tickets/{ticketId}/status")]
        public ActionResult<Dictionary<string, object>> UpdateTicketStatus(long ticketId, [FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var status = Convert.ToString(request.GetValueOrDefault("status", "")) ?? "";
            var success = DataCenter.BusinessProcess.Maintenance.UpdateTicketStatus(ticketId, status, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/maintenance/tickets/{ticketId}")]
        public ActionResult<Dictionary<string, object>> DeleteTicket(long ticketId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Maintenance == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Maintenance.DeleteTicket(ticketId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
