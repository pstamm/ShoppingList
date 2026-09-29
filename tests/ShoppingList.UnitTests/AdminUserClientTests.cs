using System.Net;
using System.Text;
using System.Text.Json;
using ShoppingList.Services.Admin;

namespace ShoppingList.UnitTests;

public class AdminUserClientTests
{
    [Fact]
    public async Task Update_roles_sends_role_set_and_reads_updated_user()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/api/users/user-1/roles", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""
                    {
                      "id": "user-1",
                      "email": "user@example.com",
                      "isActive": true,
                      "roles": ["Admin"],
                      "createdAt": "2026-01-01T00:00:00+00:00"
                    }
                    """)
            };
        });
        var client = CreateClient(handler);

        var result = await client.UpdateRolesAsync(
            "user-1",
            new UpdateUserRolesRequest(["Admin"]));

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Admin", body.RootElement.GetProperty("roles")[0].GetString());
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal(["Admin"], result.Roles);
    }

    [Fact]
    public async Task Last_admin_conflict_message_is_preserved()
    {
        var client = CreateClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent("""{"message":"The last active administrator cannot be demoted."}""")
            }));

        var exception = await Assert.ThrowsAsync<AdminUserApiException>(
            () => client.UpdateRolesAsync("user-1", new UpdateUserRolesRequest(["User"])));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("The last active administrator cannot be demoted.", exception.Message);
    }

    private static AdminUserClient CreateClient(StubHttpMessageHandler handler)
    {
        return new AdminUserClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example/")
        });
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
