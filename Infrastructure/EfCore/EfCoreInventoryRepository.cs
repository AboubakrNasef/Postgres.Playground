using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.EfCore;

public sealed class EfCoreInventoryRepository : IInventoryRepository
{
    private readonly IDbContextFactory<PlaygroundDbContext> _contextFactory;

    public EfCoreInventoryRepository(IDbContextFactory<PlaygroundDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<int> GetQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        UseExternalConnection(ctx, conn, tx);
        return await ctx.Inventories
            .Where(i => i.ProductName == productName)
            .Select(i => i.Quantity)
            .FirstOrDefaultAsync();
    }

    public async Task DecrementQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        UseExternalConnection(ctx, conn, tx);
        await ctx.Inventories
            .Where(i => i.ProductName == productName)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => i.Quantity - 1));
    }

    public async Task<IReadOnlyList<Inventory>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        await using var ctx = _contextFactory.CreateDbContext();
        UseExternalConnection(ctx, conn, tx);
        return await ctx.Inventories.AsNoTracking().OrderBy(i => i.Id).ToListAsync();
    }

    private static void UseExternalConnection(PlaygroundDbContext ctx, DbConnection conn, DbTransaction? tx)
    {
        ctx.Database.SetDbConnection(conn);
        if (tx != null) ctx.Database.UseTransaction(tx);
    }
}
