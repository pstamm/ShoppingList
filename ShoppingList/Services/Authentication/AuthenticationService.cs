using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingList.Services.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthenticationStateProvider _authenticationStateProvider;

    public AuthenticationService(
        HttpClient httpClient,
        JwtAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task LoginAsync(string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }

        var session = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
        if (session is null || !JwtAuthenticationStateProvider.IsValid(session))
        {
            throw new InvalidOperationException("The authentication response did not contain valid tokens.");
        }

        await _authenticationStateProvider.SignInAsync(session);
    }

    public async Task RegisterAsync(string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            new RegisterRequest(email, password));
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _authenticationStateProvider.GetAuthenticationStateAsync();
            var session = await _authenticationStateProvider.GetCurrentSessionAsync();
            if (session is not null)
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    "api/auth/logout",
                    new RefreshRequest(session.RefreshToken));
                response.EnsureSuccessStatusCode();
            }
        }
        finally
        {
            await _authenticationStateProvider.SignOutAsync();
        }
    }

    private static async Task<AuthenticationApiException> CreateApiExceptionAsync(HttpResponseMessage response)
    {
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The email or password is incorrect.",
            HttpStatusCode.Conflict => "An account with this email already exists.",
            _ => "The request could not be completed."
        };

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var apiMessage) && apiMessage.ValueKind == JsonValueKind.String)
            {
                message = apiMessage.GetString() ?? message;
            }
            else if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                        ? error.Value.EnumerateArray()
                            .Where(value => value.ValueKind == JsonValueKind.String)
                            .Select(value => value.GetString())
                        : [])
                    .Where(value => !string.IsNullOrWhiteSpace(value));
                var validationMessage = string.Join(" ", messages);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    message = validationMessage;
                }
            }
            else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                message = title.GetString() ?? message;
            }
        }
        catch (JsonException)
        {
        }

        return new AuthenticationApiException(response.StatusCode, message);
    }
}

public sealed class AuthenticationApiException : Exception
{
    public AuthenticationApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
