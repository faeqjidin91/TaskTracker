using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskTracker.Web.Services;

// Turns HTTP and network failures into ApiResult, so pages only deal with success or a message.
public abstract class ApiClientBase(HttpClient http)
{
    private const string UnreachableMessage = "Could not reach the server. Check that the API is running.";
    private const string UnexpectedMessage = "Something went wrong. Please try again.";

    // Must match the API, which sends and accepts enums by name.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    protected virtual string UnauthorizedMessage => "Your session has expired. Please log in again.";

    protected Task<ApiResult<T>> GetAsync<T>(string uri) =>
        SendForValueAsync<T>(() => http.GetAsync(uri));

    protected Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest body) =>
        SendForValueAsync<TResponse>(() => http.PostAsJsonAsync(uri, body, JsonOptions));

    protected Task<ApiResult> PostNoContentAsync<TRequest>(string uri, TRequest body) =>
        SendAsync(() => http.PostAsJsonAsync(uri, body, JsonOptions));

    protected Task<ApiResult> PutAsync<TRequest>(string uri, TRequest body) =>
        SendAsync(() => http.PutAsJsonAsync(uri, body, JsonOptions));

    protected Task<ApiResult> PatchAsync<TRequest>(string uri, TRequest body) =>
        SendAsync(() => http.PatchAsJsonAsync(uri, body, JsonOptions));

    protected Task<ApiResult> SendDeleteAsync(string uri) =>
        SendAsync(() => http.DeleteAsync(uri));

    private async Task<ApiResult<T>> SendForValueAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.FromError(await ReadErrorAsync(response));
            }

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return value is null ? ApiResult<T>.FromError(UnexpectedMessage) : ApiResult<T>.FromValue(value);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.FromError(UnreachableMessage);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.FromError(UnreachableMessage);
        }
        catch (JsonException)
        {
            return ApiResult<T>.FromError(UnexpectedMessage);
        }
    }

    private async Task<ApiResult> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            return response.IsSuccessStatusCode
                ? ApiResult.Succeeded()
                : ApiResult.Failed(await ReadErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return ApiResult.Failed(UnreachableMessage);
        }
        catch (TaskCanceledException)
        {
            return ApiResult.Failed(UnreachableMessage);
        }
    }

    private async Task<string> ReadErrorAsync(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.Unauthorized => UnauthorizedMessage,
        HttpStatusCode.NotFound => "That item was not found. It may have been deleted.",
        HttpStatusCode.Forbidden => "You do not have permission to do that.",
        HttpStatusCode.Conflict => await ReadProblemTitleAsync(response),
        HttpStatusCode.BadRequest => await ReadValidationErrorsAsync(response),
        _ => UnexpectedMessage
    };

    private static async Task<string> ReadProblemTitleAsync(HttpResponseMessage response)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var problem = await JsonDocument.ParseAsync(stream);

            return problem.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                ? title.GetString() ?? UnexpectedMessage
                : UnexpectedMessage;
        }
        catch (JsonException)
        {
            return UnexpectedMessage;
        }
    }

    private static async Task<string> ReadValidationErrorsAsync(HttpResponseMessage response)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var problem = await JsonDocument.ParseAsync(stream);

            if (problem.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .Where(field => field.Value.ValueKind == JsonValueKind.Array)
                    .SelectMany(field => field.Value.EnumerateArray())
                    .Select(message => message.GetString())
                    .Where(message => !string.IsNullOrEmpty(message));

                var combined = string.Join(" ", messages);
                if (combined.Length > 0)
                {
                    return combined;
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to the generic message.
        }

        return UnexpectedMessage;
    }
}
