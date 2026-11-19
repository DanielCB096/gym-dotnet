using System.Data;
using AxDatabase;
using AxLogger;
using AxObjectOrientedDatabase;
using GymService.Services;
using Microsoft.Data.SqlClient;
using Xunit;

namespace GymService.Tests.Integration;

/// <summary>
/// Crea una base de datos temporal en el servidor indicado, aplica el esquema del
/// modelo Anxo y la elimina al terminar. Las pruebas de integración se omiten
/// (pasan sin ejecutar) si no se define GYM_TEST_DB_PASSWORD.
///
/// Variables:
///   GYM_TEST_DB_SERVER   (default "localhost,1433")
///   GYM_TEST_DB_USER     (default "sa")
///   GYM_TEST_DB_PASSWORD (obligatoria para ejecutar)
/// </summary>
public sealed class DatabaseFixture : IDisposable
{
    public bool Available { get; }
    public string SkipReason { get; } = string.Empty;
    public string ConnectionString { get; } = string.Empty;
    public OODatabaseManager? Manager { get; private set; }
    public DatabaseTools? Tools { get; private set; }

    private string? _masterConnectionString;
    private string? _databaseName;

    public DatabaseFixture()
    {
        string password = Environment.GetEnvironmentVariable("GYM_TEST_DB_PASSWORD") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            Available = false;
            SkipReason = "Defina GYM_TEST_DB_PASSWORD (y opcionalmente GYM_TEST_DB_SERVER/GYM_TEST_DB_USER) para ejecutar las pruebas de integración.";
            return;
        }

        LogLevel level = LogLevel.Normal;
        Logger.ConfigLogger("GYMSERVICE_TESTS", level, true, false, false, Path.Combine(Path.GetTempPath(), "gym-tests-logs"));

        string serverRaw = Environment.GetEnvironmentVariable("GYM_TEST_DB_SERVER") ?? "localhost,1433";
        string user = Environment.GetEnvironmentVariable("GYM_TEST_DB_USER") ?? "sa";
        var (server, port) = SplitServer(serverRaw);

        _databaseName = $"GymServiceTests_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
        _masterConnectionString = BuildConnectionString("master", server, port, user, password);

        Execute(_masterConnectionString, $"CREATE DATABASE [{_databaseName}]");

        ConnectionString = BuildConnectionString(_databaseName, server, port, user, password);

        var data = new ConnectionData
        {
            Server = server,
            Port = port,
            Database = _databaseName,
            User = user,
            Password = password,
            DatabaseType = DatabaseTypes.SQLServer,
            PoolSize = 5
        };

        Manager = new OODatabaseManager();
        Manager.Connect(data);
        Tools = Manager.Tools;

        new DatabaseService(Manager).InitializeSchema();

        Available = true;
    }

    public object? Scalar(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);

        return command.ExecuteScalar();
    }

    public void Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try { Manager?.Disconnect(); } catch { /* la BD se va a borrar igualmente */ }
        Manager = null;

        if (_masterConnectionString == null || _databaseName == null)
            return;

        try
        {
            Execute(_masterConnectionString,
                $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{_databaseName}]; END");
        }
        catch { /* best effort */ }
    }

    private static (string Server, int Port) SplitServer(string value)
    {
        int comma = value.LastIndexOf(',');

        if (comma > 0 && int.TryParse(value[(comma + 1)..], out int port))
            return (value[..comma], port);

        return (value, 1433);
    }

    private static string BuildConnectionString(string database, string server, int port, string user, string password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{server},{port}",
            InitialCatalog = database,
            UserID = user,
            Password = password,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        };

        return builder.ConnectionString;
    }

    private static void Execute(string connectionString, string sql)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
