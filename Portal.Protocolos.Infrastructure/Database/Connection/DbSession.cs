using System.Data;

namespace Portal.Protocolos.Infrastructure.Database.Connection;

public sealed class DbSession
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DbSession(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbConnection CreateConnection()
    {
        try
        {
            return _connectionFactory.CreateConnection();
        }
        catch (Exception ex)
        {

            throw;
        }

        
    }
}
