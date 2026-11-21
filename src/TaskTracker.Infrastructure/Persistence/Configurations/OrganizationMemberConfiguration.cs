using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMembers");
        builder.HasKey(m => new { m.OrganizationId, m.UserId });

        builder.Property(m => m.Role).IsRequired().HasConversion<string>().HasMaxLength(20);

        // One organization per user for now. Dropping this index would allow several.
        builder.HasIndex(m => m.UserId).IsUnique();

        builder.HasOne(m => m.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
