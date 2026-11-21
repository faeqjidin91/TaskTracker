using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Application.Abstractions;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<IdentityUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }
}
