using System.Data.Common;
using Scenarios.Domain.Entities;

namespace Scenarios.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(int id, DbConnection conn, DbTransaction? tx = null);
    Task<decimal> GetBalanceAsync(int id, DbConnection conn, DbTransaction? tx = null);
    Task<int> CountAboveBalanceAsync(decimal minBalance, DbConnection conn, DbTransaction? tx = null);
    Task UpdateBalanceAsync(int id, decimal newBalance, DbConnection conn, DbTransaction? tx = null);
    Task AdjustBalanceAsync(int id, decimal delta, DbConnection conn, DbTransaction? tx = null);
    Task<int> TryUpdateBalanceOptimisticAsync(int id, decimal newBalance, int expectedVersion, DbConnection conn, DbTransaction? tx = null);
    Task LockForUpdateAsync(int id, DbConnection conn, DbTransaction? tx = null);
    Task InsertAsync(string name, decimal balance, DbConnection conn, DbTransaction? tx = null);
    Task<IReadOnlyList<Account>> GetAllAsync(DbConnection conn, DbTransaction? tx = null);
}
