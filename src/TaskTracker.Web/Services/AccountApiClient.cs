using TaskTracker.Contracts.Auth;
using TaskTracker.Web.Auth;

namespace TaskTracker.Web.Services;

public sealed class AccountApiClient(HttpClient http, JwtAuthenticationStateProvider authState) : ApiClientBase(http)
{
    public async Task<ApiResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var result = await PostAsync<ChangePasswordRequest, LoginResponse>(
            "api/auth/change-password",
            new ChangePasswordRequest(currentPassword, newPassword));

        if (result.IsSuccess)
        {
            // The old token stopped working when the password changed.
            await authState.SignInAsync(result.Value.AccessToken, result.Value.ExpiresAtUtc);
        }

        return result;
    }
}
