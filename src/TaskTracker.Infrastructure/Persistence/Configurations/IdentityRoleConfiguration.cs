using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Infrastructure.Auth;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

// SuperAdmin is the only global role. Organization and project roles have their own tables.
public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(new IdentityRole
        {
            Id = SeedIds.SuperAdminRoleId,
            Name = TokenClaims.SuperAdminRole,
            NormalizedName = TokenClaims.SuperAdminRole.ToUpperInvariant(),
            ConcurrencyStamp = SeedIds.SuperAdminRoleId
        });
    }
}
