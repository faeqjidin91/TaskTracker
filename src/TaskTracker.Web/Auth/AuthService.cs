using TaskTracker.Contracts.Auth;
using TaskTracker.Web.Services;

namespace TaskTracker.Web.Auth;

public sealed class AuthService(HttpClient http, JwtAuthenticationStateProvider authState) : ApiClientBase(http)
{
    protected override string UnauthorizedMessage => "Invalid email or password.";

    public async Task<ApiResult> LoginAsync(string email, string password)
    {
        var result = await PostAsync<LoginRequest, LoginResponse>("api/auth/login", new LoginRequest(email, password));
        if (!result.IsSuccess)
        {
            return result;
        }

        await authState.SignInAsync(result.Value.AccessToken, result.Value.ExpiresAtUtc);
        return result;
    }

    public Task LogoutAsync() => authState.SignOutAsync();
}
