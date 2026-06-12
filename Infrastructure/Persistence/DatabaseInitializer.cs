using Npgsql;
using Scenarios.Application.Interfaces;

namespace Scenarios.Infrastructure.Persistence;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DatabaseInitializer(IDbConnectionFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await using var conn = (NpgsqlConnection)await _factory.OpenConnectionAsync();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                DROP TABLE IF EXISTS inventory CASCADE;
                DROP TABLE IF EXISTS orders CASCADE;
                DROP TABLE IF EXISTS accounts CASCADE;
                DROP TABLE IF EXISTS account_groups CASCADE;";
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = conn.CreateCommand())
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
                    ('iPhone', 5);";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
