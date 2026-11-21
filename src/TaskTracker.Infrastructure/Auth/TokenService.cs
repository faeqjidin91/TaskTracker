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

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(TokenClaims.Role, user.Role.ToString()),
                new Claim(TokenClaims.SecurityStamp, TokenClaims.HashStamp(user.SecurityStamp))
            ]),
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAtUtc);
    }
}
