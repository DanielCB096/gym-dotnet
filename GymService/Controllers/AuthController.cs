using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost]
        [Route("api/auth/login")]
        public ActionResult<Dictionary<string, object>> Login([FromBody] Dictionary<string, string> request)
        {
            var response = new Dictionary<string, object>();

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var username = request.GetValueOrDefault("username", "");
            var password = request.GetValueOrDefault("password", "");
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var success = DataCenter.BusinessProcess.Identity.Login(username, password, deviceInfo, ipAddress, out var loginResponse);

            if (!success)
            {
                if (loginResponse.ContainsKey("requiresMfa") && Convert.ToBoolean(loginResponse["requiresMfa"]))
                    return Ok(loginResponse);

                return Unauthorized(loginResponse);
            }

            return Ok(loginResponse);
        }

        [HttpPost]
        [Route("api/auth/logout")]
        public ActionResult<Dictionary<string, object>> Logout()
        {
            var response = new Dictionary<string, object>();

            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (DataCenter.BusinessProcess.Identity != null)
            {
                DataCenter.BusinessProcess.Identity.Logout(token);
            }

            response["success"] = true;
            response["message"] = "Sesión cerrada";
            return Ok(response);
        }

        [HttpPost]
        [Route("api/auth/mfa/setup")]
        public ActionResult<Dictionary<string, object>> SetupMfa()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            var userId = SecurityValidation.GetUserId(userData!);

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.SetupMfa(userId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/mfa/verify")]
        public ActionResult<Dictionary<string, object>> VerifyMfa([FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            var userId = SecurityValidation.GetUserId(userData!);
            var code = request.GetValueOrDefault("code", "");

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.VerifyMfa(userId, code, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/mfa/login")]
        public ActionResult<Dictionary<string, object>> VerifyMfaLogin([FromBody] Dictionary<string, string> request)
        {
            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var mfaToken = request.GetValueOrDefault("mfaToken", "");
            var code = request.GetValueOrDefault("code", "");
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var success = DataCenter.BusinessProcess.Identity.VerifyMfaLogin(mfaToken, code, deviceInfo, ipAddress, out var response);

            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/auth/mfa/status")]
        public ActionResult<Dictionary<string, object>> GetMfaStatus()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.GetMfaStatus(SecurityValidation.GetUserId(userData!), out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/mfa/disable")]
        public ActionResult<Dictionary<string, object>> DisableMfa([FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var code = request.GetValueOrDefault("code", "");
            var success = DataCenter.BusinessProcess.Identity.DisableMfa(SecurityValidation.GetUserId(userData!), code, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/mfa/reset")]
        public ActionResult<Dictionary<string, object>> ResetMfa([FromBody] Dictionary<string, object> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            if (!SecurityValidation.HasPermission(userData!, "admin.settings"))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "No tienes permiso para resetear MFA" });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            long targetUserId;
            try
            {
                targetUserId = Convert.ToInt64(request.GetValueOrDefault("userId", 0));
            }
            catch
            {
                targetUserId = 0;
            }

            if (targetUserId <= 0)
                return BadRequest(new { success = false, message = "userId es requerido" });

            var actorUserId = SecurityValidation.GetUserId(userData!);
            var actorRole = SecurityValidation.GetUserRole(userData!);
            var success = DataCenter.BusinessProcess.Identity.ResetMfa(targetUserId, actorUserId, actorRole, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/change-password")]
        public ActionResult<Dictionary<string, object>> ChangePassword([FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { returnCode = "ERROR", errorMessage });

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var currentPassword = request.GetValueOrDefault("currentPassword", "");
            var newPassword = request.GetValueOrDefault("newPassword", "");
            var success = DataCenter.BusinessProcess.Identity.ChangeOwnPassword(
                SecurityValidation.GetUserId(userData!), currentPassword, newPassword, out var response);

            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/otp/request")]
        [EnableRateLimiting("strict")]
        public ActionResult<Dictionary<string, object>> RequestOtp([FromBody] Dictionary<string, string> request)
        {
            var response = new Dictionary<string, object>();
            var email = request.GetValueOrDefault("email", "");
            var purpose = request.GetValueOrDefault("purpose", "login");

            if (string.IsNullOrEmpty(email))
            {
                response["success"] = false;
                response["message"] = "Correo electrónico requerido";
                return BadRequest(response);
            }

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.RequestOtpByEmail(email, purpose, out response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/otp/verify")]
        [EnableRateLimiting("strict")]
        public ActionResult<Dictionary<string, object>> VerifyOtp([FromBody] Dictionary<string, string> request)
        {
            var email = request.GetValueOrDefault("email", "");
            var code = request.GetValueOrDefault("code", "");
            var purpose = request.GetValueOrDefault("purpose", "login");

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.VerifyOtp(email, code, purpose, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/auth/reset-admin-password")]
        public ActionResult<Dictionary<string, object>> ResetAdminPassword([FromBody] Dictionary<string, string> request)
        {
            var expectedToken = DataCenter.Configuration?["Authentication:AdminResetToken"];

            // Endpoint deshabilitado si no hay token configurado.
            if (string.IsNullOrWhiteSpace(expectedToken))
                return NotFound(new { success = false, message = "No disponible" });

            // Endpoint de recuperación: solo se permite desde la misma máquina.
            var remote = HttpContext.Connection.RemoteIpAddress;
            if (remote != null && !System.Net.IPAddress.IsLoopback(remote))
                return NotFound(new { success = false, message = "No disponible" });

            // Además del loopback, se exige el token compartido en la cabecera.
            var providedToken = Request.Headers["X-Admin-Reset-Token"].ToString();
            if (!FixedTimeEquals(providedToken, expectedToken))
                return NotFound(new { success = false, message = "No disponible" });

            var newPassword = request.GetValueOrDefault("password", "");

            if (string.IsNullOrEmpty(newPassword))
            {
                return BadRequest(new { success = false, message = "Password requerido" });
            }

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.ResetOwnerPassword(newPassword, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        private static bool FixedTimeEquals(string? a, string? b)
        {
            var bytesA = System.Text.Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bytesB = System.Text.Encoding.UTF8.GetBytes(b ?? string.Empty);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }

        [HttpPost]
        [Route("api/auth/bootstrap")]
        public ActionResult<Dictionary<string, object>> BootstrapOwner([FromBody] Dictionary<string, string> request)
        {
            var username = request.GetValueOrDefault("username", "");
            var email = request.GetValueOrDefault("email", "");
            var password = request.GetValueOrDefault("password", "");
            var fullName = request.GetValueOrDefault("fullName", "");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { success = false, message = "Username y password son requeridos" });
            }

            if (DataCenter.BusinessProcess.Identity == null)
                return BadRequest(new { returnCode = "ERROR", errorMessage = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Identity.BootstrapOwner(username, email, password, fullName, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}

