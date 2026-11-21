using TaskTracker.Application.Abstractions;
using TaskTracker.Contracts.Users;
using ContractRole = TaskTracker.Contracts.Users.OrganizationRole;
using DomainRole = TaskTracker.Domain.Enums.OrganizationRole;

namespace TaskTracker.Application.Mapping;

public static class UserMappings
{
    public static UserDto ToDto(this UserRecord user) =>
        new(user.Id, user.Email, (user.OrganizationRole ?? DomainRole.Member).ToContract(), user.IsActive);

    public static CurrentUserDto ToCurrentUserDto(this UserRecord user) => new(
        user.Id,
        user.Email,
        user.IsSuperAdmin,
        user.OrganizationId,
        user.OrganizationName,
        user.OrganizationRole?.ToContract());

    public static ContractRole ToContract(this DomainRole role) => role switch
    {
        DomainRole.Member => ContractRole.Member,
        DomainRole.Admin => ContractRole.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    public static DomainRole ToDomain(this ContractRole role) => role switch
    {
        ContractRole.Member => DomainRole.Member,
        ContractRole.Admin => DomainRole.Admin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };
}
