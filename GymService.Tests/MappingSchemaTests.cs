using AxDatabase;
using AxDatabase.Administration;
using AxObjectOrientedDatabase;
using Xunit;

namespace GymService.Tests;

public class MappingSchemaTests
{
    static MappingSchemaTests()
    {
        // Registra todas las entidades antes de resolver las FK; si no, el framework
        // puede buscar una tabla externa antes de que su mapping exista.
        ModelGym.Register();
    }

    private static DatabaseTable TableOf(MappingObject mapping) => mapping.GetDatabaseTable();

    [Fact]
    public void Every_Table_Has_Exactly_One_PrimaryKey_Without_Identity()
    {
        foreach (var mapping in ModelGym.MappingObjects)
        {
            var table = TableOf(mapping);
            var primaryKeys = table.Columns.Where(c => c.ColumnType == ColumnType.PrimaryKey).ToList();

            Assert.True(primaryKeys.Count == 1,
                $"{mapping.ClassName}: se esperaba 1 PK y hay {primaryKeys.Count}");

            var pk = primaryKeys[0];
            Assert.False(pk.ExistProperty("IDENTITY"), $"{mapping.ClassName}: la PK no debe ser IDENTITY");
            Assert.Equal(typeof(long), TypeOf(mapping, pk));
        }
    }

    [Fact]
    public void GymUsers_Has_Unique_Indexes_And_Audit_Columns()
    {
        var table = TableOf(GYM_USERS.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "USERNAME");
        Assert.Contains(table.Columns, c => c.ColumnName == "EMAIL");
        Assert.Contains(table.Columns, c => c.ColumnName == "CREATED_AT");
        Assert.Contains(table.Columns, c => c.ColumnName == "UPDATED_AT");
        Assert.Contains(table.Columns, c => c.ColumnName == "CREATED_BY");
        Assert.Contains(table.Columns, c => c.ColumnName == "OBJECT_VERSION");

        var unique = table.Indexes.Where(i => i.IndexType == IndexTypes.Unique).ToList();
        Assert.Equal(2, unique.Count);
        Assert.Contains(unique, i => i.IndexColumns.Any(c => c.ColumnName == "USERNAME"));
        Assert.Contains(unique, i => i.IndexColumns.Any(c => c.ColumnName == "EMAIL"));
    }

    [Fact]
    public void GymUsers_Has_Lockout_Columns()
    {
        var table = TableOf(GYM_USERS.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "FAILED_LOGIN_ATTEMPTS");
        Assert.Contains(table.Columns, c => c.ColumnName == "LOCKED_UNTIL");
    }

    [Fact]
    public void GymSales_Has_Status_ReturnedAt_And_Unique_SaleNumber()
    {
        var table = TableOf(GYM_SALES.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "STATUS");
        Assert.Contains(table.Columns, c => c.ColumnName == "RETURNED_AT");

        var unique = Assert.Single(table.Indexes, i => i.IndexType == IndexTypes.Unique);
        Assert.Contains(unique.IndexColumns, c => c.ColumnName == "SALE_NUMBER");
    }

    [Fact]
    public void GymRolePermissions_Has_Surrogate_Pk_And_Composite_Unique()
    {
        var table = TableOf(GYM_ROLE_PERMISSIONS.DatabaseMappingObject);

        Assert.Single(table.Columns, c => c.ColumnType == ColumnType.PrimaryKey);
        Assert.Contains(table.Columns, c => c.ColumnName == "ID");

        var unique = Assert.Single(table.Indexes, i => i.IndexType == IndexTypes.Unique);
        Assert.Equal(2, unique.IndexColumns.Count);
        Assert.Contains(unique.IndexColumns, c => c.ColumnName == "ROLE");
        Assert.Contains(unique.IndexColumns, c => c.ColumnName == "PERMISSION_ID");
    }

    [Fact]
    public void GymMemberships_Has_Two_ForeignKeys()
    {
        var table = TableOf(GYM_MEMBERSHIPS.DatabaseMappingObject);

        Assert.Equal(2, table.ForeignKeys.Count);
        Assert.Contains(table.ForeignKeys, fk => fk.ExternalTable.Equals("gym_members", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(table.ForeignKeys, fk => fk.ExternalTable.Equals("gym_membership_plans", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GymClassTypes_Uses_MaxCapacity_Column()
    {
        var table = TableOf(GYM_CLASS_TYPES.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "MAX_CAPACITY");
        Assert.DoesNotContain(table.Columns, c => c.ColumnName == "MAX_PARTICIPANTS");
    }

    [Fact]
    public void GymFisioPackages_Has_Name_And_Optional_Member()
    {
        var table = TableOf(GYM_FISIO_PACKAGES.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "NAME");
        var member = table.Columns.Single(c => c.ColumnName == "MEMBER_ID");
        Assert.Equal(ColumnType.Null, member.ColumnType);
    }

    [Fact]
    public void GymBankAccounts_Tracks_Balance_And_Currency()
    {
        var table = TableOf(GYM_BANK_ACCOUNTS.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "BALANCE");
        Assert.Contains(table.Columns, c => c.ColumnName == "CURRENCY");
    }

    private static Type TypeOf(MappingObject mapping, DatabaseColumn column)
    {
        var attribute = mapping.MappingAttributes.GetMappingAttributeByAttributeName(column.ColumnName);
        return attribute.ColumnDataType switch
        {
            DataType.Int16 => typeof(short),
            DataType.Int32 => typeof(int),
            DataType.Int64 => typeof(long),
            DataType.String => typeof(string),
            DataType.Boolean => typeof(bool),
            DataType.Decimal => typeof(decimal),
            _ => typeof(object)
        };
    }

    [Fact]
    public void MembershipEvents_Has_Timeline_Columns()
    {
        var table = TableOf(GYM_MEMBERSHIP_EVENTS.DatabaseMappingObject);

        Assert.Contains(table.Columns, c => c.ColumnName == "MEMBER_ID");
        Assert.Contains(table.Columns, c => c.ColumnName == "MEMBERSHIP_ID");
        Assert.Contains(table.Columns, c => c.ColumnName == "EVENT_TYPE");
        Assert.Contains(table.Columns, c => c.ColumnName == "EVENT_AT");
        Assert.Contains(table.Columns, c => c.ColumnName == "PREVIOUS_END_DATE");
        Assert.Contains(table.Columns, c => c.ColumnName == "NEW_END_DATE");
        Assert.Contains(table.Columns, c => c.ColumnName == "AMOUNT");
        Assert.Contains(table.Columns, c => c.ColumnName == "SOURCE");
        Assert.Contains(table.Columns, c => c.ColumnName == "CREATED_AT");
    }
}
