using System.Data;
using System.Data.Common;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public abstract class ScenarioBase
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly IAccountRepository AccountRepo;
    protected readonly IInventoryRepository InventoryRepo;
    protected readonly IOrderRepository OrderRepo;

    protected ScenarioBase(
        IDbConnectionFactory connectionFactory,
        IAccountRepository accountRepo,
        IInventoryRepository inventoryRepo,
        IOrderRepository orderRepo)
    {
        ConnectionFactory = connectionFactory;
        AccountRepo = accountRepo;
        InventoryRepo = inventoryRepo;
        OrderRepo = orderRepo;
    }

    public abstract string Name { get; }
    public abstract string Difficulty { get; }
    public abstract string Description { get; }
    public abstract Task ExecuteAsync();

    protected void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<DbConnection, DbTransaction, Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync(isolationLevel);
        try
        {
            var result = await action(conn, tx);
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    protected Task ExecuteInTransactionAsync(
        Func<DbConnection, DbTransaction, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) =>
        ExecuteInTransactionAsync(async (conn, tx) => { await action(conn, tx); return true; }, isolationLevel);
}
