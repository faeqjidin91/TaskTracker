using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
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
    }
}
