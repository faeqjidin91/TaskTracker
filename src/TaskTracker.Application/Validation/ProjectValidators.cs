using FluentValidation;
using TaskTracker.Contracts.Projects;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Validation;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Project.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Project.DescriptionMaxLength);
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
