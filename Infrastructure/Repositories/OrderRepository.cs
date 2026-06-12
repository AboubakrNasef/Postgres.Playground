using System.Data.Common;
using Npgsql;
using Scenarios.Application.Interfaces;

namespace Scenarios.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    public async Task InsertAsync(int userId, decimal amount, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd(
            "INSERT INTO orders (user_id, amount, status) VALUES (@userId, @amount, 'pending')",
            conn, tx);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("amount", amount);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> CountByUserAsync(int userId, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT COUNT(*) FROM orders WHERE user_id = @userId", conn, tx);
        cmd.Parameters.AddWithValue("userId", userId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static NpgsqlCommand Cmd(string sql, DbConnection conn, DbTransaction? tx)
    {
        var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn);
        if (tx != null) cmd.Transaction = (NpgsqlTransaction)tx;
        return cmd;
    }
}
