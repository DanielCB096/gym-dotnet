using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AxServiceTools;
using AxLogger;

namespace GymService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load configuration
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Initialize logging
            var logPath = configuration["Logging:LogPath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            Logger.ConfigLogger(
                "GymService",
                AxLogger.LogLevel.Debug,
                true,
                true,
                false,
                logPath
            );

            Logger.AddInfo("GymService iniciando...");
            Logger.AddInfo($"Versión: {GetVersion()}");
            Logger.AddInfo($"Directorio base: {AppDomain.CurrentDomain.BaseDirectory}");

            try
            {
                // Execute service (Windows Service or Console mode)
                if (args.Length > 0 && args[0].ToLower().Trim().Replace("-", "") == "console")
                {
                    // Console mode: auto-start the service directly without interactive shell
                    Logger.AddInfo("Ejecución en modalidad de CONSOLA (auto-start)");
                    var serviceImpl = new ServiceImplementation(configuration);
                    var result = serviceImpl.Start();
                    if (result.IsError)
                    {
                        Logger.AddErrorDevException($"Error al iniciar el servicio: {result.Message}");
                        Console.WriteLine($"Error al iniciar: {result.Message}");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("Servicio iniciado. Presiona Enter para detener...");
                    Console.ReadLine();
                    serviceImpl.Stop();
                }
                else
                {
                    // Windows Service mode or other commands
                    ServiceConsole.Execute(args, DataCenter.SERVICE_DATA, new ServiceImplementation(configuration));
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevException($"Error fatal al iniciar GymService: {ex.Message}");
                Console.WriteLine($"Error fatal: {ex.Message}");
                Environment.Exit(1);
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
