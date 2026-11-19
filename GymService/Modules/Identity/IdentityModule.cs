using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AxLogger;
using AxLibraries;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Services;
using GymService.Tools;

namespace GymService.Modules.Identity
{
    /// <summary>
    /// Identity module: authentication, sessions, MFA TOTP, OTP by email, RBAC.
    /// </summary>
    public class IdentityModule
    {
        private const string SubModule = "IDENTITY_MODULE";

        // Formato de contraseÃ±as: pbkdf2-sha256$<iteraciones>$<salBase64>$<hashBase64>
        private const string Pbkdf2PasswordPrefix = "pbkdf2-sha256$";
        private const int Pbkdf2Iterations = 210_000;
        private const int Pbkdf2SaltSize = 16;
        private const int Pbkdf2KeySize = 32;

        // Prefijo del formato antiguo (en realidad SHA-256(sal + password)).
        private const string LegacyPasswordPrefix = "argon2id$";

        // Prefijo de los secretos MFA cifrados con AES-256.
        private const string MfaEncryptedPrefix = "enc:";

        // Prefijo de los cÃ³digos de respaldo hasheados con SHA-256 (hexadecimal).
        private const string BackupCodeHashPrefix = "sha256:";

        private DatabaseTools _tools;
        private IConfiguration _configuration;
        private readonly EmailService _email;

        public IdentityModule(DatabaseTools tools, IConfiguration configuration)
        {
            _tools = tools;
            _configuration = configuration;
            _email = new EmailService(configuration);
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
                    "SELECT id, username, email, password_hash, role, full_name, is_active, is_owner, mfa_enabled, " +
                    "failed_login_attempts, locked_until FROM gym_users WHERE username = ?",
                    username
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Usuario o contraseÃ±a invÃ¡lidos";
                    return false;
                }

                var user = result.Rows[0];
                var userId = Convert.ToInt64(GetRowValue(user, result, "id"));
                var storedHash = Convert.ToString(GetRowValue(user, result, "password_hash")) ?? string.Empty;
                var role = Convert.ToString(GetRowValue(user, result, "role")) ?? string.Empty;
                var isActive = Convert.ToBoolean(GetRowValue(user, result, "is_active"));
                var mfaEnabled = Convert.ToBoolean(GetRowValue(user, result, "mfa_enabled"));

                int failedAttempts = Convert.ToInt32(GetRowValue(user, result, "failed_login_attempts") ?? 0);
                object? lockedUntilValue = GetRowValue(user, result, "locked_until");
                DateTime? lockedUntil = lockedUntilValue == null || lockedUntilValue == DBNull.Value
                    ? null
                    : Convert.ToDateTime(lockedUntilValue);

                int maxLoginAttempts = int.Parse(_configuration["Authentication:MaxLoginAttempts"] ?? "5");
                int lockoutMinutes = int.Parse(_configuration["Authentication:LockoutMinutes"] ?? "10");

                if (lockedUntil.HasValue && lockedUntil.Value > DateTime.Now)
                {
                    Logger.AddWarningSubModule(SubModule, $"Login rechazado: la cuenta '{username}' esta bloqueada hasta {lockedUntil.Value:yyyy-MM-dd HH:mm}");

                    response["success"] = false;
                    response["lockedUntil"] = lockedUntil.Value;
                    response["message"] = $"Cuenta bloqueada por intentos fallidos. Intente de nuevo a las {lockedUntil.Value:HH:mm}.";
                    return false;
                }

                if (!isActive)
                {
                    response["success"] = false;
                    response["message"] = "Usuario desactivado";
                    return false;
                }

                // Verify password (PBKDF2-SHA256, con migraciÃ³n transparente del formato antiguo)
                if (!VerifyPassword(password, storedHash, userId))
                {
                    int attempts = failedAttempts + 1;
                    bool locked = attempts >= maxLoginAttempts;

                    var lockChanges = new GYM_USERS
                    {
                        ID = userId,
                        FAILED_LOGIN_ATTEMPTS = locked ? 0 : attempts
                    };

                    if (locked)
                    {
                        lockChanges.LOCKED_UNTIL = DateTime.Now.AddMinutes(lockoutMinutes);
                    }

                    var lockSession = _tools.CreateSession(SubModule, userId);
                    lockSession.UpdateObjectChanges(lockChanges);
                    lockSession.TryExecute();

                    Logger.AddWarningSubModule(SubModule,
                        locked
                            ? $"La cuenta '{username}' fue bloqueada por {lockoutMinutes} minutos tras {maxLoginAttempts} intentos fallidos"
                            : $"Login fallido para '{username}' (intento {attempts} de {maxLoginAttempts})");

                    response["success"] = false;
                    response["message"] = locked
                        ? $"Cuenta bloqueada por {lockoutMinutes} minutos por intentos fallidos."
                        : "Usuario o contraseña inválidos";
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
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(new GYM_SESSIONS
                {
                    USER_ID = userId,
                    TOKEN_HASH = tokenHash,
                    DEVICE_INFO = deviceInfo,
                    IP_ADDRESS = ipAddress,
                    EXPIRES_AT = expiresAt,
                    IS_ACTIVE = true
                });

                // Update last login
                session.UpdateObjectChanges(new GYM_USERS
                {
                    ID = userId,
                    LAST_LOGIN = DateTime.Now,
                    FAILED_LOGIN_ATTEMPTS = 0,
                    LOCKED_UNTIL = null
                });

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al guardar la sesiÃ³n de usuario");
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

                // El token no es la PK: se resuelve el ID de la sesiÃ³n antes de actualizar el objeto.
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id FROM gym_sessions WHERE token_hash = ?",
                    tokenHash
                );

                if (result == null || result.Rows.Count == 0)
                    return true;

                var sessionId = Convert.ToInt64(GetRowValue(result.Rows[0], result, "id"));

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_SESSIONS
                {
                    ID = sessionId,
                    IS_ACTIVE = false
                });
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
                // El secreto TOTP debe cifrarse con una clave configurada.
                var cipher = CreateMfaCipher();
                if (cipher == null)
                {
                    Logger.AddErrorSubModule(SubModule, "Authentication:MfaEncryptionKey no estÃ¡ configurada; no se puede cifrar el secreto MFA");
                    response["success"] = false;
                    response["message"] = "La configuraciÃ³n de cifrado de MFA no estÃ¡ disponible";
                    return false;
                }

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

                // Encrypt secret (AES-256) and hash backup codes before storing them
                var encryptedSecret = MfaEncryptedPrefix + cipher.EncryptAsBASE64(secret);
                var hashedBackupCodes = backupCodes.Select(HashBackupCode);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_USERS
                {
                    ID = userId,
                    MFA_SECRET_ENCRYPTED = encryptedSecret,
                    MFA_BACKUP_CODES = string.Join(",", hashedBackupCodes)
                });

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al guardar la configuraciÃ³n MFA");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                response["success"] = true;
                response["totpUri"] = totpUri;
                response["backupCodes"] = backupCodes;
                response["message"] = "Escanea el cÃ³digo QR en tu app de autenticaciÃ³n";

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
                // El secreto TOTP estÃ¡ cifrado: se necesita la clave de configuraciÃ³n.
                var cipher = CreateMfaCipher();
                if (cipher == null)
                {
                    Logger.AddErrorSubModule(SubModule, "Authentication:MfaEncryptionKey no estÃ¡ configurada; no se puede verificar MFA");
                    response["success"] = false;
                    response["message"] = "La configuraciÃ³n de cifrado de MFA no estÃ¡ disponible";
                    return false;
                }

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

                if (string.IsNullOrEmpty(encryptedSecret))
                {
                    response["success"] = false;
                    response["message"] = "MFA no configurado";
                    return false;
                }

                // Decrypt secret (soporta tanto "enc:" como el Base64 legacy)
                string secret;
                try
                {
                    secret = DecryptMfaSecret(encryptedSecret, cipher);
                }
                catch (Exception ex)
                {
                    Logger.AddErrorSubModule(SubModule, $"Error al descifrar el secreto MFA: {ex.Message}");
                    response["success"] = false;
                    response["message"] = "No se pudo descifrar la configuraciÃ³n MFA";
                    return false;
                }

                // Verify TOTP code (simple implementation)
                var isValid = VerifyTotpCode(secret, code);

                // Or check backup codes (hasheados con "sha256:" o legacy en texto plano)
                if (!isValid)
                {
                    isValid = VerifyBackupCode(code, backupCodes);
                }

                if (isValid)
                {
                    var userChanges = new GYM_USERS
                    {
                        ID = userId,
                        MFA_ENABLED = true
                    };

                    // Migracion transparente: secretos legacy (Base64 plano) -> AES-256.
                    if (!encryptedSecret.StartsWith(MfaEncryptedPrefix, StringComparison.Ordinal))
                    {
                        userChanges.MFA_SECRET_ENCRYPTED = MfaEncryptedPrefix + cipher.EncryptAsBASE64(secret);
                        Logger.AddInfoSubModule(SubModule, "Secreto MFA migrado de Base64 legacy a AES-256");
                    }

                    // Migracion transparente: codigos de respaldo en texto plano -> SHA-256.
                    var migratedCodes = MigrateBackupCodes(backupCodes);
                    if (migratedCodes != null)
                    {
                        userChanges.MFA_BACKUP_CODES = migratedCodes;
                        Logger.AddInfoSubModule(SubModule, "Codigos de respaldo MFA migrados a SHA-256");
                    }

                    var session = _tools.CreateSession(SubModule, userId);
                    session.UpdateObjectChanges(userChanges);
                    session.TryExecute();

                    response["success"] = true;
                    response["message"] = "MFA activado correctamente";
                    return true;
                }

                response["success"] = false;
                response["message"] = "CÃ³digo invÃ¡lido";
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

                var otp = new GYM_OTP_CODES
                {
                    EMAIL = email,
                    CODE_HASH = otpHash,
                    PURPOSE = purpose,
                    EXPIRES_AT = DateTime.Now.AddMinutes(expiryMinutes),
                    USED = false
                };

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(otp);

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al generar código OTP");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                if (_email.TrySendOtp(email, otpCode, purpose, out string emailError))
                {
                    response["success"] = true;
                    response["message"] = "Código OTP enviado por correo";
                    return true;
                }

                // Modo desarrollo: si no hay SMTP, se devuelve el código en la respuesta.
                if (string.Equals(_configuration["Email:ShowCodeInResponse"], "true", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.AddWarningSubModule(SubModule, "SMTP no configurado; devolviendo OTP en la respuesta (modo desarrollo)");
                    response["success"] = true;
                    response["code"] = otpCode;
                    response["message"] = "Código OTP generado (modo desarrollo, sin envío de correo)";
                    return true;
                }

                // No se pudo entregar: se elimina el OTP para no dejar códigos huérfanos.
                var cleanup = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                cleanup.DeleteObject(new GYM_OTP_CODES { ID = otp.ID });
                cleanup.TryExecute();

                Logger.AddErrorSubModule(SubModule, $"No se pudo enviar el OTP a {email}: {emailError}");
                response["success"] = false;
                response["message"] = emailError;
                return false;
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
                    response["message"] = "CÃ³digo invÃ¡lido o expirado";
                    return false;
                }

                var row = result.Rows[0];
                var storedHash = Convert.ToString(GetRowValue(row, result, "code_hash")) ?? string.Empty;

                if (storedHash != otpHash)
                {
                    response["success"] = false;
                    response["message"] = "CÃ³digo invÃ¡lido";
                    return false;
                }

                // Mark as used
                var otpId = Convert.ToInt64(GetRowValue(row, result, "id"));
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_OTP_CODES
                {
                    ID = otpId,
                    USED = true
                });
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
                // Hash password
                var passwordHash = HashPassword(password);

                // Solo puede existir un usuario dueÃ±o.
                long existingOwners = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM gym_users WHERE is_owner = 1"
                );

                if (existingOwners > 0)
                {
                    Logger.AddWarningSubModule(SubModule, "Intento de crear un segundo usuario dueÃ±o");
                    response["success"] = false;
                    response["message"] = "Ya existe un usuario dueÃ±o";
                    return false;
                }

                var usuario = new GYM_USERS
                {
                    USERNAME = username,
                    EMAIL = email,
                    PASSWORD_HASH = passwordHash,
                    ROLE = "OWNER",
                    FULL_NAME = fullName,
                    IS_ACTIVE = true,
                    IS_OWNER = true
                };

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.InsertObject(usuario);

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al crear usuario dueÃ±o");
                    response["success"] = false;
                    response["message"] = "Error interno del servidor";
                    return false;
                }

                Logger.AddInfoSubModule(SubModule, $"Usuario dueÃ±o creado con ID {usuario.ID}");

                // Seed default permissions
                SeedDefaultPermissions();

                response["success"] = true;
                response["message"] = "Usuario dueÃ±o creado exitosamente";
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
                ("billing.view", "Ver facturaciÃ³n", "billing"),
                ("billing.create", "Crear facturas", "billing"),
                ("finance.view", "Ver finanzas", "finance"),
                ("finance.reconcile", "ConciliaciÃ³n bancaria", "finance"),
                ("reports.view", "Ver reportes", "reports"),
                ("staff.view", "Ver personal", "staff"),
                ("staff.manage", "Gestionar personal", "staff"),
                ("admin.settings", "ConfiguraciÃ³n", "admin")
            };

            // Inserta en una sola transacciÃ³n los permisos que aÃºn no existen.
            var newPermissions = new List<GYM_PERMISSIONS>();

            foreach (var (code, name, module) in permissions)
            {
                long count = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM gym_permissions WHERE permission_code = ?",
                    code
                );

                if (count == 0)
                {
                    newPermissions.Add(new GYM_PERMISSIONS
                    {
                        PERMISSION_CODE = code,
                        PERMISSION_NAME = name,
                        MODULE = module,
                        DESCRIPTION = name
                    });
                }
            }

            if (newPermissions.Count > 0)
            {
                var permissionSession = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                foreach (var permission in newPermissions)
                {
                    permissionSession.InsertObject(permission);
                }

                if (permissionSession.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al insertar los permisos por defecto");
                    return;
                }
            }

            // Grant all permissions to OWNER role
            var allPermsResult = _tools.ExecuteQueryResultSet("SELECT id FROM gym_permissions");
            if (allPermsResult != null && allPermsResult.Rows.Count > 0)
            {
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                foreach (var row in allPermsResult.Rows)
                {
                    var permId = Convert.ToInt64(GetRowValue(row, allPermsResult, "id"));

                    // GYM_ROLE_PERMISSIONS tiene PK surrogate: la asigna la secuencia Anxo.
                    session.InsertObject(new GYM_ROLE_PERMISSIONS
                    {
                        ROLE = "OWNER",
                        PERMISSION_ID = permId,
                        GRANTED = true
                    });
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
        /// Hash password using PBKDF2-SHA256.
        /// Formato: pbkdf2-sha256$&lt;iteraciones&gt;$&lt;salBase64&gt;$&lt;hashBase64&gt;
        /// </summary>
        private string HashPassword(string password)
        {
            var salt = new byte[Pbkdf2SaltSize];
            RandomNumberGenerator.Fill(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, Pbkdf2KeySize);

            return Pbkdf2PasswordPrefix
                + Pbkdf2Iterations.ToString()
                + "$" + Convert.ToBase64String(salt)
                + "$" + Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify password against hash. Si el hash guardado usa el formato antiguo
        /// (argon2id$... en realidad SHA-256), lo verifica y migra la fila a PBKDF2.
        /// </summary>
        private bool VerifyPassword(string password, string hash, long userId)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                    return false;

                if (hash.StartsWith(Pbkdf2PasswordPrefix, StringComparison.Ordinal))
                    return VerifyPbkdf2Password(password, hash);

                if (hash.StartsWith(LegacyPasswordPrefix, StringComparison.Ordinal))
                {
                    if (!VerifyLegacySha256Password(password, hash))
                        return false;

                    // MigraciÃ³n transparente: re-hashear con PBKDF2 y actualizar la fila.
                    TryRehashPassword(userId, password);
                    return true;
                }

                // Formato desconocido: no se acepta.
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verify a PBKDF2-SHA256 password hash.
        /// </summary>
        private bool VerifyPbkdf2Password(string password, string hash)
        {
            var parts = hash.Split('$');
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var storedHash = Convert.FromBase64String(parts[3]);

            if (salt.Length == 0 || storedHash.Length == 0)
                return false;

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }

        /// <summary>
        /// Verify a legacy "argon2id$..." hash, which was really SHA-256(salt + password).
        /// </summary>
        private bool VerifyLegacySha256Password(string password, string hash)
        {
            var parts = hash.Split('$');
            if (parts.Length < 5)
                return false;

            var salt = Convert.FromBase64String(parts[3]);
            var storedHash = Convert.FromBase64String(parts[4]);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[salt.Length + passwordBytes.Length];
            Array.Copy(salt, 0, combined, 0, salt.Length);
            Array.Copy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

            using var sha256 = SHA256.Create();
            var computedHash = sha256.ComputeHash(combined);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }

        /// <summary>
        /// Re-hash a password with PBKDF2 after a successful legacy verification.
        /// </summary>
        private void TryRehashPassword(long userId, string password)
        {
            try
            {
                var newHash = HashPassword(password);

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_USERS
                {
                    ID = userId,
                    PASSWORD_HASH = newHash
                });

                var rc = session.TryExecute();
                if (rc.IsError)
                    Logger.AddWarningSubModule(SubModule, "No se pudo migrar el hash de contraseÃ±a a PBKDF2");
                else
                    Logger.AddInfoSubModule(SubModule, "Hash de contraseÃ±a migrado a PBKDF2");
            }
            catch (Exception ex)
            {
                Logger.AddWarningSubModule(SubModule, $"Error al migrar el hash de contraseÃ±a: {ex.Message}");
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

                // La fila se identifica por IS_OWNER (no es PK): se resuelven los IDs primero.
                var ownersResult = _tools.ExecuteQueryResultSet(
                    "SELECT id FROM gym_users WHERE is_owner = 1"
                );

                if (ownersResult != null && ownersResult.Rows.Count > 0)
                {
                    var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                    foreach (var row in ownersResult.Rows)
                    {
                        session.UpdateObjectChanges(new GYM_USERS
                        {
                            ID = Convert.ToInt64(GetRowValue(row, ownersResult, "id")),
                            PASSWORD_HASH = passwordHash
                        });
                    }

                    if (session.TryExecute().IsError)
                    {
                        Logger.AddErrorSubModule(SubModule, "Error al resetear contraseÃ±a del dueÃ±o");
                        response["success"] = false;
                        response["message"] = "Error interno del servidor";
                        return false;
                    }
                }

                response["success"] = true;
                response["message"] = "ContraseÃ±a del administrador actualizada";
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
        /// Create the AES-256 cipher used for MFA secrets, or null if no key is configured.
        /// </summary>
        private SecuritySymmetricEncryption? CreateMfaCipher()
        {
            var key = _configuration["Authentication:MfaEncryptionKey"];
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return new SecuritySymmetricEncryption(key!, SecuritySymmetricEncryption.Algorithm.AES, 256);
        }

        /// <summary>
        /// Decrypt a stored MFA secret. Values with the "enc:" prefix are AES-encrypted;
        /// anything else is the legacy Base64-encoded plaintext secret.
        /// </summary>
        private static string DecryptMfaSecret(string storedValue, SecuritySymmetricEncryption cipher)
        {
            if (storedValue.StartsWith(MfaEncryptedPrefix, StringComparison.Ordinal))
                return cipher.DecryptAsBASE64(storedValue.Substring(MfaEncryptedPrefix.Length));

            return Encoding.UTF8.GetString(Convert.FromBase64String(storedValue));
        }

        /// <summary>
        /// Hash a backup code for storage: "sha256:&lt;hex&gt;".
        /// </summary>
        private static string HashBackupCode(string code)
        {
            return BackupCodeHashPrefix + Sha256Hex(code);
        }

        /// <summary>
        /// Verify a backup code against the stored list. Supports hashed entries
        /// ("sha256:&lt;hex&gt;") and legacy plaintext entries.
        /// </summary>
        private static bool VerifyBackupCode(string code, string storedBackupCodes)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(storedBackupCodes))
                return false;

            var codeHashHex = Sha256Hex(code);

            foreach (var entry in storedBackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (entry.StartsWith(BackupCodeHashPrefix, StringComparison.Ordinal))
                {
                    var storedHex = entry.Substring(BackupCodeHashPrefix.Length);
                    if (FixedTimeEqualsStrings(storedHex, codeHashHex))
                        return true;
                }
                else if (FixedTimeEqualsStrings(entry, code))
                {
                    // Compatibilidad con cÃ³digos de respaldo antiguos en texto plano.
                    return true;
                }
            }

            return false;
        }

        private static string Sha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        /// <summary>
        /// Convierte una lista de codigos de respaldo legacy (texto plano) al formato
        /// hasheado "sha256:...". Devuelve null si no habia nada que migrar.
        /// </summary>
        private static string? MigrateBackupCodes(string storedBackupCodes)
        {
            if (string.IsNullOrEmpty(storedBackupCodes))
                return null;

            var entries = storedBackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool changed = false;
            var result = new List<string>(entries.Length);

            foreach (var entry in entries)
            {
                if (entry.StartsWith(BackupCodeHashPrefix, StringComparison.Ordinal))
                {
                    result.Add(entry);
                }
                else
                {
                    result.Add(HashBackupCode(entry));
                    changed = true;
                }
            }

            return changed ? string.Join(",", result) : null;
        }

        private static bool FixedTimeEqualsStrings(string a, string b)
        {
            var bytesA = Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bytesB = Encoding.UTF8.GetBytes(b ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }

        /// <summary>
        /// Simple TOTP verification (RFC 6238)
        /// </summary>
        private bool VerifyTotpCode(string secret, string code)
        {
            try
            {
                // Decode secret (URL-safe Base64, sin padding)
                var normalizedSecret = secret.Replace("-", "+").Replace("_", "/");
                switch (normalizedSecret.Length % 4)
                {
                    case 2: normalizedSecret += "=="; break;
                    case 3: normalizedSecret += "="; break;
                }

                var secretBytes = Convert.FromBase64String(normalizedSecret);

                // Get current time step
                var time = DateTime.UtcNow;
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var timeSpan = time - epoch;
                var timeStep = (long)(timeSpan.TotalSeconds / 30);

                // Allow window of Â±1 step
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
