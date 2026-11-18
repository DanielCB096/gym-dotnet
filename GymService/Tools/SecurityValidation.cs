using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AxLogger;

namespace GymService.Tools
{
    /// <summary>
    /// Security validation utilities for authentication and authorization.
    /// Follows the pattern of PilotTmsService SecurityValidation.
    /// </summary>
    public static class SecurityValidation
    {
        /// <summary>
        /// Validate authentication token from request header
        /// </summary>
        public static bool ValidateAuthentication(HttpRequest request, HttpResponse response, out Dictionary<string, object>? userData, out string errorMessage)
        {
            userData = null;
            errorMessage = string.Empty;

            try
            {
                // Get authorization header
                var authHeader = request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Token de autenticación no proporcionado";
                    Logger.AddWarningSubModule("SecurityValidation", errorMessage);
                    return false;
                }

                // Extract token
                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (string.IsNullOrEmpty(token))
                {
                    errorMessage = "Token de autenticación vacío";
                    Logger.AddWarningSubModule("SecurityValidation", errorMessage);
                    return false;
                }

                // Validate token with identity module
                if (DataCenter.BusinessProcess.Identity == null)
                {
                    errorMessage = "Módulo de identidad no inicializado";
                    Logger.AddErrorSubModule("SecurityValidation", errorMessage);
                    return false;
                }

                var result = DataCenter.BusinessProcess.Identity.ValidateSessionToken(token, out userData);
                if (!result)
                {
                    errorMessage = "Token de autenticación inválido o expirado";
                    Logger.AddWarningSubModule("SecurityValidation", $"Token inválido: {token.Substring(0, Math.Min(20, token.Length))}...");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error al validar autenticación: {ex.Message}";
                Logger.AddErrorDevExceptionSubModule("SecurityValidation", errorMessage);
                return false;
            }
        }

        /// <summary>
        /// Check if user has required permission
        /// </summary>
        public static bool HasPermission(Dictionary<string, object> userData, string requiredPermission)
        {
            try
            {
                if (!userData.ContainsKey("permissions"))
                    return false;

                var permissions = userData["permissions"] as List<string>;
                if (permissions == null)
                    return false;

                // Check for exact match or wildcard
                return permissions.Contains(requiredPermission) || permissions.Contains("*");
            }
            catch (Exception ex)
            {
                Logger.AddErrorDevExceptionSubModule("SecurityValidation", $"Error al verificar permiso: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if user has any of the required permissions
        /// </summary>
        public static bool HasAnyPermission(Dictionary<string, object> userData, params string[] requiredPermissions)
        {
            return requiredPermissions.Any(p => HasPermission(userData, p));
        }

        /// <summary>
        /// Get user ID from user data
        /// </summary>
        public static long GetUserId(Dictionary<string, object> userData)
        {
            if (userData.ContainsKey("userId"))
                return Convert.ToInt64(userData["userId"]);

            throw new InvalidOperationException("userId no encontrado en userData");
        }

        /// <summary>
        /// Get user role from user data
        /// </summary>
        public static string GetUserRole(Dictionary<string, object> userData)
        {
            if (userData.ContainsKey("role"))
                return Convert.ToString(userData["role"]) ?? string.Empty;

            return string.Empty;
        }

        /// <summary>
        /// Generate SHA-256 hash
        /// </summary>
        public static string Sha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generate random token
        /// </summary>
        public static string GenerateToken(int length = 32)
        {
            var randomBytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Generate OTP code
        /// </summary>
        public static string GenerateOtp(int length = 6)
        {
            var digits = "0123456789";
            var otp = new char[length];
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[1];

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(bytes);
                otp[i] = digits[bytes[0] % 10];
            }

            return new string(otp);
        }

        /// <summary>
        /// Set unauthorized response
        /// </summary>
        public static void SetUnauthorizedResponse(HttpResponse response, string message)
        {
            response.StatusCode = StatusCodes.Status401Unauthorized;
            response.ContentType = "application/json";
        }

        /// <summary>
        /// Set forbidden response
        /// </summary>
        public static void SetForbiddenResponse(HttpResponse response, string message)
        {
            response.StatusCode = StatusCodes.Status403Forbidden;
            response.ContentType = "application/json";
        }
    }
}
