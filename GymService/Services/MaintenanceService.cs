using AxLibraries;
using AxLogger;
using AxDatabase;
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
        private readonly EmailService? _email;

        public MaintenanceService(DatabaseTools tools, IConfiguration configuration, EmailService? email = null)
        {
            _tools = tools;
            _configuration = configuration;
            _email = email ?? DataCenter.EmailService;
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
                    "UPDATE GYM_MEMBERSHIPS SET STATUS = 'expired' WHERE STATUS = 'active' AND END_DATE < CAST(? AS date)",
                    now.Date);

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "La limpieza de datos fallo");
                    return rc.Set(AxNLS.ErrorData("La limpieza de datos fallo"));
                }

                Logger.AddInfoSubModule(SubModule,
                    $"Limpieza completada (sesiones>{sessionRetentionDays}d, OTP>{otpRetentionDays}d, bitacora>{accessLogRetentionDays}d)");

                SendExpiryNotices(now);

                return rc;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Excepcion en la limpieza de datos: {ex.Message}");
                Logger.AddErrorSubModule(SubModule, ex.StackTrace);
                return rc.FromException(ProcessResult.GenericError, ex);
            }
        }

        /// <summary>
        /// Avisa por correo a las membresías activas que vencen dentro de
        /// Notifications:ExpiryNoticeDays. Marca EXPIRY_NOTICE_SENT_AT para no repetir el aviso.
        /// Devuelve cuántos avisos se enviaron.
        /// </summary>
        public int SendExpiryNotices(DateTime? today = null)
        {
            var email = _email;
            if (email == null || !email.IsEnabled)
                return 0;

            int sent = 0;
            DateTime current = (today ?? DateTime.Now).Date;
            int noticeDays = GetInt("Notifications:ExpiryNoticeDays", 7);

            try
            {
                var candidates = _tools.ExecuteQueryResultSet(@"
                    SELECT mem.ID, mem.END_DATE, m.FIRST_NAME, m.LAST_NAME, m.EMAIL, pl.PLAN_NAME
                    FROM GYM_MEMBERSHIPS mem
                    INNER JOIN GYM_MEMBERS m ON m.ID = mem.MEMBER_ID
                    LEFT JOIN GYM_MEMBERSHIP_PLANS pl ON pl.ID = mem.PLAN_ID
                    WHERE mem.STATUS = 'active'
                      AND mem.EXPIRY_NOTICE_SENT_AT IS NULL
                      AND mem.END_DATE >= CAST(? AS date)
                      AND mem.END_DATE <= DATEADD(DAY, ?, CAST(? AS date))
                      AND m.EMAIL IS NOT NULL AND m.EMAIL <> ''",
                    current, noticeDays, current);

                if (candidates == null || candidates.Rows.Count == 0)
                    return sent;

                foreach (Row row in candidates.Rows)
                {
                    long membershipId = Convert.ToInt64(row.GetObject(0));
                    DateTime endDate = Convert.ToDateTime(row.GetObject(1));
                    string memberName = $"{row.GetObject(2)} {row.GetObject(3)}".Trim();
                    string to = Convert.ToString(row.GetObject(4)) ?? "";
                    string planName = Convert.ToString(row.GetObject(5)) ?? "tu plan";
                    int daysLeft = (endDate.Date - current).Days;

                    if (!email.TrySendExpiryNotice(to, memberName, planName, endDate, daysLeft, out var emailError))
                    {
                        Logger.AddWarningSubModule(SubModule, $"No se envió el aviso de vencimiento: {emailError}");
                        continue;
                    }

                    var session = _tools.CreateSession(SubModule, -1);
                    session.UpdateObjectChanges(new GYM_MEMBERSHIPS
                    {
                        ID = membershipId,
                        EXPIRY_NOTICE_SENT_AT = DateTime.Now
                    });

                    if (session.TryExecute().IsError)
                    {
                        Logger.AddWarningSubModule(SubModule,
                            $"El aviso de la membresía {membershipId} se envió pero no se pudo marcar");
                        continue;
                    }

                    sent++;
                }

                Logger.AddInfoSubModule(SubModule, $"Avisos de vencimiento enviados: {sent}");
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error al enviar avisos de vencimiento: {ex.Message}");
            }

            return sent;
        }

        private int GetInt(string key, int defaultValue)
        {
            string? value = _configuration[key];
            return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : defaultValue;
        }
    }
}

