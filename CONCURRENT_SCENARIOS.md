# PostgreSQL Concurrency Practice Scenarios

## Setup
Use this schema for the scenarios:
```sql
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

INSERT INTO accounts (name, balance) VALUES 
  ('Alice', 1000.00),
  ('Bob', 2000.00),
  ('Charlie', 500.00);
```

---

## Scenario 1: Lost Update Problem
**Difficulty:** Easy | **Isolation Level:** READ COMMITTED

### Problem
Two concurrent transactions both read an account's balance and update it based on the read value.

### Setup
1. Account has $1000
2. Transaction A reads balance: $1000, plans to add $100
3. Transaction B reads balance: $1000, plans to add $50 (simultaneously)
4. Transaction A writes $1100
5. Transaction B writes $1050
6. **Expected:** $1150, **Actual:** $1050

### Practice Tasks
- [ ] Reproduce the lost update
- [ ] Fix using SELECT FOR UPDATE
- [ ] Fix using optimistic locking with version numbers
- [ ] Compare performance of both approaches

---

## Scenario 2: Dirty Read (Write Skew)
**Difficulty:** Easy | **Isolation Level:** READ UNCOMMITTED

### Problem
One transaction reads uncommitted changes from another transaction that later rolls back.

### Setup
1. Transaction A starts and updates balance to $500
2. Transaction B reads the $500 value
3. Transaction A rolls back (balance back to $1000)
4. Transaction B now has stale data

### Practice Tasks
- [ ] Reproduce dirty read
- [ ] Set isolation level to REPEATABLE READ and verify it's prevented
- [ ] Understand the cost of higher isolation levels

---

## Scenario 3: Phantom Read
**Difficulty:** Medium | **Isolation Level:** REPEATABLE READ

### Problem
A transaction reads a set of rows, another transaction inserts/deletes rows in that range, first transaction reads again and sees different row count.

### Setup
1. Transaction A: `SELECT COUNT(*) FROM accounts WHERE balance > 500`
2. Transaction B: `INSERT INTO accounts (name, balance) VALUES ('David', 750)`
3. Transaction A: `SELECT COUNT(*) FROM accounts WHERE balance > 500` (different result!)

### Practice Tasks
- [ ] Reproduce phantom read at REPEATABLE READ level
- [ ] Use SERIALIZABLE to prevent it (note the performance hit)
- [ ] Understand when phantom reads are acceptable in your application

---

## Scenario 4: Deadlock Detection
**Difficulty:** Medium | **Isolation Level:** READ COMMITTED

### Problem
Two transactions lock resources in opposite order, creating a circular dependency.

### Setup
```sql
-- Transaction A: Lock account 1, then account 2
-- Transaction B: Lock account 2, then account 1
-- This creates a deadlock
```

### Practice Tasks
- [ ] Induce a deadlock with two concurrent transactions
- [ ] Observe PostgreSQL's deadlock detection
- [ ] Implement retry logic with exponential backoff
- [ ] Design queries to prevent deadlock (consistent lock ordering)

---

## Scenario 5: Race Condition in Inventory
**Difficulty:** Medium | **Isolation Level:** READ COMMITTED

### Problem
Multiple customers try to buy the same last item simultaneously.

### Setup
```sql
CREATE TABLE inventory (
  id SERIAL PRIMARY KEY,
  product_name VARCHAR(100),
  quantity INT
);

INSERT INTO inventory (product_name, quantity) VALUES ('MacBook', 1);
```

### Practice Tasks
- [ ] Have 5 concurrent transactions each try: `UPDATE inventory SET quantity = quantity - 1 WHERE product_name = 'MacBook'`
- [ ] Verify only one succeeds (overselling prevented)
- [ ] Add a CHECK constraint and see what happens
- [ ] Implement application-level quantity verification

---

## Scenario 6: Non-Repeatable Read
**Difficulty:** Easy | **Isolation Level:** READ COMMITTED

### Problem
Same query returns different results within same transaction because another transaction modified the data.

### Setup
1. Transaction A: `SELECT balance FROM accounts WHERE id = 1` → $1000
2. Transaction B: `UPDATE accounts SET balance = 1500 WHERE id = 1`
3. Transaction A: `SELECT balance FROM accounts WHERE id = 1` → $1500

### Practice Tasks
- [ ] Reproduce at READ COMMITTED
- [ ] Prevent using REPEATABLE READ
- [ ] Understand trade-offs of higher isolation

---

## Scenario 7: Update Conflict Resolution
**Difficulty:** Medium | **Isolation Level:** SERIALIZABLE

### Problem
Two transactions try to update the same row with different changes.

### Setup
```sql
-- Transaction A: UPDATE accounts SET balance = balance + 100 WHERE id = 1
-- Transaction B: UPDATE accounts SET balance = balance - 50 WHERE id = 1
-- These can conflict depending on isolation level
```

### Practice Tasks
- [ ] Test at READ COMMITTED (both succeed, last write wins)
- [ ] Test at SERIALIZABLE (one aborts)
- [ ] Implement conflict detection without SERIALIZABLE
- [ ] Use RETURNING clause to detect conflicts

---

## Scenario 8: Cascading Transactions
**Difficulty:** Medium | **Isolation Level:** READ COMMITTED

### Problem
Multiple transactions depend on each other, one failure should cascade.

### Setup
```sql
-- Transaction A: Create order and reduce balance
-- Transaction B: Waiting on transaction A's balance update
-- If A fails, B should also fail
```

### Practice Tasks
- [ ] Create a multi-step transaction (order → update balance → update inventory)
- [ ] Ensure atomicity (all or nothing)
- [ ] Test rollback behavior
- [ ] Verify no partial state exists

---

## Scenario 9: Long-Running Transaction Blocking
**Difficulty:** Medium | **Isolation Level:** READ COMMITTED

### Problem
A long transaction holds locks, blocking other transactions.

### Setup
1. Transaction A: `BEGIN; UPDATE accounts SET balance = balance + 1; SELECT pg_sleep(10);`
2. While A is sleeping, Transaction B: `UPDATE accounts SET balance = balance - 1 WHERE id = 1`
3. B blocks until A completes

### Practice Tasks
- [ ] Monitor blocking with `SELECT * FROM pg_locks`
- [ ] Use `pg_blocking_pids()` to find blocking queries
- [ ] Implement query timeouts with `statement_timeout`
- [ ] Analyze lock wait times

---

## Scenario 10: Connection Pool Exhaustion
**Difficulty:** Hard | **Isolation Level:** N/A

### Problem
Too many long-running transactions exhaust the connection pool.

### Setup
- Simulate 20 concurrent transactions, each holding a connection
- Connection pool size limited to 10

### Practice Tasks
- [ ] Monitor connection count with `SELECT count(*) FROM pg_stat_activity`
- [ ] Implement connection pooling (pgBouncer or PgPool)
- [ ] Test different pooling modes (session, transaction)
- [ ] Set appropriate idle connection timeouts

---

## Scenario 11: Serialization Anomaly
**Difficulty:** Hard | **Isolation Level:** SERIALIZABLE

### Problem
Constraint on sum of values in a group is violated under REPEATABLE READ.

### Setup
```sql
CREATE TABLE account_groups (
  group_id INT,
  account_id INT,
  amount DECIMAL(10, 2)
);

-- Constraint: sum of amounts per group must be <= 1000
```

### Practice Tasks
- [ ] Reproduce the anomaly at REPEATABLE READ
- [ ] Fix using SERIALIZABLE (note: may cause serialization failures)
- [ ] Implement explicit locks to prevent anomaly
- [ ] Compare SERIALIZABLE behavior vs explicit locking

---

## Scenario 12: Write Skew
**Difficulty:** Hard | **Isolation Level:** SERIALIZABLE

### Problem
Two transactions both check a constraint, both pass, but combined they violate it.

### Setup
```sql
-- Doctor scheduling: two doctors read the on-call count, both see < 2 allowed
-- Both insert themselves, now 3 doctors on call (violation!)
```

### Practice Tasks
- [ ] Reproduce write skew at REPEATABLE READ
- [ ] Use SERIALIZABLE to prevent
- [ ] Implement explicit locking strategy
- [ ] Understand when write skew is acceptable

---

## Scenario 13: Transaction Isolation Performance Test
**Difficulty:** Medium | **Isolation Level:** All levels

### Problem
Compare performance across isolation levels.

### Practice Tasks
- [ ] Run same workload at READ COMMITTED
- [ ] Run same workload at REPEATABLE READ
- [ ] Run same workload at SERIALIZABLE
- [ ] Measure throughput, latency, conflict rate
- [ ] Document trade-offs

---

## Scenario 14: Optimistic vs Pessimistic Locking
**Difficulty:** Medium | **Isolation Level:** READ COMMITTED

### Setup
Two approaches to prevent lost updates:
- **Pessimistic:** Lock immediately with SELECT FOR UPDATE
- **Optimistic:** Version number, detect conflicts on update

### Practice Tasks
- [ ] Implement pessimistic locking (SELECT FOR UPDATE)
- [ ] Implement optimistic locking (version column)
- [ ] Test under light contention (pessimistic wins)
- [ ] Test under heavy contention (optimistic wins)
- [ ] Measure deadlock rates with each approach

---

## Scenario 15: Monitoring and Debugging
**Difficulty:** Hard | **Isolation Level:** All levels

### Practice Tasks
- [ ] Use `pg_stat_statements` to find slow queries
- [ ] Use `pg_stat_activity` to monitor active connections
- [ ] Use `pg_locks` to inspect lock holders
- [ ] Enable `log_lock_waits` and analyze logs
- [ ] Use `EXPLAIN ANALYZE` to identify blocking operations
- [ ] Set up alerts for long-running transactions

---

## Tools for Testing

### Run Concurrent Transactions
```bash
# Terminal 1
psql -U postgres -d playground

# Terminal 2
psql -U postgres -d playground

# Terminal 3
psql -U postgres -d playground
```

### Useful Queries
```sql
-- View all locks
SELECT * FROM pg_locks;

-- View blocking relationships
SELECT blocked_locks.pid AS blocked_pid,
       blocked_activity.usename AS blocked_user,
       blocking_locks.pid AS blocking_pid,
       blocking_activity.usename AS blocking_user,
       blocked_activity.query AS blocked_statement,
       blocking_activity.query AS blocking_statement
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
  AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
  AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
  AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
  AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
  AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
  AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
  AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
  AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
  AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
  AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;

-- View isolation level
SHOW TRANSACTION ISOLATION LEVEL;

-- Set isolation level
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
```

---

## Progression Path
1. Start with Scenarios 1-3 (basic problems)
2. Move to Scenarios 4-8 (intermediate complexity)
3. Tackle Scenarios 9-12 (harder edge cases)
4. Scenario 13-15 (performance & debugging)

Good luck practicing! 🚀
