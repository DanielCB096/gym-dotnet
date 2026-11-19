using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class ProcurementController : ControllerBase
    {
        // ============================ Proveedores ============================

        [HttpGet]
        [Route("api/suppliers")]
        public ActionResult<Dictionary<string, object>> GetSuppliers([FromQuery] bool includeInactive = true)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.GetSuppliers(out var response, includeInactive);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/suppliers")]
        public ActionResult<Dictionary<string, object>> CreateSupplier([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.CreateSupplier(data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/suppliers/{supplierId}")]
        public ActionResult<Dictionary<string, object>> UpdateSupplier(long supplierId, [FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.UpdateSupplier(supplierId, data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ========================= Órdenes de compra ========================

        [HttpGet]
        [Route("api/purchase-orders")]
        public ActionResult<Dictionary<string, object>> GetPurchaseOrders([FromQuery] string? status = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.GetPurchaseOrders(out var response, status);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpGet]
        [Route("api/purchase-orders/{orderId}")]
        public ActionResult<Dictionary<string, object>> GetPurchaseOrder(long orderId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.GetPurchaseOrder(orderId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/purchase-orders")]
        public ActionResult<Dictionary<string, object>> CreatePurchaseOrder([FromBody] Dictionary<string, object> data)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.CreatePurchaseOrder(data, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/purchase-orders/{orderId}/send")]
        public ActionResult<Dictionary<string, object>> SendPurchaseOrder(long orderId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.SendPurchaseOrder(orderId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/purchase-orders/{orderId}/receive")]
        public ActionResult<Dictionary<string, object>> ReceivePurchaseOrder(long orderId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.ReceivePurchaseOrder(orderId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/purchase-orders/{orderId}/cancel")]
        public ActionResult<Dictionary<string, object>> CancelPurchaseOrder(long orderId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.CancelPurchaseOrder(orderId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ========================= Movimientos/kardex =======================

        [HttpGet]
        [Route("api/inventory/movements")]
        public ActionResult<Dictionary<string, object>> GetMovements(
            [FromQuery] long? productId = null,
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 100)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.GetMovements(out var response, productId, type, page, limit);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
