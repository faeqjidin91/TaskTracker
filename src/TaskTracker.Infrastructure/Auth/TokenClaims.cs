using System.Security.Cryptography;
using System.Text;

namespace TaskTracker.Infrastructure.Auth;

public static class TokenClaims
{
    public const string Role = "role";
    public const string SecurityStamp = "sstamp";

    // JWT payloads are readable by the client, so only a hash of the stamp goes in the token.
    public static string HashStamp(string securityStamp) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
}
