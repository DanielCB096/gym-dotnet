using GymService.Modules.Access;
using GymService.Modules.Members;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class AccessFlowTests
{
    private readonly DatabaseFixture _fx;

    public AccessFlowTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Generate_And_Validate_Access_Code_Is_One_Time()
    {
        if (!_fx.Available) return;

        long memberId = CreateMember();

        var access = new AccessModule(_fx.Tools!);

        Assert.True(access.GenerateAccessCode(memberId, out var generated));
        string code = Convert.ToString(generated["code"]) ?? string.Empty;
        Assert.Equal(6, code.Length);

        Assert.True(access.ValidateAccessCode(code, "kiosk-test", out var validated));
        Assert.Equal(memberId, Convert.ToInt64(validated["memberId"]));

        Assert.False(access.ValidateAccessCode(code, "kiosk-test", out _));

        Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_ACCESS_CODES WHERE MEMBER_ID = @m AND USED = 1", ("@m", memberId))));
        Assert.Equal(1, Convert.ToInt32(_fx.Scalar("SELECT COUNT(*) FROM GYM_ACCESS_LOG WHERE MEMBER_ID = @m AND ACCESS_RESULT = 'granted'", ("@m", memberId))));
    }

    private long CreateMember()
    {
        var members = new MembersModule(_fx.Tools!);
        string email = $"access_{Guid.NewGuid():N}@test.local";

        Assert.True(members.CreateMember(new Dictionary<string, object>
        {
            ["firstName"] = "Acceso",
            ["lastName"] = "Prueba",
            ["email"] = email,
            ["membershipType"] = ""
        }, out _));

        return Convert.ToInt64(_fx.Scalar("SELECT ID FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", email)));
    }
}
