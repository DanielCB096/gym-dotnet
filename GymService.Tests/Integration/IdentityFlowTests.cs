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
    private const string Password = "Prueba123!";
    private const string OwnerName = "owner";

    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TokenLifetimeHours"] = "8",
            ["Authentication:OtpExpiryMinutes"] = "10",
            ["Authentication:OtpLength"] = "6",
            ["Authentication:MaxLoginAttempts"] = "3",
            ["Authentication:LockoutMinutes"] = "10",
            ["Authentication:MfaEncryptionKey"] = "gym-tests-key-0123456789-ABCDEFGHIJ"
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

        // Simular datos legacy: secreto Base64 plano y cÃ³digos en texto plano.
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


