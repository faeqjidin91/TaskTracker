using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Services;
using TaskTracker.Application.Validation;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Tests.Support;

public static class TestUsers
{
    public const string SuperAdmin = "super-admin";
    public const string AcmeAdmin = "acme-admin";
    public const string Owner = "acme-owner";
    public const string Contributor = "acme-contributor";
    public const string Viewer = "acme-viewer";
    public const string Outsider = "acme-outsider";
    public const string GlobexAdmin = "globex-admin";
}

// One isolated SQLite database per test, seeded with two organizations.
public sealed class TestApp : IDisposable
{
    public const int AcmeId = 1;
    public const int GlobexId = 2;
    public const int AcmeProjectId = 10;
    public const int GlobexProjectId = 20;
    public const int AcmeTaskId = 100;
    public const int GlobexTaskId = 200;

    public static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime SeedDate = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FixedTimeProvider _time = new(Now);

    public TestApp()
    {
        _connection.Open();
        Db = NewContext();
        Db.Database.EnsureCreated();
        Seed();
    }

    public TestDbContext Db { get; }
    public FakeCurrentUser CurrentUser { get; } = new();
    public FakeUserDirectory Users { get; } = new();
    public FakeNotificationPublisher Publisher { get; } = new();
    public RecordingLogger<TaskService> TaskLogger { get; } = new();

    public TaskService Tasks => new(
        Db,
        new CreateTaskRequestValidator(),
        new UpdateTaskRequestValidator(),
        new UpdateTaskStatusRequestValidator(),
        Publisher,
        CurrentUser,
        new ProjectAccess(Db, CurrentUser),
        _time,
        TaskLogger);

    public ProjectService Projects => new(
        Db,
        Users,
        CurrentUser,
        new ProjectAccess(Db, CurrentUser),
        new CreateProjectRequestValidator(),
        new UpdateProjectRequestValidator(),
        _time);

    public ProjectMemberService Members => new(
        Db,
        Users,
        CurrentUser,
        new ProjectAccess(Db, CurrentUser),
        new AddProjectMemberRequestValidator(),
        new UpdateProjectMemberRequestValidator(),
        new TransferProjectOwnerRequestValidator(),
        _time);

    public UserService UserAdmin => new(
        Users,
        Db,
        CurrentUser,
        new CreateUserRequestValidator(),
        new UpdateUserRoleRequestValidator(),
        new ResetPasswordRequestValidator());

    public OrganizationService Organizations => new(
        Db,
        Users,
        new CreateOrganizationRequestValidator(),
        new AddOrganizationAdminRequestValidator(),
        _time);

    public void SignInAs(string userId)
    {
        var user = Users.Get(userId);

        CurrentUser.Id = user.Id;
        CurrentUser.Email = user.Email;
        CurrentUser.OrganizationId = user.OrganizationId;
        CurrentUser.IsOrganizationAdmin = user.OrganizationRole == OrganizationRole.Admin;
        CurrentUser.IsSuperAdmin = user.IsSuperAdmin;
        Db.ChangeTracker.Clear();
    }

    // Reads through a separate context, so assertions see what was actually saved.
    public T Saved<T>(Func<TestDbContext, T> query)
    {
        using var context = NewContext();
        return query(context);
    }

    public TaskItem SavedTask(int id) => Saved(db => db.Tasks.IgnoreQueryFilters().AsNoTracking().Single(t => t.Id == id));

    public Project SavedProject(int id) => Saved(db => db.Projects.IgnoreQueryFilters().AsNoTracking().Single(p => p.Id == id));

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    private TestDbContext NewContext() => new(
        new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options,
        CurrentUser);

    private void Seed()
    {
        Users.Add(TestUsers.SuperAdmin, null, null, isSuperAdmin: true);
        Users.Add(TestUsers.AcmeAdmin, AcmeId, OrganizationRole.Admin);
        Users.Add(TestUsers.Owner, AcmeId, OrganizationRole.Member);
        Users.Add(TestUsers.Contributor, AcmeId, OrganizationRole.Member);
        Users.Add(TestUsers.Viewer, AcmeId, OrganizationRole.Member);
        Users.Add(TestUsers.Outsider, AcmeId, OrganizationRole.Member);
        Users.Add(TestUsers.GlobexAdmin, GlobexId, OrganizationRole.Admin);

        Db.Organizations.AddRange(
            new Organization { Id = AcmeId, Name = "Acme", CreatedAtUtc = SeedDate },
            new Organization { Id = GlobexId, Name = "Globex", CreatedAtUtc = SeedDate });

        Db.Projects.AddRange(
            new Project { Id = AcmeProjectId, OrganizationId = AcmeId, OwnerUserId = TestUsers.Owner, Name = "Rocket", CreatedAtUtc = SeedDate },
            new Project { Id = GlobexProjectId, OrganizationId = GlobexId, OwnerUserId = TestUsers.GlobexAdmin, Name = "Secret", CreatedAtUtc = SeedDate });

        Db.ProjectMembers.AddRange(
            new ProjectMember { OrganizationId = AcmeId, ProjectId = AcmeProjectId, UserId = TestUsers.Contributor, Role = ProjectRole.Contributor, AddedAtUtc = SeedDate },
            new ProjectMember { OrganizationId = AcmeId, ProjectId = AcmeProjectId, UserId = TestUsers.Viewer, Role = ProjectRole.Viewer, AddedAtUtc = SeedDate });

        Db.Tasks.AddRange(
            new TaskItem { Id = AcmeTaskId, ProjectId = AcmeProjectId, Title = "Build", Status = TaskItemStatus.Todo, CreatedAtUtc = SeedDate, UpdatedAtUtc = SeedDate },
            new TaskItem { Id = GlobexTaskId, ProjectId = GlobexProjectId, Title = "Hidden", Status = TaskItemStatus.Todo, CreatedAtUtc = SeedDate, UpdatedAtUtc = SeedDate });

        Db.SaveChanges();
        Db.ChangeTracker.Clear();
    }
}
