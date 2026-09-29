using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingList.Services.ShoppingLists;

public sealed class ShoppingListClient : IShoppingListClient
{
    private readonly HttpClient _httpClient;

    public ShoppingListClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ShoppingListDto>> GetListsAsync()
    {
        using var response = await _httpClient.GetAsync("api/lists");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ShoppingListDto>>() ?? [];
    }

    public async Task<ShoppingListDto> GetListAsync(int listId)
    {
        using var response = await _httpClient.GetAsync($"api/lists/{listId}");
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ShoppingListDto>(response);
    }

    public async Task<ShoppingListDto> CreateListAsync(string name)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/lists", new CreateShoppingListRequest(name));
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ShoppingListDto>(response);
    }

    public async Task<ShoppingListDto> UpdateListAsync(int listId, string name, string rowVersion)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/lists/{listId}",
            new UpdateShoppingListRequest(name, rowVersion));
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ShoppingListDto>(response);
    }

    public async Task DeleteListAsync(int listId, string rowVersion)
    {
        var path = $"api/lists/{listId}?rowVersion={Uri.EscapeDataString(rowVersion)}";
        using var response = await _httpClient.DeleteAsync(path);
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyList<ListProductDto>> GetListProductsAsync(int listId)
    {
        using var response = await _httpClient.GetAsync($"api/lists/{listId}/products");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ListProductDto>>() ?? [];
    }

    public async Task<ListProductDto> AddListProductAsync(int listId, AddListProductRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/lists/{listId}/products", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ListProductDto>(response);
    }

    public async Task<ListProductDto> UpdateListProductAsync(
        int listId,
        int listProductId,
        UpdateListProductRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/lists/{listId}/products/{listProductId}",
            request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ListProductDto>(response);
    }

    public async Task DeleteListProductAsync(int listId, int listProductId, string rowVersion)
    {
        var path = $"api/lists/{listId}/products/{listProductId}?rowVersion={Uri.EscapeDataString(rowVersion)}";
        using var response = await _httpClient.DeleteAsync(path);
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyList<ListShareDto>> GetSharesAsync(int listId)
    {
        using var response = await _httpClient.GetAsync($"api/lists/{listId}/shares");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ListShareDto>>() ?? [];
    }

    public async Task<ListShareDto> CreateShareAsync(int listId, string userName)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/lists/{listId}/shares",
            new ShareListRequest(userName));
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ListShareDto>(response);
    }

    public async Task RemoveShareAsync(int listId, string userId)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        using var response = await _httpClient.DeleteAsync($"api/lists/{listId}/shares/{encodedUserId}");
        await EnsureSuccessAsync(response);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("The API returned an empty response.");
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
            HttpStatusCode.Forbidden => "You do not have permission to access this list.",
            HttpStatusCode.NotFound => "The requested shopping list or product was not found.",
            HttpStatusCode.Conflict => "The list changed or the operation conflicts with its current contents.",
            _ => "The shopping-list request could not be completed."
        };

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var apiMessage) &&
                apiMessage.ValueKind == JsonValueKind.String)
            {
                message = apiMessage.GetString() ?? message;
            }
            else if (root.TryGetProperty("errors", out var errors) &&
                     errors.ValueKind == JsonValueKind.Object)
            {
                var validationMessages = errors.EnumerateObject()
                    .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                        ? error.Value.EnumerateArray()
                            .Where(value => value.ValueKind == JsonValueKind.String)
                            .Select(value => value.GetString())
                        : [])
                    .Where(value => !string.IsNullOrWhiteSpace(value));
                var validationMessage = string.Join(" ", validationMessages);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    message = validationMessage;
                }
            }
        }
        catch (JsonException)
        {
        }

        throw new ShoppingListApiException(response.StatusCode, message);
    }
}

public sealed class ShoppingListApiException : Exception
{
    public ShoppingListApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
