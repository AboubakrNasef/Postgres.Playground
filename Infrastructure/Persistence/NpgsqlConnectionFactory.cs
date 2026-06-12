using System.Data.Common;
using Npgsql;
using Scenarios.Application.Interfaces;

namespace Scenarios.Infrastructure.Persistence;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public NpgsqlConnectionFactory(string connectionString) => ConnectionString = connectionString;

    public async Task<DbConnection> OpenConnectionAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
}
