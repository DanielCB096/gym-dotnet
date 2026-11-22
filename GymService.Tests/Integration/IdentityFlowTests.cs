using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using GymService.Modules.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class IdentityFlowTests
{
    private const string Password = "TestOwner!2026";
    private const string OwnerName = "owner";

    private static readonly IConfiguration Config = ConfigWithKey("gym-tests-key-0123456789-ABCDEFGHIJ");

    private static IConfiguration ConfigWithKey(string mfaKey) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TokenLifetimeHours"] = "8",
            ["Authentication:OtpExpiryMinutes"] = "10",
            ["Authentication:OtpLength"] = "6",
            ["Authentication:MaxLoginAttempts"] = "3",
            ["Authentication:LockoutMinutes"] = "10",
            ["Authentication:MfaEncryptionKey"] = mfaKey
        })
        .Build();

    private readonly DatabaseFixture _fx;

    public IdentityFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Owner_Bootstrap_Is_Unique()
    {
        if (!_fx.Available) return;

        var (identity, _) = EnsureOwner();

        Assert.False(identity.BootstrapOwner("otro_owner", "otro@test.local", Password, "Otro", out var response));
        Assert.Contains("ya existe", (response.GetValueOrDefault("message")?.ToString() ?? "").ToLowerInvariant());
        Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_USERS WHERE IS_OWNER = 1")));
    }

    [Fact]
    public void Login_Migrates_Legacy_Password_Hash_And_Session_Works()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        // Simular el hash falso antiguo (SHA-256 con prefijo argon2id).
        _fx.Execute("UPDATE GYM_USERS SET PASSWORD_HASH = @h, MFA_ENABLED = 0 WHERE ID = @id",
            ("@h", BuildLegacyHash(Password)), ("@id", userId));

        Assert.True(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var login), login.GetValueOrDefault("message")?.ToString());

        string token = Convert.ToString(login["token"]) ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(token));

        string hash = Convert.ToString(_fx.Scalar("SELECT PASSWORD_HASH FROM GYM_USERS WHERE ID = @id", ("@id", userId))) ?? string.Empty;
        Assert.StartsWith("pbkdf2-sha256$", hash);

        Assert.True(identity.ValidateSessionToken(token, out var userData));
        Assert.Equal(userId, Convert.ToInt64(userData!["userId"]));

        Assert.True(identity.Logout(token));
        Assert.False(identity.ValidateSessionToken(token, out _));
    }

    [Fact]
    public void Mfa_Setup_And_Verify_Migrates_Legacy_Secret_And_Backup_Codes()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        Assert.True(identity.SetupMfa(userId, out var setup), setup.GetValueOrDefault("message")?.ToString());

        string totpUri = Convert.ToString(setup["totpUri"]) ?? string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(totpUri, "secret=([^&]+)");
        Assert.True(match.Success);

        string secret = match.Groups[1].Value;
        var codes = Assert.IsType<List<string>>(setup["backupCodes"]);

        // Simular datos legacy: secreto Base64 plano y códigos en texto plano.
        _fx.Execute("UPDATE GYM_USERS SET MFA_SECRET_ENCRYPTED = @s, MFA_BACKUP_CODES = @c, MFA_ENABLED = 0 WHERE ID = @id",
            ("@s", Convert.ToBase64String(Encoding.UTF8.GetBytes(secret))),
            ("@c", string.Join(",", codes)),
            ("@id", userId));

        Assert.True(identity.VerifyMfa(userId, Totp(secret), out var verify), verify.GetValueOrDefault("message")?.ToString());

        string storedSecret = Convert.ToString(_fx.Scalar("SELECT MFA_SECRET_ENCRYPTED FROM GYM_USERS WHERE ID = @id", ("@id", userId))) ?? string.Empty;
        string storedCodes = Convert.ToString(_fx.Scalar("SELECT MFA_BACKUP_CODES FROM GYM_USERS WHERE ID = @id", ("@id", userId))) ?? string.Empty;

        Assert.StartsWith("enc:", storedSecret);
        Assert.StartsWith("sha256:", storedCodes);
        Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT MFA_ENABLED FROM GYM_USERS WHERE ID = @id", ("@id", userId))));
    }

    [Fact]
    public void Mfa_Login_Requires_Challenge_And_Consumes_Backup_Codes()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        try
        {
            Assert.True(identity.SetupMfa(userId, out var setup), setup.GetValueOrDefault("message")?.ToString());

            string secret = Convert.ToString(setup["secret"]) ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(secret));
            var codes = Assert.IsType<List<string>>(setup["backupCodes"]);

            Assert.True(identity.VerifyMfa(userId, Totp(secret), out var verify), verify.GetValueOrDefault("message")?.ToString());

            // La contraseña sola ya no crea sesión y no revela el id del usuario.
            Assert.False(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var login));
            Assert.True(Convert.ToBoolean(login["requiresMfa"]));
            Assert.False(login.ContainsKey("userId"));
            string challenge = Convert.ToString(login["mfaToken"]) ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(challenge));

            // Un código incorrecto no crea sesión.
            Assert.False(identity.VerifyMfaLogin(challenge, "000000", "tests", "127.0.0.1", out var bad));
            Assert.False(bad.ContainsKey("token"));

            // TOTP correcto: crea sesión y el desafío queda usado.
            Assert.True(identity.VerifyMfaLogin(challenge, Totp(secret), "tests", "127.0.0.1", out var ok),
                ok.GetValueOrDefault("message")?.ToString());
            string token = Convert.ToString(ok["token"]) ?? string.Empty;
            Assert.True(identity.ValidateSessionToken(token, out _));
            Assert.False(identity.VerifyMfaLogin(challenge, Totp(secret), "tests", "127.0.0.1", out _));

            // Un código de respaldo se consume y no se puede reutilizar.
            Assert.False(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var loginWithBackup));
            string backupChallenge = Convert.ToString(loginWithBackup["mfaToken"]) ?? string.Empty;
            Assert.True(identity.VerifyMfaLogin(backupChallenge, codes[0], "tests", "127.0.0.1", out var backupOk),
                backupOk.GetValueOrDefault("message")?.ToString());

            string storedCodes = Convert.ToString(_fx.Scalar("SELECT MFA_BACKUP_CODES FROM GYM_USERS WHERE ID = @id", ("@id", userId))) ?? string.Empty;
            Assert.Equal(codes.Count - 1, storedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length);

            Assert.False(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var loginReuse));
            string reuseChallenge = Convert.ToString(loginReuse["mfaToken"]) ?? string.Empty;
            Assert.False(identity.VerifyMfaLogin(reuseChallenge, codes[0], "tests", "127.0.0.1", out _));
        }
        finally
        {
            _fx.Execute("UPDATE GYM_USERS SET MFA_ENABLED = 0, MFA_SECRET_ENCRYPTED = NULL, MFA_BACKUP_CODES = NULL, FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL WHERE ID = @id",
                ("@id", userId));
        }
    }

    [Fact]
    public void Mfa_Backup_Code_Works_With_Different_Encryption_Key()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        try
        {
            Assert.True(identity.SetupMfa(userId, out var setup), setup.GetValueOrDefault("message")?.ToString());
            string secret = Convert.ToString(setup["secret"]) ?? string.Empty;
            var codes = Assert.IsType<List<string>>(setup["backupCodes"]);
            Assert.True(identity.VerifyMfa(userId, Totp(secret), out var verify), verify.GetValueOrDefault("message")?.ToString());

            // Simula una reconstrucción del servidor con otra MfaEncryptionKey.
            var identityWithOtherKey = new IdentityModule(_fx.Tools!, ConfigWithKey("gym-tests-key-OTRA-9876543210-ZYXWVUTS"));

            Assert.False(identityWithOtherKey.Login(OwnerName, Password, "tests", "127.0.0.1", out var login));
            Assert.True(Convert.ToBoolean(login["requiresMfa"]));
            string challenge = Convert.ToString(login["mfaToken"]) ?? string.Empty;

            // El TOTP no se puede descifrar, pero el código de respaldo sí debe funcionar.
            Assert.True(identityWithOtherKey.VerifyMfaLogin(challenge, codes[0], "tests", "127.0.0.1", out var ok),
                ok.GetValueOrDefault("message")?.ToString());
            string token = Convert.ToString(ok["token"]) ?? string.Empty;
            Assert.True(identityWithOtherKey.ValidateSessionToken(token, out _));

            string storedCodes = Convert.ToString(_fx.Scalar("SELECT MFA_BACKUP_CODES FROM GYM_USERS WHERE ID = @id", ("@id", userId))) ?? string.Empty;
            Assert.Equal(codes.Count - 1, storedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length);
        }
        finally
        {
            _fx.Execute("UPDATE GYM_USERS SET MFA_ENABLED = 0, MFA_SECRET_ENCRYPTED = NULL, MFA_BACKUP_CODES = NULL, FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL WHERE ID = @id",
                ("@id", userId));
        }
    }

    [Fact]
    public void ResetMfa_Clears_Secret_And_Revokes_Sessions()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        try
        {
            Assert.True(identity.SetupMfa(userId, out var setup), setup.GetValueOrDefault("message")?.ToString());
            string secret = Convert.ToString(setup["secret"]) ?? string.Empty;
            Assert.True(identity.VerifyMfa(userId, Totp(secret), out var verify), verify.GetValueOrDefault("message")?.ToString());

            Assert.False(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var login));
            string challenge = Convert.ToString(login["mfaToken"]) ?? string.Empty;
            Assert.True(identity.VerifyMfaLogin(challenge, Totp(secret), "tests", "127.0.0.1", out var ok),
                ok.GetValueOrDefault("message")?.ToString());
            string token = Convert.ToString(ok["token"]) ?? string.Empty;
            Assert.True(identity.ValidateSessionToken(token, out _));

            Assert.True(identity.ResetMfa(userId, userId, "OWNER", out var reset), reset.GetValueOrDefault("message")?.ToString());

            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT MFA_ENABLED FROM GYM_USERS WHERE ID = @id", ("@id", userId))));
            var secretValue = _fx.Scalar("SELECT MFA_SECRET_ENCRYPTED FROM GYM_USERS WHERE ID = @id", ("@id", userId));
            var codesValue = _fx.Scalar("SELECT MFA_BACKUP_CODES FROM GYM_USERS WHERE ID = @id", ("@id", userId));
            Assert.True(secretValue is null || secretValue is DBNull);
            Assert.True(codesValue is null || codesValue is DBNull);
            Assert.False(identity.ValidateSessionToken(token, out _));
        }
        finally
        {
            _fx.Execute("UPDATE GYM_USERS SET MFA_ENABLED = 0, MFA_SECRET_ENCRYPTED = NULL, MFA_BACKUP_CODES = NULL, FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL WHERE ID = @id",
                ("@id", userId));
        }
    }

    [Fact]
    public void ChangeOwnPassword_Invalidates_Old_Password_And_Sessions()
    {
        if (!_fx.Available) return;

        var (identity, _) = EnsureOwner();
        string username = "pwd_" + DateTime.UtcNow.Ticks;
        string email = username + "@test.local";

        try
        {
            Assert.True(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["password"] = "OldPass!2026",
                ["fullName"] = "Cambio Password",
                ["role"] = "STAFF"
            }, out var created), created.GetValueOrDefault("message")?.ToString());

            long userId = Convert.ToInt64(created["userId"]);
            Assert.True(identity.Login(username, "OldPass!2026", "tests", "127.0.0.1", out var login));
            string token = Convert.ToString(login["token"]) ?? string.Empty;

            Assert.False(identity.ChangeOwnPassword(userId, "incorrecta", "NewPass!2026", out var wrong));
            Assert.True(identity.ChangeOwnPassword(userId, "OldPass!2026", "NewPass!2026", out var changed),
                changed.GetValueOrDefault("message")?.ToString());

            Assert.False(identity.ValidateSessionToken(token, out _));
            Assert.False(identity.Login(username, "OldPass!2026", "tests", "127.0.0.1", out _));
            Assert.True(identity.Login(username, "NewPass!2026", "tests", "127.0.0.1", out _));
        }
        finally
        {
            _fx.Execute("DELETE FROM GYM_SESSIONS WHERE USER_ID IN (SELECT ID FROM GYM_USERS WHERE USERNAME = @u); DELETE FROM GYM_USERS WHERE USERNAME = @u",
                ("@u", username));
        }
    }

    [Fact]
    public void Admin_ResetPassword_UpdateUser_And_Owner_Protection()
    {
        if (!_fx.Available) return;

        var (identity, ownerId) = EnsureOwner();
        string username = "adm_" + DateTime.UtcNow.Ticks;
        string email = username + "@test.local";

        try
        {
            Assert.True(identity.CreateUser(new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["password"] = "OldPass!2026",
                ["fullName"] = "Admin User",
                ["role"] = "STAFF"
            }, out var created), created.GetValueOrDefault("message")?.ToString());

            long userId = Convert.ToInt64(created["userId"]);
            Assert.True(identity.Login(username, "OldPass!2026", "tests", "127.0.0.1", out var login));
            string token = Convert.ToString(login["token"]) ?? string.Empty;

            // Un ADMIN no puede resetear al owner.
            Assert.False(identity.ResetUserPassword(ownerId, userId, "ADMIN", "OwnerReset!2026", out var denied));
            Assert.False(identity.UpdateUser(ownerId, userId, "ADMIN", new Dictionary<string, object>
            {
                ["email"] = "owner@test.local",
                ["fullName"] = "Owner",
                ["role"] = "ADMIN",
                ["isActive"] = false
            }, out _));

            Assert.True(identity.ResetUserPassword(userId, ownerId, "OWNER", "ResetPass!2026", out var reset),
                reset.GetValueOrDefault("message")?.ToString());
            Assert.False(identity.ValidateSessionToken(token, out _));

            Assert.True(identity.UpdateUser(userId, ownerId, "OWNER", new Dictionary<string, object>
            {
                ["email"] = "updated-" + email,
                ["fullName"] = "Usuario Actualizado",
                ["role"] = "CASHIER",
                ["isActive"] = false
            }, out var updated), updated.GetValueOrDefault("message")?.ToString());

            Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT IS_ACTIVE FROM GYM_USERS WHERE ID = @id", ("@id", userId))));
            Assert.False(identity.Login(username, "ResetPass!2026", "tests", "127.0.0.1", out _));
        }
        finally
        {
            _fx.Execute("DELETE FROM GYM_SESSIONS WHERE USER_ID IN (SELECT ID FROM GYM_USERS WHERE USERNAME = @u); DELETE FROM GYM_USERS WHERE USERNAME = @u",
                ("@u", username));
        }
    }

    [Fact]
    public void Login_Locks_Account_After_Max_Failed_Attempts()
    {
        if (!_fx.Available) return;

        var (identity, userId) = EnsureOwner();

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            bool ok = identity.Login(OwnerName, "clave-incorrecta", "tests", "127.0.0.1", out var failed);

            Assert.False(ok);
        }

        Assert.NotNull(_fx.Scalar("SELECT LOCKED_UNTIL FROM GYM_USERS WHERE ID = @id", ("@id", userId)));

        // Aun con la contraseña correcta, la cuenta sigue bloqueada.
        Assert.False(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out var blocked));
        Assert.NotNull(blocked["lockedUntil"]);
        Assert.Contains("bloqueada", blocked["message"]?.ToString() ?? "");

        // Al expirar/limpiar el bloqueo, el login vuelve a funcionar y reinicia el contador.
        _fx.Execute("UPDATE GYM_USERS SET LOCKED_UNTIL = NULL, FAILED_LOGIN_ATTEMPTS = 0 WHERE ID = @id", ("@id", userId));

        Assert.True(identity.Login(OwnerName, Password, "tests", "127.0.0.1", out _));
        Assert.Equal(0, Convert.ToInt32(_fx.Scalar("SELECT FAILED_LOGIN_ATTEMPTS FROM GYM_USERS WHERE ID = @id", ("@id", userId))));
    }

    private (IdentityModule Identity, long UserId) EnsureOwner()
    {
        var identity = new IdentityModule(_fx.Tools!, Config);

        long existing = Convert.ToInt64(_fx.Scalar("SELECT COUNT(*) FROM GYM_USERS WHERE USERNAME = @u", ("@u", OwnerName)));

        if (existing == 0)
        {
            Assert.True(identity.BootstrapOwner(OwnerName, "owner@test.local", Password, "Owner Test", out var response),
                response.GetValueOrDefault("message")?.ToString());
        }

        long userId = Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_USERS WHERE USERNAME = @u", ("@u", OwnerName)));

        // Estado limpio para que las pruebas no dependan del orden de ejecución.
        _fx.Execute("UPDATE GYM_USERS SET FAILED_LOGIN_ATTEMPTS = 0, LOCKED_UNTIL = NULL, MFA_ENABLED = 0 WHERE ID = @id",
            ("@id", userId));

        return (identity, userId);
    }

    private static string BuildLegacyHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var combined = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

        var hash = SHA256.HashData(combined);
        return "argon2id$v=19$m=65536,t=3,p=4$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    private static string Totp(string secret)
    {
        string normalized = secret.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2: normalized += "=="; break;
            case 3: normalized += "="; break;
        }

        var key = Convert.FromBase64String(normalized);
        long step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        Span<byte> message = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(message, step);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(message.ToArray());

        int offset = hash[^1] & 0x0f;
        int binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);

        return (binary % 1_000_000).ToString("D6");
    }
}


