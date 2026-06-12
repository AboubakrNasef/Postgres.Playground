using System.Data.Common;
using Npgsql;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT id, name, balance, version FROM accounts WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Account(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(3));
    }

    public async Task<decimal> GetBalanceAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT balance FROM accounts WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("id", id);
        var result = await cmd.ExecuteScalarAsync();
        return result is decimal d ? d : 0m;
    }

    public async Task<int> CountAboveBalanceAsync(decimal minBalance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT COUNT(*) FROM accounts WHERE balance > @min", conn, tx);
        cmd.Parameters.AddWithValue("min", minBalance);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateBalanceAsync(int id, decimal newBalance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("UPDATE accounts SET balance = @balance WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("balance", newBalance);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AdjustBalanceAsync(int id, decimal delta, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("UPDATE accounts SET balance = balance + @delta WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("delta", delta);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> TryUpdateBalanceOptimisticAsync(int id, decimal newBalance, int expectedVersion, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd(
            "UPDATE accounts SET balance = @balance, version = version + 1 WHERE id = @id AND version = @version",
            conn, tx);
        cmd.Parameters.AddWithValue("balance", newBalance);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("version", expectedVersion);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task LockForUpdateAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT id FROM accounts WHERE id = @id FOR UPDATE", conn, tx);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteScalarAsync();
    }

    public async Task InsertAsync(string name, decimal balance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd(
            "INSERT INTO accounts (name, balance, version) VALUES (@name, @balance, 0)",
            conn, tx);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("balance", balance);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT id, name, balance, version FROM accounts ORDER BY id", conn, tx);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Account>();
        while (await reader.ReadAsync())
            list.Add(new Account(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(3)));
        return list;
    }

    private static NpgsqlCommand Cmd(string sql, DbConnection conn, DbTransaction? tx)
    {
        var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn);
        if (tx != null) cmd.Transaction = (NpgsqlTransaction)tx;
        return cmd;
    }
}
