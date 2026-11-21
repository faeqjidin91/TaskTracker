using FluentValidation;
using TaskTracker.Contracts.Auth;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Validation;

// Password strength rules are enforced by ASP.NET Core Identity, so they live in one place.
internal static class UserRules
{
    public const int EmailMaxLength = 256;
    public const int PasswordMaxLength = 128;
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
    }
}
