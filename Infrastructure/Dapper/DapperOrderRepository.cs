using System.Data.Common;
using Dapper;
using Scenarios.Application.Interfaces;

namespace Scenarios.Infrastructure.Dapper;

public sealed class DapperOrderRepository : IOrderRepository
{
    public async Task InsertAsync(int userId, decimal amount, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "INSERT INTO orders (user_id, amount, status) VALUES (@userId, @amount, 'pending')",
            new { userId, amount }, tx);

    public async Task<int> CountByUserAsync(int userId, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM orders WHERE user_id = @userId",
            new { userId }, tx);
}
