using System.Data;

namespace TestVault.Infrastructure.Data;

/// <summary>
/// Creates open ADO.NET connections for repositories to use with Dapper.
/// Abstracted behind an interface (rather than repositories new-ing up a
/// SqlConnection directly) so the connection string is configured in one
/// place and repositories stay unit-testable against a fake factory.
/// </summary>
public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
