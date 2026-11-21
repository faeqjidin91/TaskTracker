using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskTracker.Api.Extensions;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskTracker API", Version = "v1" });
            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken from POST /api/auth/login."
            });
            options.OperationFilter<AuthorizeOperationFilter>();
        });

        return services;
    }

    // Shows the lock icon only on endpoints that actually require a token.
    private sealed class AuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>() is null
                && context.MethodInfo.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>() is null)
            {
                return;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
            });
        }
    }
}
