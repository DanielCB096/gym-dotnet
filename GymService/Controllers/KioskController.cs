using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class KioskController : ControllerBase
    {
        [HttpPost]
        [Route("api/kiosk/device/register")]
        public ActionResult<Dictionary<string, object>> RegisterDevice([FromBody] Dictionary<string, string> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Kiosk == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var deviceName = request.GetValueOrDefault("deviceName", "");

            var success = DataCenter.BusinessProcess.Kiosk.RegisterDevice(deviceName, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/kiosk/device/validate")]
        public ActionResult<Dictionary<string, object>> ValidateDeviceToken([FromBody] Dictionary<string, string> request)
        {
            if (DataCenter.BusinessProcess.Kiosk == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var token = request.GetValueOrDefault("token", "");

            var success = DataCenter.BusinessProcess.Kiosk.ValidateDeviceToken(token, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/kiosk/equipment/unavailable")]
        public ActionResult<Dictionary<string, object>> GetUnavailableEquipment()
        {
            if (DataCenter.BusinessProcess.Kiosk == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Kiosk.GetUnavailableEquipment(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/kiosk/devices")]
        public ActionResult<Dictionary<string, object>> GetDevices()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Kiosk == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Kiosk.GetDevices(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/kiosk/devices/{deviceId}/deactivate")]
        public ActionResult<Dictionary<string, object>> DeactivateDevice(long deviceId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Kiosk == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Kiosk.DeactivateDevice(deviceId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
