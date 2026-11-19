using AxLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymService.Services
{
    /// <summary>
    /// Ejecuta la limpieza de datos al arrancar y cada Maintenance:CleanupIntervalMinutes.
    /// </summary>
    public sealed class CleanupHostedService : BackgroundService
    {
        private readonly MaintenanceService _maintenance;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CleanupHostedService> _logger;

        public CleanupHostedService(
            MaintenanceService maintenance,
            IConfiguration configuration,
            ILogger<CleanupHostedService> logger)
        {
            _maintenance = maintenance;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int intervalMinutes = 60;
            if (int.TryParse(_configuration["Maintenance:CleanupIntervalMinutes"], out int configured) && configured > 0)
                intervalMinutes = configured;

            // Pequeña espera para no competir con el arranque del servicio.
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                RunOnce();

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken))
                        break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void RunOnce()
        {
            try
            {
                var rc = _maintenance.RunCleanup();

                if (rc.IsError)
                {
                    Logger.AddWarningSubModule("MAINTENANCE", $"La limpieza periodica reporto un error: {rc.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("MAINTENANCE", $"Excepcion en la limpieza periodica: {ex.Message}");
            }
        }
    }
}

