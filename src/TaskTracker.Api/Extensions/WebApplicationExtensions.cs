using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDevelopmentDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            await app.Services.InitializeDatabaseAsync();
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
