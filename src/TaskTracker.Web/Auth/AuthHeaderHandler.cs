using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace TaskTracker.Web.Auth;

public sealed class AuthHeaderHandler(
    TokenStore tokens,
    JwtAuthenticationStateProvider authState,
    NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await tokens.GetAsync();

        if (session is { IsExpired: false })
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }
        else if (session is not null)
        {
            await SignOutAndRedirectAsync();
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SignOutAndRedirectAsync();
        }

        return response;
    }

    private async Task SignOutAndRedirectAsync()
    {
        await authState.SignOutAsync();

        if (!navigation.ToBaseRelativePath(navigation.Uri).StartsWith("login", StringComparison.OrdinalIgnoreCase))
        {
            navigation.NavigateTo("login");
        }
    }
}
