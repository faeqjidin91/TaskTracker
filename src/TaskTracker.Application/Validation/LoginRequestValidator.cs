using FluentValidation;
using TaskTracker.Contracts.Auth;

namespace TaskTracker.Application.Validation;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // The limits stop oversized input from reaching the deliberately slow password hasher.
        RuleFor(x => x.Email).NotEmpty().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
    }
}
