using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TestVault.Infrastructure.Data;

/// <summary>
/// Opens SQL Server connections using the "DefaultConnection" connection
/// string from configuration (appsettings.json / environment variables /
/// User Secrets - never hardcoded, and never committed with a real value;
/// see TestVault.Web/appsettings.Production.json).
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set ConnectionStrings:DefaultConnection in appsettings.json, " +
                "an environment variable, or User Secrets.");
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
