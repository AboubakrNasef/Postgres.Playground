using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Scenarios.Application.Interfaces;

namespace Scenarios.Infrastructure.EfCore;

public sealed class EfCoreOrderRepository : IOrderRepository
{
    private readonly IDbContextFactory<PlaygroundDbContext> _contextFactory;

    public EfCoreOrderRepository(IDbContextFactory<PlaygroundDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task InsertAsync(int userId, decimal amount, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO orders (user_id, amount, status) VALUES ({userId}, {amount}, 'pending')");
    }

    public async Task<int> CountByUserAsync(int userId, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = Use(conn, tx);
        return await ctx.Orders.CountAsync(o => o.UserId == userId);
    }

    private PlaygroundDbContext Use(DbConnection conn, DbTransaction? tx)
    {
        var ctx = _contextFactory.CreateDbContext();
        ctx.Database.SetDbConnection(conn);
        if (tx != null) ctx.Database.UseTransaction(tx);
        return ctx;
    }
}
