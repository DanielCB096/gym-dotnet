using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;
using GymService.Services;

namespace GymService.Controllers
{
    [ApiController]
    public class ReportsController : ControllerBase
    {
        [HttpGet]
        [Route("api/reports/scheduled")]
        public ActionResult<Dictionary<string, object>> GetScheduledReports()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Reports == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Reports.GetScheduledReports(out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpPost]
        [Route("api/reports/scheduled")]
        public ActionResult<Dictionary<string, object>> CreateScheduledReport([FromBody] Dictionary<string, object> reportData)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Reports == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Reports.CreateScheduledReport(reportData, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete]
        [Route("api/reports/scheduled/{reportId}")]
        public ActionResult<Dictionary<string, object>> DeleteScheduledReport(long reportId)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.BusinessProcess.Reports == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            var success = DataCenter.BusinessProcess.Reports.DeleteScheduledReport(reportId, out var response);
            return success ? Ok(response) : BadRequest(response);
        }

        // ==================== CASH EXPORT ====================

        [HttpGet]
        [Route("api/reports/cash/excel")]
        public ActionResult ExportCashExcel([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!TryParseOptionalDate(startDate, out var start) || !TryParseOptionalDate(endDate, out var end))
                return BadRequest(new { success = false, message = "Fecha inválida. Usa el formato aaaa-mm-dd." });

            var service = GetExportService();
            var bytes = service.ExportCashToExcel(start, end);
            return File(bytes, XlsxContentType, $"caja_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== MEMBERSHIPS EXPORT ====================

        [HttpGet]
        [Route("api/reports/memberships/excel")]
        public ActionResult ExportMembershipsExcel(
            [FromQuery] string? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!TryParseOptionalDate(startDate, out var start) || !TryParseOptionalDate(endDate, out var end))
                return BadRequest(new { success = false, message = "Fecha inválida. Usa el formato aaaa-mm-dd." });

            var service = GetExportService();
            var bytes = service.ExportMembershipsToExcel(status, start, end);
            return File(bytes, XlsxContentType, $"membresias_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== KARDEX EXPORT ====================

        [HttpGet]
        [Route("api/reports/kardex/excel")]
        public ActionResult ExportKardexExcel(
            [FromQuery] long? productId,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (!TryParseOptionalDate(startDate, out var start) || !TryParseOptionalDate(endDate, out var end))
                return BadRequest(new { success = false, message = "Fecha inválida. Usa el formato aaaa-mm-dd." });

            var service = GetExportService();
            var bytes = service.ExportKardexToExcel(productId, start, end);
            return File(bytes, XlsxContentType, $"kardex_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private static bool TryParseOptionalDate(string? value, out DateTime? date)
        {
            date = null;
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!DateTime.TryParse(value, out var parsed))
                return false;

            date = parsed;
            return true;
        }

        private static readonly string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private ReportExportService GetExportService()
        {
            if (DataCenter.DatabaseManager == null)
                throw new InvalidOperationException("Base de datos no disponible");
            return new ReportExportService(DataCenter.DatabaseManager.Tools);
        }

        // ==================== SALES EXPORTS ====================

        [HttpGet]
        [Route("api/reports/sales/excel")]
        public ActionResult ExportSalesExcel([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

            var service = GetExportService();
            var bytes = service.ExportSalesToExcel(start, end);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ventas_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== MEMBERS EXPORTS ====================

        [HttpGet]
        [Route("api/reports/members/excel")]
        public ActionResult ExportMembersExcel([FromQuery] string? status)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetExportService();
            var bytes = service.ExportMembersToExcel(status);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"miembros_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== COLLECTIONS EXPORT ====================

        [HttpGet]
        [Route("api/reports/collections/excel")]
        public ActionResult ExportCollectionsExcel([FromQuery] int days = 7)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetExportService();
            var bytes = service.ExportCollectionsToExcel(days);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"cobranza_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== ATTENDANCE EXPORTS ====================

        [HttpGet]
        [Route("api/reports/attendance/excel")]
        public ActionResult ExportAttendanceExcel([FromQuery] string? startDate, [FromQuery] string? endDate)
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            DateTime? start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
            DateTime? end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

            var service = GetExportService();
            var bytes = service.ExportAttendanceToExcel(start, end);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"asistencia_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ==================== INVENTORY EXPORT ====================

        [HttpGet]
        [Route("api/reports/inventory/excel")]
        public ActionResult ExportInventoryExcel()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            var service = GetExportService();
            var bytes = service.ExportInventoryToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"inventario_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
