using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    private static readonly DateTime SeedDate = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(Project.NameMaxLength);
        builder.Property(p => p.Description).HasMaxLength(Project.DescriptionMaxLength);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasData(
            new Project
            {
                Id = 1,
                Name = "Website Redesign",
                Description = "Refresh the marketing site with a new design system.",
                CreatedAtUtc = SeedDate
            },
            new Project
            {
                Id = 2,
                Name = "Mobile App v2",
                Description = "Second major release of the mobile app.",
                CreatedAtUtc = SeedDate
            });
    }
}
