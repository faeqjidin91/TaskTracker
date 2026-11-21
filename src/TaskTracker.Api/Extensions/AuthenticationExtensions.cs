using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskTracker.Api.Auth;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keeps claim names as issued ("email") instead of long XML SOAP URIs.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = TokenClaims.Role
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectStaleSessionAsync };
            });

        services.AddAuthorization(Policies.Configure);

        return services;
    }

    private static async Task RejectStaleSessionAsync(TokenValidatedContext context)
    {
        var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessionValidator>();
        var user = context.Principal;

        var isCurrent = await sessions.IsCurrentAsync(
            user?.FindFirstValue(JwtRegisteredClaimNames.Sub),
            user?.FindFirstValue(TokenClaims.SecurityStamp),
            user?.HasClaim(c => c.Type == TokenClaims.OrganizationId) == true,
            context.HttpContext.RequestAborted);

        if (!isCurrent)
        {
            context.Fail("The session is no longer valid.");
        }
    }
}
