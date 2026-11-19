using GymService;
using GymService.Tools;
using Xunit;

namespace GymService.Tests;

public class SecurityValidationTests
{
    [Fact]
    public void Sha256Hash_ReturnsKnownBase64()
    {
        // SHA-256("abc") en Base64
        Assert.Equal("ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", SecurityValidation.Sha256Hash("abc"));
    }

    [Fact]
    public void Sha256Hash_IsStableForSameInput()
    {
        Assert.Equal(SecurityValidation.Sha256Hash("gym"), SecurityValidation.Sha256Hash("gym"));
        Assert.NotEqual(SecurityValidation.Sha256Hash("gym"), SecurityValidation.Sha256Hash("gym2"));
    }

    [Fact]
    public void GenerateToken_IsUrlSafeAndUnique()
    {
        var first = SecurityValidation.GenerateToken(32);
        var second = SecurityValidation.GenerateToken(32);

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
        Assert.DoesNotContain("=", first);
    }

    [Fact]
    public void GenerateOtp_ReturnsRequestedDigits()
    {
        var otp = SecurityValidation.GenerateOtp(8);

        Assert.Equal(8, otp.Length);
        Assert.All(otp, c => Assert.True(char.IsDigit(c)));
    }
}

public class CurrentUserTests
{
    [Fact]
    public void ForAudit_DefaultsToMinusOne()
    {
        CurrentUser.Id = 0;

        Assert.Equal(-1, CurrentUser.ForAudit);
    }

    [Fact]
    public void ForAudit_UsesAuthenticatedUser()
    {
        try
        {
            CurrentUser.Id = 7;
            Assert.Equal(7, CurrentUser.ForAudit);
        }
        finally
        {
            CurrentUser.Id = 0;
        }
    }
}
