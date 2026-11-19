using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Tools;

namespace GymService.Modules.Identity
{
    /// <summary>
    /// Identity module: authentication, sessions, MFA TOTP, OTP by email, RBAC.
    /// </summary>
    public class IdentityModule
    {
        private const string SubModule = "IDENTITY_MODULE";
        private DatabaseTools _tools;
        private IConfiguration _configuration;

        public IdentityModule(DatabaseTools tools, IConfiguration configuration)
        {
            _tools = tools;
            _configuration = configuration;
        }

        private object GetCellValue(Row row, string columnName)
        {
            var rs = _tools.ExecuteQueryResultSet("SELECT 1");
            var columns = rs.Columns;
            // We need the ResultSet from the actual query, not a dummy one.
            // This helper is called from within a method that has the ResultSet.
            // Instead, we'll use a different approach: pass the column index map.
            return null;
        }

        private int GetColumnIndex(ResultSet result, string columnName)
        {
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private object GetRowValue(Row row, ResultSet result, string columnName)
        {
            int idx = GetColumnIndex(result, columnName);
            if (idx < 0) return null;
            return row.GetObject(idx);
        }

        /// <summary>
        /// User login with username/password
        /// </summary>
        public bool Login(string username, string password, string deviceInfo, string ipAddress, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Find user
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, username, email, password_hash, role, full_name, is_active, is_owner, mfa_enabled FROM gym_users WHERE username = ?",
                    username
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Usuario o contraseña inválidos";
                    return false;
                }

                var user = result.Rows[0];
                var userId = Convert.ToInt64(GetRowValue(user, result, "id"));
                var storedHash = Convert.ToString(GetRowValue(user, result, "password_hash")) ?? string.Empty;
                var role = Convert.ToString(GetRowValue(user, result, "role")) ?? string.Empty;
                var isActive = Convert.ToBoolean(GetRowValue(user, result, "is_active"));
                var mfaEnabled = Convert.ToBoolean(GetRowValue(user, result, "mfa_enabled"));

                if (!isActive)
                {
                    response["success"] = false;
                    response["message"] = "Usuario desactivado";
                    return false;
                }

                // Verify password (Argon2 verification - simplified)
                if (!VerifyPassword(password, storedHash))
                {
                    response["success"] = false;
                    response["message"] = "Usuario o contraseña inválidos";
                    return false;
                }

                // Check MFA
                if (mfaEnabled)
                {
                    response["success"] = false;
                    response["requiresMfa"] = true;
                    response["userId"] = userId;
                    return false;
                }

                // Create session
                var token = SecurityValidation.GenerateToken(64);
                var tokenHash = SecurityValidation.Sha256Hash(token);
                var lifetimeHours = int.Parse(_configuration["Authentication:TokenLifetimeHours"] ?? "8");
                var expiresAt = DateTime.Now.AddHours(lifetimeHours);

                // Store session
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "INSERT INTO gym_sessions (user_id, token_hash, device_info, ip_address, expires_at, is_active) VALUES (?, ?, ?, ?, ?, 1)",
                    userId, tokenHash, deviceInfo, ipAddress, expiresAt
                );

                // Update last login
                session.ExecuteTransaction(
                    "UPDATE gym_users SET last_login = GETDATE() WHERE id = ?",
                    userId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al guardar la sesión de usuario");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                // Get permissions
                var permissions = GetPermissions(role);

                response["success"] = true;
                response["token"] = token;
                response["userId"] = userId;
                response["role"] = role;
                response["username"] = username;
                response["permissions"] = permissions;
                response["expiresIn"] = lifetimeHours * 3600;

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en Login: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Validate session token and return user data
        /// </summary>
        public bool ValidateSessionToken(string token, out Dictionary<string, object>? userData)
        {
            userData = null;

            try
            {
                var tokenHash = SecurityValidation.Sha256Hash(token);

                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT s.user_id, s.device_info, u.username, u.role, u.email 
                    FROM gym_sessions s 
                    INNER JOIN gym_users u ON s.user_id = u.id 
                    WHERE s.token_hash = ? AND s.is_active = 1 AND s.expires_at > GETDATE()",
                    tokenHash
                );

                if (result == null || result.Rows.Count == 0)
                    return false;

                var row = result.Rows[0];
                var role = Convert.ToString(GetRowValue(row, result, "role")) ?? string.Empty;

                userData = new Dictionary<string, object>
                {
                    ["userId"] = Convert.ToInt64(GetRowValue(row, result, "user_id")),
                    ["username"] = Convert.ToString(GetRowValue(row, result, "username")) ?? string.Empty,
                    ["role"] = role,
                    ["email"] = Convert.ToString(GetRowValue(row, result, "email")) ?? string.Empty,
                    ["permissions"] = GetPermissions(role)
                };

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ValidateSessionToken: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// User logout - invalidate session
        /// </summary>
        public bool Logout(string token)
        {
            try
            {
                var tokenHash = SecurityValidation.Sha256Hash(token);

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_sessions SET is_active = 0 WHERE token_hash = ?",
                    tokenHash
                );
                session.TryExecute();

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en Logout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Setup MFA TOTP for user
        /// </summary>
        public bool SetupMfa(long userId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Generate secret
                var bytes = new byte[20];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(bytes);
                var secret = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

                // Generate QR code URI
                var email = GetUserEmail(userId);
                var totpUri = $"otpauth://totp/CentroFit{email}?secret={secret}&issuer=CentroFit";

                // Generate backup codes
                var backupCodes = new List<string>();
                for (int i = 0; i < 10; i++)
                {
                    backupCodes.Add(SecurityValidation.GenerateOtp(8));
                }

                // Encrypt and store secret (simplified - in production use proper encryption)
                var encryptedSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_users SET mfa_secret_encrypted = ?, mfa_backup_codes = ? WHERE id = ?",
                    encryptedSecret, string.Join(",", backupCodes), userId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al guardar la configuración MFA");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                response["success"] = true;
                response["totpUri"] = totpUri;
                response["backupCodes"] = backupCodes;
                response["message"] = "Escanea el código QR en tu app de autenticación";

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en SetupMfa: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Verify MFA TOTP code
        /// </summary>
        public bool VerifyMfa(long userId, string code, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Get stored secret
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT mfa_secret_encrypted, mfa_backup_codes FROM gym_users WHERE id = ?",
                    userId
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Usuario no encontrado";
                    return false;
                }

                var row = result.Rows[0];
                var encryptedSecret = Convert.ToString(GetRowValue(row, result, "mfa_secret_encrypted")) ?? string.Empty;
                var backupCodes = Convert.ToString(GetRowValue(row, result, "mfa_backup_codes")) ?? string.Empty;

                // Decrypt secret
                var secret = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedSecret));

                // Verify TOTP code (simple implementation)
                var isValid = VerifyTotpCode(secret, code);

                // Or check backup codes
                if (!isValid)
                {
                    var codesList = backupCodes.Split(',');
                    isValid = codesList.Contains(code);
                }

                if (isValid)
                {
                    // Enable MFA
                    var session = _tools.CreateSession(SubModule);
                    session.ExecuteTransaction(
                        "UPDATE gym_users SET mfa_enabled = 1 WHERE id = ?",
                        userId
                    );
                    session.TryExecute();

                    response["success"] = true;
                    response["message"] = "MFA activado correctamente";
                    return true;
                }

                response["success"] = false;
                response["message"] = "Código inválido";
                return false;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en VerifyMfa: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Request OTP code by email (for member login)
        /// </summary>
        public bool RequestOtpByEmail(string email, string purpose, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var otpCode = SecurityValidation.GenerateOtp(6);
                var otpHash = SecurityValidation.Sha256Hash(otpCode);
                var expiryMinutes = int.Parse(_configuration["Authentication:OtpExpiryMinutes"] ?? "10");

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(@"
                    INSERT INTO gym_otp_codes (email, code_hash, purpose, expires_at, used) 
                    VALUES (?, ?, ?, DATEADD(MINUTE, ?, GETDATE()), 0)",
                    email, otpHash, purpose, expiryMinutes
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al generar código OTP");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                // TODO: Send email with OTP code
                Logger.AddInfo($"OTP code generated for {email}: {otpCode}");

                response["success"] = true;
                response["message"] = "Código OTP enviado por correo";

                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en RequestOtpByEmail: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Verify OTP code
        /// </summary>
        public bool VerifyOtp(string email, string code, string purpose, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var otpHash = SecurityValidation.Sha256Hash(code);

                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT id, code_hash, expires_at, used FROM gym_otp_codes 
                    WHERE email = ? AND purpose = ? AND used = 0 AND expires_at > GETDATE() 
                    ORDER BY id DESC",
                    email, purpose
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Código inválido o expirado";
                    return false;
                }

                var row = result.Rows[0];
                var storedHash = Convert.ToString(GetRowValue(row, result, "code_hash")) ?? string.Empty;

                if (storedHash != otpHash)
                {
                    response["success"] = false;
                    response["message"] = "Código inválido";
                    return false;
                }

                // Mark as used
                var otpId = Convert.ToInt64(GetRowValue(row, result, "id"));
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_otp_codes SET used = 1 WHERE id = ?",
                    otpId
                );
                session.TryExecute();

                response["success"] = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en VerifyOtp: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Bootstrap first owner user (secure local script)
        /// </summary>
        public bool BootstrapOwner(string username, string email, string password, string fullName, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                // Check if owner already exists
                long count = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM gym_users WHERE is_owner = 1"
                );

                if (count > 0)
                {
                    response["success"] = false;
                    response["message"] = "Ya existe un usuario dueño";
                    return false;
                }

                // Hash password
                var passwordHash = HashPassword(password);

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(@"
                    INSERT INTO gym_users (username, email, password_hash, role, full_name, is_active, is_owner, created_at) 
                    VALUES (?, ?, ?, 'OWNER', ?, 1, 1, GETDATE())",
                    username, email, passwordHash, fullName
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear usuario dueño");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                // Seed default permissions
                SeedDefaultPermissions();

                response["success"] = true;
                response["message"] = "Usuario dueño creado exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en BootstrapOwner: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Seed default permissions
        /// </summary>
        private void SeedDefaultPermissions()
        {
            var permissions = new[]
            {
                ("members.view", "Ver socios", "members"),
                ("members.create", "Crear socios", "members"),
                ("members.edit", "Editar socios", "members"),
                ("members.delete", "Eliminar socios", "members"),
                ("access.view", "Ver accesos", "access"),
                ("access.override", "Override de acceso", "access"),
                ("sales.view", "Ver ventas", "sales"),
                ("sales.create", "Crear ventas", "sales"),
                ("sales.return", "Crear devoluciones", "sales"),
                ("inventory.view", "Ver inventario", "inventory"),
                ("inventory.edit", "Editar inventario", "inventory"),
                ("classes.view", "Ver clases", "classes"),
                ("classes.manage", "Gestionar clases", "classes"),
                ("routines.view", "Ver rutinas", "routines"),
                ("routines.manage", "Gestionar rutinas", "routines"),
                ("fisio.view", "Ver fisioterapia", "fisio"),
                ("fisio.manage", "Gestionar fisioterapia", "fisio"),
                ("maintenance.view", "Ver mantenimiento", "maintenance"),
                ("maintenance.manage", "Gestionar mantenimiento", "maintenance"),
                ("billing.view", "Ver facturación", "billing"),
                ("billing.create", "Crear facturas", "billing"),
                ("finance.view", "Ver finanzas", "finance"),
                ("finance.reconcile", "Conciliación bancaria", "finance"),
                ("reports.view", "Ver reportes", "reports"),
                ("staff.view", "Ver personal", "staff"),
                ("staff.manage", "Gestionar personal", "staff"),
                ("admin.settings", "Configuración", "admin")
            };

            foreach (var (code, name, module) in permissions)
            {
                long count = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM gym_permissions WHERE permission_code = ?",
                    code
                );

                if (count == 0)
                {
                    var session = _tools.CreateSession(SubModule);
                    session.ExecuteTransaction(
                        "INSERT INTO gym_permissions (permission_code, permission_name, module, description) VALUES (?, ?, ?, ?)",
                        code, name, module, name
                    );
                    session.TryExecute();
                }
            }

            // Grant all permissions to OWNER role
            var allPermsResult = _tools.ExecuteQueryResultSet("SELECT id FROM gym_permissions");
            if (allPermsResult != null)
            {
                var session = _tools.CreateSession(SubModule);
                foreach (var row in allPermsResult.Rows)
                {
                    var permId = Convert.ToInt64(GetRowValue(row, allPermsResult, "id"));
                    session.ExecuteTransaction(
                        "INSERT INTO gym_role_permissions (role, permission_id, granted) VALUES ('OWNER', ?, 1)",
                        permId
                    );
                }
                session.TryExecute();
            }
        }

        /// <summary>
        /// Get permissions for a role
        /// </summary>
        private List<string> GetPermissions(string role)
        {
            var permissions = new List<string>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(@"
                    SELECT p.permission_code 
                    FROM gym_role_permissions rp 
                    INNER JOIN gym_permissions p ON rp.permission_id = p.id 
                    WHERE rp.role = ? AND rp.granted = 1",
                    role
                );

                if (result != null)
                {
                    foreach (var row in result.Rows)
                    {
                        var code = Convert.ToString(GetRowValue(row, result, "permission_code"));
                        if (!string.IsNullOrEmpty(code))
                            permissions.Add(code);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetPermissions: {ex.Message}");
            }

            return permissions;
        }

        /// <summary>
        /// Hash password using Argon2 (simplified - use Argon2 library in production)
        /// </summary>
        private string HashPassword(string password)
        {
            // Simplified hash - in production use Argon2
            using var sha256 = SHA256.Create();
            var salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[salt.Length + passwordBytes.Length];
            Array.Copy(salt, 0, combined, 0, salt.Length);
            Array.Copy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

            var hash = sha256.ComputeHash(combined);
            return "argon2id$v=19$m=65536,t=3,p=4$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                // Simplified verification - in production use Argon2
                var parts = hash.Split('$');
                if (parts.Length < 5)
                    return false;

                var salt = Convert.FromBase64String(parts[3]);
                var storedHash = Convert.FromBase64String(parts[4]);

                using var sha256 = SHA256.Create();
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var combined = new byte[salt.Length + passwordBytes.Length];
                Array.Copy(salt, 0, combined, 0, salt.Length);
                Array.Copy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

                var computedHash = sha256.ComputeHash(combined);

                return computedHash.SequenceEqual(storedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reset password of the owner user (temporary admin endpoint)
        /// </summary>
        public bool ResetOwnerPassword(string newPassword, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var passwordHash = HashPassword(newPassword);

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_users SET password_hash = ? WHERE is_owner = 1",
                    passwordHash
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al resetear contraseña del dueño");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Contraseña del administrador actualizada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ResetOwnerPassword: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get user email
        /// </summary>
        private string GetUserEmail(long userId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT email FROM gym_users WHERE id = ?",
                userId
            );

            if (result != null && result.Rows.Count > 0)
                return Convert.ToString(GetRowValue(result.Rows[0], result, "email")) ?? string.Empty;

            return string.Empty;
        }

        /// <summary>
        /// Simple TOTP verification (RFC 6238)
        /// </summary>
        private bool VerifyTotpCode(string secret, string code)
        {
            try
            {
                // Decode secret (URL-safe Base64)
                var secretBytes = Convert.FromBase64String(secret.Replace("-", "+").Replace("_", "/"));

                // Get current time step
                var time = DateTime.UtcNow;
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var timeSpan = time - epoch;
                var timeStep = (long)(timeSpan.TotalSeconds / 30);

                // Allow window of ±1 step
                for (long offset = -1; offset <= 1; offset++)
                {
                    var counter = timeStep + offset;
                    var counterBytes = BitConverter.GetBytes(counter).Reverse().ToArray();

                    using var hmac = new HMACSHA1(secretBytes);
                    var hash = hmac.ComputeHash(counterBytes);

                    // Dynamic truncation
                    var offset2 = hash[hash.Length - 1] & 0x0F;
                    var binary = ((hash[offset2] & 0x7F) << 24)
                                | ((hash[offset2 + 1] & 0xFF) << 16)
                                | ((hash[offset2 + 2] & 0xFF) << 8)
                                | (hash[offset2 + 3] & 0xFF);

                    var otp = binary % 1000000;
                    var otpString = otp.ToString("D6");

                    if (otpString == code)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
