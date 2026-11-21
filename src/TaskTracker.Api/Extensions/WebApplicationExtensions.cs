using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            await app.Services.InitializeDevelopmentDatabaseAsync();
        }

        // Outside Development, the first SuperAdmin comes from secrets and is created only once.
        var email = app.Configuration["Bootstrap:SuperAdminEmail"];
        var password = app.Configuration["Bootstrap:SuperAdminPassword"];

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrEmpty(password))
        {
            await app.Services.BootstrapSuperAdminAsync(email, password);
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
