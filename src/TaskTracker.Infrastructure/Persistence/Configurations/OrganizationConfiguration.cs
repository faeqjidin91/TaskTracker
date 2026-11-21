using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(Organization.NameMaxLength);
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.HasIndex(o => o.Name).IsUnique();

        builder.HasData(new Organization
        {
            Id = SeedIds.DefaultOrganizationId,
            Name = "Default Organization",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc)
        });
    }
}
