namespace TaskTracker.Infrastructure.Persistence.Seed;

public static class SeedIds
{
    // Older databases may still have this locked system user if it owns real data.
    // It has no password and stays hidden from every user list.
    public const string SystemUserId = "3c0f6e1a-8d42-4b7e-9f55-6a2b1c7d9e01";

    public const string SuperAdminRoleId = "7f1c9a52-3b64-4c1e-9a10-1d5e8b2f6a04";
}
