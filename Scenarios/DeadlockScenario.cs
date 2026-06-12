using Npgsql;
using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class DeadlockScenario : ScenarioBase
{
    public DeadlockScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 4: Deadlock Detection";
    public override string Difficulty => "Medium";
    public override string Description => @"
Two transactions lock resources in opposite order.
TX1 locks account 1, then tries to lock account 2.
TX2 locks account 2, then tries to lock account 1.
Creates a circular dependency leading to deadlock.";

    public override async Task ExecuteAsync()
    {
        Log("Setting up deadlock scenario...\n");

        var task1 = Task.Run(async () =>
        {
            try
            {
                await ExecuteInTransactionAsync(async (conn, tx) =>
                {
                    Log("TX1: Locking account 1...");
                    await AccountRepo.LockForUpdateAsync(1, conn, tx);
                    Log("TX1: Locked account 1, waiting before locking account 2...");
                    await Task.Delay(500);
                    Log("TX1: Trying to lock account 2...");
                    await AccountRepo.LockForUpdateAsync(2, conn, tx);
                    Log("TX1: Locked both accounts");
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01")
            {
                Log($"✓ TX1: Deadlock detected! {ex.Message}");
            }
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(100);
            try
            {
                await ExecuteInTransactionAsync(async (conn, tx) =>
                {
                    Log("TX2: Locking account 2...");
                    await AccountRepo.LockForUpdateAsync(2, conn, tx);
                    Log("TX2: Locked account 2, waiting before locking account 1...");
                    await Task.Delay(500);
                    Log("TX2: Trying to lock account 1...");
                    await AccountRepo.LockForUpdateAsync(1, conn, tx);
                    Log("TX2: Locked both accounts");
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01")
            {
                Log($"✓ TX2: Deadlock detected! {ex.Message}");
            }
        });

        try { await Task.WhenAll(task1, task2); }
        catch (AggregateException) { }

        Log("\nNote: PostgreSQL detected and resolved the deadlock by aborting one transaction.");
    }
}
