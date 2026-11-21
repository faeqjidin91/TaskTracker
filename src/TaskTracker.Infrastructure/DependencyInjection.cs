using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskTracker.Application.Abstractions;
using TaskTracker.Infrastructure.Auth;
using TaskTracker.Infrastructure.Notifications;
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

        services.AddTokenAuthentication(configuration);
        services.AddNotifications(configuration);

        return services;
    }

    private static void AddTokenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // ValidateOnStart, not at registration, so dotnet ef commands work without a signing key.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => o.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be greater than 0.")
            .Validate(
                o => System.Text.Encoding.UTF8.GetByteCount(o.SigningKey) >= JwtOptions.MinimumKeyBytes,
                $"Jwt:SigningKey must be at least {JwtOptions.MinimumKeyBytes} bytes. " +
                "For local development, set it with dotnet user-secrets.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IUserAuthenticator, IdentityUserAuthenticator>();
    }
}
