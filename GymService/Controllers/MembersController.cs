using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class MembersController : ControllerBase
    {
        [HttpGet]
        [Route("api/members")]
        public ActionResult<Dictionary<string, object>> GetMembers([FromQuery] long? id, [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int limit = 50)
        {
            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.GetMembers(out var response, id, query, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/members")]
        public ActionResult<Dictionary<string, object>> CreateMember([FromBody] Dictionary<string, object> memberData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.CreateMember(memberData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/memberships/{membershipId}/freeze")]
        public ActionResult<Dictionary<string, object>> FreezeMembership(long membershipId, [FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var freezeStart = DateTime.Parse(request.GetValueOrDefault("freezeStart", DateTime.Now.ToString("o")));
            var freezeEnd = DateTime.Parse(request.GetValueOrDefault("freezeEnd", DateTime.Now.AddMonths(1).ToString("o")));

            var success = DataCenter.BusinessProcess.Members.FreezeMembership(membershipId, freezeStart, freezeEnd, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/memberships/{membershipId}/resume")]
        public ActionResult<Dictionary<string, object>> ResumeMembership(long membershipId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.ResumeMembership(membershipId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/members/{memberId}/payments")]
        public ActionResult<Dictionary<string, object>> RecordPayment(long memberId, [FromBody] Dictionary<string, object> paymentData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var amount = decimal.Parse(Convert.ToString(paymentData.GetValueOrDefault("amount", "0")));
            var paymentMethod = Convert.ToString(paymentData.GetValueOrDefault("paymentMethod", "cash")) ?? "cash";
            var membershipId = paymentData.ContainsKey("membershipId") ? Convert.ToInt64(paymentData["membershipId"]) : (long?)null;
            var createdBy = SecurityValidation.GetUserId(userData!);

            var success = DataCenter.BusinessProcess.Members.RecordPayment(memberId, amount, paymentMethod, membershipId, createdBy, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
