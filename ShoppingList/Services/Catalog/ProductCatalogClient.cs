using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingList.Services.Catalog;

public sealed class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public ProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? search, int? productTypeId)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (productTypeId.HasValue)
        {
            query.Add($"productTypeId={productTypeId.Value}");
        }

        var path = "api/products";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        using var response = await _httpClient.GetAsync(path);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>() ?? [];
    }

    public async Task<ProductDto> GetProductAsync(int id)
    {
        using var response = await _httpClient.GetAsync($"api/products/{id}");
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ProductDto>(response);
    }

    public async Task<ProductDto> CreateProductAsync(SaveProductRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/products", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ProductDto>(response);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, SaveProductRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", request);
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ProductDto>(response);
    }

    public async Task DeleteProductAsync(int id)
    {
        using var response = await _httpClient.DeleteAsync($"api/products/{id}");
        await EnsureSuccessAsync(response);
    }

    public async Task<IReadOnlyList<ProductTypeDto>> GetProductTypesAsync()
    {
        using var response = await _httpClient.GetAsync("api/product-types");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductTypeDto>>() ?? [];
    }

    public async Task<ProductTypeDto> CreateProductTypeAsync(string name)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/product-types",
            new CreateProductTypeRequest(name));
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ProductTypeDto>(response);
    }

    public async Task<ProductTypeDto> UpdateProductTypeAsync(int id, string name)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/product-types/{id}",
            new UpdateProductTypeRequest(name));
        await EnsureSuccessAsync(response);
        return await ReadRequiredAsync<ProductTypeDto>(response);
    }

    public async Task DeleteProductTypeAsync(int id)
    {
        using var response = await _httpClient.DeleteAsync($"api/product-types/{id}");
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
            HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested catalogue item was not found.",
            HttpStatusCode.Conflict => "The operation conflicts with existing catalogue data.",
            _ => "The catalogue request could not be completed."
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

        throw new CatalogApiException(response.StatusCode, message);
    }
}

public sealed class CatalogApiException : Exception
{
    public CatalogApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
