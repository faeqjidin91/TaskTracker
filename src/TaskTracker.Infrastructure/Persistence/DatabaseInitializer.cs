using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDevelopmentDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(ct);
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedDevelopmentAsync();
    }

    public static async Task BootstrapSuperAdminAsync(this IServiceProvider services, string email, string password)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().BootstrapSuperAdminAsync(email, password);
    }
}
