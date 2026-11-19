using AxLogger;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace GymService.Services
{
    /// <summary>
    /// EnvÃ­o de correo por SMTP (MailKit). La configuraciÃ³n vive en la secciÃ³n Email.
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
        /// EnvÃ­a el cÃ³digo OTP al destinatario. Devuelve false con el motivo en error.
        /// </summary>
        public bool TrySendOtp(string to, string code, string purpose, out string error)
        {
            error = string.Empty;

            if (!IsConfigured)
            {
                error = "El envÃ­o de correo no estÃ¡ configurado";
                return false;
            }

            try
            {
                string fromAddress = _configuration["Email:FromAddress"]!;
                string fromName = _configuration["Email:FromName"] ?? "Centro Fit";
                string server = _configuration["Email:SmtpServer"]!;
                int port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                bool useSsl = !string.Equals(_configuration["Email:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromAddress));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = "CÃ³digo de verificaciÃ³n - Centro Fit";
                message.Body = new TextPart("html") { Text = BuildOtpHtml(code, purpose) };

                using var client = new SmtpClient();

                client.Connect(server, port,
                    useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                client.Authenticate(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPassword"]);

                client.Send(new SmtpSendRequest(message));
                client.Disconnect(true);

                Logger.AddInfoSubModule(SubModule, $"CÃ³digo OTP enviado a {MaskEmail(to)}");
                return true;
            }
            catch (Exception ex)
            {
                error = $"No se pudo enviar el correo: {ex.Message}";
                Logger.AddErrorSubModule(SubModule, $"Error al enviar OTP a {MaskEmail(to)}: {ex.Message}");
                return false;
            }
        }

        private string BuildOtpHtml(string code, string purpose)
        {
            int expiryMinutes = int.Parse(_configuration["Authentication:OtpExpiryMinutes"] ?? "10");
            string purposeText = purpose switch
            {
                "login" => "iniciar sesiÃ³n",
                "reset" => "restablecer tu contraseÃ±a",
                _ => "verificar tu identidad"
            };

            return $@"<div style=""font-family:Arial,sans-serif;max-width:480px;margin:0 auto"">
  <h2 style=""color:#111"">Centro Fit</h2>
  <p>Usa este cÃ³digo para {purposeText}:</p>
  <p style=""font-size:32px;letter-spacing:8px;font-weight:bold;background:#f3f4f6;padding:16px;text-align:center;border-radius:8px"">{code}</p>
  <p>El cÃ³digo caduca en {expiryMinutes} minutos. Si no solicitaste este cÃ³digo, ignora este correo.</p>
</div>";
        }

        private static string MaskEmail(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 1) return "***";

            return email[0] + new string('*', Math.Min(at - 1, 5)) + email[at..];
        }
    }
}

