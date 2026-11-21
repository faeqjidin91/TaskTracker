using TaskTracker.Application.Abstractions;
using TaskTracker.Contracts.Users;
using ContractRole = TaskTracker.Contracts.Users.UserRole;
using DomainRole = TaskTracker.Domain.Enums.UserRole;

namespace TaskTracker.Application.Mapping;

public static class UserMappings
{
    public static UserDto ToDto(this UserRecord user) =>
        new(user.Id, user.Email, user.Role.ToContract(), user.IsActive);

    public static ContractRole ToContract(this DomainRole role) => role switch
    {
        DomainRole.Viewer => ContractRole.Viewer,
        DomainRole.Member => ContractRole.Member,
        DomainRole.Admin => ContractRole.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    public static DomainRole ToDomain(this ContractRole role) => role switch
    {
        ContractRole.Viewer => DomainRole.Viewer,
        ContractRole.Member => DomainRole.Member,
        ContractRole.Admin => DomainRole.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };
}
