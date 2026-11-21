namespace TaskTracker.Infrastructure.Persistence.Seed;

// Bound from the "Seed" section. In .env the keys are written as Seed__SuperAdminEmail, and so on.
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    // The local-only password in .env.example, which must never protect a deployed SuperAdmin.
    public const string ExamplePassword = "Passw0rd!";

    public string? SuperAdminEmail { get; set; }
    public string? SuperAdminPassword { get; set; }
    public string? OrganizationName { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }

    public void EnsureValidForDevelopment()
    {
        var missing = new[]
            {
                (Key: nameof(SuperAdminEmail), Value: SuperAdminEmail),
                (Key: nameof(SuperAdminPassword), Value: SuperAdminPassword),
                (Key: nameof(OrganizationName), Value: OrganizationName),
                (Key: nameof(AdminEmail), Value: AdminEmail),
                (Key: nameof(AdminPassword), Value: AdminPassword)
            }
            .Where(setting => string.IsNullOrWhiteSpace(setting.Value))
            .Select(setting => $"Seed__{setting.Key}")
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing development seed settings: {string.Join(", ", missing)}. Add them to .env (see .env.example).");
        }

        if (string.Equals(SuperAdminEmail, AdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seed__SuperAdminEmail and Seed__AdminEmail must be different users.");
        }
    }

    /// <returns>True when a SuperAdmin is configured, or false when seeding should be skipped.</returns>
    public bool EnsureValidForDeployment()
    {
        var hasEmail = !string.IsNullOrWhiteSpace(SuperAdminEmail);
        var hasPassword = !string.IsNullOrEmpty(SuperAdminPassword);

        if (hasEmail != hasPassword)
        {
            throw new InvalidOperationException("Set both Seed__SuperAdminEmail and Seed__SuperAdminPassword, or neither.");
        }

        if (hasPassword && SuperAdminPassword == ExamplePassword)
        {
            throw new InvalidOperationException(
                "Seed__SuperAdminPassword still has the example value from .env.example. Set a strong password.");
        }

        return hasEmail;
    }
}
