using FluentValidation;
using TaskTracker.Contracts.Organizations;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Validation;

public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Organization.NameMaxLength);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(x => x.AdminPassword).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
    }
}

public class AddOrganizationAdminRequestValidator : AbstractValidator<AddOrganizationAdminRequest>
{
    public AddOrganizationAdminRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(UserRules.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(UserRules.PasswordMaxLength);
    }
}
