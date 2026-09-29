using System.Net;
using System.Text;
using System.Text.Json;
using ShoppingList.Services.ShoppingLists;

namespace ShoppingList.UnitTests;

public class ShoppingListClientTests
{
    [Fact]
    public async Task Update_list_includes_its_row_version_for_concurrency_checks()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                    {
                      "id": 9,
                      "name": "Weekend",
                      "ownerUserId": "user-a",
                      "createdAt": "2026-01-01T00:00:00+00:00",
                      "updatedAt": "2026-01-02T00:00:00+00:00",
                      "products": [],
                      "rowVersion": "AQIDBAUGBwg="
                    }
                    """)
            };
        });
        var client = CreateClient(handler);

        var list = await client.UpdateListAsync(9, "Weekend", "AQIDBAUGBwg=");

        Assert.Equal("/api/lists/9", handler.LastRequestUri!.AbsolutePath);
        using var request = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Weekend", request.RootElement.GetProperty("name").GetString());
        Assert.Equal("AQIDBAUGBwg=", request.RootElement.GetProperty("rowVersion").GetString());
        Assert.Equal("AQIDBAUGBwg=", list.RowVersion);
    }

    [Fact]
    public async Task List_products_deserialize_type_and_price_for_the_shopping_table()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                    [{
                      "id": 4,
                      "listId": 9,
                      "productId": 2,
                      "productName": "Bread",
                      "productTypeId": 5,
                      "productTypeName": "Bakery",
                      "productPrice": 2.75,
                      "tipicalOrder": -1,
                      "toOrderNow": 0,
                      "notes": "Wholegrain",
                      "createdAt": "2026-01-01T00:00:00+00:00",
                      "updatedAt": "2026-01-01T00:00:00+00:00",
                      "rowVersion": "AQIDBAUGBwg="
                    }]
                    """)
            });
        var client = CreateClient(handler);

        var item = Assert.Single(await client.GetListProductsAsync(9));

        Assert.Equal("Bread", item.ProductName);
        Assert.Equal("Bakery", item.ProductTypeName);
        Assert.Equal(2.75m, item.ProductPrice);
        Assert.Equal(-1, item.TipicalOrder);
        Assert.Equal(0, item.ToOrderNow);
    }

    [Fact]
    public async Task Delete_product_escapes_row_version_and_reports_conflicts()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent("""{"message":"The product changed elsewhere."}""")
            });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ShoppingListApiException>(
            () => client.DeleteListProductAsync(9, 4, "AQIDBAUGBwg+"));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("The product changed elsewhere.", exception.Message);
        Assert.Contains("rowVersion=AQIDBAUGBwg%2B", handler.LastRequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Create_share_posts_username_and_deserializes_share()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent("""
                    {
                      "id": 3,
                      "listId": 9,
                      "userId": "user-b",
                      "userName": "user-b",
                      "email": "user-b@example.com",
                      "createdAt": "2026-01-01T00:00:00+00:00"
                    }
                    """)
            };
        });
        var client = CreateClient(handler);

        var share = await client.CreateShareAsync(9, "user-b");

        Assert.Equal("/api/lists/9/shares", handler.LastRequestUri!.AbsolutePath);
        using var request = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("user-b", request.RootElement.GetProperty("userName").GetString());
        Assert.Equal("user-b", share.UserName);
        Assert.Equal("user-b@example.com", share.Email);
    }

    [Fact]
    public async Task Remove_share_escapes_user_id_in_route()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        await client.RemoveShareAsync(9, "user/id+tag");

        Assert.Equal("/api/lists/9/shares/user%2Fid%2Btag", handler.LastRequestUri!.AbsolutePath);
    }

    private static ShoppingListClient CreateClient(StubHttpMessageHandler handler)
    {
        return new ShoppingListClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example/")
        });
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
