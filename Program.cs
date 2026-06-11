using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    const string ConnectionString = "Host=localhost;Username=postgres;Password=postgres;Database=playground";

    static async Task Main(string[] args)
    {
        await InitializeDatabase();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== PostgreSQL Concurrency Practice ===\n");
            Console.WriteLine("Select a scenario:\n");
            Console.WriteLine("1. Lost Update Problem");
            Console.WriteLine("2. Dirty Read");
            Console.WriteLine("3. Phantom Read");
            Console.WriteLine("4. Deadlock Detection");
            Console.WriteLine("5. Race Condition in Inventory");
            Console.WriteLine("6. Non-Repeatable Read");
            Console.WriteLine("7. Update Conflict Resolution");
            Console.WriteLine("8. Cascading Transactions");
            Console.WriteLine("9. Long-Running Transaction Blocking");
            Console.WriteLine("10. Optimistic vs Pessimistic Locking");
            Console.WriteLine("11. View Database State");
            Console.WriteLine("0. Exit");
            Console.Write("\nChoice: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
                continue;

            switch (choice)
            {
                case 1:
                    await RunScenario(new LostUpdateScenario());
                    break;
                case 2:
                    await RunScenario(new DirtyReadScenario());
                    break;
                case 3:
                    await RunScenario(new PhantomReadScenario());
                    break;
                case 4:
                    await RunScenario(new DeadlockScenario());
                    break;
                case 5:
                    await RunScenario(new InventoryRaceConditionScenario());
                    break;
                case 6:
                    await RunScenario(new NonRepeatableReadScenario());
                    break;
                case 7:
                    await RunScenario(new UpdateConflictScenario());
                    break;
                case 8:
                    await RunScenario(new CascadingTransactionScenario());
                    break;
                case 9:
                    await RunScenario(new LongRunningTransactionScenario());
                    break;
                case 10:
                    await RunScenario(new OptimisticVsPessimisticScenario());
                    break;
                case 11:
                    await ViewDatabaseState();
                    break;
                case 0:
                    return;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    static async Task RunScenario(ScenarioBase scenario)
    {
        Console.Clear();
        Console.WriteLine($"=== {scenario.Name} ===\n");
        Console.WriteLine($"Difficulty: {scenario.Difficulty}");
        Console.WriteLine($"Description:\n{scenario.Description}\n");
        Console.WriteLine("Press any key to start scenario...");
        Console.ReadKey();

        Console.Clear();
        Console.WriteLine($"Running {scenario.Name}...\n");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await scenario.Execute(ConnectionString);
            stopwatch.Stop();
            Console.WriteLine($"\n✓ Scenario completed in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"\n✗ Scenario failed: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  Details: {ex.InnerException.Message}");
        }
    }

    static async Task ViewDatabaseState()
    {
        Console.Clear();
        Console.WriteLine("=== Database State ===\n");

        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            Console.WriteLine("Accounts:");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, balance, version FROM accounts ORDER BY id";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Console.WriteLine($"{"ID",-5} {"Name",-15} {"Balance",-12} {"Version",-8}");
                    Console.WriteLine(new string('-', 40));
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"{reader.GetInt32(0),-5} {reader.GetString(1),-15} {reader.GetDecimal(2),-12:C} {reader.GetInt32(3),-8}");
                    }
                }
            }

            Console.WriteLine("\nActive Connections:");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT usename, application_name, state, query
                    FROM pg_stat_activity
                    WHERE datname = 'playground' AND pid != pg_backend_pid()";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Console.WriteLine($"{"User",-12} {"App",-20} {"State",-12} {"Query",-40}");
                    Console.WriteLine(new string('-', 85));
                    while (await reader.ReadAsync())
                    {
                        var query = reader.GetString(3);
                        if (query.Length > 40)
                            query = query.Substring(0, 37) + "...";
                        Console.WriteLine($"{reader.GetString(0),-12} {reader.GetString(1),-20} {reader.GetString(2),-12} {query,-40}");
                    }
                }
            }

            Console.WriteLine("\nLocks:");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT locktype, relation::regclass as table_name, mode, granted
                    FROM pg_locks
                    WHERE relation IS NOT NULL
                    LIMIT 10";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    Console.WriteLine($"{"Type",-12} {"Table",-20} {"Mode",-15} {"Granted",-8}");
                    Console.WriteLine(new string('-', 55));
                    while (await reader.ReadAsync())
                    {
                        Console.WriteLine($"{reader.GetString(0),-12} {reader.GetString(1),-20} {reader.GetString(2),-15} {reader.GetBoolean(3),-8}");
                    }
                }
            }
        }
    }

    static async Task InitializeDatabase()
    {
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            // Drop existing tables
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    DROP TABLE IF EXISTS inventory CASCADE;
                    DROP TABLE IF EXISTS orders CASCADE;
                    DROP TABLE IF EXISTS accounts CASCADE;
                    DROP TABLE IF EXISTS account_groups CASCADE;
                ";
                await cmd.ExecuteNonQueryAsync();
            }

            // Create tables
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE accounts (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(100),
                        balance DECIMAL(10, 2),
                        version INT DEFAULT 0
                    );

                    CREATE TABLE orders (
                        id SERIAL PRIMARY KEY,
                        user_id INT REFERENCES accounts(id),
                        amount DECIMAL(10, 2),
                        status VARCHAR(20) DEFAULT 'pending',
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE inventory (
                        id SERIAL PRIMARY KEY,
                        product_name VARCHAR(100),
                        quantity INT
                    );

                    CREATE TABLE account_groups (
                        group_id INT,
                        account_id INT,
                        amount DECIMAL(10, 2)
                    );

                    INSERT INTO accounts (name, balance, version) VALUES
                        ('Alice', 1000.00, 0),
                        ('Bob', 2000.00, 0),
                        ('Charlie', 500.00, 0);

                    INSERT INTO inventory (product_name, quantity) VALUES
                        ('MacBook', 1),
                        ('iPhone', 5);
                ";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}

// Base scenario class
abstract class ScenarioBase
{
    public abstract string Name { get; }
    public abstract string Difficulty { get; }
    public abstract string Description { get; }
    public abstract Task Execute(string connectionString);

    protected void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

    protected async Task<T> ExecuteInTransaction<T>(
        string connectionString,
        Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> action,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
    {
        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            using (var transaction = conn.BeginTransaction(isolationLevel))
            {
                try
                {
                    var result = await action(conn, transaction);
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }

    protected async Task<decimal> GetAccountBalance(NpgsqlConnection conn, int accountId)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT balance FROM accounts WHERE id = @id";
            cmd.Parameters.AddWithValue("id", accountId);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (decimal)result : 0m;
        }
    }

    protected async Task UpdateAccountBalance(NpgsqlConnection conn, int accountId, decimal newBalance)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "UPDATE accounts SET balance = @balance WHERE id = @id";
            cmd.Parameters.AddWithValue("balance", newBalance);
            cmd.Parameters.AddWithValue("id", accountId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

// Scenario 1: Lost Update
class LostUpdateScenario : ScenarioBase
{
    public override string Name => "Scenario 1: Lost Update Problem";
    public override string Difficulty => "Easy";
    public override string Description => @"
Two concurrent transactions both read an account balance and update it.
Without proper locking, one update is lost.
Expected: $1150 | Actual (with bug): $1050";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;
        const decimal amount1 = 100m;
        const decimal amount2 = 50m;

        Log($"Initial balance for account {accountId}: $1000");
        Log("Starting two concurrent transactions...\n");

        var task1 = Task.Run(async () =>
        {
            await Task.Delay(100); // Ensure they read at same time
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX1: Reading balance...");
                var balance = await GetAccountBalance(conn, accountId);
                Log($"TX1: Read balance = ${balance}");

                await Task.Delay(200); // Simulate processing

                Log($"TX1: Updating balance to ${balance + amount1}...");
                await UpdateAccountBalance(conn, accountId, balance + amount1);
                Log($"TX1: Update complete");
                return true;
            });
        });

        var task2 = Task.Run(async () =>
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX2: Reading balance...");
                var balance = await GetAccountBalance(conn, accountId);
                Log($"TX2: Read balance = ${balance}");

                await Task.Delay(300); // Simulate processing

                Log($"TX2: Updating balance to ${balance + amount2}...");
                await UpdateAccountBalance(conn, accountId, balance + amount2);
                Log($"TX2: Update complete");
                return true;
            });
        });

        await Task.WhenAll(task1, task2);

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var finalBalance = await GetAccountBalance(conn, accountId);
            Log($"\nFinal balance: ${finalBalance}");
            Log($"Expected: $1150, Actual: ${finalBalance}");
            if (finalBalance == 1150m)
                Log("✓ Lost update prevented!");
            else
                Log("✗ Lost update detected!");
        }
    }
}

// Scenario 2: Dirty Read
class DirtyReadScenario : ScenarioBase
{
    public override string Name => "Scenario 2: Dirty Read";
    public override string Difficulty => "Easy";
    public override string Description => @"
Transaction A updates a balance.
Transaction B reads the updated value before Transaction A commits.
If A rolls back, B has dirty (uncommitted) data.";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;

        Log($"Initial balance: $1000");
        Log("Starting concurrent transactions...\n");

        var task1 = Task.Run(async () =>
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    try
                    {
                        Log("TX1: Starting long transaction, updating balance to $1500...");
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "UPDATE accounts SET balance = 1500 WHERE id = @id";
                            cmd.Parameters.AddWithValue("id", accountId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await Task.Delay(1000); // Hold transaction open
                        Log("TX1: Rolling back transaction...");
                        await tx.RollbackAsync();
                    }
                    catch (Exception ex)
                    {
                        Log($"TX1: Error - {ex.Message}");
                    }
                }
            }
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(200); // Let TX1 update first

            Log("TX2: Reading balance at READ UNCOMMITTED...");
            var balance = await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                var bal = await GetAccountBalance(conn, accountId);
                Log($"TX2: Read balance = ${bal}");
                return bal;
            }, System.Data.IsolationLevel.ReadUncommitted);

            Log($"TX2: After TX1 rollback, balance is actually $1000");
            Log(balance == 1500m ? "✓ Dirty read occurred!" : "✗ Dirty read prevented");
        });

        await Task.WhenAll(task1, task2);
    }
}

// Scenario 3: Phantom Read
class PhantomReadScenario : ScenarioBase
{
    public override string Name => "Scenario 3: Phantom Read";
    public override string Difficulty => "Medium";
    public override string Description => @"
Transaction A reads count of accounts with balance > 500.
Transaction B inserts a new account.
Transaction A reads count again and sees different result.";

    public override async Task Execute(string connectionString)
    {
        Log("TX1: Reading count of accounts with balance > 500...");
        var count1 = 0;

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM accounts WHERE balance > 500";
                    count1 = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    Log($"TX1: Count = {count1}");
                }

                await Task.Delay(500); // Hold transaction open

                Log("TX1: Reading count again...");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM accounts WHERE balance > 500";
                    var count2 = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    Log($"TX1: Count = {count2}");
                    if (count2 != count1)
                        Log("✓ Phantom read detected!");
                }
                return true;
            }, System.Data.IsolationLevel.RepeatableRead);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(200);
            Log("TX2: Inserting new account with balance $750...");
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO accounts (name, balance, version) VALUES (@name, @balance, 0)";
                    cmd.Parameters.AddWithValue("name", "NewAccount");
                    cmd.Parameters.AddWithValue("balance", 750m);
                    await cmd.ExecuteNonQueryAsync();
                }
                Log("TX2: Insert complete");
                return true;
            });
        });

        await Task.WhenAll(task1, task2);
    }
}

// Scenario 4: Deadlock Detection
class DeadlockScenario : ScenarioBase
{
    public override string Name => "Scenario 4: Deadlock Detection";
    public override string Difficulty => "Medium";
    public override string Description => @"
Two transactions lock resources in opposite order.
TX1 locks account 1, then tries to lock account 2.
TX2 locks account 2, then tries to lock account 1.
Creates a circular dependency leading to deadlock.";

    public override async Task Execute(string connectionString)
    {
        Log("Setting up deadlock scenario...\n");

        var task1 = Task.Run(async () =>
        {
            try
            {
                await ExecuteInTransaction(connectionString, async (conn, tx) =>
                {
                    Log("TX1: Locking account 1...");
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM accounts WHERE id = 1 FOR UPDATE";
                        await cmd.ExecuteScalarAsync();
                    }
                    Log("TX1: Locked account 1, waiting before locking account 2...");
                    await Task.Delay(500);

                    Log("TX1: Trying to lock account 2...");
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM accounts WHERE id = 2 FOR UPDATE";
                        await cmd.ExecuteScalarAsync();
                    }
                    Log("TX1: Locked both accounts");
                    return true;
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01")
            {
                Log($"✓ TX1: Deadlock detected! {ex.Message}");
            }
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(100); // Start slightly after TX1

            try
            {
                await ExecuteInTransaction(connectionString, async (conn, tx) =>
                {
                    Log("TX2: Locking account 2...");
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM accounts WHERE id = 2 FOR UPDATE";
                        await cmd.ExecuteScalarAsync();
                    }
                    Log("TX2: Locked account 2, waiting before locking account 1...");
                    await Task.Delay(500);

                    Log("TX2: Trying to lock account 1...");
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM accounts WHERE id = 1 FOR UPDATE";
                        await cmd.ExecuteScalarAsync();
                    }
                    Log("TX2: Locked both accounts");
                    return true;
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01")
            {
                Log($"✓ TX2: Deadlock detected! {ex.Message}");
            }
        });

        try
        {
            await Task.WhenAll(task1, task2);
        }
        catch (AggregateException)
        {
            // Expected - deadlock will fail one transaction
        }

        Log("\nNote: PostgreSQL detected and resolved the deadlock by aborting one transaction.");
    }
}

// Scenario 5: Inventory Race Condition
class InventoryRaceConditionScenario : ScenarioBase
{
    public override string Name => "Scenario 5: Race Condition in Inventory";
    public override string Difficulty => "Medium";
    public override string Description => @"
Multiple customers try to buy the last item simultaneously.
Without proper synchronization, inventory can go negative.";

    public override async Task Execute(string connectionString)
    {
        Log("Starting: 5 customers trying to buy MacBook (only 1 available)\n");

        var tasks = new List<Task>();
        for (int i = 1; i <= 5; i++)
        {
            int customerId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteInTransaction(connectionString, async (conn, tx) =>
                    {
                        Log($"Customer {customerId}: Checking inventory...");
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT quantity FROM inventory WHERE product_name = 'MacBook'";
                            var qty = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                            Log($"Customer {customerId}: Found {qty} in stock");

                            if (qty > 0)
                            {
                                await Task.Delay(100); // Simulate processing
                                Log($"Customer {customerId}: Purchasing...");
                                using (var updateCmd = conn.CreateCommand())
                                {
                                    updateCmd.CommandText = "UPDATE inventory SET quantity = quantity - 1 WHERE product_name = 'MacBook'";
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                                Log($"Customer {customerId}: ✓ Purchase successful");
                            }
                            else
                            {
                                Log($"Customer {customerId}: ✗ Out of stock");
                            }
                        }
                        return true;
                    });
                }
                catch (Exception ex)
                {
                    Log($"Customer {customerId}: Error - {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT quantity FROM inventory WHERE product_name = 'MacBook'";
                var finalQty = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Log($"\nFinal inventory: {finalQty}");
                if (finalQty < 0)
                    Log("✗ Race condition: inventory went negative!");
                else
                    Log("✓ Inventory protected (likely due to lock contention)");
            }
        }
    }
}

// Scenario 6: Non-Repeatable Read
class NonRepeatableReadScenario : ScenarioBase
{
    public override string Name => "Scenario 6: Non-Repeatable Read";
    public override string Difficulty => "Easy";
    public override string Description => @"
Transaction A reads a value, then another transaction modifies it,
and Transaction A reads again with different result.";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;
        decimal read1 = 0, read2 = 0;

        Log($"Account {accountId} initial balance: $1000\n");

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX1: Reading balance...");
                read1 = await GetAccountBalance(conn, accountId);
                Log($"TX1: Balance = ${read1}");

                await Task.Delay(300); // Wait for TX2 to modify

                Log("TX1: Reading balance again...");
                read2 = await GetAccountBalance(conn, accountId);
                Log($"TX1: Balance = ${read2}");

                if (read1 != read2)
                    Log("✓ Non-repeatable read detected!");
                return true;
            }, System.Data.IsolationLevel.ReadCommitted);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(100);
            Log("TX2: Modifying balance to $1500...");
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                await UpdateAccountBalance(conn, accountId, 1500m);
                Log("TX2: Update complete");
                return true;
            });
        });

        await Task.WhenAll(task1, task2);
    }
}

// Scenario 7: Update Conflict
class UpdateConflictScenario : ScenarioBase
{
    public override string Name => "Scenario 7: Update Conflict Resolution";
    public override string Difficulty => "Medium";
    public override string Description => @"
Two transactions try to update the same row.
At READ COMMITTED, both succeed.
At SERIALIZABLE, one fails.";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;

        Log("Scenario A: READ COMMITTED (last write wins)\n");

        var task1 = Task.Run(async () =>
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX1: Adding $100 to account 1...");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE accounts SET balance = balance + 100 WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await Task.Delay(100);
                Log("TX1: Complete");
                return true;
            }, System.Data.IsolationLevel.ReadCommitted);
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(50);
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX2: Subtracting $50 from account 1...");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE accounts SET balance = balance - 50 WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }
                Log("TX2: Complete");
                return true;
            }, System.Data.IsolationLevel.ReadCommitted);
        });

        await Task.WhenAll(task1, task2);

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var balance = await GetAccountBalance(conn, accountId);
            Log($"\nFinal balance at READ COMMITTED: ${balance} (both updates applied: +100-50=$1050)");
        }
    }
}

// Scenario 8: Cascading Transactions
class CascadingTransactionScenario : ScenarioBase
{
    public override string Name => "Scenario 8: Cascading Transactions";
    public override string Difficulty => "Medium";
    public override string Description => @"
A multi-step transaction: create order, reduce balance, update inventory.
Either all succeed or all fail (atomicity).";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;
        const decimal orderAmount = 200m;

        Log("Starting transaction: Create order, reduce balance, update inventory\n");

        try
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("Step 1: Checking account balance...");
                var balance = await GetAccountBalance(conn, accountId);
                if (balance < orderAmount)
                {
                    Log($"✗ Insufficient funds (${balance} < ${orderAmount})");
                    throw new InvalidOperationException("Insufficient funds");
                }

                Log("Step 2: Creating order...");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO orders (user_id, amount, status) VALUES (@user_id, @amount, 'pending')";
                    cmd.Parameters.AddWithValue("user_id", accountId);
                    cmd.Parameters.AddWithValue("amount", orderAmount);
                    await cmd.ExecuteNonQueryAsync();
                }

                Log("Step 3: Reducing account balance...");
                await UpdateAccountBalance(conn, accountId, balance - orderAmount);

                Log("Step 4: Simulating inventory update...");
                await Task.Delay(50);

                Log("✓ All steps completed successfully");
                return true;
            });
        }
        catch (Exception ex)
        {
            Log($"✗ Transaction rolled back: {ex.Message}");
        }

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var balance = await GetAccountBalance(conn, accountId);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM orders WHERE user_id = @user_id";
                cmd.Parameters.AddWithValue("user_id", accountId);
                var orderCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Log($"\nFinal state: Balance=${balance}, Orders={orderCount}");
            }
        }
    }
}

// Scenario 9: Long-Running Transaction Blocking
class LongRunningTransactionScenario : ScenarioBase
{
    public override string Name => "Scenario 9: Long-Running Transaction Blocking";
    public override string Difficulty => "Medium";
    public override string Description => @"
A long transaction holds locks, blocking other transactions.
Demonstrates importance of keeping transactions short.";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 1;

        Log("Starting long-running transaction...\n");

        var longTask = Task.Run(async () =>
        {
            await ExecuteInTransaction(connectionString, async (conn, tx) =>
            {
                Log("TX1: Starting long transaction, updating balance...");
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE accounts SET balance = balance + 1 WHERE id = @id";
                    cmd.Parameters.AddWithValue("id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }

                Log("TX1: Holding lock for 3 seconds...");
                await Task.Delay(3000);
                Log("TX1: Releasing lock");
                return true;
            });
        });

        var blockingTask = Task.Run(async () =>
        {
            await Task.Delay(500); // Start after TX1

            var stopwatch = Stopwatch.StartNew();
            Log("TX2: Attempting to update account (will be blocked)...");

            try
            {
                await ExecuteInTransaction(connectionString, async (conn, tx) =>
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE accounts SET balance = balance - 1 WHERE id = @id";
                        cmd.Parameters.AddWithValue("id", accountId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    return true;
                });
                stopwatch.Stop();
                Log($"TX2: ✓ Update complete (waited {stopwatch.ElapsedMilliseconds}ms)");
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

// Scenario 10: Optimistic vs Pessimistic Locking
class OptimisticVsPessimisticScenario : ScenarioBase
{
    public override string Name => "Scenario 10: Optimistic vs Pessimistic Locking";
    public override string Difficulty => "Medium";
    public override string Description => @"
Compare two locking strategies:
1. Pessimistic: Lock immediately with SELECT FOR UPDATE
2. Optimistic: Use version numbers to detect conflicts";

    public override async Task Execute(string connectionString)
    {
        const int accountId = 3; // Use Charlie's account
        int successCount = 0;
        int conflictCount = 0;

        Log("Running 5 concurrent updates with optimistic locking...\n");

        var tasks = new List<Task>();
        for (int i = 1; i <= 5; i++)
        {
            int txId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteInTransaction(connectionString, async (conn, tx) =>
                    {
                        Log($"TX{txId}: Reading account...");
                        var (currentBalance, currentVersion) = await GetAccountWithVersion(conn, accountId);
                        Log($"TX{txId}: Balance=${currentBalance}, Version={currentVersion}");

                        await Task.Delay(50); // Simulate processing

                        Log($"TX{txId}: Attempting update...");
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                UPDATE accounts
                                SET balance = @newBalance, version = version + 1
                                WHERE id = @id AND version = @version";
                            cmd.Parameters.AddWithValue("newBalance", currentBalance + 10m);
                            cmd.Parameters.AddWithValue("id", accountId);
                            cmd.Parameters.AddWithValue("version", currentVersion);
                            var affected = await cmd.ExecuteNonQueryAsync();

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
                        }
                        return true;
                    });
                }
                catch (Exception ex)
                {
                    Log($"TX{txId}: Error - {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var (finalBalance, finalVersion) = await GetAccountWithVersion(conn, accountId);
            Log($"\nResults:");
            Log($"  Successful updates: {successCount}");
            Log($"  Conflicts detected: {conflictCount}");
            Log($"  Final balance: ${finalBalance}");
            Log($"  Final version: {finalVersion}");
        }
    }

    private async Task<(decimal balance, int version)> GetAccountWithVersion(NpgsqlConnection conn, int accountId)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT balance, version FROM accounts WHERE id = @id";
            cmd.Parameters.AddWithValue("id", accountId);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                    return ((decimal)reader[0], (int)reader[1]);
            }
        }
        return (0m, 0);
    }
}
