using TaskTracker.Application.Validation;
using TaskTracker.Contracts.Tasks;

namespace TaskTracker.Application.Tests;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_EmptyTitle(string title)
    {
        var result = _validator.Validate(new CreateTaskRequest(title, null, TaskPriority.Low, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public void Rejects_TitleOver200Characters()
    {
        var result = _validator.Validate(new CreateTaskRequest(new string('a', 201), null, TaskPriority.Low, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public void Accepts_TitleOfExactly200Characters()
    {
        var result = _validator.Validate(new CreateTaskRequest(new string('a', 200), null, TaskPriority.Low, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_DescriptionOver2000Characters()
    {
        var result = _validator.Validate(new CreateTaskRequest("Title", new string('a', 2001), TaskPriority.Low, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Description));
    }

    [Fact]
    public void Rejects_UndefinedPriority()
    {
        var result = _validator.Validate(new CreateTaskRequest("Title", null, (TaskPriority)99, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Priority));
    }
}
