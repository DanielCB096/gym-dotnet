using System.Reflection;
using System.Text.RegularExpressions;
using GymService.Controllers;
using GymService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace GymService.Tests;

public class PermissionCoverageTests
{
    /// <summary>
    /// Regla del proyecto: toda ruta de API debe tener su entrada en PermissionMatrix.
    /// Si no la tiene, el middleware la deja pasar sin comprobar permisos.
    /// </summary>
    [Fact]
    public void Every_Controller_Route_Has_A_Permission()
    {
        var missing = new List<string>();
        var assembly = typeof(AuthController).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ControllerBase))))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var routes = method.GetCustomAttributes<RouteAttribute>(inherit: false).ToList();
                var verbs = method.GetCustomAttributes<HttpMethodAttribute>(inherit: false).ToList();

                if (routes.Count == 0 || verbs.Count == 0)
                    continue;

                foreach (var route in routes)
                {
                    if (string.IsNullOrWhiteSpace(route.Template))
                        continue;

                    string path = "/" + route.Template.TrimStart('/');
                    path = Regex.Replace(path, "\\{[^}]+\\}", "1");

                    foreach (var verb in verbs)
                    {
                        string httpMethod = verb.HttpMethods.FirstOrDefault() ?? "GET";

                        if (IsPublicRoute(path))
                            continue;

                        if (PermissionMatrix.GetRequiredPermission(path.ToLowerInvariant(), httpMethod) == null)
                            missing.Add($"{httpMethod} {path} ({type.Name}.{method.Name})");
                    }
                }
            }
        }

        Assert.True(missing.Count == 0,
            "Rutas sin permiso en RolePermissionMiddleware:\n" + string.Join("\n", missing.Distinct()));
    }

    private static bool IsPublicRoute(string path)
    {
        return path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/access/code/validate", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/kiosk/device/validate", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/kiosk/equipment/unavailable", StringComparison.OrdinalIgnoreCase);
    }
}
