using Microsoft.EntityFrameworkCore;

namespace Scenarios.Infrastructure.EfCore;

public sealed class PlaygroundDbContextFactory : IDbContextFactory<PlaygroundDbContext>
{
    private readonly DbContextOptions<PlaygroundDbContext> _options;

    public PlaygroundDbContextFactory(string connectionString)
    {
        _options = new DbContextOptionsBuilder<PlaygroundDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public PlaygroundDbContext CreateDbContext() => new(_options);
}
