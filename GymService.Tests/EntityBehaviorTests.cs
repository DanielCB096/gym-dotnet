using AxDatabase;
using AxLibraries;
using AxObjectOrientedDatabase;
using Xunit;

namespace GymService.Tests;

public class EntityBehaviorTests
{
    [Fact]
    public void Setting_A_Property_Tracks_The_Change()
    {
        var user = new GYM_USERS();

        Assert.False(user.IsChanged);
        Assert.Empty(user.GetChanges());

        user.USERNAME = "ana";

        Assert.True(user.IsChanged);
        Assert.Contains(GYM_USERS.AttributeID_USERNAME, user.GetChanges());
        Assert.DoesNotContain(GYM_USERS.AttributeID_EMAIL, user.GetChanges());
    }

    [Fact]
    public void ResetChanges_Clears_Tracking()
    {
        var member = new GYM_MEMBERS { FIRST_NAME = "Ana", LAST_NAME = "Perez" };

        Assert.True(member.IsChanged);

        member.ResetChanges();

        Assert.False(member.IsChanged);
        Assert.Empty(member.GetChanges());
    }

    [Fact]
    public void Indexer_RoundTrips_Values_And_Tracks_Change()
    {
        var member = new GYM_MEMBERS();

        member[GYM_MEMBERS.AttributeID_FIRST_NAME] = "Ana";
        member[GYM_MEMBERS.AttributeID_EMAIL] = "ana@test.local";

        Assert.Equal("Ana", member[GYM_MEMBERS.AttributeID_FIRST_NAME]);
        Assert.Equal("ana@test.local", member[GYM_MEMBERS.AttributeID_EMAIL]);
        Assert.Contains(GYM_MEMBERS.AttributeID_FIRST_NAME, member.GetChanges());
        Assert.Contains(GYM_MEMBERS.AttributeID_EMAIL, member.GetChanges());
    }

    [Fact]
    public void InDatabase_Flag_Is_Controlled_By_SetInDatabase()
    {
        var product = new GYM_INVENTORY();

        Assert.False(product.InDatabase);

        product.SetInDatabase(true);
        Assert.True(product.InDatabase);

        product.SetInDatabase(false);
        Assert.False(product.InDatabase);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrips_All_Values()
    {
        // Se usa una entidad sin columnas DateTime2: DatabaseObjectTools.Deserialize
        // no contempla DataType.DateTime2 (limitación del framework de Anxo).
        var original = new GYM_CLASS_TYPES
        {
            ID = 42,
            TYPE_NAME = "Spinning",
            DESCRIPTION = "Clase de prueba",
            MAX_CAPACITY = 20,
            DURATION_MINUTES = 45,
            IS_PREMIUM = true,
            PREMIUM_PRICE = 99.5m,
            IS_ACTIVE = true
        };

        original.SetInDatabase(true);

        using var block = new MemBlock();

        using (var writer = new BlockWriter(block))
        {
            DatabaseObjectTools.Serialize(writer, original, false);
            writer.Flush();
        }

        var copy = new GYM_CLASS_TYPES();
        DatabaseObjectTools.Deserialize(new BlockReader(block), copy);

        Assert.Equal(original.ID, copy.ID);
        Assert.Equal(original.TYPE_NAME, copy.TYPE_NAME);
        Assert.Equal(original.DESCRIPTION, copy.DESCRIPTION);
        Assert.Equal(original.MAX_CAPACITY, copy.MAX_CAPACITY);
        Assert.Equal(original.DURATION_MINUTES, copy.DURATION_MINUTES);
        Assert.Equal(original.IS_PREMIUM, copy.IS_PREMIUM);
        Assert.Equal(original.PREMIUM_PRICE, copy.PREMIUM_PRICE);
        Assert.Equal(original.IS_ACTIVE, copy.IS_ACTIVE);
        Assert.Equal(original.InDatabase, copy.InDatabase);
    }

    [Fact]
    public void Tag_Is_Available_For_Business_Logic()
    {
        var member = new GYM_MEMBERS();
        var payload = new object();

        member.Tag = payload;

        Assert.Same(payload, member.Tag);
    }
}
