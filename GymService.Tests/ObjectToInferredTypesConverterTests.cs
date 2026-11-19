using System.Text.Json;
using GymService.Tools;
using Xunit;

namespace GymService.Tests;

public class ObjectToInferredTypesConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ObjectToInferredTypesConverter() }
    };

    [Fact]
    public void Deserializes_Primitives()
    {
        const string json = """{"id":42,"price":10.5,"active":true,"name":"Ana","none":null}""";

        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, Options)!;

        Assert.IsType<long>(data["id"]);
        Assert.IsType<decimal>(data["price"]);
        Assert.IsType<bool>(data["active"]);
        Assert.IsType<string>(data["name"]);
        Assert.Null(data["none"]);
    }

    [Fact]
    public void Deserializes_NestedObjectAndArray()
    {
        const string json = """{"items":[{"productId":1,"quantity":2}],"memberId":3}""";

        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, Options)!;

        var items = Assert.IsType<List<object?>>(data["items"]);
        var item = Assert.IsType<Dictionary<string, object?>>(items[0]);

        Assert.Equal(1L, item["productId"]);
        Assert.Equal(2L, item["quantity"]);
        Assert.Equal(3L, data["memberId"]);
    }

    [Fact]
    public void Serializes_RuntimeTypes()
    {
        var value = new Dictionary<string, object?>
        {
            ["count"] = 5L,
            ["ok"] = true,
            ["text"] = "hola"
        };

        var json = JsonSerializer.Serialize(value, Options);

        Assert.Contains("\"count\":5", json);
        Assert.Contains("\"ok\":true", json);
        Assert.Contains("\"text\":\"hola\"", json);
    }
}
