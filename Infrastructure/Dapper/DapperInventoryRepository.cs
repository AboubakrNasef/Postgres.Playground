using System.Data.Common;
using Dapper;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.Dapper;

public sealed class DapperInventoryRepository : IInventoryRepository
{
    public async Task<int> GetQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteScalarAsync<int>(
            "SELECT quantity FROM inventory WHERE product_name = @productName",
            new { productName }, tx);

    public async Task DecrementQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null) =>
        await conn.ExecuteAsync(
            "UPDATE inventory SET quantity = quantity - 1 WHERE product_name = @productName",
            new { productName }, tx);

    public async Task<IReadOnlyList<Inventory>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        var rows = await conn.QueryAsync<InventoryRow>(
            "SELECT id, product_name, quantity FROM inventory ORDER BY id",
            transaction: tx);
        return rows.Select(r => r.ToEntity()).ToList();
    }

    private sealed record InventoryRow(int id, string product_name, int quantity)
    {
        public Inventory ToEntity() => new(id, product_name, quantity);
    }
}
