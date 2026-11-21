using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TaskTracker.Web.Auth;

public sealed class JwtAuthenticationStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await tokens.GetAsync();
        if (session is null)
        {
            return Anonymous;
        }

        if (session.IsExpired || !JwtClaimsParser.TryParse(session.AccessToken, out var claims))
        {
            await tokens.ClearAsync();
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "email", roleType: "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(string token, DateTime expiresAtUtc)
    {
        await tokens.SetAsync(token, expiresAtUtc);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
