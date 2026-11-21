using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Infrastructure.Persistence.Seed;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class SystemUserConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder)
    {
        builder.HasData(new IdentityUser
        {
            Id = SeedIds.SystemUserId,
            UserName = SeedIds.SystemUserEmail,
            NormalizedUserName = SeedIds.SystemUserEmail.ToUpperInvariant(),
            Email = SeedIds.SystemUserEmail,
            NormalizedEmail = SeedIds.SystemUserEmail.ToUpperInvariant(),
            EmailConfirmed = false,
            PasswordHash = null,
            SecurityStamp = SeedIds.SystemUserId,
            ConcurrencyStamp = SeedIds.SystemUserId,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.MaxValue
        });
    }
}
