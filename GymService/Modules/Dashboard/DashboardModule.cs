using System;
using System.Collections.Generic;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Dashboard
{
    /// <summary>
    /// Resumen del panel: contadores del día, tendencias de los últimos 7 días
    /// y próximos vencimientos de membresías.
    /// </summary>
    public class DashboardModule
    {
        private const string SubModule = "DASHBOARD";
        private const int TrendDays = 7;

        private readonly DatabaseTools _tools;

        public DashboardModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        public bool GetSummary(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                DateTime today = DateTime.Today;
                DateTime weekStart = today.AddDays(-(TrendDays - 1));
                DateTime tomorrow = today.AddDays(1);

                var counters = _tools.ExecuteQueryResultSet(@"
                    SELECT
                      (SELECT COUNT(*) FROM GYM_MEMBERS WHERE STATUS = 'active') AS ACTIVE_MEMBERS,
                      (SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE STATUS = 'active' AND END_DATE >= CAST(? AS DATE)) AS ACTIVE_MEMBERSHIPS,
                      (SELECT COUNT(*) FROM GYM_MEMBERSHIPS WHERE STATUS = 'active' AND END_DATE BETWEEN CAST(? AS DATE) AND DATEADD(DAY, 7, CAST(? AS DATE))) AS EXPIRING_SOON,
                      (SELECT ISNULL(SUM(TOTAL_AMOUNT),0) FROM GYM_SALES WHERE STATUS = 'completed' AND CAST(SALE_DATE AS DATE) = CAST(? AS DATE)) AS SALES_TODAY,
                      (SELECT COUNT(*) FROM GYM_SALES WHERE STATUS = 'completed' AND CAST(SALE_DATE AS DATE) = CAST(? AS DATE)) AS SALES_TODAY_COUNT,
                      (SELECT ISNULL(SUM(AMOUNT),0) FROM GYM_PAYMENTS WHERE CAST(PAYMENT_DATE AS DATE) = CAST(? AS DATE)) AS PAYMENTS_TODAY,
                      (SELECT COUNT(*) FROM GYM_ACCESS_LOG WHERE ACCESS_RESULT = 'granted' AND CAST(ACCESSED_AT AS DATE) = CAST(? AS DATE)) AS ACCESS_TODAY,
                      (SELECT COUNT(*) FROM GYM_INVENTORY WHERE IS_ACTIVE = 1 AND STOCK_QUANTITY <= MIN_STOCK) AS LOW_STOCK,
                      (SELECT COUNT(*) FROM GYM_CASH_SESSIONS WHERE STATUS = 'open') AS OPEN_CASH",
                    today, today, today, today, today, today, today);

                if (counters == null || counters.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "No se pudo obtener el resumen";
                    return false;
                }

                var row = counters.Rows[0];
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
                    ["openCash"] = Convert.ToInt64(row.GetObject(8)) > 0,
                    ["trends"] = new Dictionary<string, object>
                    {
                        ["revenue"] = BuildRevenueTrend(weekStart, tomorrow),
                        ["access"] = BuildAccessTrend(weekStart, tomorrow),
                        ["upcoming"] = GetUpcomingExpirations(today)
                    }
                };

                response["success"] = true;
                response["data"] = data;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetSummary: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error al obtener el resumen";
                return false;
            }
        }

        private List<Dictionary<string, object>> BuildRevenueTrend(DateTime start, DateTime end)
        {
            var salesByDay = ReadAmountsByDay(
                "SELECT CAST(SALE_DATE AS DATE) AS DAY, ISNULL(SUM(TOTAL_AMOUNT),0) AS TOTAL " +
                "FROM GYM_SALES WHERE STATUS = 'completed' AND SALE_DATE >= ? AND SALE_DATE < ? " +
                "GROUP BY CAST(SALE_DATE AS DATE)", start, end);

            var paymentsByDay = ReadAmountsByDay(
                "SELECT CAST(PAYMENT_DATE AS DATE) AS DAY, ISNULL(SUM(AMOUNT),0) AS TOTAL " +
                "FROM GYM_PAYMENTS WHERE PAYMENT_DATE >= ? AND PAYMENT_DATE < ? " +
                "GROUP BY CAST(PAYMENT_DATE AS DATE)", start, end);

            var trend = new List<Dictionary<string, object>>();

            for (int i = 0; i < TrendDays; i++)
            {
                DateTime day = start.AddDays(i);
                string key = day.ToString("yyyy-MM-dd");
                decimal sales = salesByDay.GetValueOrDefault(key);
                decimal payments = paymentsByDay.GetValueOrDefault(key);

                trend.Add(new Dictionary<string, object>
                {
                    ["date"] = key,
                    ["sales"] = sales,
                    ["payments"] = payments,
                    ["total"] = sales + payments
                });
            }

            return trend;
        }

        private List<Dictionary<string, object>> BuildAccessTrend(DateTime start, DateTime end)
        {
            var countsByDay = new Dictionary<string, long>();
            var result = _tools.ExecuteQueryResultSet(
                "SELECT CAST(ACCESSED_AT AS DATE) AS DAY, COUNT(*) AS TOTAL " +
                "FROM GYM_ACCESS_LOG WHERE ACCESS_RESULT = 'granted' AND ACCESSED_AT >= ? AND ACCESSED_AT < ? " +
                "GROUP BY CAST(ACCESSED_AT AS DATE)", start, end);

            if (result != null)
            {
                foreach (Row row in result.Rows)
                    countsByDay[DayKey(row.GetObject(0))] = Convert.ToInt64(row.GetObject(1));
            }

            var trend = new List<Dictionary<string, object>>();

            for (int i = 0; i < TrendDays; i++)
            {
                DateTime day = start.AddDays(i);
                string key = day.ToString("yyyy-MM-dd");

                trend.Add(new Dictionary<string, object>
                {
                    ["date"] = key,
                    ["count"] = countsByDay.GetValueOrDefault(key)
                });
            }

            return trend;
        }

        private List<Dictionary<string, object>> GetUpcomingExpirations(DateTime today)
        {
            var upcoming = new List<Dictionary<string, object>>();

            var result = _tools.ExecuteQueryResultSet(@"
                SELECT TOP 10 mem.ID, mem.END_DATE, m.FIRST_NAME, m.LAST_NAME, pl.PLAN_NAME
                FROM GYM_MEMBERSHIPS mem
                INNER JOIN GYM_MEMBERS m ON m.ID = mem.MEMBER_ID
                LEFT JOIN GYM_MEMBERSHIP_PLANS pl ON pl.ID = mem.PLAN_ID
                WHERE mem.STATUS = 'active'
                  AND mem.END_DATE >= CAST(? AS DATE)
                  AND mem.END_DATE <= DATEADD(DAY, 7, CAST(? AS DATE))
                ORDER BY mem.END_DATE, m.FIRST_NAME",
                today, today);

            if (result != null)
            {
                foreach (Row row in result.Rows)
                {
                    DateTime endDate = Convert.ToDateTime(row.GetObject(1));

                    upcoming.Add(new Dictionary<string, object>
                    {
                        ["id"] = Convert.ToInt64(row.GetObject(0)),
                        ["memberName"] = $"{row.GetObject(2)} {row.GetObject(3)}".Trim(),
                        ["planName"] = Convert.ToString(row.GetObject(4)) ?? "",
                        ["endDate"] = endDate.ToString("yyyy-MM-dd"),
                        ["daysLeft"] = (endDate.Date - today).Days
                    });
                }
            }

            return upcoming;
        }

        private Dictionary<string, decimal> ReadAmountsByDay(string sql, DateTime start, DateTime end)
        {
            var amounts = new Dictionary<string, decimal>();
            var result = _tools.ExecuteQueryResultSet(sql, start, end);

            if (result != null)
            {
                foreach (Row row in result.Rows)
                    amounts[DayKey(row.GetObject(0))] = Convert.ToDecimal(row.GetObject(1));
            }

            return amounts;
        }

        private static string DayKey(object? value)
        {
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToDateTime(value).ToString("yyyy-MM-dd");
        }
    }
}
