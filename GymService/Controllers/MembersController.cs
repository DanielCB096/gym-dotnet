using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
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
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

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

            // El módulo usa createdBy para la auditoría (CREATED_BY) del socio.
            if (!memberData.ContainsKey("createdBy"))
                memberData["createdBy"] = SecurityValidation.GetUserId(userData!);

            var success = DataCenter.BusinessProcess.Members.CreateMember(memberData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/members/photo")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public ActionResult<Dictionary<string, object>> UploadMemberPhoto()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { success = false, message = "Selecciona una imagen" });

            long memberId;
            try
            {
                memberId = Convert.ToInt64(Request.Form["memberId"].ToString());
            }
            catch
            {
                memberId = 0;
            }

            if (memberId <= 0)
                return BadRequest(new { success = false, message = "memberId es requerido" });

            var file = Request.Form.Files[0];
            if (file.Length <= 0)
                return BadRequest(new { success = false, message = "El archivo está vacío" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { success = false, message = "La imagen no puede superar 5 MB" });

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".jpeg") extension = ".jpg";
            if (extension != ".jpg" && extension != ".png" && extension != ".webp")
                return BadRequest(new { success = false, message = "Usa una imagen JPG, PNG o WebP" });

            string uploadDir = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "members");
            Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString("N") + extension;
            string filePath = Path.Combine(uploadDir, fileName);

            try
            {
                using (var input = file.OpenReadStream())
                {
                    var header = new byte[12];
                    int read = input.Read(header, 0, header.Length);
                    if (!HasValidImageSignature(header, read, extension))
                        return BadRequest(new { success = false, message = "El archivo no parece una imagen válida" });
                }

                using (var output = System.IO.File.Create(filePath))
                {
                    file.CopyTo(output);
                }

                string photoUrl = "/uploads/members/" + fileName;
                var success = DataCenter.BusinessProcess.Members.SetMemberPhoto(memberId, photoUrl, out var response);
                if (!success)
                {
                    System.IO.File.Delete(filePath);
                    return BadRequest(response);
                }

                var oldPhoto = Convert.ToString(response.GetValueOrDefault("oldPhotoUrl", "")) ?? "";
                DeleteManagedPhoto(oldPhoto, uploadDir);

                response["photoUrl"] = photoUrl;
                return Ok(response);
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                return BadRequest(new { success = false, message = $"No se pudo guardar la imagen: {ex.Message}" });
            }
        }

        private static bool HasValidImageSignature(byte[] header, int length, string extension)
        {
            if (length < 12) return false;

            return extension switch
            {
                ".jpg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".webp" => header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                           header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',
                _ => false
            };
        }

        private static void DeleteManagedPhoto(string photoUrl, string uploadDir)
        {
            if (string.IsNullOrWhiteSpace(photoUrl) ||
                !photoUrl.StartsWith("/uploads/members/", StringComparison.OrdinalIgnoreCase))
                return;

            var fileName = Path.GetFileName(photoUrl);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var path = Path.Combine(uploadDir, fileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        [HttpPut]
        [Route("api/members/{memberId}")]
        public ActionResult<Dictionary<string, object>> UpdateMember(long memberId, [FromBody] Dictionary<string, object> memberData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.UpdateMember(memberId, memberData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/members/{memberId}/memberships")]
        public ActionResult<Dictionary<string, object>> GetMembershipHistory(long memberId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.GetMembershipHistory(memberId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/members/plans")]
        public ActionResult<Dictionary<string, object>> GetMembershipPlans([FromQuery] bool includeInactive = false)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.GetMembershipPlans(out var response, includeInactive);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/members/plans")]
        public ActionResult<Dictionary<string, object>> CreatePlan([FromBody] Dictionary<string, object> planData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.CreatePlan(planData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/members/plans/{planId}")]
        public ActionResult<Dictionary<string, object>> UpdatePlan(long planId, [FromBody] Dictionary<string, object> planData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.UpdatePlan(planId, planData, out var response);
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
        [Route("api/members/{memberId}/membership")]
        public ActionResult<Dictionary<string, object>> AssignMembership(long memberId, [FromBody] Dictionary<string, object> membershipData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.AssignMembership(memberId, membershipData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/memberships/{membershipId}/cancel")]
        public ActionResult<Dictionary<string, object>> CancelMembership(long membershipId, [FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var reason = Convert.ToString(request.GetValueOrDefault("reason", "")) ?? "";
            var success = DataCenter.BusinessProcess.Members.CancelMembership(membershipId, reason, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/members/{memberId}/payments")]
        public ActionResult<Dictionary<string, object>> GetPayments(long memberId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Members.GetPayments(memberId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/members/{memberId}/payments/{paymentId}/receipt")]
        public ActionResult GetPaymentReceipt(long memberId, long paymentId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Members == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            if (!DataCenter.BusinessProcess.Members.GetPaymentReceipt(memberId, paymentId, out var html))
                return NotFound(new { success = false, message = "El pago no existe" });

            return Content(html, "text/html; charset=utf-8");
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
