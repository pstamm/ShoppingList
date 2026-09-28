using System.Net;
using System.Text;
using System.Text.Json;
using ShoppingList.Services.Catalog;

namespace ShoppingList.UnitTests;

public class ProductCatalogClientTests
{
    [Fact]
    public async Task Product_search_encodes_filters_and_deserializes_catalogue_results()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                    [{
                      "id": 7,
                      "name": "Organic Oats",
                      "productTypeId": 2,
                      "price": 3.5,
                      "picture": "AQID",
                      "pictureContentType": "image/png",
                      "pictureWidth": 20,
                      "pictureHeight": 30,
                      "createdAt": "2026-01-01T00:00:00+00:00",
                      "updatedAt": "2026-01-01T00:00:00+00:00",
                      "deletedAt": null
                    }]
                    """)
            });
        var client = new ProductCatalogClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example/")
        });

        var products = await client.GetProductsAsync("oats & grains", 2);

            Assert.Equal("/api/products?search=oats%20%26%20grains&productTypeId=2", handler.LastRequestUri!.PathAndQuery);
        var product = Assert.Single(products);
        Assert.Equal("Organic Oats", product.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, product.Picture);
    }

    [Fact]
    public async Task Create_product_sends_image_as_base64_json_and_returns_created_product()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent("""
                    {
                      "id": 1,
                      "name": "Bread",
                      "productTypeId": 2,
                      "price": 2.25,
                      "picture": "AQID",
                      "pictureContentType": "image/jpeg",
                      "pictureWidth": 10,
                      "pictureHeight": 10,
                      "createdAt": "2026-01-01T00:00:00+00:00",
                      "updatedAt": "2026-01-01T00:00:00+00:00",
                      "deletedAt": null
                    }
                    """)
            };
        });
        var client = new ProductCatalogClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example/")
        });

        var product = await client.CreateProductAsync(
            new SaveProductRequest("Bread", 2, 2.25m, [1, 2, 3], "image/jpeg", null, null));

        using var requestJson = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("AQID", requestJson.RootElement.GetProperty("picture").GetString());
        Assert.Equal("Bread", product.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, product.Picture);
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
