using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingList.Services.Admin;

public sealed class AdminUserClient(HttpClient httpClient) : IAdminUserClient
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync()
    {
        using var response = await httpClient.GetAsync("api/users");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<AdminUserDto>>() ?? [];
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequest request)
    {
        using var response = await httpClient.PostAsJsonAsync("api/users", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync(response);
    }

    public async Task<AdminUserDto> UpdateRolesAsync(string id, UpdateUserRolesRequest request)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(id)}/roles", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync(response);
    }

    public async Task<AdminUserDto> UpdateStatusAsync(string id, UpdateUserStatusRequest request)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(id)}/status", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync(response);
    }

    private static async Task<AdminUserDto> ReadRequiredAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<AdminUserDto>()
            ?? throw new InvalidOperationException("The API returned an empty user response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Sign in and try again.",
            HttpStatusCode.Forbidden => "Only administrators can manage users.",
            HttpStatusCode.NotFound => "The requested user was not found.",
            HttpStatusCode.Conflict => "The operation conflicts with user-management rules.",
            _ => "The user-management request could not be completed."
        };

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (document.RootElement.TryGetProperty("message", out var apiMessage) &&
                apiMessage.ValueKind == JsonValueKind.String)
            {
                message = apiMessage.GetString() ?? message;
            }
        }
        catch (JsonException)
        {
        }

        throw new AdminUserApiException(response.StatusCode, message);
    }
}

public sealed class AdminUserApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
