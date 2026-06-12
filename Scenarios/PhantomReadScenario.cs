using System.Data;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class PhantomReadScenario : ScenarioBase
{
    public PhantomReadScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 3: Phantom Read";
    public override string Difficulty => "Medium";
    public override string Description => @"
Transaction A reads count of accounts with balance > 500.
Transaction B inserts a new account.
Transaction A reads count again and sees different result.";

    public override async Task ExecuteAsync()
    {
        Log("TX1: Reading count of accounts with balance > 500...");
        var count1 = 0;

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                count1 = await AccountRepo.CountAboveBalanceAsync(500m, conn, tx);
                Log($"TX1: Count = {count1}");
                await Task.Delay(500);
                Log("TX1: Reading count again...");
                var count2 = await AccountRepo.CountAboveBalanceAsync(500m, conn, tx);
                Log($"TX1: Count = {count2}");
                if (count2 != count1)
                    Log("✓ Phantom read detected!");
            }, IsolationLevel.RepeatableRead);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(200);
            Log("TX2: Inserting new account with balance $750...");
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                await AccountRepo.InsertAsync("NewAccount", 750m, conn, tx);
                Log("TX2: Insert complete");
            });
        });

        await Task.WhenAll(task1, task2);
    }
}
