using System.Net;
using System.Text;
using AxLogger;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace GymService.Services
{
    /// <summary>
    /// Datos de un cobro de membresía para el correo de recibo.
    /// </summary>
    public class MembershipReceipt
    {
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MXN";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public decimal CreditApplied { get; set; }
        public bool NoCharge { get; set; }
    }

    /// <summary>
    /// Envío de correo por SMTP (MailKit). La configuración vive en la sección Email.
    /// Si no hay SMTP pero sí Email:OutboxDirectory, los correos se guardan como HTML
    /// (útil para desarrollo y pruebas sin servidor de correo).
    /// </summary>
    public class EmailService
    {
        private const string SubModule = "EMAIL";

        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Hay SMTP configurado si existen servidor, remitente y usuario.
        /// </summary>
        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_configuration["Email:SmtpServer"])
                    && !string.IsNullOrWhiteSpace(_configuration["Email:FromAddress"])
                    && !string.IsNullOrWhiteSpace(_configuration["Email:SmtpUser"]);
            }
        }

        /// <summary>
        /// Se puede enviar o dejar en outbox. Sirve para omitir notificaciones no configuradas.
        /// </summary>
        public bool IsEnabled => IsConfigured || HasOutbox;

        private bool HasOutbox => !string.IsNullOrWhiteSpace(_configuration["Email:OutboxDirectory"]);

        /// <summary>
        /// Envía el código OTP al destinatario. Devuelve false con el motivo en error.
        /// </summary>
        public bool TrySendOtp(string to, string code, string purpose, out string error)
        {
            return TrySendEmail(
                to,
                "Código de verificación - Centro Fit",
                BuildOtpHtml(code, purpose),
                out error);
        }

        /// <summary>
        /// Correo de bienvenida para un socio recién registrado.
        /// </summary>
        public bool TrySendWelcome(string to, string memberName, out string error)
        {
            string name = Html(memberName);
            string body = $@"<p>Hola <strong>{name}</strong>,</p>
  <p>Tu registro en <strong>Centro Fit</strong> quedó listo.</p>
  <p>Para comenzar a entrenar, activa tu membresía en recepción o desde el panel.
     Tu cuenta, tus pagos y tu historial de acceso quedarán vinculados a este correo.</p>
  <p style=""padding:12px 16px;background:#eef2ff;border-radius:8px;color:#3730a3"">
     Si no reconoces este registro, ignora este mensaje o avísanos en recepción.
  </p>";

            return TrySendEmail(to, "Bienvenido a Centro Fit", BuildHtml("¡Bienvenido a Centro Fit!", body), out error);
        }

        /// <summary>
        /// Recibo del cobro o renovación de membresía.
        /// </summary>
        public bool TrySendMembershipReceipt(string to, string memberName, MembershipReceipt receipt, out string error)
        {
            string amount = receipt.NoCharge
                ? "Cortesía (sin cobro)"
                : $"{receipt.Amount:0.00} {Html(receipt.Currency)}";
            string method = receipt.NoCharge ? "—" : PaymentMethodText(receipt.PaymentMethod);
            string reference = string.IsNullOrWhiteSpace(receipt.Reference) ? "—" : Html(receipt.Reference);
            string creditRow = receipt.CreditApplied > 0
                ? $"<tr><td style=\"padding:6px 0;color:#6b7280\">Crédito aplicado</td><td style=\"padding:6px 0;text-align:right\">-{receipt.CreditApplied:0.00} {Html(receipt.Currency)}</td></tr>"
                : string.Empty;

            string body = $@"<p>Hola <strong>{Html(memberName)}</strong>,</p>
  <p>Registramos tu membresía correctamente. Este es el detalle:</p>
  <table style=""width:100%;border-collapse:collapse;margin:16px 0"">
    <tr><td style=""padding:6px 0;color:#6b7280"">Plan</td><td style=""padding:6px 0;text-align:right""><strong>{Html(receipt.PlanName)}</strong></td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280"">Periodo</td><td style=""padding:6px 0;text-align:right"">{receipt.StartDate:dd/MM/yyyy} — {receipt.EndDate:dd/MM/yyyy}</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280"">Importe</td><td style=""padding:6px 0;text-align:right""><strong>{amount}</strong></td></tr>
    {creditRow}
    <tr><td style=""padding:6px 0;color:#6b7280"">Método</td><td style=""padding:6px 0;text-align:right"">{method}</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280"">Referencia</td><td style=""padding:6px 0;text-align:right"">{reference}</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280"">Fecha del cobro</td><td style=""padding:6px 0;text-align:right"">{receipt.PaymentDate:dd/MM/yyyy HH:mm}</td></tr>
  </table>
  <p>Conserva este correo como comprobante. También puedes solicitar tu recibo impreso en recepción.</p>";

            return TrySendEmail(to, "Recibo de membresía - Centro Fit", BuildHtml("Recibo de membresía", body), out error);
        }

        /// <summary>
        /// Aviso previo al vencimiento de la membresía.
        /// </summary>
        public bool TrySendExpiryNotice(
            string to,
            string memberName,
            string planName,
            DateTime endDate,
            int daysLeft,
            out string error)
        {
            string when = daysLeft <= 0
                ? "hoy"
                : daysLeft == 1 ? "mañana" : $"en {daysLeft} días";

            string subject = daysLeft <= 0
                ? "Tu membresía vence hoy - Centro Fit"
                : $"Tu membresía vence {when} - Centro Fit";

            string body = $@"<p>Hola <strong>{Html(memberName)}</strong>,</p>
  <p>Tu membresía <strong>{Html(planName)}</strong> vence <strong>{when}</strong> ({endDate:dd/MM/yyyy}).</p>
  <p>Para seguir entrenando sin interrupciones, renueva en recepción o desde el panel.
     El acceso se bloquea en la fecha de vencimiento; no hay periodo de gracia.</p>
  <p style=""padding:12px 16px;background:#fff7ed;border-radius:8px;color:#9a3412"">
     Si ya renovaste, ignora este aviso.
  </p>";

            return TrySendEmail(to, subject, BuildHtml("Tu membresía está por vencer", body), out error);
        }

        /// <summary>
        /// Correo de prueba para comprobar la configuración SMTP.
        /// </summary>
        public bool TrySendTest(string to, out string error)
        {
            string body = $@"<p>Este es un correo de prueba de <strong>Centro Fit</strong>.</p>
  <p>Si lo recibes, el envío SMTP está configurado correctamente.</p>
  <p style=""color:#6b7280"">Enviado el {DateTime.Now:dd/MM/yyyy HH:mm}.</p>";

            return TrySendEmail(to, "Prueba de correo - Centro Fit", BuildHtml("El correo funciona", body), out error);
        }

        /// <summary>
        /// Elige el modo de conexión: 465 usa SSL directo, 587 STARTTLS.
        /// </summary>
        public static SecureSocketOptions ResolveSocketOptions(int port, bool useSsl)
        {
            if (port == 465)
                return SecureSocketOptions.SslOnConnect;

            return useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        }

        /// <summary>
        /// Envía un reporte programado en Excel como adjunto.
        /// </summary>
        public bool TrySendScheduledReport(string to, string reportName, byte[] excel, out string error)
        {
            string subject = $"Reporte {reportName} - Centro Fit";
            string html = $@"<p>Adjuntamos el reporte <strong>{Html(reportName)}</strong> generado por Centro Fit.</p>
  <p>Este mensaje se envió automáticamente según la programación configurada.</p>";
            string fileName = $"{SanitizeFileName(reportName)}-{DateTime.Now:yyyyMMdd}.xlsx";
            return TrySendEmail(to, subject, html, out error, excel, fileName);
        }

        private bool TrySendEmail(string to, string subject, string html, out string error, byte[]? attachment = null, string? attachmentName = null)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(to))
            {
                error = "El destinatario no tiene correo";
                return false;
            }

            try
            {
                if (!IsConfigured)
                {
                    if (!HasOutbox)
                    {
                        error = "El envío de correo no está configurado";
                        return false;
                    }

                    SaveToOutbox(to, subject, html, attachment, attachmentName);
                    return true;
                }

                string fromAddress = _configuration["Email:FromAddress"]!;
                string fromName = _configuration["Email:FromName"] ?? "Centro Fit";
                string server = _configuration["Email:SmtpServer"]!;
                int port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                bool useSsl = !string.Equals(_configuration["Email:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromAddress));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                if (attachment != null && attachment.Length > 0)
                {
                    var builder = new BodyBuilder { HtmlBody = html };
                    builder.Attachments.Add(
                        attachmentName ?? "reporte.xlsx",
                        attachment,
                        new ContentType("application", "vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                    message.Body = builder.ToMessageBody();
                }
                else
                {
                    message.Body = new TextPart("html") { Text = html };
                }

                using var client = new SmtpClient();
                client.Connect(server, port, ResolveSocketOptions(port, useSsl));
                client.Authenticate(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPassword"]);
                client.Send(new SmtpSendRequest(message));
                client.Disconnect(true);

                Logger.AddInfoSubModule(SubModule, $"Correo '{subject}' enviado a {MaskEmail(to)}");
                return true;
            }
            catch (Exception ex)
            {
                string detail = ex.Message ?? string.Empty;

                if (detail.Contains("535", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("5.7.8", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("authentication", StringComparison.OrdinalIgnoreCase))
                {
                    error = "El servidor rechazó las credenciales. En Gmail usa una contraseña de aplicación " +
                            "(16 caracteres) con verificación en 2 pasos, no la contraseña de la cuenta.";
                }
                else
                {
                    error = $"No se pudo enviar el correo: {detail}";
                }

                Logger.AddErrorSubModule(SubModule, $"Error al enviar '{subject}' a {MaskEmail(to)}: {detail}");
                return false;
            }
        }

        private void SaveToOutbox(string to, string subject, string html, byte[]? attachment = null, string? attachmentName = null)
        {
            string outbox = _configuration["Email:OutboxDirectory"]!;
            Directory.CreateDirectory(outbox);

            string baseName = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}_{SanitizeFileName(to)}_{Guid.NewGuid():N}";
            string file = Path.Combine(outbox, baseName + ".html");

            string content = $"<!-- Para: {to} | Asunto: {subject} -->{Environment.NewLine}{html}";
            File.WriteAllText(file, content, new UTF8Encoding(false));

            if (attachment != null && attachment.Length > 0)
            {
                string attachmentFile = Path.Combine(
                    outbox,
                    $"{baseName}_{SanitizeFileName(attachmentName ?? "reporte.xlsx")}");
                File.WriteAllBytes(attachmentFile, attachment);
            }

            Logger.AddInfoSubModule(SubModule, $"Correo '{subject}' guardado en outbox para {MaskEmail(to)}");
        }

        private string BuildOtpHtml(string code, string purpose)
        {
            int expiryMinutes = int.Parse(_configuration["Authentication:OtpExpiryMinutes"] ?? "10");
            string purposeText = purpose switch
            {
                "login" => "iniciar sesión",
                "reset" => "restablecer tu contraseña",
                _ => "verificar tu identidad"
            };

            string body = $@"<p>Usa este código para {purposeText}:</p>
  <p style=""font-size:32px;letter-spacing:8px;font-weight:bold;background:#f3f4f6;padding:16px;text-align:center;border-radius:8px"">{Html(code)}</p>
  <p>El código caduca en {expiryMinutes} minutos. Si no solicitaste este código, ignora este correo.</p>";

            return BuildHtml("Código de verificación", body);
        }

        private static string BuildHtml(string title, string body)
        {
            return $@"<!doctype html>
<html lang=""es"">
<body style=""margin:0;padding:24px;background:#f8fafc;font-family:Arial,Helvetica,sans-serif;color:#111827"">
  <div style=""max-width:560px;margin:0 auto"">
    <div style=""background:#465fff;color:#ffffff;padding:18px 24px;border-radius:12px 12px 0 0"">
      <h1 style=""margin:0;font-size:20px"">Centro Fit</h1>
    </div>
    <div style=""background:#ffffff;border:1px solid #e5e7eb;border-top:0;border-radius:0 0 12px 12px;padding:24px"">
      <h2 style=""margin:0 0 16px;font-size:18px;color:#111827"">{Html(title)}</h2>
      {body}
      <p style=""margin-top:24px;color:#9ca3af;font-size:12px"">
        Este correo se envió automáticamente; no respondas a este mensaje.
      </p>
    </div>
  </div>
</body>
</html>";
        }

        private static string PaymentMethodText(string paymentMethod)
        {
            return paymentMethod.ToLowerInvariant() switch
            {
                "cash" => "Efectivo",
                "card" => "Tarjeta",
                "transfer" => "Transferencia",
                _ => Html(paymentMethod)
            };
        }

        private static string Html(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string SanitizeFileName(string email)
        {
            var builder = new StringBuilder(email.Length);
            foreach (char c in email)
                builder.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '-');

            return builder.ToString();
        }

        private static string MaskEmail(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 1) return "***";

            return email[0] + new string('*', Math.Min(at - 1, 5)) + email[at..];
        }
    }
}
