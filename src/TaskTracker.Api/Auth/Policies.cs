using Microsoft.AspNetCore.Authorization;
using TaskTracker.Contracts.Users;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Auth;

// Coarse access only. Per-project rules depend on the resource, so services enforce them
// through ProjectPermissions.
public static class Policies
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string InOrganization = nameof(InOrganization);
    public const string OrganizationAdmin = nameof(OrganizationAdmin);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdmin, policy => policy.RequireRole(TokenClaims.SuperAdminRole));
        options.AddPolicy(InOrganization, policy => policy.RequireClaim(TokenClaims.OrganizationId));
        options.AddPolicy(OrganizationAdmin, policy =>
            policy.RequireClaim(TokenClaims.OrganizationRole, nameof(OrganizationRole.Admin)));
    }
}
