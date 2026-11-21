using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(m => new { m.ProjectId, m.UserId });

        builder.Property(m => m.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.AddedAtUtc).IsRequired();
        builder.HasIndex(m => m.UserId);

        // Both keys include OrganizationId, so the database itself rejects a member
        // from a different organization than the project.
        builder.HasOne(m => m.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(m => new { m.OrganizationId, m.ProjectId })
            .HasPrincipalKey(p => new { p.OrganizationId, p.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(m => new { m.OrganizationId, m.UserId })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
