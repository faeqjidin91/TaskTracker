using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskTracker.Application.Abstractions;
using TaskTracker.Contracts.Users;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string? Id => User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public int? OrganizationId =>
        int.TryParse(User?.FindFirstValue(TokenClaims.OrganizationId), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public bool IsOrganizationAdmin =>
        User?.HasClaim(TokenClaims.OrganizationRole, nameof(OrganizationRole.Admin)) == true;

    public bool IsSuperAdmin => User?.IsInRole(TokenClaims.SuperAdminRole) == true;
}
