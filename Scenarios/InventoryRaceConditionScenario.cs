using Scenarios.Application.Interfaces;

namespace Scenarios.Scenarios;

public sealed class InventoryRaceConditionScenario : ScenarioBase
{
    public InventoryRaceConditionScenario(IDbConnectionFactory f, IAccountRepository a, IInventoryRepository i, IOrderRepository o)
        : base(f, a, i, o) { }

    public override string Name => "Scenario 5: Race Condition in Inventory";
    public override string Difficulty => "Medium";
    public override string Description => @"
Multiple customers try to buy the last item simultaneously.
Without proper synchronization, inventory can go negative.";

    public override async Task ExecuteAsync()
    {
        Log("Starting: 5 customers trying to buy MacBook (only 1 available)\n");

        var tasks = Enumerable.Range(1, 5).Select(customerId => Task.Run(async () =>
        {
            try
            {
                await ExecuteInTransactionAsync(async (conn, tx) =>
                {
                    Log($"Customer {customerId}: Checking inventory...");
                    var qty = await InventoryRepo.GetQuantityAsync("MacBook", conn, tx);
                    Log($"Customer {customerId}: Found {qty} in stock");

                    if (qty > 0)
                    {
                        await Task.Delay(100);
                        Log($"Customer {customerId}: Purchasing...");
                        await InventoryRepo.DecrementQuantityAsync("MacBook", conn, tx);
                        Log($"Customer {customerId}: ✓ Purchase successful");
                    }
                    else
                    {
                        Log($"Customer {customerId}: ✗ Out of stock");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Customer {customerId}: Error - {ex.Message}");
            }
        })).ToList();

        await Task.WhenAll(tasks);

        await using var conn = await ConnectionFactory.OpenConnectionAsync();
        var finalQty = await InventoryRepo.GetQuantityAsync("MacBook", conn);
        Log($"\nFinal inventory: {finalQty}");
        Log(finalQty < 0 ? "✗ Race condition: inventory went negative!" : "✓ Inventory protected (likely due to lock contention)");
    }
}
