using System.Text;
using GymService.Services;
using Xunit;

namespace GymService.Tests;

public class BulkImportEncodingTests
{
    private static MemoryStream MemberCsv()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes("FirstName,LastName\r\nAna,Prueba\r\n"));
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("UTF8")]
    [InlineData("latin1")]
    [InlineData("ISO-8859-1")]
    [InlineData("windows-1252")]
    public void Supported_encodings_are_not_rejected(string encoding)
    {
        var service = new BulkImportService();
        using var stream = MemberCsv();

        var result = service.ImportMembersFromCsv(stream, encoding);

        Assert.DoesNotContain("Codificación no admitida", result.Message);
    }

    [Theory]
    [InlineData("koi8-r")]
    [InlineData("utf-32")]
    public void Unsupported_encoding_returns_controlled_error(string encoding)
    {
        var service = new BulkImportService();
        using var stream = MemberCsv();

        var result = service.ImportMembersFromCsv(stream, encoding);

        Assert.False(result.Success);
        Assert.Contains("Codificación no admitida", result.Message);
        Assert.Empty(result.Errors);
    }
}
