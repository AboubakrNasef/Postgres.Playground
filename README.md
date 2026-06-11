# PostgreSQL Concurrency Practice Application

A .NET console application for practicing PostgreSQL concurrency scenarios with real concurrent transactions.

## Prerequisites

- .NET 8.0 or later
- PostgreSQL running in Docker (via `docker-compose.yml`)
- Npgsql package (auto-installed)

## Setup

### 1. Start PostgreSQL

```powershell
docker-compose up -d
```

Verify it's running:
```powershell
docker-compose ps
```

### 2. Build the Application

```powershell
dotnet build
```

### 3. Run the Application

```powershell
dotnet run
```

## Usage

The application presents an interactive menu:

1. **Scenario 1: Lost Update Problem**
   - Two transactions read-modify-write the same value
   - Demonstrates how one update can be lost
   - Learn: SELECT FOR UPDATE or optimistic locking

2. **Scenario 2: Dirty Read**
   - Transaction reads uncommitted changes from another transaction
   - Learn: ACID properties and isolation levels

3. **Scenario 3: Phantom Read**
   - Transaction reads different row counts when querying twice
   - Learn: Range queries and serialization

4. **Scenario 4: Deadlock Detection**
   - Two transactions lock resources in opposite order
   - Observe PostgreSQL's deadlock detection
   - Learn: Lock ordering and retry strategies

5. **Scenario 5: Race Condition in Inventory**
   - Multiple concurrent customers buying last item
   - Learn: Synchronization and atomic operations

6. **Scenario 6: Non-Repeatable Read**
   - Same query returns different results within one transaction
   - Learn: Isolation level trade-offs

7. **Scenario 7: Update Conflict Resolution**
   - Two transactions updating the same row
   - Learn: Conflict handling strategies

8. **Scenario 8: Cascading Transactions**
   - Multi-step transaction that must be atomic
   - Learn: Savepoints and partial rollback

9. **Scenario 9: Long-Running Transaction Blocking**
   - One transaction blocks others by holding locks
   - Learn: Lock contention and performance impact

10. **Scenario 10: Optimistic vs Pessimistic Locking**
    - Compare version-based vs lock-based strategies
    - Learn: Trade-offs under different contention levels

11. **View Database State**
    - See current accounts, connections, and locks
    - Useful for debugging

## Example: Running Scenario 1

```
=== PostgreSQL Concurrency Practice ===

Select a scenario:

1. Lost Update Problem
...

Choice: 1

=== Scenario 1: Lost Update Problem ===

Difficulty: Easy
Description:
Two concurrent transactions both read an account balance and update it.
Without proper locking, one update is lost.
Expected: $1150 | Actual (with bug): $1050

Press any key to start scenario...

[Running Scenario 1: Lost Update Problem...]

[14:32:45.123] Initial balance for account 1: $1000
[14:32:45.234] Starting two concurrent transactions...
[14:32:45.345] TX1: Reading balance...
[14:32:45.456] TX1: Read balance = $1000
[14:32:45.567] TX2: Reading balance...
[14:32:45.678] TX2: Read balance = $1000
[14:32:45.789] TX1: Updating balance to $1100...
[14:32:45.890] TX2: Updating balance to $1050...
[14:32:46.001] TX1: Update complete
[14:32:46.112] TX2: Update complete

Final balance: $1050
Expected: $1150, Actual: $1050
✗ Lost update detected!

✓ Scenario completed in 901ms
```

## Key Learning Points

### Isolation Levels (in PostgreSQL)
- **READ UNCOMMITTED**: Like READ COMMITTED (PostgreSQL doesn't support true READ UNCOMMITTED)
- **READ COMMITTED**: Default, allows dirty reads within transaction scope
- **REPEATABLE READ**: Prevents non-repeatable reads (default in app)
- **SERIALIZABLE**: Prevents all anomalies, highest consistency

### Lock Types
- **AccessShareLock**: SELECT queries
- **RowShareLock**: SELECT FOR SHARE
- **RowExclusiveLock**: UPDATE, DELETE
- **ExclusiveLock**: SELECT FOR UPDATE
- **AccessExclusiveLock**: ALTER TABLE, DROP

### Strategies

**Pessimistic Locking**
```sql
SELECT * FROM accounts WHERE id = 1 FOR UPDATE;
UPDATE accounts SET balance = balance + 100 WHERE id = 1;
```
- Good when: High contention, critical sections
- Bad when: Low contention, unnecessary waiting

**Optimistic Locking**
```sql
SELECT balance, version FROM accounts WHERE id = 1;
-- ... modify balance ...
UPDATE accounts SET balance = @balance, version = version + 1
WHERE id = 1 AND version = @oldVersion;
```
- Good when: Low contention, high throughput
- Bad when: High conflicts, complex retry logic

## Troubleshooting

### "Connection refused"
- Check Docker: `docker-compose ps`
- Restart: `docker-compose down && docker-compose up -d`

### "Deadlock detected"
- Normal! The deadlock scenario intentionally triggers this
- PostgreSQL handles it automatically

### Application hangs
- A transaction might be waiting for a lock
- Check "View Database State" for active connections
- Restart the application if stuck

## Advanced: Manual Testing

You can also use `psql` directly while running scenarios:

```powershell
# Terminal 1
docker-compose exec postgres psql -U postgres -d playground

# Terminal 2
dotnet run
```

Then in `psql`:
```sql
-- View all connections
SELECT pid, usename, application_name, state FROM pg_stat_activity;

-- View locks
SELECT locktype, relation::regclass, mode, granted FROM pg_locks;

-- View accounts
SELECT * FROM accounts;
```

## Extension Ideas

1. **Add more scenarios**
   - Saga pattern failures
   - Write skew
   - Serialization anomalies

2. **Performance metrics**
   - Measure latency per scenario
   - Compare isolation levels
   - Lock wait times

3. **Connection pooling**
   - Test with pgBouncer
   - Compare pooling modes

4. **Retry logic**
   - Implement exponential backoff
   - Compare retry strategies

## Files

- `Scenarios.csproj` - Project file
- `Program.cs` - Main application with all scenarios
- `docker-compose.yml` - PostgreSQL setup
- `CONCURRENT_SCENARIOS.md` - Detailed scenario descriptions

## Notes

- Database is reset on each application startup
- Timestamps show millisecond precision for accurate sequencing
- All scenarios run with real PostgreSQL (no mocks)
- Transaction boundaries are explicit and visible

Good luck practicing! 🚀
