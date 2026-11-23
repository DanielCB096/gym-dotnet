using System;
using System.Threading;
using System.Threading.Tasks;
using AxLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymService.Services
{
    /// <summary>
    /// Ejecuta los reportes programados cuya NEXT_RUN ya venció.
    /// La comprobación es periódica; el envío real lo hace ReportsModule.
    /// </summary>
    public sealed class ScheduledReportHostedService : BackgroundService
    {
        private const string SubModule = "SCHEDULED_REPORTS";
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScheduledReportHostedService> _logger;

        public ScheduledReportHostedService(
            IConfiguration configuration,
            ILogger<ScheduledReportHostedService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int intervalMinutes = 5;
            if (int.TryParse(_configuration["Reports:CheckIntervalMinutes"], out int configured) && configured > 0)
                intervalMinutes = configured;

            // Espera breve para no competir con el arranque del servicio.
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
                var reports = DataCenter.BusinessProcess.Reports;
                if (reports == null)
                {
                    Logger.AddWarningSubModule(SubModule, "ReportsModule no está inicializado; no se ejecutan reportes programados");
                    return;
                }

                int processed = reports.RunDueScheduledReports();
                if (processed > 0)
                    Logger.AddInfoSubModule(SubModule, $"Reportes programados procesados: {processed}");
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error ejecutando reportes programados: {ex.Message}");
                _logger.LogError(ex, "Error ejecutando reportes programados");
            }
        }
    }
}
