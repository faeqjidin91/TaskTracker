using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

// Seeded by migration so the roles exist in every environment. Values are fixed so that
// EF Core does not detect a model change on each build.
public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            Role("7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a01", UserRole.Viewer),
            Role("7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a02", UserRole.Member),
            Role("7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a03", UserRole.Admin));
    }

    private static IdentityRole Role(string id, UserRole role) => new()
    {
        Id = id,
        Name = role.ToString(),
        NormalizedName = role.ToString().ToUpperInvariant(),
        ConcurrencyStamp = id
    };
}
