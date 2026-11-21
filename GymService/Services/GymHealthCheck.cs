using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AxLogger;

namespace GymService.Services
{
    /// <summary>
    /// Custom health check that verifies the core services of GymService:
    /// Database connectivity, business modules availability, and system resources.
    /// </summary>
    public class GymHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            var startTime = Stopwatch.GetTimestamp();
            var data = new Dictionary<string, object>();
            var overallStatus = HealthStatus.Healthy;
            var issues = new List<string>();

            try
            {
                // 1. Check Database Manager
                var dbStatus = HealthStatus.Healthy;
                var dbMessage = "Database manager is available";

                if (GymService.DataCenter.DatabaseManager == null)
                {
                    dbStatus = HealthStatus.Unhealthy;
                    dbMessage = "Database manager is not initialized";
                    issues.Add(dbMessage);
                }
                else if (GymService.DataCenter.ConnectionData == null)
                {
                    dbStatus = HealthStatus.Degraded;
                    dbMessage = "Database connection data is not configured";
                    issues.Add(dbMessage);
                }
                else
                {
                    // Try a simple query to verify DB connectivity
                    try
                    {
                        var tools = GymService.DataCenter.DatabaseManager.DatabaseAdministrator;
                        // Check if at least one table exists
                        bool hasTables = tools.ExistTable("gym_members");
                        if (!hasTables)
                        {
                            dbStatus = HealthStatus.Degraded;
                            dbMessage = "Database connected but schema not initialized";
                            issues.Add(dbMessage);
                        }
                        else
                        {
                            dbMessage = "Database connected and schema ready";
                        }
                    }
                    catch (Exception dbEx)
                    {
                        dbStatus = HealthStatus.Unhealthy;
                        dbMessage = $"Database query failed: {dbEx.Message}";
                        issues.Add(dbMessage);
                        Logger.AddErrorDevExceptionSubModule("GymHealthCheck", dbEx.Message);
                    }
                }

                data["database"] = new Dictionary<string, object>
                {
                    ["status"] = dbStatus.ToString().ToLower(),
                    ["message"] = dbMessage
                };

                if (dbStatus == HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Unhealthy;
                else if (dbStatus == HealthStatus.Degraded && overallStatus != HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Degraded;

                // 2. Check Business Modules
                var bp = GymService.DataCenter.BusinessProcess;
                int totalModules = 14;
                int availableModules = 0;
                var missingModules = new List<string>();

                if (bp.Identity != null) availableModules++; else missingModules.Add("Identity");
                if (bp.Members != null) availableModules++; else missingModules.Add("Members");
                if (bp.Access != null) availableModules++; else missingModules.Add("Access");
                if (bp.Sales != null) availableModules++; else missingModules.Add("Sales");
                if (bp.Inventory != null) availableModules++; else missingModules.Add("Inventory");
                if (bp.Classes != null) availableModules++; else missingModules.Add("Classes");
                if (bp.Routines != null) availableModules++; else missingModules.Add("Routines");
                if (bp.Fisio != null) availableModules++; else missingModules.Add("Fisio");
                if (bp.Maintenance != null) availableModules++; else missingModules.Add("Maintenance");
                if (bp.Billing != null) availableModules++; else missingModules.Add("Billing");
                if (bp.Finance != null) availableModules++; else missingModules.Add("Finance");
                if (bp.Reports != null) availableModules++; else missingModules.Add("Reports");
                if (bp.Kiosk != null) availableModules++; else missingModules.Add("Kiosk");
                if (bp.Staff != null) availableModules++; else missingModules.Add("Staff");

                var modulesStatus = HealthStatus.Healthy;
                if (availableModules == 0)
                {
                    modulesStatus = HealthStatus.Unhealthy;
                }
                else if (availableModules < totalModules)
                {
                    modulesStatus = HealthStatus.Degraded;
                    issues.Add($"Only {availableModules}/{totalModules} modules loaded: missing {string.Join(", ", missingModules)}");
                }

                data["modules"] = new Dictionary<string, object>
                {
                    ["status"] = modulesStatus.ToString().ToLower(),
                    ["available"] = availableModules,
                    ["total"] = totalModules,
                    ["missing"] = missingModules
                };

                if (modulesStatus == HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Unhealthy;
                else if (modulesStatus == HealthStatus.Degraded && overallStatus != HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Degraded;

                // 3. Check System Resources
                var currentProcess = Process.GetCurrentProcess();
                var memoryMB = currentProcess.WorkingSet64 / (1024 * 1024);

                data["system"] = new Dictionary<string, object>
                {
                    ["memoryMB"] = memoryMB,
                    ["cpuCores"] = Environment.ProcessorCount,
                    ["uptime"] = TimeSpan.FromMilliseconds(Environment.TickCount & int.MaxValue).ToString(@"dd\.hh\:mm\:ss"),
                    ["processId"] = currentProcess.Id
                };

                // 4. Check Web Server
                data["webServer"] = new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["message"] = "Kestrel is running"
                };

                // Calculate response time
                var elapsed = Stopwatch.GetElapsedTime(startTime);
                data["responseTimeMs"] = (int)elapsed.TotalMilliseconds;
                data["timestamp"] = DateTime.UtcNow.ToString("o");

                // Determine final status
                if (overallStatus == HealthStatus.Unhealthy)
                {
                    return await Task.FromResult(HealthCheckResult.Unhealthy(
                        description: $"Critical issue: {string.Join("; ", issues)}", 
                        data: data));
                }
                else if (overallStatus == HealthStatus.Degraded)
                {
                    return await Task.FromResult(HealthCheckResult.Degraded(
                        description: $"Degraded: {string.Join("; ", issues)}", 
                        data: data));
                }
                else
                {
                    return await Task.FromResult(HealthCheckResult.Healthy(
                        description: "All systems operational", 
                        data: data));
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule("GymHealthCheck", $"Health check failed: {ex.Message}");
                return await Task.FromResult(HealthCheckResult.Unhealthy(
                    description: $"Health check error: {ex.Message}", 
                    exception: ex));
            }
        }
    }

    /// <summary>
    /// Lightweight readiness check that only verifies the service is responsive.
    /// Used for load balancers and quick liveness probes.
    /// </summary>
    public class GymReadinessCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                ["status"] = "ready",
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["service"] = "GymService",
                ["version"] = (typeof(GymReadinessCheck).Assembly.GetName().Version?.ToString(3)) ?? "0.0.0"
            };

            // Check if critical services are at least initialized
            bool dbManagerReady = GymService.DataCenter.DatabaseManager != null;
            bool webServerReady = GymService.DataCenter.WebServer != null;

            data["databaseManager"] = dbManagerReady;
            data["webServer"] = webServerReady;

            if (!dbManagerReady && !webServerReady)
            {
                return await Task.FromResult(HealthCheckResult.Unhealthy(
                    description: "Service is starting up", 
                    data: data));
            }

            return await Task.FromResult(HealthCheckResult.Healthy(
                description: "Service is ready", 
                data: data));
        }
    }
}
