using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.EfCore;

public sealed class EfCoreAccountRepository : IAccountRepository
{
    private readonly IDbContextFactory<PlaygroundDbContext> _contextFactory;

    public EfCoreAccountRepository(IDbContextFactory<PlaygroundDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<Account?> GetByIdAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<decimal> GetBalanceAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Accounts.Where(a => a.Id == id).Select(a => a.Balance).FirstOrDefaultAsync();
    }

    public async Task<int> CountAboveBalanceAsync(decimal minBalance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Accounts.CountAsync(a => a.Balance > minBalance);
    }

    public async Task UpdateBalanceAsync(int id, decimal newBalance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        await ctx.Accounts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, newBalance));
    }

    public async Task AdjustBalanceAsync(int id, decimal delta, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        await ctx.Accounts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance + delta));
    }

    public async Task<int> TryUpdateBalanceOptimisticAsync(int id, decimal newBalance, int expectedVersion, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Accounts
            .Where(a => a.Id == id && a.Version == expectedVersion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Balance, newBalance)
                .SetProperty(a => a.Version, a => a.Version + 1));
    }

    public async Task LockForUpdateAsync(int id, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        await ctx.Accounts
            .FromSqlRaw("SELECT id, name, balance, version FROM accounts WHERE id = {0} FOR UPDATE", id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task InsertAsync(string name, decimal balance, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO accounts (name, balance, version) VALUES ({name}, {balance}, 0)");
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync();
    }

    private PlaygroundDbContext Use(DbConnection conn, DbTransaction? tx)
    {
        var ctx = _contextFactory.CreateDbContext();
        ctx.Database.SetDbConnection(conn);
        if (tx != null) ctx.Database.UseTransaction(tx);
        return ctx;
    }
}
