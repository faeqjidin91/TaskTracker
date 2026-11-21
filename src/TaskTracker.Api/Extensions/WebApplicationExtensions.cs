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

        return app;
    }
}
