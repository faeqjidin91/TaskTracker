using Microsoft.AspNetCore.Authorization;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Web.Auth;

// These only decide what the UI shows. The API enforces the same rules on every request.
public static class WebPolicies
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string InOrganization = nameof(InOrganization);
    public const string OrganizationAdmin = nameof(OrganizationAdmin);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdmin, policy => policy.RequireRole(SuperAdmin));
        options.AddPolicy(InOrganization, policy => policy.RequireClaim("org"));
        options.AddPolicy(OrganizationAdmin, policy => policy.RequireClaim("org_role", nameof(OrganizationRole.Admin)));
    }
}
