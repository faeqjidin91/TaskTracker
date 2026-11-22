using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TaskTracker.Infrastructure.Health;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var seed = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

        if (app.Environment.IsDevelopment())
        {
            await app.Services.InitializeDevelopmentDatabaseAsync(seed);
        }
        else
        {
            await app.Services.SeedDeploymentAsync(seed);
        }
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // CORS runs first so browsers can also read error responses.
        app.UseCors(ApiServiceCollectionExtensions.WebCorsPolicy);
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthEndpoints();

        return app;
    }

    // Probes cannot send a JWT, so these are public and return only the overall status.
    private static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckSettings.ReadyTag)
        }).AllowAnonymous();
    }
}
