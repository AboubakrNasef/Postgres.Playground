using System.Diagnostics;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class LongRunningTransactionScenario : ScenarioBase
{
    public LongRunningTransactionScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 9: Long-Running Transaction Blocking";
    public override string Difficulty => "Medium";
    public override string Description => @"
A long transaction holds locks, blocking other transactions.
Demonstrates importance of keeping transactions short.";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;

        Log("Starting long-running transaction...\n");

        var longTask = Task.Run(async () =>
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX1: Starting long transaction, adjusting balance...");
                await AccountRepo.AdjustBalanceAsync(accountId, +1m, conn, tx);
                Log("TX1: Holding lock for 3 seconds...");
                await Task.Delay(3000);
                Log("TX1: Releasing lock");
            });
        });

        var blockingTask = Task.Run(async () =>
        {
            await Task.Delay(500);
            var sw = Stopwatch.StartNew();
            Log("TX2: Attempting to update account (will be blocked)...");
            try
            {
                await ExecuteInTransactionAsync(async (conn, tx) =>
                {
                    await AccountRepo.AdjustBalanceAsync(accountId, -1m, conn, tx);
                });
                sw.Stop();
                Log($"TX2: ✓ Update complete (waited {sw.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                Log($"TX2: ✗ Error: {ex.Message}");
            }
        });

        await Task.WhenAll(longTask, blockingTask);
        Log("\nNote: TX2 had to wait for TX1 to release its lock.");
    }
}
