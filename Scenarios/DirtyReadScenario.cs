using System.Data;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class DirtyReadScenario : ScenarioBase
{
    public DirtyReadScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 2: Dirty Read";
    public override string Difficulty => "Easy";
    public override string Description => @"
Transaction A updates a balance.
Transaction B reads the updated value before Transaction A commits.
If A rolls back, B has dirty (uncommitted) data.";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;

        Log("Initial balance: $1000");
        Log("Starting concurrent transactions...\n");

        var task1 = Task.Run(async () =>
        {
            await using var conn = await ConnectionFactory.OpenConnectionAsync();
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
            try
            {
                Log("TX1: Starting long transaction, updating balance to $1500...");
                await AccountRepo.UpdateBalanceAsync(accountId, 1500m, conn, tx);
                await Task.Delay(1000);
                Log("TX1: Rolling back transaction...");
                await tx.RollbackAsync();
            }
            catch (Exception ex)
            {
                Log($"TX1: Error - {ex.Message}");
            }
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(200);
            Log("TX2: Reading balance at READ UNCOMMITTED...");
            var balance = await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                var bal = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                Log($"TX2: Read balance = ${bal}");
                return bal;
            }, IsolationLevel.ReadUncommitted);

            Log("TX2: After TX1 rollback, balance is actually $1000");
            Log(balance == 1500m ? "✓ Dirty read occurred!" : "✗ Dirty read prevented (PostgreSQL ignores ReadUncommitted)");
        });

        await Task.WhenAll(task1, task2);
    }
}
