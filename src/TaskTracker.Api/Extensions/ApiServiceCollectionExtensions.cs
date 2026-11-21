using System.Text.Json.Serialization;
using TaskTracker.Api.Auth;
using TaskTracker.Api.Errors;
using TaskTracker.Application.Abstractions;

namespace TaskTracker.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public const string WebCorsPolicy = "WebApp";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Enums travel as names ("InProgress"), so the API contract does not depend on numbers.
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddJwtAuthentication();
        services.AddSwaggerWithJwt();
        services.AddWebCors(configuration);

        return services;
    }

    private static void AddWebCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Semicolon-separated so the Web app's http and https dev URLs can both be allowed.
        var origins = (configuration["WebOrigin"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Location")));
    }
}
