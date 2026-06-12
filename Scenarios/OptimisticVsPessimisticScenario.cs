using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class OptimisticVsPessimisticScenario : ScenarioBase
{
    public OptimisticVsPessimisticScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 10: Optimistic vs Pessimistic Locking";
    public override string Difficulty => "Medium";
    public override string Description => @"
Compare two locking strategies:
1. Pessimistic: Lock immediately with SELECT FOR UPDATE
2. Optimistic: Use version numbers to detect conflicts";

    public override async Task ExecuteAsync()
    {
        const int accountId = 3;
        int successCount = 0, conflictCount = 0;

        Log("Running 5 concurrent updates with optimistic locking...\n");

        var tasks = Enumerable.Range(1, 5).Select(txId => Task.Run(async () =>
        {
            try
            {
                await ExecuteInTransactionAsync(async (conn, tx) =>
                {
                    Log($"TX{txId}: Reading account...");
                    var account = await AccountRepo.GetByIdAsync(accountId, conn, tx);
                    Log($"TX{txId}: Balance=${account!.Balance}, Version={account.Version}");
                    await Task.Delay(50);

                    Log($"TX{txId}: Attempting update...");
                    var affected = await AccountRepo.TryUpdateBalanceOptimisticAsync(
                        accountId, account.Balance + 10m, account.Version, conn, tx);

                    if (affected > 0)
                    {
                        Log($"TX{txId}: ✓ Update successful");
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Log($"TX{txId}: ✗ Conflict detected (version mismatch)");
                        Interlocked.Increment(ref conflictCount);
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"TX{txId}: Error - {ex.Message}");
            }
        })).ToList();

        await Task.WhenAll(tasks);

        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        var final = await AccountRepo.GetByIdAsync(accountId, conn);
        Log($"\nResults:");
        Log($"  Successful updates: {successCount}");
        Log($"  Conflicts detected: {conflictCount}");
        Log($"  Final balance: ${final!.Balance}");
        Log($"  Final version: {final.Version}");
    }
}
