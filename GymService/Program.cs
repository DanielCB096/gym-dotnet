using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using AxLibraries;
using AxServiceTools;
using AxLogger;

namespace GymService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load configuration (fail fast: sin appsettings.json no hay BD ni puerto)
            IConfiguration configuration;

            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo cargar appsettings.json: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }

            // Initialize logging
            var logPath = ResolveLogPath(configuration["Logging:LogPath"]);

            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            Logger.ConfigLogger(
                "GymService",
                ResolveLogLevel(configuration["Logging:LogLevel"]),
                true,
                true,
                false,
                logPath
            );

            try
            {
                // Registra el nls.xml embebido para este assembly. Sin esto, AxNLS.Data
                // intentaria resolverlo con Assembly.GetCallingAssembly() (poco fiable con inlining).
                NlsTools.LoadAssemblyMessages(DataCenter.NLS_ASSEMBLY_CODE, typeof(Program).Assembly);

                Logger.AddInfo(AxNLS.Data(DataCenter.NLS_ASSEMBLY_CODE, 2001, "GymService iniciando..."));
                Logger.AddInfo($"Versión: {GetVersion()}");
                Logger.AddInfo($"Directorio base: {AppDomain.CurrentDomain.BaseDirectory}");
                Logger.AddInfo($"Ruta de logs: {logPath}");

                // Sin argumentos:
                //   - si se ejecuta desde consola (dotnet run / exe a mano) -> modo consola
                //   - si lo arranca el SCM (UserInteractive = false)        -> modo servicio
                // ServiceConsole con 0 argumentos siempre hace ServiceBase.Run, que falla
                // con el error 1063 si no viene del SCM.
                var effectiveArgs = args;

                if (effectiveArgs.Length == 0 && Environment.UserInteractive)
                {
                    effectiveArgs = new[] { "-console" };
                }

                // Diagnóstico de correo: no arranca el servicio, solo prueba el SMTP.
                if (effectiveArgs.Length >= 2 &&
                    effectiveArgs[0].ToLower().Trim().Replace("-", "") == "testemail")
                {
                    var emailService = new GymService.Services.EmailService(configuration);
                    string recipient = effectiveArgs[1];

                    Console.WriteLine($"Enviando correo de prueba a {recipient}...");
                    bool sent = emailService.TrySendTest(recipient, out var emailError);
                    Console.WriteLine(sent ? "Correo enviado correctamente." : $"Error: {emailError}");
                    Environment.ExitCode = sent ? 0 : 1;
                    return;
                }

                if (effectiveArgs.Length > 0 && effectiveArgs[0].ToLower().Trim().Replace("-", "") == "console")
                {
                    // Console mode: auto-start the service directly without interactive shell.
                    // -daemon: no espera a Enter (util para CI/contenedores); termina al matar el proceso.
                    bool daemon = effectiveArgs.Any(a => a.ToLower().Trim().Replace("-", "") == "daemon");

                    Logger.AddInfo("Ejecución en modalidad de CONSOLA (auto-start)");
                    var serviceImpl = new ServiceImplementation(configuration);
                    var result = serviceImpl.Start();

                    if (result.IsError)
                    {
                        Logger.AddErrorDevException($"Error al iniciar el servicio: {result.Message}");
                        Console.WriteLine($"Error al iniciar: {result.Message}");
                        Environment.ExitCode = 1;
                        return;
                    }

                    if (daemon)
                    {
                        Logger.AddInfo("Modo daemon: el servicio queda en ejecución hasta que se detenga el proceso");
                        new System.Threading.ManualResetEvent(false).WaitOne();
                    }
                    else
                    {
                        Console.WriteLine("Servicio iniciado. Presiona Enter para detener...");
                        Console.ReadLine();
                    }

                    serviceImpl.Stop();
                }
                else
                {
                    // Windows Service mode or installation commands (-install, -uninstall, ...)
                    ServiceConsole.Execute(effectiveArgs, DataCenter.SERVICE_DATA, new ServiceImplementation(configuration));
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevException($"Error fatal al iniciar GymService: {ex.Message}");
                Console.WriteLine($"Error fatal: {ex.Message}");
                Environment.ExitCode = 1;
            }
            finally
            {
                // El logger usa un buffer/thread interno: sin Close() se pierden las
                // ultimas lineas al terminar el proceso.
                Logger.Close();
            }
        }

        /// <summary>
        /// Resuelve la ruta de logs. Si es relativa se ancla al directorio del ejecutable:
        /// cuando corre como servicio Windows el directorio actual es C:\Windows\System32.
        /// </summary>
        private static string ResolveLogPath(string? configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return Path.Combine(AppContext.BaseDirectory, "logs");

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);
        }

        /// <summary>
        /// Traduce Logging:LogLevel (niveles de Microsoft.Extensions.Logging) a AxLogger.LogLevel.
        /// </summary>
        private static LogLevel ResolveLogLevel(string? configuredLevel)
        {
            if (string.IsNullOrWhiteSpace(configuredLevel))
                return LogLevel.Debug;

            switch (configuredLevel.Trim().ToUpperInvariant())
            {
                case "TRACE":
                case "DEBUG":
                    return LogLevel.Debug;

                case "INFORMATION":
                case "INFO":
                case "DETAIL":
                case "DETAILS":
                    return LogLevel.Detail;

                case "WARNING":
                case "WARN":
                case "ERROR":
                case "CRITICAL":
                case "NONE":
                case "NORMAL":
                    return LogLevel.Normal;

                default:
                    return LogLevelParser.Parse(configuredLevel);
            }
        }

        private static string GetVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }
    }
}

