using Microsoft.AspNetCore.Authorization;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Web.Auth;

// These only decide what the UI shows. The API enforces the same rules on every request.
public static class WebPolicies
{
    public const string CanManageTasks = nameof(CanManageTasks);
    public const string CanManageUsers = nameof(CanManageUsers);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageTasks, policy => policy.RequireRole(nameof(UserRole.Member), nameof(UserRole.Admin)));
        options.AddPolicy(CanManageUsers, policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
