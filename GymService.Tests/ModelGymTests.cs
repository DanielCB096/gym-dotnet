using AxDatabase;
using AxObjectOrientedDatabase;
using Xunit;

namespace GymService.Tests;

public class ModelGymTests
{
    private static readonly IReadOnlyList<MappingObject> Mappings = ModelGym.MappingObjects;

    [Fact]
    public void Contains_All_Objects()
    {
        Assert.Equal(40, Mappings.Count);
    }

    [Fact]
    public void Table_And_Class_Names_Are_Unique()
    {
        Assert.Equal(Mappings.Count, Mappings.Select(m => m.TableName.ToLowerInvariant()).Distinct().Count());
        Assert.Equal(Mappings.Count, Mappings.Select(m => m.ClassName.ToLowerInvariant()).Distinct().Count());
    }

    [Fact]
    public void Every_Object_Has_One_PrimaryKey_And_A_Sequence()
    {
        foreach (var mapping in Mappings)
        {
            var primary = mapping.MappingAttributes.MappingAttributesPrimary;

            Assert.True(primary.Count == 1, $"{mapping.ClassName}: se esperaba 1 PK y hay {primary.Count}");
            Assert.False(string.IsNullOrWhiteSpace(mapping.Sequence), $"{mapping.ClassName}: sin secuencia");

            // Las PK las asigna Anxo con secuencias: no debe haber IDENTITY.
            Assert.False(primary[0].Autoincrement, $"{mapping.ClassName}: la PK usa IDENTITY");
            Assert.Equal(DataType.Int64, primary[0].ColumnDataType);
        }
    }

    [Fact]
    public void Every_Object_Has_Audit_Tracking()
    {
        foreach (var mapping in Mappings)
        {
            var attributes = mapping.MappingAttributes;

            Assert.True(attributes.InsertTrackingMappingAttribute != null, $"{mapping.ClassName}: sin CREATION_DATETIME");
            Assert.True(attributes.UpdateTrackingMappingAttribute != null, $"{mapping.ClassName}: sin LAST_UPDATED_DATETIME");
            Assert.True(attributes.InsertTrackingUserIdMappingAttribute != null, $"{mapping.ClassName}: sin CREATED_BY");
            Assert.True(attributes.UpdateTrackingUserIdMappingAttribute != null, $"{mapping.ClassName}: sin LAST_UPDATED_BY");
            Assert.True(attributes.LogTrackingMappingAttribute != null, $"{mapping.ClassName}: sin LAST_UPDATE_LOG");
            Assert.True(attributes.VersionTrackingMappingAttribute != null, $"{mapping.ClassName}: sin OBJECT_VERSION");
        }
    }

    [Fact]
    public void ForeignKeys_Reference_Model_Tables_And_Valid_Columns()
    {
        foreach (var mapping in Mappings)
        {
            foreach (var fk in mapping.MappingForeignKeys)
            {
                var target = Mappings.FirstOrDefault(m =>
                    string.Equals(m.TableName, fk.ExternalTable, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.ClassName, fk.ExternalTable, StringComparison.OrdinalIgnoreCase));

                Assert.True(target != null, $"{mapping.ClassName}: FK a tabla desconocida '{fk.ExternalTable}'");

                foreach (var column in fk.Columns)
                {
                    // GetMappingAttributeByAttributeName normaliza a mayúsculas, igual que Anxo.
                    var local = mapping.MappingAttributes.GetMappingAttributeByAttributeName(column.LocalColumn);
                    var external = target!.MappingAttributes.GetMappingAttributeByAttributeName(column.ExternalColumn);

                    Assert.True(local != null, $"{mapping.ClassName}: columna local '{column.LocalColumn}' inexistente");
                    Assert.True(external != null, $"{target!.ClassName}: columna externa '{column.ExternalColumn}' inexistente");
                }
            }
        }
    }

    [Fact]
    public void Index_Columns_Reference_Existing_Attributes()
    {
        foreach (var mapping in Mappings)
        {
            foreach (var index in mapping.MappingIndexes)
            {
                Assert.False(string.IsNullOrWhiteSpace(index.IndexName), $"{mapping.ClassName}: índice sin nombre");

                foreach (var column in index.Columns)
                {
                    var attribute = mapping.MappingAttributes.GetMappingAttributeByAttributeName(column.ColumnName);
                    Assert.True(attribute != null,
                        $"{mapping.ClassName}: el índice '{index.IndexName}' referencia '{column.ColumnName}' inexistente");
                }
            }
        }
    }

    [Theory]
    [InlineData("gym_users", "GYM_USERS")]
    [InlineData("gym_members", "GYM_MEMBERS")]
    [InlineData("gym_sales", "GYM_SALES")]
    [InlineData("gym_role_permissions", "GYM_ROLE_PERMISSIONS")]
    public void Mapping_Exists_For_Table(string tableName, string className)
    {
        var mapping = Mappings.SingleOrDefault(m =>
            string.Equals(m.TableName, tableName, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(mapping);
        Assert.Equal(className, mapping!.ClassName);
    }
}
