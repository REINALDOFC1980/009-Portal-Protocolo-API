using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Portal.Protocolos.Infrastructure.Database.Connection;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada.");
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);

        connection.Open();

        return connection;
    }
}
