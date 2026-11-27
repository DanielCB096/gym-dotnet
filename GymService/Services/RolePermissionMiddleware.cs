using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GymService.Tools;
using AxLogger;

namespace GymService.Services
{
    /// <summary>
    /// Middleware that validates user permissions based on the requested endpoint.
    /// Uses the permission matrix defined in PermissionMatrix to check if the user
    /// has the required permission for the route.
    /// </summary>
    public class RolePermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RolePermissionMiddleware> _logger;

        public RolePermissionMiddleware(RequestDelegate next, ILogger<RolePermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            var method = context.Request.Method.ToUpperInvariant();

            // Skip health checks, Swagger, and auth endpoints
            if (path.StartsWith("/health") || path.StartsWith("/ready") || 
                path.StartsWith("/swagger") || path.StartsWith("/api/auth") ||
                path.StartsWith("/api/access/code/validate") ||
                path.StartsWith("/api/kiosk/device/validate") ||
                path.StartsWith("/api/kiosk/equipment/unavailable"))
            {
                await _next(context);
                return;
            }

            // Determine required permission from the path
            var requiredPermission = PermissionMatrix.GetRequiredPermission(path, method);

            // If no permission is defined for this route, allow access.
            // Excepción de seguridad: una ruta /api sin entrada en la matriz no debe
            // pasar sin permiso; se deniega y se registra (ver PermissionCoverageTests).
            if (string.IsNullOrEmpty(requiredPermission))
            {
                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.AddErrorSubModule("RolePermissionMiddleware",
                        $"Ruta de API sin permiso registrado: {method} {path}");

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var denied = JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "Ruta de API no autorizada"
                    });
                    await context.Response.WriteAsync(denied);
                    return;
                }

                await _next(context);
                return;
            }

            // Validate authentication first
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var response = JsonSerializer.Serialize(new { success = false, message = "Token de autenticación requerido" });
                await context.Response.WriteAsync(response);
                return;
            }

            // Extract and validate token
            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (DataCenter.BusinessProcess.Identity == null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                var response = JsonSerializer.Serialize(new { success = false, message = "Servicio de identidad no disponible" });
                await context.Response.WriteAsync(response);
                return;
            }

            var isValid = DataCenter.BusinessProcess.Identity.ValidateSessionToken(token, out var userData);
            if (!isValid || userData == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var response = JsonSerializer.Serialize(new { success = false, message = "Token inválido o expirado" });
                await context.Response.WriteAsync(response);
                return;
            }

            // Check permission
            var userRole = SecurityValidation.GetUserRole(userData);
            if (!SecurityValidation.HasPermission(userData, requiredPermission))
            {
                _logger.LogWarning($"User '{userData["username"]}' (role: {userRole}) denied access to {method} {path} — requires '{requiredPermission}'");
                Logger.AddWarningSubModule("RolePermissionMiddleware", 
                    $"Acceso denegado: usuario '{userData["username"]}' (rol: {userRole}) intentó acceder a {method} {path} — requiere '{requiredPermission}'");

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var response = JsonSerializer.Serialize(new 
                { 
                    success = false, 
                    message = "No tienes permiso para realizar esta acción",
                    requiredPermission = requiredPermission,
                    userRole = userRole
                });
                await context.Response.WriteAsync(response);
                return;
            }

            // Permission granted — continue
            await _next(context);
        }
    }

    /// <summary>
    /// Permission matrix that maps API routes to required permissions.
    /// This is the single source of truth for endpoint authorization.
    /// </summary>
    public static class PermissionMatrix
    {
        // Permission definitions by role
        public static readonly Dictionary<string, HashSet<string>> RolePermissions = new()
        {
            ["OWNER"] = new HashSet<string> { "*" },  // Full access
            ["ADMIN"] = new HashSet<string> 
            {
                // Members
                "members.view", "members.create", "members.edit", "members.delete",
                // Access
                "access.view", "access.override",
                // Sales
                "sales.view", "sales.create", "sales.return",
                // Inventory
                "inventory.view", "inventory.edit",
                // Classes
                "classes.view", "classes.manage",
                // Routines
                "routines.view", "routines.manage",
                // Fisio
                "fisio.view", "fisio.manage",
                // Maintenance
                "maintenance.view", "maintenance.manage",
                // Billing
                "billing.view", "billing.create",
                // Finance
                "finance.view", "finance.reconcile",
                // Reports
                "reports.view",
                // Staff
                "staff.view", "staff.manage",
                // Auditoría
                "audit.view",
                // Admin
                "admin.settings"
            },
            ["STAFF"] = new HashSet<string> 
            {
                // Members
                "members.view", "members.create", "members.edit",
                // Access
                "access.view",
                // Classes
                "classes.view", "classes.manage",
                // Routines
                "routines.view", "routines.manage",
                // Fisio
                "fisio.view", "fisio.manage",
                // Maintenance
                "maintenance.view", "maintenance.manage",
                // Reports
                "reports.view"
            },
            ["CASHIER"] = new HashSet<string> 
            {
                // Members
                "members.view",
                // Sales
                "sales.view", "sales.create", "sales.return",
                // Inventory
                "inventory.view",
                // Billing
                "billing.view"
            }
        };

        // Route to permission mapping
        private static readonly Dictionary<string, Dictionary<string, string>> RoutePermissions = new()
        {
            // Members
            ["/api/members"] = new Dictionary<string, string>
            {
                ["GET"] = "members.view",
                ["POST"] = "members.create",
                ["PUT"] = "members.edit",
                ["DELETE"] = "members.delete"
            },
            ["/api/members/membership"] = new Dictionary<string, string>
            {
                ["POST"] = "members.create"
            },
            ["/api/members/photo"] = new Dictionary<string, string>
            {
                ["POST"] = "members.edit"
            },
            ["/api/members/freeze"] = new Dictionary<string, string>
            {
                ["POST"] = "members.edit"
            },
            ["/api/members/payment"] = new Dictionary<string, string>
            {
                ["POST"] = "members.edit"
            },
            ["/api/members/collections"] = new Dictionary<string, string>
            {
                ["GET"] = "members.view"
            },
            ["/api/members/plans"] = new Dictionary<string, string>
            {
                ["GET"] = "members.view",
                ["POST"] = "members.edit",
                ["PUT"] = "members.edit",
                ["DELETE"] = "members.edit"
            },
            // Access
            ["/api/access/generate"] = new Dictionary<string, string>
            {
                ["POST"] = "access.view"
            },
            ["/api/access/validate"] = new Dictionary<string, string>
            {
                ["POST"] = "access.view"
            },
            ["/api/access/logs"] = new Dictionary<string, string>
            {
                ["GET"] = "access.view"
            },
            ["/api/access/clean"] = new Dictionary<string, string>
            {
                ["DELETE"] = "access.override"
            },
            // Sales
            ["/api/sales"] = new Dictionary<string, string>
            {
                ["GET"] = "sales.view",
                ["POST"] = "sales.create",
                ["DELETE"] = "sales.return"
            },
            ["/api/sales/return"] = new Dictionary<string, string>
            {
                ["POST"] = "sales.return"
            },
            // Inventory
            ["/api/inventory"] = new Dictionary<string, string>
            {
                ["GET"] = "inventory.view",
                ["POST"] = "inventory.edit",
                ["PUT"] = "inventory.edit",
                ["DELETE"] = "inventory.edit"
            },
            ["/api/inventory/adjust"] = new Dictionary<string, string>
            {
                ["POST"] = "inventory.edit"
            },
            ["/api/suppliers"] = new Dictionary<string, string>
            {
                ["GET"] = "inventory.view",
                ["POST"] = "inventory.edit",
                ["PUT"] = "inventory.edit",
                ["DELETE"] = "inventory.edit"
            },
            ["/api/purchase-orders"] = new Dictionary<string, string>
            {
                ["GET"] = "inventory.view",
                ["POST"] = "inventory.edit",
                ["PUT"] = "inventory.edit",
                ["DELETE"] = "inventory.edit"
            },
            // Caja / turnos
            ["/api/cash-sessions"] = new Dictionary<string, string>
            {
                ["GET"] = "sales.view",
                ["POST"] = "sales.create",
                ["PUT"] = "sales.create",
                ["DELETE"] = "sales.create"
            },
            // Classes
            ["/api/classes"] = new Dictionary<string, string>
            {
                ["GET"] = "classes.view",
                ["POST"] = "classes.manage",
                ["PUT"] = "classes.manage",
                ["DELETE"] = "classes.manage"
            },
            ["/api/classes/schedule"] = new Dictionary<string, string>
            {
                ["GET"] = "classes.view",
                ["POST"] = "classes.manage"
            },
            ["/api/classes/sessions"] = new Dictionary<string, string>
            {
                ["GET"] = "classes.view",
                ["POST"] = "classes.manage",
                ["PUT"] = "classes.manage",
                ["DELETE"] = "classes.manage"
            },
            ["/api/classes/reservations"] = new Dictionary<string, string>
            {
                ["PUT"] = "classes.manage",
                ["DELETE"] = "classes.manage"
            },
            // Routines
            ["/api/routines"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["POST"] = "routines.manage",
                ["PUT"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            ["/api/routines/exercises"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["POST"] = "routines.manage",
                ["PUT"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            ["/api/routines/plan-exercises"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["PUT"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            ["/api/routines/assignments"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["POST"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            ["/api/routines/workouts"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["POST"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            ["/api/routines/measurements"] = new Dictionary<string, string>
            {
                ["GET"] = "routines.view",
                ["POST"] = "routines.manage",
                ["DELETE"] = "routines.manage"
            },
            // Fisio
            ["/api/fisio"] = new Dictionary<string, string>
            {
                ["GET"] = "fisio.view",
                ["POST"] = "fisio.manage",
                ["PUT"] = "fisio.manage",
                ["DELETE"] = "fisio.manage"
            },
            ["/api/fisio/packages"] = new Dictionary<string, string>
            {
                ["GET"] = "fisio.view",
                ["POST"] = "fisio.manage",
                ["PUT"] = "fisio.manage",
                ["DELETE"] = "fisio.manage"
            },
            ["/api/fisio/appointments"] = new Dictionary<string, string>
            {
                ["GET"] = "fisio.view",
                ["POST"] = "fisio.manage",
                ["PUT"] = "fisio.manage",
                ["DELETE"] = "fisio.manage"
            },
            // Maintenance
            ["/api/maintenance"] = new Dictionary<string, string>
            {
                ["GET"] = "maintenance.view",
                ["POST"] = "maintenance.manage",
                ["PUT"] = "maintenance.manage",
                ["DELETE"] = "maintenance.manage"
            },
            ["/api/maintenance/equipment"] = new Dictionary<string, string>
            {
                ["GET"] = "maintenance.view",
                ["POST"] = "maintenance.manage",
                ["PUT"] = "maintenance.manage",
                ["DELETE"] = "maintenance.manage"
            },
            ["/api/maintenance/tickets"] = new Dictionary<string, string>
            {
                ["GET"] = "maintenance.view",
                ["POST"] = "maintenance.manage",
                ["PUT"] = "maintenance.manage",
                ["DELETE"] = "maintenance.manage"
            },
            // Billing
            ["/api/billing"] = new Dictionary<string, string>
            {
                ["GET"] = "billing.view",
                ["POST"] = "billing.create",
                ["PUT"] = "billing.create",
                ["DELETE"] = "billing.create"
            },
            // Finance
            ["/api/finance"] = new Dictionary<string, string>
            {
                ["GET"] = "finance.view",
                ["POST"] = "finance.reconcile",
                ["PUT"] = "finance.reconcile",
                ["DELETE"] = "finance.reconcile"
            },
            // Reports
            ["/api/reports"] = new Dictionary<string, string>
            {
                ["GET"] = "reports.view",
                ["POST"] = "reports.view",
                ["PUT"] = "reports.view",
                ["DELETE"] = "reports.view"
            },
            ["/api/dashboard"] = new Dictionary<string, string>
            {
                ["GET"] = "reports.view"
            },
            // Auditoría de seguridad
            ["/api/audit"] = new Dictionary<string, string>
            {
                ["GET"] = "audit.view"
            },
            // Kiosk
            ["/api/kiosk/register"] = new Dictionary<string, string>
            {
                ["POST"] = "admin.settings"
            },
            ["/api/kiosk/validate"] = new Dictionary<string, string>
            {
                ["POST"] = "access.view"
            },
            ["/api/kiosk/devices"] = new Dictionary<string, string>
            {
                ["GET"] = "admin.settings",
                ["POST"] = "admin.settings",
                ["PUT"] = "admin.settings"
            },
            // Staff
            ["/api/staff"] = new Dictionary<string, string>
            {
                ["GET"] = "staff.view",
                ["POST"] = "staff.manage",
                ["PUT"] = "staff.manage",
                ["DELETE"] = "staff.manage"
            },
            ["/api/staff/users"] = new Dictionary<string, string>
            {
                ["GET"] = "staff.view",
                ["POST"] = "staff.manage",
                ["PUT"] = "staff.manage"
            },
            ["/api/staff/users/all"] = new Dictionary<string, string>
            {
                ["GET"] = "staff.view"
            },
            ["/api/staff/push-subscriptions"] = new Dictionary<string, string>
            {
                ["GET"] = "staff.view"
            },
            // Memberships (freeze/resume)
            ["/api/memberships"] = new Dictionary<string, string>
            {
                ["POST"] = "members.edit"
            },
            // Access codes (rutas reales por debajo de /api/access)
            ["/api/access"] = new Dictionary<string, string>
            {
                ["GET"] = "access.view",
                ["POST"] = "access.view",
                ["PUT"] = "access.override",
                ["DELETE"] = "access.override"
            },
            ["/api/access/manual"] = new Dictionary<string, string>
            {
                ["POST"] = "access.override"
            },
            // Kiosco (validate es público y se omite antes de este punto)
            ["/api/kiosk"] = new Dictionary<string, string>
            {
                ["GET"] = "access.view",
                ["POST"] = "access.override",
                ["PUT"] = "access.override",
                ["DELETE"] = "access.override"
            },
            // Importación de datos
            ["/api/import"] = new Dictionary<string, string>
            {
                ["GET"] = "members.view",
                ["POST"] = "members.create",
                ["PUT"] = "members.edit",
                ["DELETE"] = "members.delete"
            },
            ["/api/import/members"] = new Dictionary<string, string>
            {
                ["POST"] = "members.create"
            },
            ["/api/import/inventory"] = new Dictionary<string, string>
            {
                ["POST"] = "inventory.edit"
            },
            ["/api/import/templates/members"] = new Dictionary<string, string>
            {
                ["GET"] = "members.view"
            },
            ["/api/import/templates/inventory"] = new Dictionary<string, string>
            {
                ["GET"] = "inventory.view"
            }
        };

        /// <summary>
        /// Get the required permission for a given route and HTTP method.
        /// Returns null if no permission is required for this route.
        /// </summary>
        public static string? GetRequiredPermission(string path, string method)
        {
            // La devolución de una venta tiene su propio permiso aunque la ruta
            // incluya el ID: /api/sales/{id}/return.
            if (path.StartsWith("/api/sales/", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith("/return", StringComparison.OrdinalIgnoreCase))
            {
                return method == "POST" ? "sales.return" : null;
            }

            // Se usa la ruta registrada más específica que sea prefijo del path.
            // Así también quedan protegidas subrutas con nombre (accounts, invoices,
            // templates, devices, etc.) y no solo las que terminan en un ID numérico.
            Dictionary<string, string>? bestMethods = null;
            string? bestRoute = null;

            foreach (var (route, routeMethods) in RoutePermissions)
            {
                if (!path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (bestRoute == null || route.Length > bestRoute.Length)
                {
                    bestRoute = route;
                    bestMethods = routeMethods;
                }
            }

            return bestMethods?.GetValueOrDefault(method);
        }
    }
}
