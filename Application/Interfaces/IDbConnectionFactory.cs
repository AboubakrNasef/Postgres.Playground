using System.Data.Common;

namespace Scenarios.Application.Interfaces;

public interface IDbConnectionFactory
{
    string ConnectionString { get; }
    Task<DbConnection> OpenConnectionAsync();
}
