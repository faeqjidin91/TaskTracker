using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence.Seed;

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
        builder.Property(p => p.OwnerUserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        // The key includes OrganizationId, so an owner must belong to the project's organization.
        builder.HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(p => new { p.OrganizationId, p.OwnerUserId })
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasData(
            new Project
            {
                Id = 1,
                OrganizationId = SeedIds.DefaultOrganizationId,
                OwnerUserId = SeedIds.SystemUserId,
                Name = "Website Redesign",
                Description = "Refresh the marketing site with a new design system.",
                CreatedAtUtc = SeedDate
            },
            new Project
            {
                Id = 2,
                OrganizationId = SeedIds.DefaultOrganizationId,
                OwnerUserId = SeedIds.SystemUserId,
                Name = "Mobile App v2",
                Description = "Second major release of the mobile app.",
                CreatedAtUtc = SeedDate
            });
    }
}
