using System.Data.Common;
using Npgsql;
using Scenarios.Application.Interfaces;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    public async Task<int> GetQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT quantity FROM inventory WHERE product_name = @name", conn, tx);
        cmd.Parameters.AddWithValue("name", productName);
        var result = await cmd.ExecuteScalarAsync();
        return result is int i ? i : 0;
    }

    public async Task DecrementQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd(
            "UPDATE inventory SET quantity = quantity - 1 WHERE product_name = @name",
            conn, tx);
        cmd.Parameters.AddWithValue("name", productName);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Inventory>> GetAllAsync(DbConnection conn, DbTransaction? tx = null)
    {
        await using var cmd = Cmd("SELECT id, product_name, quantity FROM inventory ORDER BY id", conn, tx);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Inventory>();
        while (await reader.ReadAsync())
            list.Add(new Inventory(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
        return list;
    }

    private static NpgsqlCommand Cmd(string sql, DbConnection conn, DbTransaction? tx)
    {
        var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn);
        if (tx != null) cmd.Transaction = (NpgsqlTransaction)tx;
        return cmd;
    }
}
