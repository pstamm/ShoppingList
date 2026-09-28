using System.Text.Json;
using Microsoft.JSInterop;

namespace ShoppingList.Services.Authentication;

public sealed class BrowserAuthSessionStore : IAuthSessionStore
{
    private const string StorageKey = "shopping-list.auth-session";
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthSessionEvents _sessionEvents;

    public BrowserAuthSessionStore(IJSRuntime jsRuntime, AuthSessionEvents sessionEvents)
    {
        _jsRuntime = jsRuntime;
        _sessionEvents = sessionEvents;
    }

    public async ValueTask<AuthSession?> GetAsync()
    {
        var serialized = await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            StorageKey);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AuthSession>(serialized);
        }
        catch (JsonException)
        {
            await ClearAsync();
            return null;
        }
    }

    public async ValueTask SetAsync(AuthSession session)
    {
        var serialized = JsonSerializer.Serialize(session);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, serialized);
        _sessionEvents.Publish(session);
    }

    public async ValueTask ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        _sessionEvents.Publish(null);
    }
}
