using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Reports
{
    /// <summary>
    /// Reports module: reportes programados.
    /// </summary>
    public class ReportsModule
    {
        private const string SubModule = "REPORTS_MODULE";
        private DatabaseTools _tools;

        public ReportsModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        private int GetColumnIndex(ResultSet result, string columnName)
        {
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private object GetRowValue(Row row, ResultSet result, string columnName)
        {
            int idx = GetColumnIndex(result, columnName);
            if (idx < 0) return null;
            return row.GetObject(idx);
        }

        /// <summary>
        /// Create a scheduled report
        /// </summary>
        public bool CreateScheduledReport(Dictionary<string, object> reportData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var reportName = Convert.ToString(reportData.GetValueOrDefault("reportName", "")) ?? "";
                var reportType = Convert.ToString(reportData.GetValueOrDefault("reportType", "")) ?? "";
                var schedule = Convert.ToString(reportData.GetValueOrDefault("schedule", "")) ?? "";

                if (string.IsNullOrEmpty(reportName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del reporte es requerido";
                    return false;
                }

                var session = _tools.CreateSession();
                session.ExecuteTransaction("INSERT INTO gym_scheduled_reports (report_name, report_type, schedule, is_active) VALUES (?, ?, ?, 1)", reportName, reportType, schedule);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Reporte programado creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el reporte";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateScheduledReport: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a scheduled report
        /// </summary>
        public bool DeleteScheduledReport(long reportId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("DELETE FROM gym_scheduled_reports WHERE id = ?", reportId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Reporte eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el reporte";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteScheduledReport: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get scheduled reports
        /// </summary>
        public bool GetScheduledReports(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_scheduled_reports WHERE is_active = 1 ORDER BY report_name"
                );

                var reports = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        reports.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["reportName"] = Convert.ToString(GetRowValue(row, result, "report_name")) ?? "",
                            ["schedule"] = Convert.ToString(GetRowValue(row, result, "schedule")) ?? "",
                            ["lastRun"] = GetRowValue(row, result, "last_run") != null ? Convert.ToDateTime(GetRowValue(row, result, "last_run")) : (DateTime?)null
                        });
                    }
                }

                response["success"] = true;
                response["data"] = reports;
                response["count"] = reports.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetScheduledReports: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
