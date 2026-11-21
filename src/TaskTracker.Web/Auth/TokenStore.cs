using System.Globalization;
using Blazored.LocalStorage;

namespace TaskTracker.Web.Auth;

public sealed record AuthSession(string AccessToken, DateTime ExpiresAtUtc)
{
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
}

// localStorage keeps the user signed in across refreshes. The trade-off is that script
// injected through XSS could read the token, which is why its lifetime is short.
public sealed class TokenStore(ILocalStorageService storage)
{
    private const string TokenKey = "tasktracker.accessToken";
    private const string ExpiryKey = "tasktracker.expiresAtUtc";

    public async Task<AuthSession?> GetAsync()
    {
        var token = await storage.GetItemAsStringAsync(TokenKey);
        var expiry = await storage.GetItemAsStringAsync(ExpiryKey);

        if (string.IsNullOrEmpty(token)
            || !DateTime.TryParse(expiry, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAtUtc))
        {
            return null;
        }

        return new AuthSession(token, expiresAtUtc.ToUniversalTime());
    }

    public async Task SetAsync(string token, DateTime expiresAtUtc)
    {
        await storage.SetItemAsStringAsync(TokenKey, token);
        await storage.SetItemAsStringAsync(ExpiryKey, expiresAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    public async Task ClearAsync()
    {
        await storage.RemoveItemAsync(TokenKey);
        await storage.RemoveItemAsync(ExpiryKey);
    }
}
