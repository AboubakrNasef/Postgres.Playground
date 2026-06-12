using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class LostUpdateScenario : ScenarioBase
{
    public LostUpdateScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 1: Lost Update Problem";
    public override string Difficulty => "Easy";
    public override string Description => @"
Two concurrent transactions both read an account balance and update it.
Without proper locking, one update is lost.
Expected: $1150 | Actual (with bug): $1050";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;
        const decimal amount1 = 100m;
        const decimal amount2 = 50m;

        Log($"Initial balance for account {accountId}: $1000");
        Log("Starting two concurrent transactions...\n");

        var task1 = Task.Run(async () =>
        {
            await Task.Delay(100);
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX1: Reading balance...");
                var balance = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                Log($"TX1: Read balance = ${balance}");
                await Task.Delay(200);
                Log($"TX1: Updating balance to ${balance + amount1}...");
                await AccountRepo.UpdateBalanceAsync(accountId, balance + amount1, conn, tx);
                Log("TX1: Update complete");
            });
        });

        var task2 = Task.Run(async () =>
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX2: Reading balance...");
                var balance = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                Log($"TX2: Read balance = ${balance}");
                await Task.Delay(300);
                Log($"TX2: Updating balance to ${balance + amount2}...");
                await AccountRepo.UpdateBalanceAsync(accountId, balance + amount2, conn, tx);
                Log("TX2: Update complete");
            });
        });

        await Task.WhenAll(task1, task2);

        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        var finalBalance = await AccountRepo.GetBalanceAsync(accountId, conn);
        Log($"\nFinal balance: ${finalBalance}");
        Log($"Expected: $1150, Actual: ${finalBalance}");
        Log(finalBalance == 1150m ? "✓ Lost update prevented!" : "✗ Lost update detected!");
    }
}
