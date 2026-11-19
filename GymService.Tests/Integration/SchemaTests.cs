using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class SchemaTests
{
    private readonly DatabaseFixture _fx;

    public SchemaTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Schema_Creates_All_Tables_ForeignKeys_And_Unique_Indexes()
    {
        if (!_fx.Available) return;

        Assert.Equal(35, Count("SELECT COUNT(*) FROM sys.tables WHERE name LIKE 'GYM[_]%'"));
        Assert.Equal(26, Count("SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE name LIKE 'GYM[_]%')"));
        Assert.Equal(11, Count("SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'UX[_]GYM%'"));
    }

    [Fact]
    public void Schema_Has_No_Identity_Columns()
    {
        if (!_fx.Available) return;

        int identities = Count(
            "SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id " +
            "WHERE t.name LIKE 'GYM[_]%' AND c.is_identity = 1");

        Assert.Equal(0, identities);
    }

    [Fact]
    public void Schema_Creates_NonUnique_Indexes()
    {
        if (!_fx.Available) return;

        Assert.True(Count("SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'IX[_]GYM%'") >= 20);
    }

    [Fact]
    public void Sequence_Table_Is_Created()
    {
        if (!_fx.Available) return;

        Assert.Equal(1, Count("SELECT COUNT(*) FROM sys.tables WHERE name = 'ANXO_SEQUENCES'"));
    }

    [Fact]
    public void Audit_Columns_Exist_In_Every_Table()
    {
        if (!_fx.Available) return;

        foreach (string column in new[] { "CREATED_AT", "UPDATED_AT", "CREATED_BY", "LAST_UPDATED_BY", "LAST_UPDATE_LOG", "OBJECT_VERSION" })
        {
            int missing = Count(
                "SELECT COUNT(*) FROM sys.tables t " +
                "WHERE t.name LIKE 'GYM[_]%' AND NOT EXISTS (" +
                "  SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = @column)",
                ("@column", column));

            Assert.Equal(0, missing);
        }
    }

    private int Count(string sql, params (string, object?)[] parameters)
    {
        return Convert.ToInt32(_fx.Scalar(sql, parameters));
    }
}
