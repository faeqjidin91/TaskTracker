using FluentValidation;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Validation;

internal static class ProjectRules
{
    public const int UserIdMaxLength = 450;
}

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Project.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Project.DescriptionMaxLength);
        RuleFor(x => x.OwnerUserId).MaximumLength(ProjectRules.UserIdMaxLength);
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Project.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Project.DescriptionMaxLength);
    }
}

public class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
{
    public AddProjectMemberRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(ProjectRules.UserIdMaxLength);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateProjectMemberRequestValidator : AbstractValidator<UpdateProjectMemberRequest>
{
    public UpdateProjectMemberRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class TransferProjectOwnerRequestValidator : AbstractValidator<TransferProjectOwnerRequest>
{
    public TransferProjectOwnerRequestValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty().MaximumLength(ProjectRules.UserIdMaxLength);
    }
}
