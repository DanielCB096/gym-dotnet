using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
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
        [Route("api/auth/otp/request")]
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
