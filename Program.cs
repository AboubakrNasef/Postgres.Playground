using System.Diagnostics;
using Npgsql;
using Scenarios.Application.Interfaces;
using Scenarios.Infrastructure.Dapper;
using Scenarios.Infrastructure.EfCore;
using Scenarios.Infrastructure.Persistence;
using Scenarios.Infrastructure.Repositories;
using Scenarios.Scenarios;

const string ConnectionString = "Host=localhost:5455;Username=postgres;Password=postgres;Database=playground";

IDbConnectionFactory connectionFactory = new NpgsqlConnectionFactory(ConnectionString);

(IAccountRepository accounts, IInventoryRepository inventory, IOrderRepository orders) repos = PickRepositories();

IDatabaseInitializer initializer = new DatabaseInitializer(connectionFactory);
await initializer.InitializeAsync();

ScenarioBase[] scenarios =
[
    new LostUpdateScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new DirtyReadScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new PhantomReadScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new DeadlockScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new InventoryRaceConditionScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new NonRepeatableReadScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new UpdateConflictScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new CascadingTransactionScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new LongRunningTransactionScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
    new OptimisticVsPessimisticScenario(connectionFactory, repos.accounts, repos.inventory, repos.orders),
];

while (true)
{
    Console.Clear();
    Console.WriteLine("=== PostgreSQL Concurrency Practice ===\n");
    Console.WriteLine("Select a scenario:\n");
    for (int i = 0; i < scenarios.Length; i++)
        Console.WriteLine($"{i + 1}. {scenarios[i].Name}");
    Console.WriteLine("11. View Database State");
    Console.WriteLine("0. Exit");
    Console.Write("\nChoice: ");

    if (!int.TryParse(Console.ReadLine(), out int choice)) continue;
    if (choice == 0) break;

    if (choice == 11)
        await ViewDatabaseStateAsync();
    else if (choice >= 1 && choice <= scenarios.Length)
        await RunScenarioAsync(scenarios[choice - 1]);

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey();
}

(IAccountRepository, IInventoryRepository, IOrderRepository) PickRepositories()
{
    Console.Clear();
    Console.WriteLine("=== Select Repository Implementation ===\n");
    Console.WriteLine("1. Npgsql  — raw SQL via Npgsql (default)");
    Console.WriteLine("2. Dapper  — lightweight micro-ORM");
    Console.WriteLine("3. EF Core — full ORM (Entity Framework Core)");
    Console.Write("\nChoice [1]: ");

    var input = Console.ReadLine()?.Trim();

    return input switch
    {
        "2" => (
            new DapperAccountRepository(),
            new DapperInventoryRepository(),
            new DapperOrderRepository()),
        "3" => CreateEfCoreRepositories(),
        _ => (
            new AccountRepository(),
            new InventoryRepository(),
            new OrderRepository()),
    };
}

(IAccountRepository, IInventoryRepository, IOrderRepository) CreateEfCoreRepositories()
{
    var factory = new PlaygroundDbContextFactory(ConnectionString);
    return (
        new EfCoreAccountRepository(factory),
        new EfCoreInventoryRepository(factory),
        new EfCoreOrderRepository(factory));
}

static async Task RunScenarioAsync(ScenarioBase scenario)
{
    Console.Clear();
    Console.WriteLine($"=== {scenario.Name} ===\n");
    Console.WriteLine($"Difficulty: {scenario.Difficulty}");
    Console.WriteLine($"Description:\n{scenario.Description}\n");
    Console.WriteLine("Press any key to start scenario...");
    Console.ReadKey();

    Console.Clear();
    Console.WriteLine($"Running {scenario.Name}...\n");
    var sw = Stopwatch.StartNew();
    try
    {
        await scenario.ExecuteAsync();
        sw.Stop();
        Console.WriteLine($"\n✓ Scenario completed in {sw.ElapsedMilliseconds}ms");
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.WriteLine($"\n✗ Scenario failed: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"  Details: {ex.InnerException.Message}");
    }
}

async Task ViewDatabaseStateAsync()
{
    Console.Clear();
    Console.WriteLine("=== Database State ===\n");

    await using var conn = (NpgsqlConnection)await connectionFactory.OpenConnectionAsync();

    Console.WriteLine("Accounts:");
    var accounts = await repos.accounts.GetAllAsync(conn);
    Console.WriteLine($"{"ID",-5} {"Name",-15} {"Balance",-12} {"Version",-8}");
    Console.WriteLine(new string('-', 40));
    foreach (var a in accounts)
        Console.WriteLine($"{a.Id,-5} {a.Name,-15} {a.Balance,-12:C} {a.Version,-8}");

    Console.WriteLine("\nActive Connections:");
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            SELECT usename, application_name, state, query
            FROM pg_stat_activity
            WHERE datname = 'playground' AND pid != pg_backend_pid()";
        await using var reader = await cmd.ExecuteReaderAsync();
        Console.WriteLine($"{"User",-12} {"App",-20} {"State",-12} {"Query",-40}");
        Console.WriteLine(new string('-', 85));
        while (await reader.ReadAsync())
        {
            var query = reader.GetString(3);
            if (query.Length > 40) query = query[..37] + "...";
            Console.WriteLine($"{reader.GetString(0),-12} {reader.GetString(1),-20} {reader.GetString(2),-12} {query,-40}");
        }
    }

    Console.WriteLine("\nLocks:");
    await using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
            SELECT locktype, relation::regclass as table_name, mode, granted
            FROM pg_locks
            WHERE relation IS NOT NULL
            LIMIT 10";
        await using var reader = await cmd.ExecuteReaderAsync();
        Console.WriteLine($"{"Type",-12} {"Table",-20} {"Mode",-15} {"Granted",-8}");
        Console.WriteLine(new string('-', 55));
        while (await reader.ReadAsync())
            Console.WriteLine($"{reader.GetString(0),-12} {reader.GetString(1),-20} {reader.GetString(2),-15} {reader.GetBoolean(3),-8}");
    }
}
