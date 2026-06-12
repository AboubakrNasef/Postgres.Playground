using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class CascadingTransactionScenario : ScenarioBase
{
    public CascadingTransactionScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 8: Cascading Transactions";
    public override string Difficulty => "Medium";
    public override string Description => @"
A multi-step transaction: create order, reduce balance, update inventory.
Either all succeed or all fail (atomicity).";

    public override async Task ExecuteAsync()
    {
        const int accountId = 1;
        const decimal orderAmount = 200m;

        Log("Starting transaction: Create order, reduce balance, update inventory\n");

        try
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                Log("Step 1: Checking account balance...");
                var balance = await AccountRepo.GetBalanceAsync(accountId, conn, tx);
                if (balance < orderAmount)
                {
                    Log($"✗ Insufficient funds (${balance} < ${orderAmount})");
                    throw new InvalidOperationException("Insufficient funds");
                }

                Log("Step 2: Creating order...");
                await OrderRepo.InsertAsync(accountId, orderAmount, conn, tx);

                Log("Step 3: Reducing account balance...");
                await AccountRepo.UpdateBalanceAsync(accountId, balance - orderAmount, conn, tx);

                Log("Step 4: Simulating inventory update...");
                await Task.Delay(50);

                Log("✓ All steps completed successfully");
            });
        }
        catch (Exception ex)
        {
            Log($"✗ Transaction rolled back: {ex.Message}");
        }

        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        var finalBalance = await AccountRepo.GetBalanceAsync(accountId, conn);
        var orderCount = await OrderRepo.CountByUserAsync(accountId, conn);
        Log($"\nFinal state: Balance=${finalBalance}, Orders={orderCount}");
    }
}
