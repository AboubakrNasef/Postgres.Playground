using System.Data.Common;

namespace Scenarios.Application.Interfaces;

public interface IOrderRepository
{
    Task InsertAsync(int userId, decimal amount, DbConnection conn, DbTransaction? tx = null);
    Task<int> CountByUserAsync(int userId, DbConnection conn, DbTransaction? tx = null);
}
