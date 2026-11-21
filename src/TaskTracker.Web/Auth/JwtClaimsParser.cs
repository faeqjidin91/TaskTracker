using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;

namespace TaskTracker.Web.Auth;

// Reads claims for display only. The API is what verifies the signature.
public static class JwtClaimsParser
{
    public static bool TryParse(string token, [NotNullWhen(true)] out List<Claim>? claims)
    {
        claims = null;

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            if (payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            claims = payload.RootElement.EnumerateObject()
                .Where(property => property.Value.ValueKind != JsonValueKind.Array)
                .Select(property => new Claim(property.Name, property.Value.ToString()))
                .ToList();

            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
