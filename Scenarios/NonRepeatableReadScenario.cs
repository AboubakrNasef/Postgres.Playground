using System.Data;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class NonRepeatableReadScenario : ScenarioBase
{
    public NonRepeatableReadScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 6: Non-Repeatable Read";
    public override string Difficulty => "Easy";
    public override string Description => @"
Transaction A reads a value, then another transaction modifies it,
and Transaction A reads again with different result.";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;
        decimal read1 = 0, read2 = 0;

        Log($"Account {accountId} initial balance: $1000\n");

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("TX1: Reading balance...");
                read1 = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                Log($"TX1: Balance = ${read1}");
                await Task.Delay(300);
                Log("TX1: Reading balance again...");
                read2 = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                Log($"TX1: Balance = ${read2}");
                if (read1 != read2)
                    Log("✓ Non-repeatable read detected!");
            }, IsolationLevel.ReadCommitted);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(100);
            Log("TX2: Modifying balance to $1500...");
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                await AccountRepo.UpdateBalanceAsync(accountId, 1500m, conn, tx);
                Log("TX2: Update complete");
            });
        });

        await Task.WhenAll(task1, task2);
    }
}
