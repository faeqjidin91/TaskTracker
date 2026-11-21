using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TaskTracker.Web.Auth;
using TaskTracker.Web.Services;

namespace TaskTracker.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseUrl = configuration["ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
        }

        var apiBaseUri = new Uri(apiBaseUrl.TrimEnd('/') + "/");

        // Singletons because IHttpClientFactory builds handlers in its own DI scope. A scoped
        // auth provider there would be a different instance, and the UI would miss sign-outs.
        services.AddBlazoredLocalStorageAsSingleton();
        services.AddSingleton<TokenStore>();
        services.AddSingleton<JwtAuthenticationStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(
            provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddAuthorizationCore(WebPolicies.Configure);
        services.AddTransient<AuthHeaderHandler>();

        // Login has no auth handler: a 401 there means a wrong password, not an expired session.
        services.AddHttpClient<AuthService>(client => client.BaseAddress = apiBaseUri);
        services.AddHttpClient<ProjectApiClient>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<AuthHeaderHandler>();
        services.AddHttpClient<TaskApiClient>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<AuthHeaderHandler>();
        services.AddHttpClient<UserApiClient>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<AuthHeaderHandler>();
        services.AddHttpClient<AccountApiClient>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<AuthHeaderHandler>();

        return services;
    }
}
