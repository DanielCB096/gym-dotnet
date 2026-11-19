using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GymService.Tools;

namespace GymService.Controllers
{
    [ApiController]
    public class DashboardController : ControllerBase
    {
        [HttpGet]
        [Route("api/dashboard/summary")]
        public ActionResult<Dictionary<string, object>> GetSummary()
        {
            if (!SecurityValidation.ValidateAuthentication(Request, Response, out var userData, out var errorMessage))
                return Unauthorized(new { success = false, message = errorMessage });

            if (DataCenter.DatabaseManager == null)
                return BadRequest(new { success = false, message = "Servicio no disponible" });

            try
            {
                var result = DataCenter.DatabaseManager.Tools.ExecuteQueryResultSet(@"
                    SELECT
                      (SELECT COUNT(*) FROM GYM_MEMBERS WHERE STATUS = 'active') AS ACTIVE_MEMBERS,
                      (SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE STATUS = 'active' AND END_DATE >= CAST(GETDATE() AS DATE)) AS ACTIVE_MEMBERSHIPS,
                      (SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE STATUS = 'active' AND END_DATE BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE))) AS EXPIRING_SOON,
                      (SELECT ISNULL(SUM(TOTAL_AMOUNT),0) FROM GYM_SALES WHERE STATUS = 'completed' AND CAST(SALE_DATE AS DATE) = CAST(GETDATE() AS DATE)) AS SALES_TODAY,
                      (SELECT COUNT(*) FROM GYM_SALES WHERE STATUS = 'completed' AND CAST(SALE_DATE AS DATE) = CAST(GETDATE() AS DATE)) AS SALES_TODAY_COUNT,
                      (SELECT ISNULL(SUM(AMOUNT),0) FROM GYM_PAYMENTS WHERE CAST(PAYMENT_DATE AS DATE) = CAST(GETDATE() AS DATE)) AS PAYMENTS_TODAY,
                      (SELECT COUNT(*) FROM GYM_ACCESS_LOG WHERE ACCESS_RESULT = 'granted' AND CAST(ACCESSED_AT AS DATE) = CAST(GETDATE() AS DATE)) AS ACCESS_TODAY,
                      (SELECT COUNT(*) FROM GYM_INVENTORY WHERE IS_ACTIVE = 1 AND STOCK_QUANTITY <= MIN_STOCK) AS LOW_STOCK,
                      (SELECT COUNT(*) FROM GYM_CASH_SESSIONS WHERE STATUS = 'open') AS OPEN_CASH");

                var row = result.Rows[0];
                var data = new Dictionary<string, object>
                {
                    ["activeMembers"] = Convert.ToInt64(row.GetObject(0)),
                    ["activeMemberships"] = Convert.ToInt64(row.GetObject(1)),
                    ["expiringSoon"] = Convert.ToInt64(row.GetObject(2)),
                    ["salesToday"] = Convert.ToDecimal(row.GetObject(3)),
                    ["salesTodayCount"] = Convert.ToInt64(row.GetObject(4)),
                    ["paymentsToday"] = Convert.ToDecimal(row.GetObject(5)),
                    ["accessToday"] = Convert.ToInt64(row.GetObject(6)),
                    ["lowStock"] = Convert.ToInt64(row.GetObject(7)),
                    ["openCash"] = Convert.ToInt64(row.GetObject(8)) > 0
                };

                return Ok(new Dictionary<string, object> { ["success"] = true, ["data"] = data });
            }
            catch (Exception ex)
            {
                AxLogger.Logger.AddErrorSubModule("DASHBOARD", $"Error en GetSummary: {ex.Message}");
                return BadRequest(new { success = false, message = "Error al obtener el resumen" });
            }
        }
    }
}
