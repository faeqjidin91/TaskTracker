using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private const int EnumMaxLength = 20;
    private static readonly DateTime SeedDate = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(TaskItem.TitleMaxLength);
        builder.Property(t => t.Description).HasMaxLength(TaskItem.DescriptionMaxLength);
        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(t => t.Priority).IsRequired().HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.UpdatedAtUtc).IsRequired();

        builder.HasIndex(t => t.ProjectId);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            Seed(1, 1, "Audit existing pages", TaskItemStatus.Done, TaskPriority.Medium, null),
            Seed(2, 1, "Build new landing page", TaskItemStatus.InProgress, TaskPriority.High,
                new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc)),
            Seed(3, 1, "Migrate blog content", TaskItemStatus.Todo, TaskPriority.Low, null),
            Seed(4, 2, "Design onboarding flow", TaskItemStatus.InProgress, TaskPriority.Medium,
                new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc)),
            Seed(5, 2, "Add push notifications", TaskItemStatus.Todo, TaskPriority.High, null));
    }

    private static TaskItem Seed(
        int id, int projectId, string title, TaskItemStatus status, TaskPriority priority, DateTime? dueDateUtc) =>
        new()
        {
            Id = id,
            ProjectId = projectId,
            Title = title,
            Status = status,
            Priority = priority,
            DueDateUtc = dueDateUtc,
            CreatedAtUtc = SeedDate,
            UpdatedAtUtc = SeedDate
        };
}
