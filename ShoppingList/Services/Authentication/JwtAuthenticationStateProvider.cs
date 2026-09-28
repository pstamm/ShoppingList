using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace ShoppingList.Services.Authentication;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly IAuthSessionStore _sessionStore;
    private readonly HttpClient _httpClient;
    private readonly AuthSessionEvents _sessionEvents;
    private readonly AuthSessionRefreshLock _refreshLock;

    public JwtAuthenticationStateProvider(
        IAuthSessionStore sessionStore,
        HttpClient httpClient,
        AuthSessionEvents sessionEvents,
        AuthSessionRefreshLock refreshLock)
    {
        _sessionStore = sessionStore;
        _httpClient = httpClient;
        _sessionEvents = sessionEvents;
        _refreshLock = refreshLock;
        _sessionEvents.SessionChanged += OnSessionChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await _sessionStore.GetAsync();
        if (session is null)
        {
            return new AuthenticationState(Anonymous);
        }

        if (!IsValid(session))
        {
            await _sessionStore.ClearAsync();
            return new AuthenticationState(Anonymous);
        }

        var expiry = JwtTokenReader.ReadExpiration(session.AccessToken);
        if (expiry is null)
        {
            await _sessionStore.ClearAsync();
            return new AuthenticationState(Anonymous);
        }

        if (expiry <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            session = await RefreshAsync(session);
            if (session is null)
            {
                return new AuthenticationState(Anonymous);
            }
        }

        return new AuthenticationState(CreatePrincipal(session));
    }

    public async Task SignInAsync(AuthSessionResponse response)
    {
        var session = new AuthSession(
            response.AccessToken,
            response.RefreshToken,
            response.Email,
            response.UserId,
            response.Roles.ToArray());
        await _sessionStore.SetAsync(session);
    }

    public async Task SignOutAsync()
    {
        await _sessionStore.ClearAsync();
    }

    public ValueTask<AuthSession?> GetCurrentSessionAsync()
    {
        return _sessionStore.GetAsync();
    }

    private async Task<AuthSession?> RefreshAsync(AuthSession session)
    {
        await _refreshLock.Semaphore.WaitAsync();
        try
        {
            var latest = await _sessionStore.GetAsync();
            if (latest is null)
            {
                return null;
            }

            if (!string.Equals(latest.AccessToken, session.AccessToken, StringComparison.Ordinal))
            {
                return latest;
            }

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    "api/auth/refresh",
                    new RefreshRequest(latest.RefreshToken));
                if (!response.IsSuccessStatusCode)
                {
                    await _sessionStore.ClearAsync();
                    return null;
                }

                var refreshed = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
                if (refreshed is null || !IsValid(refreshed))
                {
                    await _sessionStore.ClearAsync();
                    return null;
                }

                var updated = ToSession(refreshed);
                await _sessionStore.SetAsync(updated);
                return updated;
            }
            catch (HttpRequestException)
            {
                await _sessionStore.ClearAsync();
                return null;
            }
            catch (JsonException)
            {
                await _sessionStore.ClearAsync();
                return null;
            }
        }
        finally
        {
            _refreshLock.Semaphore.Release();
        }
    }

    internal static AuthSession ToSession(AuthSessionResponse response)
    {
        return new AuthSession(
            response.AccessToken,
            response.RefreshToken,
            response.Email,
            response.UserId,
            response.Roles.ToArray());
    }

    internal static bool IsValid(AuthSessionResponse response)
    {
        return !string.IsNullOrWhiteSpace(response.AccessToken) &&
               !string.IsNullOrWhiteSpace(response.RefreshToken) &&
               !string.IsNullOrWhiteSpace(response.Email) &&
               !string.IsNullOrWhiteSpace(response.UserId) &&
               response.Roles is not null &&
               response.Roles.All(role => !string.IsNullOrWhiteSpace(role)) &&
               JwtTokenReader.ReadExpiration(response.AccessToken) is not null;
    }

    private static bool IsValid(AuthSession session)
    {
        return !string.IsNullOrWhiteSpace(session.AccessToken) &&
               !string.IsNullOrWhiteSpace(session.RefreshToken) &&
               !string.IsNullOrWhiteSpace(session.Email) &&
               !string.IsNullOrWhiteSpace(session.UserId) &&
               session.Roles is not null &&
               session.Roles.All(role => !string.IsNullOrWhiteSpace(role));
    }

    private static ClaimsPrincipal CreatePrincipal(AuthSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId),
            new(ClaimTypes.Name, session.Email),
            new(ClaimTypes.Email, session.Email)
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
    }

    private void OnSessionChanged(AuthSession? session)
    {
        var principal = session is null ? Anonymous : CreatePrincipal(session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public void Dispose()
    {
        _sessionEvents.SessionChanged -= OnSessionChanged;
    }
}
