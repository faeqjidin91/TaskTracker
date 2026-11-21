using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDevelopmentDatabaseAsync(
        this IServiceProvider services, SeedOptions seed, CancellationToken ct = default)
    {
        // Fails before migrating, so a missing .env value does not leave a half-prepared database.
        seed.EnsureValidForDevelopment();

        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedDevelopmentAsync(seed, ct);
    }

    public static async Task SeedDeploymentAsync(
        this IServiceProvider services, SeedOptions seed, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedDeploymentAsync(seed, ct);
    }
}
