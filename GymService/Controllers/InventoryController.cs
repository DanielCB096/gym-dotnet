using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class InventoryController : ControllerBase
    {
        [HttpGet]
        [Route("api/inventory")]
        public ActionResult<Dictionary<string, object>> GetProducts(
            [FromQuery] string? query = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] bool lowStock = false,
            [FromQuery] bool includeInactive = false)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.GetProducts(
                out var response, query, page, limit, lowStock, includeInactive);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/inventory")]
        public ActionResult<Dictionary<string, object>> CreateProduct([FromBody] Dictionary<string, object> productData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.CreateProduct(productData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/inventory/{productId}")]
        public ActionResult<Dictionary<string, object>> UpdateProduct(long productId, [FromBody] Dictionary<string, object> productData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.UpdateProduct(productId, productData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPut]
        [Route("api/inventory/{productId}/stock")]
        public ActionResult<Dictionary<string, object>> UpdateStock(long productId, [FromBody] Dictionary<string, int> request)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var newStock = request.GetValueOrDefault("stock", 0);
            var success = DataCenter.BusinessProcess.Inventory.UpdateStock(productId, newStock, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/inventory/{productId}")]
        public ActionResult<Dictionary<string, object>> DeleteProduct(long productId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Inventory == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Inventory.DeleteProduct(productId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }
    }
}
