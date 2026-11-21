using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskTracker.Application.Abstractions;

namespace TaskTracker.Api.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Email => accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);
}
