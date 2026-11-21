using Microsoft.AspNetCore.Authorization;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Api.Auth;

public static class Policies
{
    public const string CanManageTasks = nameof(CanManageTasks);
    public const string CanManageProjects = nameof(CanManageProjects);
    public const string CanManageUsers = nameof(CanManageUsers);

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageTasks, policy => policy.RequireRole(nameof(UserRole.Member), nameof(UserRole.Admin)));
        options.AddPolicy(CanManageProjects, policy => policy.RequireRole(nameof(UserRole.Admin)));
        options.AddPolicy(CanManageUsers, policy => policy.RequireRole(nameof(UserRole.Admin)));
    }
}
