using System.Text;
using AxDatabase;
using GymService.Services;
using Xunit;

namespace GymService.Tests.Integration;

[Collection("Database")]
public class BulkImportMembersTests
{
    private readonly DatabaseFixture _fx;

    public BulkImportMembersTests(DatabaseFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public void Csv_RechazaCorreoExistenteYRepetidoEnArchivo()
    {
        if (!_fx.Available) return;

        var session = _fx.Tools!.CreateSession("TEST");
        session.InsertObject(new GYM_MEMBERS
        {
            FIRST_NAME = "Existente",
            LAST_NAME = "Socio",
            EMAIL = "existente@test.local",
            STATUS = "active"
        });
        Assert.False(session.TryExecute().IsError);

        const string csv =
            "FirstName,LastName,Email,Phone\r\n" +
            "Nuevo,Uno,nuevo@test.local,5550000001\r\n" +
            "Existente,Dos,existente@test.local,5550000002\r\n" +
            "Repetido,Tres,nuevo@test.local,5550000003\r\n";

        var service = new BulkImportService(_fx.Tools!);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = service.ImportMembersFromCsv(stream);

        Assert.Equal(1, result.ImportedRows);
        Assert.Equal(2, result.ErrorRows);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Ya existe un socio"));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("repetido en el archivo"));
        Assert.Equal(1, Convert.ToInt32(_fx.Scalar(
            "SELECT COUNT(*) FROM GYM_MEMBERS WHERE EMAIL = @e", ("@e", "nuevo@test.local"))));
    }
}
