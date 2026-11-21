using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Application.Validation;
using TaskTracker.Contracts.Auth;
using TaskTracker.Contracts.Organizations;
using TaskTracker.Contracts.Projects;
using TaskTracker.Contracts.Tasks;
using TaskTracker.Contracts.Users;

namespace TaskTracker.Application.Tests;

public class RequestContractTests
{
    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    // Without JsonRequired, each of these would bind a missing field to its default value:
    // for example, an empty body would deactivate an organization.
    [Theory]
    [InlineData(typeof(UpdateOrganizationStatusRequest), "{}")]
    [InlineData(typeof(UpdateUserStatusRequest), "{}")]
    [InlineData(typeof(UpdateUserRoleRequest), "{}")]
    [InlineData(typeof(UpdateTaskStatusRequest), "{}")]
    [InlineData(typeof(UpdateProjectMemberRequest), "{}")]
    [InlineData(typeof(AddProjectMemberRequest), """{"userId":"u"}""")]
    [InlineData(typeof(CreateUserRequest), """{"email":"a@test.local","password":"p"}""")]
    [InlineData(typeof(CreateTaskRequest), """{"title":"T"}""")]
    [InlineData(typeof(UpdateTaskRequest), """{"title":"T"}""")]
    public void MissingValueField_IsRejected(Type requestType, string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, requestType, ApiJson));
    }

    [Theory]
    [InlineData(typeof(UpdateOrganizationStatusRequest), """{"isActive":false}""")]
    [InlineData(typeof(UpdateUserStatusRequest), """{"isActive":true}""")]
    [InlineData(typeof(UpdateUserStatusRequest), """{"isActive":false,"newOwnerUserId":"u"}""")]
    [InlineData(typeof(UpdateUserRoleRequest), """{"role":"Admin"}""")]
    [InlineData(typeof(UpdateTaskStatusRequest), """{"status":"Done"}""")]
    [InlineData(typeof(UpdateProjectMemberRequest), """{"role":"Viewer"}""")]
    [InlineData(typeof(AddProjectMemberRequest), """{"userId":"u","role":"Contributor"}""")]
    [InlineData(typeof(CreateUserRequest), """{"email":"a@test.local","password":"p","role":"Member"}""")]
    [InlineData(typeof(CreateTaskRequest), """{"title":"T","priority":"High"}""")]
    [InlineData(typeof(UpdateTaskRequest), """{"title":"T","priority":"Low"}""")]
    public void CompleteRequest_IsAccepted(Type requestType, string json)
    {
        Assert.NotNull(JsonSerializer.Deserialize(json, requestType, ApiJson));
    }

    [Fact]
    public void Login_RejectsOversizedPassword()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest("a@test.local", new string('x', 129)));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Login_AcceptsPasswordAtTheLimit()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest("a@test.local", new string('x', 128)));

        Assert.True(result.IsValid);
    }
}
