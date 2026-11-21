using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskTracker.Application.Abstractions;

namespace TaskTracker.Infrastructure.Auth;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken CreateToken(UserRecord user)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenClaims.SecurityStamp, TokenClaims.HashStamp(user.SecurityStamp))
        };

        if (user.IsSuperAdmin)
        {
            claims.Add(new Claim(TokenClaims.Role, TokenClaims.SuperAdminRole));
        }

        if (user.OrganizationId is { } organizationId)
        {
            claims.Add(new Claim(TokenClaims.OrganizationId, organizationId.ToString(CultureInfo.InvariantCulture)));
            claims.Add(new Claim(TokenClaims.OrganizationName, user.OrganizationName ?? string.Empty));
            claims.Add(new Claim(TokenClaims.OrganizationRole, user.OrganizationRole?.ToString() ?? string.Empty));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAtUtc);
    }
}
