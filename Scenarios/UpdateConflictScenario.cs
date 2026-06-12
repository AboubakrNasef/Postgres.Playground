using System.Data;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class UpdateConflictScenario : ScenarioBase
{
    public UpdateConflictScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 7: Update Conflict Resolution";
    public override string Difficulty => "Medium";
    public override string Description => @"
Two transactions try to update the same row.
At READ COMMITTED, both succeed.
At SERIALIZABLE, one fails.";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;

        Log("Scenario A: READ COMMITTED (last write wins)\n");

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX1: Adding $100 to account 1...");
                await AccountRepo.AdjustBalanceAsync(accountId, +100m, conn, tx);
                await Task.Delay(100);
                Log("TX1: Complete");
            }, IsolationLevel.ReadCommitted);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(50);
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX2: Subtracting $50 from account 1...");
                await AccountRepo.AdjustBalanceAsync(accountId, -50m, conn, tx);
                Log("TX2: Complete");
            }, IsolationLevel.ReadCommitted);
        });

        await Task.WhenAll(task1, task2);

        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        var balance = await AccountRepo.GetBalanceAsync(accountId, conn);
        Log($"\nFinal balance at READ COMMITTED: ${balance} (both updates applied: +100-50=$1050)");
    }
}
