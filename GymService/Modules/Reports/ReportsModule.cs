using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Services;
using Microsoft.Extensions.Configuration;

namespace GymService.Modules.Reports
{
    /// <summary>
    /// Reports module: reportes programados y su ejecución.
    /// </summary>
    public class ReportsModule
    {
        private const string SubModule = "REPORTS_MODULE";
        private readonly DatabaseTools _tools;
        private readonly EmailService? _email;
        private readonly IConfiguration? _configuration;
        private readonly ReportExportService _export;

        private static readonly HashSet<string> AllowedReportTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "sales", "members", "attendance", "inventory", "cash", "memberships", "kardex"
        };

        private static readonly HashSet<string> AllowedSchedules = new(StringComparer.OrdinalIgnoreCase)
        {
            "daily", "weekly", "monthly"
        };

        public ReportsModule(DatabaseTools tools, EmailService? email = null, IConfiguration? configuration = null)
        {
            _tools = tools;
            _email = email;
            _configuration = configuration;
            _export = new ReportExportService(tools);
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
                var reportName = Convert.ToString(reportData.GetValueOrDefault("reportName", ""))?.Trim() ?? "";
                var reportType = Convert.ToString(reportData.GetValueOrDefault("reportType", ""))?.Trim().ToLowerInvariant() ?? "";
                var schedule = Convert.ToString(reportData.GetValueOrDefault("schedule", ""))?.Trim().ToLowerInvariant() ?? "";
                var recipientsRaw = Convert.ToString(reportData.GetValueOrDefault("emailRecipients", "")) ?? "";

                if (string.IsNullOrEmpty(reportName))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del reporte es requerido";
                    return false;
                }

                if (!AllowedReportTypes.Contains(reportType))
                {
                    response["success"] = false;
                    response["message"] = "Tipo de reporte no válido";
                    return false;
                }

                if (!AllowedSchedules.Contains(schedule))
                {
                    response["success"] = false;
                    response["message"] = "Frecuencia no válida. Usa daily, weekly o monthly";
                    return false;
                }

                var recipients = ParseRecipients(recipientsRaw);
                if (recipients.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Agrega al menos un correo destinatario";
                    return false;
                }

                var report = new GYM_SCHEDULED_REPORTS
                {
                    REPORT_NAME = reportName,
                    REPORT_TYPE = reportType,
                    SCHEDULE = schedule,
                    EMAIL_RECIPIENTS = string.Join(",", recipients),
                    IS_ACTIVE = true,
                    NEXT_RUN = CalculateNextRun(schedule, DateTime.Now)
                };

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(report);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Reporte programado creado correctamente";
                    response["nextRun"] = report.NEXT_RUN;
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
                var report = new GYM_SCHEDULED_REPORTS { ID = reportId };

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(report);

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
                            ["reportType"] = Convert.ToString(GetRowValue(row, result, "report_type")) ?? "",
                            ["schedule"] = Convert.ToString(GetRowValue(row, result, "schedule")) ?? "",
                            ["emailRecipients"] = Convert.ToString(GetRowValue(row, result, "email_recipients")) ?? "",
                            ["isActive"] = GetRowValue(row, result, "is_active") != null && Convert.ToBoolean(GetRowValue(row, result, "is_active")),
                            ["lastRun"] = GetDateTimeOrNull(GetRowValue(row, result, "last_run")),
                            ["nextRun"] = GetDateTimeOrNull(GetRowValue(row, result, "next_run"))
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

        /// <summary>
        /// Ejecuta los reportes programados cuya NEXT_RUN ya venció.
        /// Devuelve cuántos reportes se intentaron ejecutar.
        /// </summary>
        public int RunDueScheduledReports()
        {
            int processed = 0;
            var now = DateTime.Now;

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, report_name, report_type, schedule, email_recipients, last_run, next_run " +
                    "FROM gym_scheduled_reports WHERE is_active = 1 AND (next_run IS NULL OR next_run <= ?) ORDER BY id",
                    now
                );

                if (result == null || result.Rows.Count == 0)
                    return 0;

                foreach (Row row in result.Rows)
                {
                    var id = Convert.ToInt64(GetRowValue(row, result, "id"));
                    var reportName = Convert.ToString(GetRowValue(row, result, "report_name")) ?? "";
                    var reportType = Convert.ToString(GetRowValue(row, result, "report_type")) ?? "";
                    var schedule = Convert.ToString(GetRowValue(row, result, "schedule")) ?? "";
                    var recipients = ParseRecipients(Convert.ToString(GetRowValue(row, result, "email_recipients")) ?? "");
                    var nextRun = GetDateTimeOrNull(GetRowValue(row, result, "next_run"));

                    if (nextRun == null)
                    {
                        // Migración de reportes antiguos: programa la siguiente ejecución sin enviar.
                        var lastRun = GetDateTimeOrNull(GetRowValue(row, result, "last_run"));
                        var calculated = CalculateNextRun(schedule, lastRun ?? now);
                        UpdateSchedule(id, lastRun, calculated);
                        continue;
                    }

                    processed++;
                    var error = string.Empty;

                    try
                    {
                        if (recipients.Count == 0)
                        {
                            error = "No tiene destinatarios";
                        }
                        else if (_email == null)
                        {
                            error = "EmailService no disponible";
                        }
                        else
                        {
                            var file = BuildReport(reportType, schedule, now);
                            if (file == null || file.Length == 0)
                            {
                                error = "El exportador devolvió un archivo vacío";
                            }
                            else
                            {
                                int sent = 0;
                                foreach (var recipient in recipients)
                                {
                                    if (_email.TrySendScheduledReport(recipient, reportName, file, out var sendError))
                                    {
                                        sent++;
                                    }
                                    else
                                    {
                                        error = sendError;
                                    }
                                }

                                if (sent == 0 && string.IsNullOrEmpty(error))
                                    error = "No se pudo enviar a ningún destinatario";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                    }

                    var next = CalculateNextRun(schedule, now);
                    UpdateSchedule(id, now, next);

                    if (string.IsNullOrEmpty(error))
                        Logger.AddInfoSubModule(SubModule, $"Reporte '{reportName}' enviado; próxima ejecución {next:yyyy-MM-dd HH:mm}");
                    else
                        Logger.AddWarningSubModule(SubModule, $"Reporte '{reportName}' no enviado: {error}; próxima ejecución {next:yyyy-MM-dd HH:mm}");
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en RunDueScheduledReports: {ex.Message}");
            }

            return processed;
        }

        private void UpdateSchedule(long id, DateTime? lastRun, DateTime nextRun)
        {
            var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
            session.UpdateObjectChanges(new GYM_SCHEDULED_REPORTS
            {
                ID = id,
                LAST_RUN = lastRun,
                NEXT_RUN = nextRun
            });
            session.TryExecute();
        }

        private byte[]? BuildReport(string reportType, string schedule, DateTime now)
        {
            DateTime start = schedule.ToLowerInvariant() switch
            {
                "weekly" => now.AddDays(-7),
                "monthly" => now.AddMonths(-1),
                _ => now.AddDays(-1)
            };

            return reportType.ToLowerInvariant() switch
            {
                "sales" => _export.ExportSalesToExcel(start, now),
                "members" => _export.ExportMembersToExcel(),
                "attendance" => _export.ExportAttendanceToExcel(start, now),
                "inventory" => _export.ExportInventoryToExcel(),
                "cash" => _export.ExportCashToExcel(start, now),
                "memberships" => _export.ExportMembershipsToExcel(null, start, now),
                "kardex" => _export.ExportKardexToExcel(null, start, now),
                _ => null
            };
        }

        private DateTime CalculateNextRun(string schedule, DateTime after)
        {
            int hour = GetScheduledHour();
            var candidate = new DateTime(after.Year, after.Month, after.Day, hour, 0, 0);

            switch (schedule.ToLowerInvariant())
            {
                case "weekly":
                    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)after.DayOfWeek + 7) % 7;
                    candidate = after.Date.AddDays(daysUntilMonday).AddHours(hour);
                    if (candidate <= after) candidate = candidate.AddDays(7);
                    break;

                case "monthly":
                    candidate = new DateTime(after.Year, after.Month, 1, hour, 0, 0);
                    if (candidate <= after) candidate = candidate.AddMonths(1);
                    break;

                default: // daily
                    if (candidate <= after) candidate = candidate.AddDays(1);
                    break;
            }

            return candidate;
        }

        private int GetScheduledHour()
        {
            if (int.TryParse(_configuration?["Reports:ScheduledHour"], out int hour) && hour >= 0 && hour <= 23)
                return hour;
            return 8;
        }

        private static List<string> ParseRecipients(string raw)
        {
            return (raw ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(email => email.Contains('@') && !email.Contains(' '))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DateTime? GetDateTimeOrNull(object? value)
        {
            return value == null || value == DBNull.Value ? null : Convert.ToDateTime(value);
        }
    }
}
