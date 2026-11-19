using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using AxLogger;
using GymService.Tools;

namespace GymService.Services
{
    /// <summary>
    /// Manages the Kestrel web server lifecycle.
    /// Follows the pattern of PilotTmsService WebServerService.
    /// </summary>
    public class WebServerService
    {
        private IConfiguration _configuration;
        private IHost? _host;

        public WebServerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Start the Kestrel web server
        /// </summary>
        public void Start()
        {
            try
            {
                var port = int.Parse(_configuration["WebServer:Port"] ?? "5000");
                var urls = _configuration["WebServer:Urls"] ?? $"http://localhost:{port}";
                var corsOrigins = (_configuration["WebServer:CorsOrigins"] ?? "http://localhost:3000")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                Logger.AddInfo($"Configurando Kestrel en: {urls}");
                Logger.AddInfo($"CORS origins: {string.Join(", ", corsOrigins)}");

                _host = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        // Como servicio Windows el directorio actual es System32;
                        // el contenido (appsettings, wwwroot, logs) vive junto al exe.
                        webBuilder.UseContentRoot(AppContext.BaseDirectory);
                        webBuilder.UseKestrel(options =>
                        {
                            // Configure Kestrel settings
                            options.Limits.MaxConcurrentConnections = 1000;
                            options.Limits.MaxConcurrentUpgradedConnections = 100;
                        });
                        webBuilder.UseUrls(urls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        webBuilder.Configure(app => ConfigureApp(app, corsOrigins));
                        webBuilder.ConfigureServices((context, services) => ConfigureServices(services));
                    })
                    .Build();

                _host.Start();

                Logger.AddInfo($"Servidor Kestrel iniciado en {urls}");
                Logger.AddInfo($"Swagger disponible en http://localhost:{port}/swagger");
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule("WebServerService", $"Error al iniciar Kestrel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop the Kestrel web server
        /// </summary>
        public void Stop()
        {
            try
            {
                if (_host != null)
                {
                    _host.StopAsync().Wait(5000);
                    _host.Dispose();
                    _host = null;
                }

                Logger.AddInfo("Servidor Kestrel detenido");
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule("WebServerService", $"Error al detener Kestrel: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure services
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // Add controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Permite que los Dictionary<string, object> reciban primitivos CLR
                    // en lugar de JsonElement (necesario para Convert.ToXxx en los módulos).
                    options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
                });

            // Add Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "GymService API",
                    Version = "v1",
                    Description = "API REST para el sistema de gestión del gimnasio Centro Fit",
                    Contact = new OpenApiContact
                    {
                        Name = "GymService Support",
                        Email = "support@centrofit.com"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proprietary",
                        Url = new Uri("https://centrofit.com/license")
                    }
                });

                // Configure JWT Bearer authentication in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Ingresa 'Bearer' seguido de un espacio y tu token JWT. Ejemplo: Bearer eyJhbG..."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Add CORS
            var corsOrigins = (_configuration["WebServer:CorsOrigins"] ?? "http://localhost:3000")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder.WithOrigins(corsOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });

            // Add Health Checks
            services.AddHealthChecks()
                .AddCheck<GymHealthCheck>("gym-health", tags: new[] { "health" })
                .AddCheck<GymReadinessCheck>("gym-readiness", tags: new[] { "ready" });

            // Limpieza periodica de datos (sesiones, OTP, codigos y membresias)
            if (DataCenter.DatabaseManager != null)
            {
                services.AddSingleton(_ => new MaintenanceService(DataCenter.DatabaseManager.Tools, _configuration));
                services.AddHostedService<CleanupHostedService>();
            }

            // Add Rate Limiting
            services.AddRateLimiter(options =>
            {
                // Global rejection handler
                options.OnRejected = async (context, cancellationToken) =>
                {
                    var httpContext = context.HttpContext;
                    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    httpContext.Response.ContentType = "application/json";

                    var retryAfter = "";
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterMetadata))
                    {
                        retryAfter = ((long)Math.Ceiling(retryAfterMetadata.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
                    }

                    httpContext.Response.Headers.RetryAfter = retryAfter;

                    var response = new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["message"] = "Demasiadas solicitudes. Por favor, intenta más tarde.",
                        ["retryAfter"] = retryAfter
                    };

                    if (!string.IsNullOrEmpty(retryAfter))
                    {
                        response["retryAfterSeconds"] = int.Parse(retryAfter);
                    }

                    Logger.AddWarningSubModule("RateLimiter",
                        $"Rate limit exceeded for IP: {httpContext.Connection.RemoteIpAddress}");

                    await httpContext.Response.WriteAsync(
                        JsonSerializer.Serialize(response), cancellationToken);
                };

                // General API policy: 100 requests per minute per IP.
                options.AddPolicy("general", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            QueueLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));

                // Auth policy: 20 requests per minute per IP (login, bootstrap, etc.).
                options.AddPolicy("auth", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            QueueLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));

                // Strict policy for OTP generation: 5 requests per minute per IP.
                options.AddPolicy("strict", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
            });
        }

        /// <summary>
        /// Configure the application pipeline
        /// </summary>
        private void ConfigureApp(IApplicationBuilder app, string[] corsOrigins)
        {
            // Enable Swagger in development
            if (app.ServerFeatures != null)
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GymService API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            // Enable CORS
            app.UseCors("AllowFrontend");

            // Panel web (si se publicó junto al servicio): archivos estáticos y SPA.
            var indexFile = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");
            if (File.Exists(indexFile))
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
                Logger.AddInfo($"Panel web habilitado desde {Path.GetDirectoryName(indexFile)}");
            }

            // Enable routing
            app.UseRouting();

            // Rate limiting middleware (before other middleware)
            app.UseRateLimiter();

            // Role-based permission middleware (before controllers)
            app.UseMiddleware<RolePermissionMiddleware>();

            // Enable authentication (placeholder for now)
            // app.UseAuthentication();
            // app.UseAuthorization();

            // Map health check endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // /health - Full health check with detailed status
                endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    ResponseWriter = WriteHealthCheckResponse
                });
                
                // /ready - Lightweight readiness check
                endpoints.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => check.Name == "gym-readiness",
                    ResponseWriter = WriteHealthCheckResponse
                });
                
                // /health/live - Liveness probe (always healthy if running)
                endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    ResponseWriter = WriteHealthCheckResponse
                });

                // SPA: cualquier ruta del panel devuelve index.html. Las rutas de API
                // y archivos inexistentes mantienen 404.
                if (File.Exists(indexFile))
                {
                    endpoints.MapFallback(async context =>
                    {
                        var path = context.Request.Path;

                        if (path.StartsWithSegments("/api") ||
                            path.StartsWithSegments("/swagger") ||
                            path.StartsWithSegments("/health") ||
                            path.StartsWithSegments("/ready") ||
                            Path.HasExtension(path.Value))
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.SendFileAsync(indexFile);
                    });
                }
            });

            Logger.AddInfo("Pipeline de aplicación configurado");
        }

        /// <summary>
        /// Custom health check response writer that returns detailed JSON.
        /// </summary>
        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var status = report.Status switch
            {
                HealthStatus.Healthy => "healthy",
                HealthStatus.Degraded => "degraded",
                HealthStatus.Unhealthy => "unhealthy",
                _ => "unknown"
            };

            var response = new Dictionary<string, object>
            {
                ["status"] = status,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["duration"] = report.TotalDuration.TotalMilliseconds.ToString("F2") + "ms",
                ["components"] = new Dictionary<string, object>()
            };

            foreach (var entry in report.Entries)
            {
                var component = new Dictionary<string, object>
                {
                    ["status"] = entry.Value.Status.ToString().ToLower(),
                    ["description"] = entry.Value.Description ?? "",
                    ["duration"] = entry.Value.Duration.TotalMilliseconds.ToString("F2") + "ms"
                };

                if (entry.Value.Data != null && entry.Value.Data.Count > 0)
                {
                    component["data"] = new Dictionary<string, object>(entry.Value.Data);
                }

                if (entry.Value.Exception != null)
                {
                    component["error"] = entry.Value.Exception.Message;
                }

                ((Dictionary<string, object>)response["components"])[entry.Key] = component;
            }

            // Set HTTP status code based on health status
            context.Response.StatusCode = report.Status switch
            {
                HealthStatus.Healthy => StatusCodes.Status200OK,
                HealthStatus.Degraded => StatusCodes.Status200OK,
                HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return context.Response.WriteAsync(json);
        }
    }
}
