using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Services;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class ImportController : ControllerBase
    {
        private BulkImportService GetImportService()
        {
            // The BulkImportService is stateless and can be created on demand
            return new BulkImportService();
        }

        /// <summary>
        /// Import members from a CSV file.
        /// Expected columns: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
        /// </summary>
        [HttpPost]
        [Route("api/import/members/csv")]
        public ActionResult<Dictionary<string, object>> ImportMembersCsv(
            [FromQuery] string? encoding = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo" });

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return BadRequest(new { success = false, message = "El archivo está vacío" });

            using var stream = file.OpenReadStream();
            var service = GetImportService();
            var result = service.ImportMembersFromCsv(stream, encoding);

            var response = BuildResponse(result);
            return result.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Import members from an Excel (.xlsx) file.
        /// Expected columns in first row: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
        /// </summary>
        [HttpPost]
        [Route("api/import/members/excel")]
        public ActionResult<Dictionary<string, object>> ImportMembersExcel()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo" });

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return BadRequest(new { success = false, message = "El archivo está vacío" });

            using var stream = file.OpenReadStream();
            var service = GetImportService();
            var result = service.ImportMembersFromExcel(stream);

            var response = BuildResponse(result);
            return result.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Import inventory items from a CSV file.
        /// Expected columns: ProductName, Description, SKU, Category, Price, Cost, StockQuantity, MinStock
        /// </summary>
        [HttpPost]
        [Route("api/import/inventory/csv")]
        public ActionResult<Dictionary<string, object>> ImportInventoryCsv(
            [FromQuery] string? encoding = null)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo" });

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return BadRequest(new { success = false, message = "El archivo está vacío" });

            using var stream = file.OpenReadStream();
            var service = GetImportService();
            var result = service.ImportInventoryFromCsv(stream, encoding);

            var response = BuildResponse(result);
            return result.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Import inventory items from an Excel (.xlsx) file.
        /// Expected columns in first row: ProductName, Description, SKU, Category, Price, Cost, StockQuantity, MinStock
        /// </summary>
        [HttpPost]
        [Route("api/import/inventory/excel")]
        public ActionResult<Dictionary<string, object>> ImportInventoryExcel()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return BadRequest(new { success = false, message = "No se proporcionó ningún archivo" });

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return BadRequest(new { success = false, message = "El archivo está vacío" });

            using var stream = file.OpenReadStream();
            var service = GetImportService();
            var result = service.ImportInventoryFromExcel(stream);

            var response = BuildResponse(result);
            return result.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Download a sample CSV template for members import.
        /// </summary>
        [HttpGet]
        [Route("api/import/templates/members/csv")]
        public ActionResult DownloadMembersCsvTemplate()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetImportService();
            var bytes = service.GenerateMembersCsvTemplate();

            return File(bytes, "text/csv", "plantilla_miembros.csv");
        }

        /// <summary>
        /// Download a sample Excel template for members import.
        /// </summary>
        [HttpGet]
        [Route("api/import/templates/members/excel")]
        public ActionResult DownloadMembersExcelTemplate()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetImportService();
            var bytes = service.GenerateMembersExcelTemplate();

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla_miembros.xlsx");
        }

        /// <summary>
        /// Download a sample CSV template for inventory import.
        /// </summary>
        [HttpGet]
        [Route("api/import/templates/inventory/csv")]
        public ActionResult DownloadInventoryCsvTemplate()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetImportService();
            var bytes = service.GenerateInventoryCsvTemplate();

            return File(bytes, "text/csv", "plantilla_inventario.csv");
        }

        /// <summary>
        /// Download a sample Excel template for inventory import.
        /// </summary>
        [HttpGet]
        [Route("api/import/templates/inventory/excel")]
        public ActionResult DownloadInventoryExcelTemplate()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetImportService();
            var bytes = service.GenerateInventoryExcelTemplate();

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla_inventario.xlsx");
        }

        private Dictionary<string, object> BuildResponse(BulkImportResult result)
        {
            var errorsList = new List<Dictionary<string, object>>();
            foreach (var error in result.Errors)
            {
                errorsList.Add(new Dictionary<string, object>
                {
                    ["rowNumber"] = error.RowNumber,
                    ["rowData"] = error.RowData,
                    ["errorMessage"] = error.ErrorMessage
                });
            }

            return new Dictionary<string, object>
            {
                ["success"] = result.Success,
                ["message"] = result.Message,
                ["tableName"] = result.TableName,
                ["totalRows"] = result.TotalRows,
                ["importedRows"] = result.ImportedRows,
                ["skippedRows"] = result.SkippedRows,
                ["errorRows"] = result.ErrorRows,
                ["errors"] = errorsList
            };
        }
    }
}
