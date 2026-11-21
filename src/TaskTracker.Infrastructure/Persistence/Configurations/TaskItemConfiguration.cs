using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private const int EnumMaxLength = 20;

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
    }
}
