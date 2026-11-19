using AxLibraries;
using AxLogger;
using AxObjectOrientedDatabase;
using Microsoft.Extensions.Configuration;

namespace GymService.Services
{
    /// <summary>
    /// Tareas de mantenimiento de datos: limpieza de sesiones/OTP/códigos caducados,
    /// retención de bitácora y expiración de membresías.
    ///
    /// Se ejecuta en una sola transacción. Son operaciones masivas de mantenimiento,
    /// por eso usan SQL parametrizado a través de Session.ExecuteTransaction.
    /// </summary>
    public class MaintenanceService
    {
        private const string SubModule = "MAINTENANCE";

        private readonly DatabaseTools _tools;
        private readonly IConfiguration _configuration;

        public MaintenanceService(DatabaseTools tools, IConfiguration configuration)
        {
            _tools = tools;
            _configuration = configuration;
        }

        public ProcessResult RunCleanup()
        {
            var rc = new ProcessResult();

            try
            {
                DateTime now = DateTime.Now;
                int sessionRetentionDays = GetInt("Maintenance:SessionRetentionDays", 7);
                int otpRetentionDays = GetInt("Maintenance:OtpRetentionDays", 1);
                int accessLogRetentionDays = GetInt("Maintenance:AccessLogRetentionDays", 90);

                var session = _tools.CreateSession(SubModule, -1);

                // Sesiones expiradas o inactivas antiguas.
                session.ExecuteTransaction(
                    "DELETE FROM GYM_SESSIONS WHERE EXPIRES_AT < ? OR (IS_ACTIVE = 0 AND CREATED_AT < ?)",
                    now, now.AddDays(-sessionRetentionDays));

                // OTP expirados o usados antiguos.
                session.ExecuteTransaction(
                    "DELETE FROM GYM_OTP_CODES WHERE EXPIRES_AT < ? OR (USED = 1 AND CREATED_AT < ?)",
                    now, now.AddDays(-otpRetentionDays));

                // Códigos de acceso caducados que nunca se usaron.
                session.ExecuteTransaction(
                    "DELETE FROM GYM_ACCESS_CODES WHERE EXPIRES_AT < ? AND USED = 0",
                    now);

                // Retención de la bitácora de accesos.
                session.ExecuteTransaction(
                    "DELETE FROM GYM_ACCESS_LOG WHERE ACCESSED_AT < ?",
                    now.AddDays(-accessLogRetentionDays));

                // Membresías vencidas.
                session.ExecuteTransaction(
                    "UPDATE GYM_MEMBERSHIPS SET STATUS = 'expired' WHERE STATUS = 'active' AND END_DATE < CAST(GETDATE() AS date)");

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "La limpieza de datos fallo");
                    return rc.Set(AxNLS.ErrorData("La limpieza de datos fallo"));
                }

                Logger.AddInfoSubModule(SubModule,
                    $"Limpieza completada (sesiones>{sessionRetentionDays}d, OTP>{otpRetentionDays}d, bitacora>{accessLogRetentionDays}d)");

                return rc;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Excepcion en la limpieza de datos: {ex.Message}");
                Logger.AddErrorSubModule(SubModule, ex.StackTrace);
                return rc.FromException(ProcessResult.GenericError, ex);
            }
        }

        private int GetInt(string key, int defaultValue)
        {
            string? value = _configuration[key];
            return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : defaultValue;
        }
    }
}

