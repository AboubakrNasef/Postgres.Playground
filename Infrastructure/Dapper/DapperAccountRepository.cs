using System.Data.Common;
using Dapper;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.Dapper;

public sealed class DapperAccountRepository : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        var row = await conn.QueryFirstOrDefaultAsync<AccountRow>(
            "SELECT id, name, balance, version FROM accounts WHERE id = @id",
            new { id }, tx);
        return row?.ToEntity();
    }

    public async Task<decimal> GetBalanceAsync(int id, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteScalarAsync<decimal>(
            "SELECT balance FROM accounts WHERE id = @id",
            new { id }, tx);

    public async Task<int> CountAboveBalanceAsync(decimal minBalance, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM accounts WHERE balance > @minBalance",
            new { minBalance }, tx);

    public async Task UpdateBalanceAsync(int id, decimal newBalance, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "UPDATE accounts SET balance = @newBalance WHERE id = @id",
            new { newBalance, id }, tx);

    public async Task AdjustBalanceAsync(int id, decimal delta, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "UPDATE accounts SET balance = balance + @delta WHERE id = @id",
            new { delta, id }, tx);

    public async Task<int> TryUpdateBalanceOptimisticAsync(int id, decimal newBalance, int expectedVersion, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "UPDATE accounts SET balance = @newBalance, version = version + 1 WHERE id = @id AND version = @expectedVersion",
            new { newBalance, id, expectedVersion }, tx);

    public async Task LockForUpdateAsync(int id, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteScalarAsync<int>(
            "SELECT id FROM accounts WHERE id = @id FOR UPDATE",
            new { id }, tx);

    public async Task InsertAsync(string name, decimal balance, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "INSERT INTO accounts (name, balance, version) VALUES (@name, @balance, 0)",
            new { name, balance }, tx);

    public async Task<IReadOnlyList<Account>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        var rows = await conn.QueryAsync<AccountRow>(
            "SELECT id, name, balance, version FROM accounts ORDER BY id",
            transaction: tx);
        return rows.Select(r => r.ToEntity()).ToList();
    }

    private sealed record AccountRow(int id, string name, decimal balance, int version)
    {
        public Account ToEntity() => new(id, name, balance, version);
    }
}
